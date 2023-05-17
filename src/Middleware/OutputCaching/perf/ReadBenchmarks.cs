// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching.Benchmark;

[MemoryDiagnoser]
public class ReadBenchmarks
{
    private string key = null!;
    private IOutputCacheStore store = null!;

    [GlobalSetup]
    public async Task Init()
    {
        var rand = new Random();
        byte[] Invent(int length)
        {
            var arr = new byte[length];
            rand.NextBytes(arr);
            return arr;
        }
        store = new DummyStore();
        List<byte[]> segments = new(7) { Invent(4096), Invent(4096), Invent(4096), Invent(4096), Invent(4096), Invent(4096), Invent(157) };
        var totalLength = segments.Sum(x => x.Length);

        var headers = new HeaderDictionary()
        {
            ContentLength = totalLength,
            [HeaderNames.ContentType] = "text/plain",
            ["Whatever"] = "Some other header"
        };

        key = Guid.NewGuid().ToString();
        using (var entry = new OutputCacheEntry(DateTime.UtcNow, StatusCodes.Status200OK))
        {
            entry.CopyHeadersFrom(headers);
            entry.SetBody(RecyclingReadOnlySequenceSegment.CreateSequence(segments), recycleBuffers: false);
            await OutputCacheEntryFormatter.StoreAsync(key, entry, TimeSpan.FromMinutes(5), store, CancellationToken.None);
        }
    }

    [Benchmark]
    public async Task Read()
    {
        using var result = await OutputCacheEntryFormatter.GetAsync(key, store, CancellationToken.None);
        if (result is null)
        {
            Throw();
        }
        static void Throw() => throw new InvalidOperationException();
    }

    sealed class DummyStore : IOutputCacheStore
    {
        private readonly ConcurrentDictionary<string, byte[]> all = new();
        ValueTask IOutputCacheStore.EvictByTagAsync(string tag, CancellationToken cancellationToken) => default;

        ValueTask<byte[]?> IOutputCacheStore.GetAsync(string key, CancellationToken cancellationToken)
            => all.TryGetValue(key, out var result) ? new(result) : default;

        ValueTask IOutputCacheStore.SetAsync(string key, byte[]? value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
        {
            if (value is null)
            {
                all.Remove(key, out _);
            }
            else
            {
                all[key] = value;
            }
            return default;
        }
    }
}
