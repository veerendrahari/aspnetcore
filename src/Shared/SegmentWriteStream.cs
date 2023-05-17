// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.OutputCaching;

namespace Microsoft.AspNetCore.WriteStream;

internal sealed class SegmentWriteStream : IDisposable
{
    RecyclingReadOnlySequenceSegment? _firstSegment, _currentSegment;
    private int _currentSegmentIndex;
    private readonly int _segmentSize;
    private bool _closed;

    public long Length { get; private set; }

    internal SegmentWriteStream(int segmentSize)
    {
        if (segmentSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(segmentSize), segmentSize, $"{nameof(segmentSize)} must be greater than 0.");
        }

        _segmentSize = segmentSize;
    }

    // Extracting the buffered segments closes the stream for writing
    internal ReadOnlySequence<byte> DetachPayload()
    {
        var payload = DetachWithoutRecycle();

        if (payload.IsEmpty)
        {
            // recycle everything including the buffers
            RecyclingReadOnlySequenceSegment.RecycleChain(payload, recycleBuffers: true);
            return default;
        }

        if (payload.IsSingleSegment)
        {
            // we can return a simple sequence (no segment complexity) - but keep the buffers
            var result = payload.First;
            RecyclingReadOnlySequenceSegment.RecycleChain(payload, recycleBuffers: false);
            return new(result);
        }

        return payload;
    }

    private ReadOnlySequence<byte> DetachWithoutRecycle()
    {
        _closed = true;
        if (_firstSegment is null)
        {
            return default;
        }
        var payload = new ReadOnlySequence<byte>(_firstSegment, 0, _currentSegment!, _currentSegmentIndex);

        // reset our local state for an abundance of caution
        _firstSegment = _currentSegment = null;
        _currentSegmentIndex = 0;

        return payload;
    }

    public void Dispose()
    {
        RecyclingReadOnlySequenceSegment.RecycleChain(DetachWithoutRecycle(), recycleBuffers: true);
    }

    private Span<byte> GetBuffer()
    {
        if (_closed)
        {
            Throw();
        }
        static void Throw() => throw new ObjectDisposedException(nameof(SegmentWriteStream), "The stream has been closed for writing.");

        if (_firstSegment is null)
        {
            _currentSegment = _firstSegment = RecyclingReadOnlySequenceSegment.Create(_segmentSize, null);
            _currentSegmentIndex = 0;
        }

        Debug.Assert(_currentSegment is not null);
        var current = _currentSegment.Memory;
        Debug.Assert(_currentSegmentIndex >= 0 && _currentSegmentIndex <= current.Length);

        if (_currentSegmentIndex == current.Length)
        {
            _currentSegment = RecyclingReadOnlySequenceSegment.Create(_segmentSize, _currentSegment);
            _currentSegmentIndex = 0;
            current = _currentSegment.Memory;
        }

        // have capacity in current chunk
        return MemoryMarshal.AsMemory(current).Span.Slice(_currentSegmentIndex);
    }
    public void Write(ReadOnlySpan<byte> buffer)
    {
        while (!buffer.IsEmpty)
        {
            var available = GetBuffer();
            if (available.Length >= buffer.Length)
            {
                buffer.CopyTo(available);
                Advance(buffer.Length);
                return; // all done
            }
            else
            {
                var toWrite = Math.Min(buffer.Length, available.Length);
                if (toWrite <= 0)
                {
                    Throw();
                }
                buffer.Slice(0, toWrite).CopyTo(available);
                Advance(toWrite);
                buffer = buffer.Slice(toWrite);
            }
        }
        static void Throw() => throw new InvalidOperationException("Unable to acquire non-empty write buffer");
    }

    private void Advance(int count)
    {
        _currentSegmentIndex += count;
        Length += count;
    }

    public void WriteByte(byte value)
    {
        GetBuffer()[0] = value;
        Advance(1);
    }
}
