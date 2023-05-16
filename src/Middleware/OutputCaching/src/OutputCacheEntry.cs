// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching;

internal sealed class OutputCacheEntry : IDisposable
{
    public OutputCacheEntry(DateTimeOffset created, int statusCode)
    {
        Created = created;
        StatusCode = statusCode;
    }

    public StringValues FindHeader(string key)
    {
        TryFindHeader(key, out var value);
        return value;
    }

    public bool TryFindHeader(string key, out StringValues values)
    {
        foreach (var header in Headers.Span)
        {
            if (string.Equals(key, header.Name, StringComparison.OrdinalIgnoreCase))
            {
                values = header.Value;
                return !StringValues.IsNullOrEmpty(values);
            }
        }
        values = StringValues.Empty;
        return false;
    }

    /// <summary>
    /// Gets the created date and time of the cache entry.
    /// </summary>
    public DateTimeOffset Created { get; }

    /// <summary>
    /// Gets the status code of the cache entry.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets the headers of the cache entry.
    /// </summary>
    public ReadOnlyMemory<(string Name, StringValues Value)> Headers { get; set; }

    /// <summary>
    /// Gets the body of the cache entry.
    /// </summary>
    public ReadOnlySequence<byte> Body { get; set; }

    /// <summary>
    /// Gets the tags of the cache entry.
    /// </summary>
    public ReadOnlyMemory<string> Tags { get; set; }

    public void Dispose()
    {
        var tags = Tags;
        var headers = Headers;
        var body = Body;
        Tags = default;
        Headers = default;
        Body = default;
        Recycle(tags);
        Recycle(headers);
        RecyclingReadOnlySequenceSegment.RecycleChain(body);
        // ^^ note that this only recycles the chain, not the actual buffers
    }
    static void Recycle<T>(ReadOnlyMemory<T> value)
    {
        if (MemoryMarshal.TryGetArray<T>(value, out var segment) && segment.Array is { Length: > 0 })
        {
            ArrayPool<T>.Shared.Return(segment.Array);
        }
    }

    internal void CopyTagsFrom(HashSet<string> tags)
    {
        // only expected in create path; don't reset/recycle existing
        if (tags is not null)
        {
            var count = tags.Count;
            if (count != 0)
            {
                var arr = ArrayPool<string>.Shared.Rent(count);
                tags.CopyTo(arr);
                Tags = new(arr, 0, count);
            }
        }
    }

    internal void CopyHeadersFrom(IHeaderDictionary headers)
    {
        // only expected in create path; don't reset/recycle existing
        if (headers is not null)
        {
            int count = headers.Count, index = 0;
            if (count != 0)
            {
                var arr = ArrayPool<(string, StringValues)>.Shared.Rent(count);
                foreach (var header in headers)
                {
                    if (string.Equals(header.Key, HeaderNames.Age, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(header.Key, HeaderNames.ContentLength))
                    {
                        // ignore (note: length is already carried via the payload)
                    }
                    else
                    {
                        arr[index++] = (header.Key, header.Value);
                    }
                }
                if (index == 0) // only ignored headers
                {
                    ArrayPool<(string, StringValues)>.Shared.Return(arr);
                }
                else
                {
                    Headers = new(arr, 0, index);
                }
            }
        }
    }

    public void CopyHeadersTo(IHeaderDictionary headers)
    {
        if (!TryFindHeader(HeaderNames.TransferEncoding, out _))
        {
            headers.ContentLength = Body.Length;
        }
        foreach (var header in Headers.Span)
        {
            headers[header.Name] = header.Value;
        }
    }

    public async ValueTask CopyToAsync(PipeWriter destination, CancellationToken cancellationToken)
    {
        var body = Body;
        if (!body.IsEmpty)
        {
            if (body.IsSingleSegment)
            {
                await destination.WriteAsync(body.First, cancellationToken);
            }
            else
            {
                foreach (var segment in body)
                {
                    await destination.WriteAsync(segment, cancellationToken);
                }
            }
        }
    }
}
