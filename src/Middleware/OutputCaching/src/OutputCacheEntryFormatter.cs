// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;
/// <summary>
/// Formats <see cref="OutputCacheEntry"/> instance to match structures supported by the <see cref="IOutputCacheStore"/> implementations.
/// </summary>
internal static class OutputCacheEntryFormatter
{
    private enum SerializationRevision
    {
        V1_Original = 1,
        V2_OriginalWithCommonHeaders = 2,
    }

    public static async ValueTask<OutputCacheEntry?> GetAsync(string key, IOutputCacheStore store, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        var content = await store.GetAsync(key, cancellationToken);

        if (content is null)
        {
            return null;
        }

        return Deserialize(content);
    }

    public static async ValueTask StoreAsync(string key, OutputCacheEntry value, TimeSpan duration, IOutputCacheStore store, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(value.Body);
        ArgumentNullException.ThrowIfNull(value.Headers);

        using var bufferStream = new MemoryStream();

        Serialize(bufferStream, value);

        await store.SetAsync(key, bufferStream.ToArray(), value.Tags.ToArray(), duration, cancellationToken);
    }

    // Format:
    // Serialization revision:
    //   7-bit encoded int
    // Creation date:
    //   Ticks: 7-bit encoded long
    //   Offset.TotalMinutes: 7-bit encoded long
    // Status code:
    //   7-bit encoded int
    // Headers:
    //   Headers count: 7-bit encoded int
    //   For each header:
    //     key name byte length: 7-bit encoded int
    //     UTF-8 encoded key name byte[]
    //     Values count: 7-bit encoded int
    //     For each header value:
    //       data byte length: 7-bit encoded int
    //       UTF-8 encoded byte[]
    // Body:
    //   Segments count: 7-bit encoded int
    //   For each segment:
    //     data byte length: 7-bit encoded int
    //     data byte[]
    // Tags:
    //   Tags count: 7-bit encoded int
    //   For each tag:
    //     data byte length: 7-bit encoded int
    //     UTF-8 encoded byte[]

    private static void Serialize(Stream output, OutputCacheEntry entry)
    {
        using var writer = new BinaryWriter(output);

        // Serialization revision:
        //   7-bit encoded int
        writer.Write7BitEncodedInt((int)SerializationRevision.V2_OriginalWithCommonHeaders);

        // Creation date:
        //   Ticks: 7-bit encoded long
        //   Offset.TotalMinutes: 7-bit encoded long

        writer.Write7BitEncodedInt64(entry.Created.Ticks);
        writer.Write7BitEncodedInt64((long)entry.Created.Offset.TotalMinutes);

        // Status code:
        //   7-bit encoded int
        writer.Write7BitEncodedInt(entry.StatusCode);

        // Headers:
        //   Headers count: 7-bit encoded int

        writer.Write7BitEncodedInt(entry.Headers.Length);

        //   For each header:
        //     key name byte length: 7-bit encoded int
        //     UTF-8 encoded key name byte[]

        foreach (var header in entry.Headers.Span)
        {
            WriteCommonHeader(writer, header.Name);

            //     Values count: 7-bit encoded int
            var count = header.Value.Count;
            writer.Write7BitEncodedInt(count);

            //     For each header value:
            //       data byte length: 7-bit encoded int
            //       UTF-8 encoded byte[]
            for (int i = 0; i < count; i++)
            {
                WriteCommonHeader(writer, header.Value[i]);
            }
        }

        // Body:
        //   Segments count: 7-bit encoded int
        //   For each segment:
        //     data byte length: 7-bit encoded int
        //     data byte[]

        writer.Write7BitEncodedInt(checked((int)entry.Body.Length));

        foreach (var segment in entry.Body)
        {
            writer.Write7BitEncodedInt(segment.Length);
            writer.BaseStream.Write(segment.Span); // note BaseStream ensures flush etc in anticipation
        }

        // Tags:
        //   Tags count: 7-bit encoded int
        //   For each tag:
        //     data byte length: 7-bit encoded int
        //     UTF-8 encoded byte[]

        writer.Write7BitEncodedInt(entry.Tags.Length);

        foreach (var tag in entry.Tags.Span)
        {
            writer.Write(tag ?? "");
        }
    }

