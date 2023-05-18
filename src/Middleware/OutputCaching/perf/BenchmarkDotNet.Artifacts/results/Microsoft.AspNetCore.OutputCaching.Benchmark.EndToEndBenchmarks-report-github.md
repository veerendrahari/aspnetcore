``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.22621
Intel Core i9-9900K CPU 3.60GHz (Coffee Lake), 1 CPU, 16 logical and 8 physical cores
.NET SDK=8.0.100-preview.5.23257.1
  [Host]     : .NET 8.0.0 (8.0.23.26003), X64 RyuJIT
  DefaultJob : .NET 8.0.0 (8.0.23.25213), X64 RyuJIT


```
|           Method |                  Categories | PayloadLength |         Mean |     Error |    StdDev |   Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|----------------- |---------------------------- |-------------- |-------------:|----------:|----------:|--------:|--------:|--------:|----------:|
|   **OCS_StreamSync** |     **Write_OutputCacheStream** |            **10** |     **939.3 ns** |   **7.84 ns** |   **6.55 ns** |  **0.0620** |       **-** |       **-** |     **520 B** |
|  OCS_StreamAsync |     Write_OutputCacheStream |            10 |     963.5 ns |   5.41 ns |   5.06 ns |  0.0610 |       - |       - |     520 B |
|  OCS_WriterAsync |     Write_OutputCacheStream |            10 |   1,166.8 ns |  10.92 ns |  10.21 ns |  0.1068 |       - |       - |     904 B |
|   **OCS_StreamSync** |     **Write_OutputCacheStream** |          **1000** |   **1,130.0 ns** |  **17.20 ns** |  **16.09 ns** |  **0.1793** |       **-** |       **-** |   **1,504 B** |
|  OCS_StreamAsync |     Write_OutputCacheStream |          1000 |   1,227.1 ns |   7.86 ns |   6.13 ns |  0.1793 |       - |       - |   1,504 B |
|  OCS_WriterAsync |     Write_OutputCacheStream |          1000 |   1,321.5 ns |   5.73 ns |   5.36 ns |  0.2251 |       - |       - |   1,888 B |
|   **OCS_StreamSync** |     **Write_OutputCacheStream** |         **65553** |  **15,172.9 ns** | **303.11 ns** | **253.11 ns** |  **7.8735** |  **1.5717** |       **-** |  **66,064 B** |
|  OCS_StreamAsync |     Write_OutputCacheStream |         65553 |  17,548.1 ns | 329.67 ns | 292.25 ns |  7.8735 |  1.5564 |       - |  66,064 B |
|  OCS_WriterAsync |     Write_OutputCacheStream |         65553 |  20,486.3 ns | 369.58 ns | 637.51 ns |  8.1177 |  1.6174 |       - |  68,594 B |
|   **OCS_StreamSync** |     **Write_OutputCacheStream** |        **262161** |  **80,801.4 ns** | **586.80 ns** | **520.19 ns** | **72.6318** | **72.5098** | **72.5098** | **263,158 B** |
|  OCS_StreamAsync |     Write_OutputCacheStream |        262161 |  91,073.7 ns | 379.99 ns | 355.44 ns | 72.9980 | 72.8760 | 72.8760 | 263,171 B |
|  OCS_WriterAsync |     Write_OutputCacheStream |        262161 | 103,216.1 ns | 652.99 ns | 545.28 ns | 74.0967 | 72.8760 | 72.8760 | 272,225 B |
|                  |                             |               |              |           |           |         |         |         |           |
|  **OCPW_StreamSync** | **Write_OutputCachePipeWriter** |            **10** |   **1,028.0 ns** |   **7.40 ns** |   **6.18 ns** |  **0.0725** |       **-** |       **-** |     **608 B** |
| OCPW_StreamAsync | Write_OutputCachePipeWriter |            10 |   1,028.2 ns |  19.10 ns |  18.76 ns |  0.0725 |       - |       - |     608 B |
| OCPW_WriterAsync | Write_OutputCachePipeWriter |            10 |     960.7 ns |   4.73 ns |   4.19 ns |  0.0668 |       - |       - |     568 B |
|  **OCPW_StreamSync** | **Write_OutputCachePipeWriter** |          **1000** |   **1,364.0 ns** |  **16.69 ns** |  **14.80 ns** |  **0.1888** |       **-** |       **-** |   **1,592 B** |
| OCPW_StreamAsync | Write_OutputCachePipeWriter |          1000 |   1,316.7 ns |   8.50 ns |   7.10 ns |  0.1888 |       - |       - |   1,592 B |
| OCPW_WriterAsync | Write_OutputCachePipeWriter |          1000 |   1,265.3 ns |   6.39 ns |   5.34 ns |  0.1850 |       - |       - |   1,552 B |
|  **OCPW_StreamSync** | **Write_OutputCachePipeWriter** |         **65553** |  **23,330.2 ns** | **461.65 ns** | **431.83 ns** |  **7.8735** |  **1.5564** |       **-** |  **66,152 B** |
| OCPW_StreamAsync | Write_OutputCachePipeWriter |         65553 |  20,562.3 ns | 218.20 ns | 193.43 ns |  7.8735 |  1.5564 |       - |  66,152 B |
| OCPW_WriterAsync | Write_OutputCachePipeWriter |         65553 |  17,493.3 ns | 344.30 ns | 322.06 ns |  7.8735 |  1.5564 |       - |  66,112 B |
|  **OCPW_StreamSync** | **Write_OutputCachePipeWriter** |        **262161** | **112,476.5 ns** | **663.35 ns** | **620.50 ns** | **73.2422** | **73.1201** | **73.1201** | **263,261 B** |
| OCPW_StreamAsync | Write_OutputCachePipeWriter |        262161 |  99,701.3 ns | 841.26 ns | 745.75 ns | 73.1201 | 72.9980 | 72.9980 | 263,248 B |
| OCPW_WriterAsync | Write_OutputCachePipeWriter |        262161 |  89,482.8 ns | 506.78 ns | 474.04 ns | 72.9980 | 72.8760 | 72.8760 | 263,207 B |
|                  |                             |               |              |           |           |         |         |         |           |
|        **ReadAsync** |                        **Read** |            **10** |     **410.4 ns** |   **5.78 ns** |   **5.12 ns** |  **0.0687** |       **-** |       **-** |     **576 B** |
|        **ReadAsync** |                        **Read** |          **1000** |     **407.7 ns** |   **1.94 ns** |   **1.51 ns** |  **0.0687** |       **-** |       **-** |     **576 B** |
|        **ReadAsync** |                        **Read** |         **65553** |     **414.7 ns** |   **5.36 ns** |   **5.01 ns** |  **0.0687** |       **-** |       **-** |     **576 B** |
|        **ReadAsync** |                        **Read** |        **262161** |     **486.4 ns** |   **2.25 ns** |   **2.10 ns** |  **0.0687** |       **-** |       **-** |     **576 B** |
