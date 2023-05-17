// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.OutputCaching;
internal sealed class RecyclingReadOnlySequenceSegment : ReadOnlySequenceSegment<byte>
{
    public int Length => Memory.Length;
    private RecyclingReadOnlySequenceSegment() { }

    public static RecyclingReadOnlySequenceSegment Create(int minimumLength, RecyclingReadOnlySequenceSegment? previous)
        => Create(Rent(minimumLength), previous);

    public static RecyclingReadOnlySequenceSegment Create(ReadOnlyMemory<byte> memory, RecyclingReadOnlySequenceSegment? previous)
    {
        var obj = s_Spares.TryDequeue(out var value) ? value : new();
        obj.Memory = memory;
        if (previous is not null)
        {
            obj.RunningIndex = previous.RunningIndex + previous.Length;
            previous.Next = obj;
        }
        return obj;
    }

    const int TARGET_MAX = 128;
    static readonly ConcurrentQueue<RecyclingReadOnlySequenceSegment> s_Spares = new();
    public static void RecycleChain(in ReadOnlySequence<byte> value, bool recycleBuffers = false)
    {
        var obj = value.Start.GetObject() as RecyclingReadOnlySequenceSegment;
        if (obj is null)
        {
            // not segment based, but memory may still need recycling
            if (recycleBuffers)
            {
                Recycle(value.First);
            }
        }
        else
        {
            do
            {
                var mem = obj.Memory;
                obj.Memory = default;
                obj.RunningIndex = 0;
                var next = obj.Next as RecyclingReadOnlySequenceSegment;
                obj.Next = default;
                if (s_Spares.Count < TARGET_MAX) // not precise, due to not wanting lock
                { // (note: we still want to break the chain, even if not reusing; no else-break)
                    s_Spares.Enqueue(obj);
                }
                if (recycleBuffers)
                {
                    Recycle(mem);
                }
                obj = next;
            }
            while (obj is not null);
        }
    }

    internal static ReadOnlySequence<byte> CreateSequence(IList<byte[]> segments)
    {
        if (segments is null)
        {
            return default;
        }
        int count = segments.Count;
        switch (count)
        {
            case 0:
                return default;
            case 1:
                return new(segments[0]);
            default:
                RecyclingReadOnlySequenceSegment first = Create(segments[0], null), last = first;
                for (int i = 1; i < count; i++)
                {
                    last = Create(segments[i], last);
                }
                return new(first, 0, last, last.Length);
        }
    }

    public static async ValueTask CopyToAsync(ReadOnlySequence<byte> source, PipeWriter destination, CancellationToken cancellationToken)
    {
        if (!source.IsEmpty)
        {
            if (source.IsSingleSegment)
            {
                await destination.WriteAsync(source.First, cancellationToken);
            }
            else
            {
                foreach (var segment in source)
                {
                    await destination.WriteAsync(segment, cancellationToken);
                }
            }
        }
    }

    private static byte[] Rent(int minimumLength)
        => ArrayPool<byte>.Shared.Rent(minimumLength);

    private static void Recycle(ReadOnlyMemory<byte> value)
    {
        if (MemoryMarshal.TryGetArray(value, out var segment) && segment.Offset == 0 && segment.Count != 0)
        {
            ArrayPool<byte>.Shared.Return(segment.Array!);
        }
    }
}