    [SkipLocalsInit]
    static void WriteCommonHeader(BinaryWriter writer, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.Write7BitEncodedInt(0);
        }
        else
        {
            if (CommonHeadersLookup.TryGetValue(value, out int known))
            {
                writer.Write7BitEncodedInt((known << 1) | 1);
            }
            else
            {
                var bytes = Encoding.UTF8.GetByteCount(value);
                writer.Write7BitEncodedInt(bytes << 1);

                const int MAX_STACK_BYTES = 256;
                byte[]? leased = null;

                Span<byte> buffer = bytes <= MAX_STACK_BYTES ? stackalloc byte[bytes] : new(leased = ArrayPool<byte>.Shared.Rent(bytes), 0, bytes);
                int actual = Encoding.UTF8.GetBytes(value, buffer);
                Debug.Assert(actual == bytes);
                writer.BaseStream.Write(buffer); // .BaseStream includes a flush in anticipation
                if (leased is not null)
                {
                    ArrayPool<byte>.Shared.Return(leased);
                }
            }
        }
    }

    private static bool CanParseRevision(int revision, out bool useCommonHeaders)
    {
        switch ((SerializationRevision)revision)
        {
            case SerializationRevision.V1_Original: // we don't actively expect this much, since only in-proc back-end was shipped
                useCommonHeaders = false;
                return true;
            case SerializationRevision.V2_OriginalWithCommonHeaders:
                useCommonHeaders = true;
                return true;
            default:
                // In future versions, also support the previous revision format.
                useCommonHeaders = default;
                return false;
        }
    }

    private static OutputCacheEntry? Deserialize(ReadOnlyMemory<byte> content)
    {
        var reader = new FormatterBinaryReader(content);

        // Serialization revision:
        //   7-bit encoded int

        if (!CanParseRevision(reader.Read7BitEncodedInt(), out var useCommonHeaders))
        {
            return null;
        }

        // Creation date:
        //   Ticks: 7-bit encoded long
        //   Offset.TotalMinutes: 7-bit encoded long

        var ticks = reader.Read7BitEncodedInt64();
        var offsetMinutes = reader.Read7BitEncodedInt64();

        var created = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offsetMinutes));

        // Status code:
        //   7-bit encoded int

        var statusCode = reader.Read7BitEncodedInt();

        var result = new OutputCacheEntry(created, statusCode);

        // Headers:
        //   Headers count: 7-bit encoded int

        var headersCount = reader.Read7BitEncodedInt();

        //   For each header:
        //     key name byte length: 7-bit encoded int
        //     UTF-8 encoded key name byte[]
        //     Values count: 7-bit encoded int
        if (headersCount > 0)
        {
            var headerArr = ArrayPool<(string Name, StringValues Values)>.Shared.Rent(headersCount);

            for (var i = 0; i < headersCount; i++)
            {
                var key = useCommonHeaders ? ReadCommonHeader(ref reader) : reader.ReadString();
                StringValues value;
                var valuesCount = reader.Read7BitEncodedInt();
                //     For each header value:
                //       data byte length: 7-bit encoded int
                //       UTF-8 encoded byte[]
                switch (valuesCount)
                {
                    case < 0:
                        throw new InvalidOperationException();
                    case 0:
                        value = StringValues.Empty;
                        break;
                    case 1:
                        value = new(useCommonHeaders ? ReadCommonHeader(ref reader) : reader.ReadString());
                        break;
                    default:
                        var values = new string[valuesCount];

                        for (var j = 0; j < valuesCount; j++)
                        {
                            values[j] = useCommonHeaders ? ReadCommonHeader(ref reader) : reader.ReadString();
                        }
                        value = new(values);
                        break;
                }
                headerArr[i] = (key, value);
            }
            result.Headers = new ReadOnlyMemory<(string Name, StringValues Values)>(headerArr, 0, headersCount);
        }

        // Body:
        //   Segments count: 7-bit encoded int

        var segmentsCount = reader.Read7BitEncodedInt();

        //   For each segment:
        //     data byte length: 7-bit encoded int
        //     data byte[]

        switch (segmentsCount)
        {
            case 0:
                // nothing to do
                break;
            case 1:
                result.Body = new ReadOnlySequence<byte>(ReadSegment(ref reader));
                break;
            case < 0:
                throw new InvalidOperationException();
            default:
                RecyclingReadOnlySequenceSegment first = RecyclingReadOnlySequenceSegment.Create(ReadSegment(ref reader), null), last = first;
                for (int i = 1; i < segmentsCount; i++)
                {
                    last = RecyclingReadOnlySequenceSegment.Create(ReadSegment(ref reader), last);
                }
                result.Body = new ReadOnlySequence<byte>(first, 0, last, last.Length);
                break;
        }

        static ReadOnlyMemory<byte> ReadSegment(ref FormatterBinaryReader reader)
        {
            var segmentLength = reader.Read7BitEncodedInt();
            return reader.ReadBytesMemory(segmentLength);
        }

        // Tags:
        //   Tags count: 7-bit encoded int

        var tagsCount = reader.Read7BitEncodedInt();
        if (tagsCount > 0)
        {
            //   For each tag:
            //     data byte length: 7-bit encoded int
            //     UTF-8 encoded byte[]
            var tagsArray = ArrayPool<string>.Shared.Rent(tagsCount);

            for (var i = 0; i < tagsCount; i++)
            {
                tagsArray[i] = reader.ReadString();
            }

            result.Tags = new ReadOnlyMemory<string>(tagsArray, 0, tagsCount);
        }
        return result;
    }

    private static string ReadCommonHeader(ref FormatterBinaryReader reader)
    {
        int preamble = reader.Read7BitEncodedInt();
        // LSB means "using common header/value"
        if ((preamble & 1) == 1)
        {
            // non-LSB is the index of the common header
            return CommonHeaders[preamble >> 1];
        }
        else
        {
            // non-LSB is the string length
            return reader.ReadString(preamble >> 1);
        }
    }

    static readonly string[] CommonHeaders = new string[]
    {
        // to remove values, use ""; DO NOT just remove the line
        ""

    };

    static readonly FrozenDictionary<string, int> CommonHeadersLookup = BuildCommonHeadersLookup();

    static FrozenDictionary<string, int> BuildCommonHeadersLookup()
    {
        var arr = CommonHeaders;
        var pairs = new List<KeyValuePair<string, int>>(arr.Length);
        for (int i = 0; i < arr.Length; i++)
        {
            var header = arr[i];
            if (!string.IsNullOrWhiteSpace(header)) // omit null/empty values
            {
                pairs.Add(new(header, i));
            }
        }
        return FrozenDictionary.ToFrozenDictionary(pairs, StringComparer.Ordinal, optimizeForReading: true);
    }
}
