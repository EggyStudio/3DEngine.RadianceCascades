# The ladder of merges

Measured on 2026-11-22 on the tree of c0beccc with the changes committed beside it, every merge the passes take by name
(`IMerge`) run over seven scenes on the CPU, as `dotnet test RadianceCascades.Tests` writes it with
`RC_LADDER` naming this file. A part of a scene is read as the light its points take over the light
traced to them, and its distance is that ratio's logarithm taken whole, 0.105 a tenth over or under.
The leak is the light the dark room takes through a wall of one cell beside the lit one, as a share
of the lit room's traced light. The cost is a texel's steps past its ray's end and its reads of the
probes above, a texel's light, its share of the sky or a reach texel, on the mean over every level's
texels. A merge stands above another where it is nearer on the mean and no worse in its worst part,
its leak and its steps and reads together, over the six scenes, and the one standing above every
other is the default. The seventh is held out of the standing and read apart, since no merge is
tuned on it.

## Standing

| Merge | Mean distance | Worst part | Leak | Steps a texel | Reads a texel | Stands above | Held out, mean and worst |
|---|---|---|---|---|---|---|---|
| at the ray's end, `MergeAtRayEnd` | 0.049 | 0.252 | 0.00000 | 1.1 | 7.1 | by reach sight | 0.038 and 0.050 |
| by reach sight, `MergeByReachSight` | 0.089 | 0.473 | 0.00000 | 1.0 | 7.7 | none | 0.088 and 0.270 |

The merge at the ray's end, `MergeAtRayEnd`, stands above every other and is the default.

## Each scene

| Scene | Merge | Each part's ratio | Mean distance | Worst part | Leak | Steps a texel | Reads a texel |
|---|---|---|---|---|---|---|---|
| a room with a door | at the ray's end, `MergeAtRayEnd` | floor 1.002, walls 1.002, ceiling 1.013 | 0.006 | 0.013 |  | 0.8 | 4.6 |
| a room with a door | by reach sight, `MergeByReachSight` | floor 0.992, walls 1.053, ceiling 0.996 | 0.021 | 0.052 |  | 0.9 | 4.6 |
| a closed box with one lamp | at the ray's end, `MergeAtRayEnd` | floor 0.944, walls 0.777 | 0.155 | 0.252 |  | 1.2 | 4.7 |
| a closed box with one lamp | by reach sight, `MergeByReachSight` | floor 1.161, walls 1.032 | 0.090 | 0.149 |  | 1.0 | 5.5 |
| a lit room beside a dark one through a wall of one cell | at the ray's end, `MergeAtRayEnd` | lit floor 0.960, lit walls 0.919 | 0.063 | 0.084 | 0.00000 | 1.0 | 3.9 |
| a lit room beside a dark one through a wall of one cell | by reach sight, `MergeByReachSight` | lit floor 0.962, lit walls 0.942 | 0.049 | 0.060 | 0.00000 | 1.0 | 4.7 |
| a room under a lintel | at the ray's end, `MergeAtRayEnd` | by the opening 0.917, between 0.981, deepest 0.917 | 0.064 | 0.087 |  | 2.0 | 18.4 |
| a room under a lintel | by reach sight, `MergeByReachSight` | by the opening 1.104, between 0.897, deepest 0.623 | 0.227 | 0.473 |  | 1.5 | 20.3 |
| an open box under a sky | at the ray's end, `MergeAtRayEnd` | floor 1.029, walls 1.002 | 0.015 | 0.028 |  | 1.2 | 6.1 |
| an open box under a sky | by reach sight, `MergeByReachSight` | floor 1.255, walls 1.048 | 0.137 | 0.227 |  | 1.0 | 6.4 |
| a long hall | at the ray's end, `MergeAtRayEnd` | by the opening 0.961, middle 0.993, far end 0.993 | 0.018 | 0.040 |  | 0.4 | 5.2 |
| a long hall | by reach sight, `MergeByReachSight` | by the opening 1.017, middle 0.994, far end 0.992 | 0.010 | 0.017 |  | 0.7 | 4.4 |
| held out, a room with a roof window and a pillar | at the ray's end, `MergeAtRayEnd` | floor 1.052, walls 1.026, ceiling 1.049, pillar 1.030 | 0.038 | 0.050 |  | 0.8 | 4.7 |
| held out, a room with a roof window and a pillar | by reach sight, `MergeByReachSight` | floor 1.310, walls 0.991, ceiling 1.060, pillar 1.017 | 0.088 | 0.270 |  | 0.9 | 4.6 |
