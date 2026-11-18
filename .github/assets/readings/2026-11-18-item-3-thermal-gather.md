# Thermal erosion gathered across threads

Thermal erosion's time and bytes a point, 20 passes, at 1,025 and 4,097 points a side, on one
thread and on eight, the fastest of three runs each after a first pass at 513 that is not
reported, under the machine's exclusive lock (`dotnet run -c Release --no-cache build/bench.cs --
<numbers.json> 1025 4097`), with the machine carrying a load of some 24 from other sessions' work.

| Side | Stage | ms | ns a point | bytes a point |
|---:|---|---:|---:|---:|
| 1,025 | thermal erosion, one thread | 473 | 450.0 | 12.00 |
| 1,025 | thermal erosion, eight threads | 74 | 70.1 | 12.11 |
| 1,025 | whole build, one thread | 1,172 | 1,116.0 | 20.02 |
| 1,025 | whole build, eight threads | 549 | 522.7 | 20.14 |
| 4,097 | thermal erosion, one thread | 7,465 | 444.7 | 12.00 |
| 4,097 | thermal erosion, eight threads | 1,160 | 69.1 | 12.01 |
| 4,097 | whole build, one thread | 16,690 | 994.3 | 20.01 |
| 4,097 | whole build, eight threads | 8,605 | 512.6 | 20.02 |

Gathered, a pass reckons what leaves every point into a field, the excess it is shared by into a
second and the moves into a third, 12 bytes a point where the moves alone took 4, so a whole build
allocates 20 bytes a point where it allocated 12. On eight threads thermal erosion runs 6.4 times as
fast at 4,097 a side and a whole build twice as fast, the droplets on one thread taking the rest.
