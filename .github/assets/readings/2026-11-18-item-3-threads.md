# A build across threads

An every-step recipe built at 1,025 and 4,097 points a side on one thread and on eight, the
fastest of three runs each after a first pass at 513 that is not reported, under the machine's
exclusive lock (`dotnet run -c Release --no-cache build/bench.cs -- <numbers.json> 1025 4097`).
The machine carried a load of 24 on its 32 threads from other sessions' work, which the lock does
not keep off the processor, so few threads stood free for the build's.

Eight threads built 1,025 a side in 922 ms where one took 1,147, and 4,097 a side in 16.7 s where
one took 16.5. The steps whose rows run across the threads, the stamps, the noise, the terraces and
the smoothing, take some 2.7 s of the 16.5 at 4,097, the noise 2.5 of them, and the two erosions,
which run on one thread, the rest, so the build's time stands on the erosions. The bits are the
same at every count, which the tests hold.

| Side | Stage | ms | MB allocated | ns a point | bytes a point |
|---:|---|---:|---:|---:|---:|
| 1025 | rebuild overhead | 5 | 8.4 | 5.0 | 8.00 |
| 1025 | Stamp | 1 | 0.0 | 1.3 | 0.00 |
| 1025 | PictureStamp | 3 | 0.0 | 2.6 | 0.00 |
| 1025 | Noise | 258 | 0.0 | 245.7 | 0.00 |
| 1025 | Terrace | 3 | 0.0 | 2.8 | 0.00 |
| 1025 | Smoothing | 7 | 4.2 | 6.4 | 4.00 |
| 1025 | ThermalErosion | 443 | 4.2 | 421.5 | 4.00 |
| 1025 | HydraulicErosion | 442 | 0.0 | 420.9 | 0.01 |
| 1025 | tiles | 27 | 35.4 | 25.3 | 33.70 |
| 1025 | splat map | 64 | 21.0 | 60.6 | 20.00 |
| 1025 | whole build | 1147 | 12.6 | 1091.5 | 12.02 |
| 1025 | whole build, 8 threads | 922 | 12.6 | 877.3 | 12.03 |
| 4097 | rebuild overhead | 77 | 134.3 | 4.6 | 8.00 |
| 4097 | Stamp | 42 | 0.0 | 2.5 | 0.00 |
| 4097 | PictureStamp | 57 | 0.0 | 3.4 | 0.00 |
| 4097 | Noise | 2481 | 0.0 | 147.8 | 0.00 |
| 4097 | Terrace | 53 | 0.0 | 3.1 | 0.00 |
| 4097 | Smoothing | 118 | 67.1 | 7.0 | 4.00 |
| 4097 | ThermalErosion | 6895 | 67.1 | 410.8 | 4.00 |
| 4097 | HydraulicErosion | 7090 | 0.2 | 422.4 | 0.01 |
| 4097 | tiles | 394 | 566.5 | 23.5 | 33.75 |
| 4097 | splat map | 969 | 335.7 | 57.7 | 20.00 |
| 4097 | whole build | 16459 | 201.6 | 980.5 | 12.01 |
| 4097 | whole build, 8 threads | 16714 | 201.6 | 995.7 | 12.01 |
