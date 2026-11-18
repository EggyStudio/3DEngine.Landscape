# Droplets a block at a time

Hydraulic erosion's time a point, the droplets a quarter a point, read with the droplets drawn
anywhere over the field (A) and a block of 64 points at a time, the blocks taking turns of 64 (B),
the two programs built from the same source with the block set to the whole field for A and run
one after the other under the machine's exclusive lock, the fastest of three runs each
(`dotnet run -c Release --no-cache build/bench.cs -- <numbers.json> 2049 4097`). The machine
carried a load of 15 from other sessions' work, which the lock does not keep off the processor,
so the two are read in turn rather than against the morning's table.

| Order | Round | 2,049 a side, ns a point | 4,097 a side, ns a point |
|---|---:|---:|---:|
| A, anywhere | 1 | 495.6 | 641.9 |
| B, blocks | 1 | 397.2 | 402.9 |
| A, anywhere | 2 | 430.7 | 592.8 |

Drawn anywhere, a droplet at 4,097 a side cost 30 and 38 hundredths more a point than at 2,049,
its field of 67 MB read afresh from memory. A block at a time, it costs the same at both sizes, and
37 hundredths less than drawn anywhere at 4,097 in the same round. The second round's B was stopped
to free the lock for a session waiting behind it.
