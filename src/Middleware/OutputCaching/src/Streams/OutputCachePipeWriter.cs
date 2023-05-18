// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.WriteStream;

namespace Microsoft.AspNetCore.OutputCaching;
internal sealed class OutputCachePipeWriter : PipeWriter, IDisposable
{
    private readonly PipeWriter _innerPipe;
    private readonly long _maxBufferSize;
    private readonly int _segmentSize;
    private readonly SegmentWriteStream _segmentWriteStream;
    private readonly Action _startResponseCallback;

    private Memory<byte> _uncommitted;

    internal bool BufferingEnabled { get; private set; } = true;

    public OutputCachePipeWriter(PipeWriter innerPipe, long maxBufferSize, int segmentSize, Action startResponseCallback)
    {
        _innerPipe = innerPipe;
        _maxBufferSize = maxBufferSize;
        _segmentSize = segmentSize;
        _startResponseCallback = startResponseCallback;
        _segmentWriteStream = new SegmentWriteStream(_segmentSize);
    }

    public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        => _innerPipe.FlushAsync(cancellationToken);

    public override void CancelPendingFlush()
        => _innerPipe.CancelPendingFlush();

    public override void Complete(Exception? exception = null)
        => _innerPipe.Complete(exception);

    public override ValueTask CompleteAsync(Exception? exception = null)
        => _innerPipe.CompleteAsync(exception);

    [Obsolete]
    public override void OnReaderCompleted(Action<Exception?, object?> callback, object? state)
        => _innerPipe.OnReaderCompleted(callback, state);

    public override long UnflushedBytes => _innerPipe.UnflushedBytes;

    public override bool CanGetUnflushedBytes => _innerPipe.CanGetUnflushedBytes;

    public override Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;

    public override Memory<byte> GetMemory(int sizeHint = 0)
    {
        _startResponseCallback();
        return _uncommitted = _innerPipe.GetMemory(sizeHint);
    }

    public override void Advance(int bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        if (BufferingEnabled && bytes != 0)
        {
            if (_segmentWriteStream.Length + bytes > _maxBufferSize)
            {
                DisableBuffering();
            }
            else
            {
                _segmentWriteStream.Write(_uncommitted.Span.Slice(0, bytes));
            }
        }
        _uncommitted = default; // invalidate
        _innerPipe.Advance(bytes);
    }

    internal void DisableBuffering()
    {
        BufferingEnabled = false;
        _segmentWriteStream.Dispose();
    }
    public void Dispose() => DisableBuffering();

    internal ReadOnlySequence<byte> GetCachedResponseBody()
    {
        if (!BufferingEnabled)
        {
            throw new InvalidOperationException("Buffer stream cannot be retrieved since buffering is disabled.");
        }
        return _segmentWriteStream.DetachPayload();
    }
}
