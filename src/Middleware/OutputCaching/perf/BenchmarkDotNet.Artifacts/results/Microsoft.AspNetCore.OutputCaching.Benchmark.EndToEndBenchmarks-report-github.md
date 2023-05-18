``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.22621
Intel Core i9-9900K CPU 3.60GHz (Coffee Lake), 1 CPU, 16 logical and 8 physical cores
.NET SDK=8.0.100-preview.5.23257.1
  [Host]     : .NET 8.0.0 (8.0.23.26003), X64 RyuJIT
  DefaultJob : .NET 8.0.0 (8.0.23.25213), X64 RyuJIT


```
|           Method |                  Categories | PayloadLength |         Mean |     Error |    StdDev |   Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|----------------- |---------------------------- |-------------- |-------------:|----------:|----------:|--------:|--------:|--------:|----------:|
|   **OCS_StreamSync** |     **Write_OutputCacheStream** |            **10** |     **901.8 ns** |  **15.01 ns** |  **12.53 ns** |  **0.0591** |       **-** |       **-** |     **496 B** |
|  OCS_StreamAsync |     Write_OutputCacheStream |            10 |     919.5 ns |   4.24 ns |   3.31 ns |  0.0591 |       - |       - |     496 B |
|  OCS_WriterAsync |     Write_OutputCacheStream |            10 |   1,080.5 ns |   5.77 ns |   4.50 ns |  0.1049 |       - |       - |     880 B |
|   **OCS_StreamSync** |     **Write_OutputCacheStream** |          **1000** |   **1,109.0 ns** |   **9.92 ns** |   **8.79 ns** |  **0.1774** |       **-** |       **-** |   **1,488 B** |
|  OCS_StreamAsync |     Write_OutputCacheStream |          1000 |   1,165.1 ns |   8.50 ns |   7.95 ns |  0.1774 |       - |       - |   1,488 B |
|  OCS_WriterAsync |     Write_OutputCacheStream |          1000 |   1,294.5 ns |   9.58 ns |   8.96 ns |  0.2232 |       - |       - |   1,872 B |
|   **OCS_StreamSync** |     **Write_OutputCacheStream** |         **65553** |  **14,779.8 ns** | **218.13 ns** | **170.30 ns** |  **7.8735** |  **1.5717** |       **-** |  **66,040 B** |
|  OCS_StreamAsync |     Write_OutputCacheStream |         65553 |  18,190.7 ns | 340.07 ns | 498.47 ns |  7.8735 |  1.5564 |       - |  66,040 B |
|  OCS_WriterAsync |     Write_OutputCacheStream |         65553 |  20,182.5 ns | 329.82 ns | 366.59 ns |  8.1177 |  1.6174 |       - |  68,570 B |
|   **OCS_StreamSync** |     **Write_OutputCacheStream** |        **262161** |  **81,319.6 ns** | **487.47 ns** | **455.98 ns** | **72.6318** | **72.5098** | **72.5098** | **263,140 B** |
|  OCS_StreamAsync |     Write_OutputCacheStream |        262161 |  88,837.5 ns | 661.94 ns | 619.18 ns | 72.8760 | 72.7539 | 72.7539 | 263,153 B |
|  OCS_WriterAsync |     Write_OutputCacheStream |        262161 | 101,934.5 ns | 472.10 ns | 441.61 ns | 74.0967 | 72.8760 | 72.8760 | 272,207 B |
|                  |                             |               |              |           |           |         |         |         |           |
|  **OCPW_StreamSync** | **Write_OutputCachePipeWriter** |            **10** |     **972.6 ns** |  **11.35 ns** |  **10.07 ns** |  **0.0687** |       **-** |       **-** |     **584 B** |
| OCPW_StreamAsync | Write_OutputCachePipeWriter |            10 |     981.0 ns |   3.49 ns |   2.92 ns |  0.0687 |       - |       - |     584 B |
| OCPW_WriterAsync | Write_OutputCachePipeWriter |            10 |     941.8 ns |   9.22 ns |   7.70 ns |  0.0648 |       - |       - |     544 B |
|  **OCPW_StreamSync** | **Write_OutputCachePipeWriter** |          **1000** |   **1,294.0 ns** |  **10.69 ns** |  **10.00 ns** |  **0.1869** |       **-** |       **-** |   **1,576 B** |
| OCPW_StreamAsync | Write_OutputCachePipeWriter |          1000 |   1,256.8 ns |  15.35 ns |  14.35 ns |  0.1869 |       - |       - |   1,576 B |
| OCPW_WriterAsync | Write_OutputCachePipeWriter |          1000 |   1,163.6 ns |   8.78 ns |   7.33 ns |  0.1831 |       - |       - |   1,536 B |
|  **OCPW_StreamSync** | **Write_OutputCachePipeWriter** |         **65553** |  **22,883.6 ns** | **446.98 ns** | **496.82 ns** |  **7.8735** |  **1.5564** |       **-** |  **66,128 B** |
| OCPW_StreamAsync | Write_OutputCachePipeWriter |         65553 |  20,115.6 ns | 124.04 ns | 109.96 ns |  7.8735 |  1.5564 |       - |  66,128 B |
| OCPW_WriterAsync | Write_OutputCachePipeWriter |         65553 |  16,707.8 ns | 144.92 ns | 113.14 ns |  7.8735 |  1.5564 |       - |  66,088 B |
|  **OCPW_StreamSync** | **Write_OutputCachePipeWriter** |        **262161** | **109,835.6 ns** | **564.89 ns** | **471.71 ns** | **73.1201** | **72.9980** | **72.9980** | **263,228 B** |
| OCPW_StreamAsync | Write_OutputCachePipeWriter |        262161 |  98,562.0 ns | 606.49 ns | 567.31 ns | 72.9980 | 72.8760 | 72.8760 | 263,230 B |
| OCPW_WriterAsync | Write_OutputCachePipeWriter |        262161 |  87,857.3 ns | 498.08 ns | 465.90 ns | 72.8760 | 72.7539 | 72.7539 | 263,195 B |
|                  |                             |               |              |           |           |         |         |         |           |
|        **ReadAsync** |                        **Read** |            **10** |     **397.4 ns** |   **5.06 ns** |   **4.48 ns** |  **0.0668** |       **-** |       **-** |     **560 B** |
|        **ReadAsync** |                        **Read** |          **1000** |     **386.4 ns** |   **2.19 ns** |   **2.05 ns** |  **0.0668** |       **-** |       **-** |     **560 B** |
|        **ReadAsync** |                        **Read** |         **65553** |     **402.8 ns** |   **2.72 ns** |   **2.41 ns** |  **0.0668** |       **-** |       **-** |     **560 B** |
|        **ReadAsync** |                        **Read** |        **262161** |     **391.1 ns** |   **2.61 ns** |   **2.31 ns** |  **0.0668** |       **-** |       **-** |     **560 B** |
