# The ladder of merges

Measured on 2026-11-22 on the tree of af7d199 with the changes committed beside it, every merge the passes take by name
(`IMerge`) run over 8 scenes on the CPU at 2 configurations, as `dotnet test RadianceCascades.Tests`
writes it with `RC_LADDER` naming this file. The module's configuration is its tests' chain of four levels
in one cascade of the field of 64 cells of 0.25. An engine's is one an engine binds the passes at, five
levels of 8 probes a side every 8 cells of the field's five cascades of 64 cells from 0.125, the finest
two traced at 16 texels a side and read at 8, every ray from its probe to four times its spacing but the
last two's to the field's diagonal, each point gathered from the finest level about it.
A part of a scene is read as the light its points take over the light traced to them, and its distance
is that ratio's logarithm taken whole, 0.105 a tenth over or under. The leak is the light the dark room
takes through a wall of one cell beside the lit one, as a share of the lit room's traced light. The cost
is a texel's steps past its ray's end, the per-probe stage's steps among them, and its reads of the probes
above, a texel's light, its share of the sky or a reach texel, on the mean over every level's texels. A
merge stands above another at a configuration where it is nearer on the mean and no worse in its worst
part, its leak and its steps and reads together, over the 7 scenes, and the one standing above every
other at every configuration is the default. The last scene is held out of the standing and read apart, since no
merge is tuned on it.

## Standing

| Configuration | Merge | Mean distance | Worst part | Leak | Steps a texel | Reads a texel | Stands above | Held out, mean and worst |
|---|---|---|---|---|---|---|---|---|
| the module's | at the ray's end, `MergeAtRayEnd` | 0.041 | 0.252 | 0.00000 | 1.1 | 6.8 | by reach sight, by marched sight | 0.038 and 0.050 |
| the module's | by reach sight, `MergeByReachSight` | 0.095 | 0.473 | 0.00000 | 1.0 | 7.2 | none | 0.088 and 0.270 |
| the module's | by marched sight, `MergeByMarchedSight` | 0.426 | 1.902 | 0.00000 | 1.0 | 6.9 | none | 0.234 and 0.563 |
| an engine's | at the ray's end, `MergeAtRayEnd` | 0.049 | 0.139 | 0.00000 | 0.6 | 14.4 | by reach sight | 0.069 and 0.177 |
| an engine's | by reach sight, `MergeByReachSight` | 0.088 | 0.343 | 0.00000 | 1.2 | 15.3 | none | 0.065 and 0.177 |
| an engine's | by marched sight, `MergeByMarchedSight` | 0.349 | 1.163 | 0.00000 | 1.3 | 12.7 | none | 0.155 and 0.479 |

No merge stands above every other at every configuration, so the default stays the merge at the ray's end, `MergeAtRayEnd`.

## Each scene

| Configuration | Scene | Merge | Each part's ratio | Mean distance | Worst part | Leak | Steps a texel | Reads a texel |
|---|---|---|---|---|---|---|---|---|
| the module's | a room with a door | at the ray's end, `MergeAtRayEnd` | floor 1.002, walls 1.002, ceiling 1.013 | 0.006 | 0.013 |  | 0.8 | 4.6 |
| the module's | a room with a door | by reach sight, `MergeByReachSight` | floor 0.992, walls 1.053, ceiling 0.996 | 0.021 | 0.052 |  | 0.9 | 4.6 |
| the module's | a room with a door | by marched sight, `MergeByMarchedSight` | floor 0.920, walls 0.872, ceiling 0.892 | 0.112 | 0.137 |  | 0.9 | 4.8 |
| the module's | a closed box with one lamp | at the ray's end, `MergeAtRayEnd` | floor 0.944, walls 0.777 | 0.155 | 0.252 |  | 1.2 | 4.7 |
| the module's | a closed box with one lamp | by reach sight, `MergeByReachSight` | floor 1.161, walls 1.032 | 0.090 | 0.149 |  | 1.0 | 5.5 |
| the module's | a closed box with one lamp | by marched sight, `MergeByMarchedSight` | floor 1.161, walls 0.986 | 0.081 | 0.149 |  | 1.0 | 5.2 |
| the module's | a lit room beside a dark one through a wall of one cell | at the ray's end, `MergeAtRayEnd` | lit floor 0.960, lit walls 0.919 | 0.063 | 0.084 | 0.00000 | 1.0 | 3.9 |
| the module's | a lit room beside a dark one through a wall of one cell | by reach sight, `MergeByReachSight` | lit floor 0.962, lit walls 0.942 | 0.049 | 0.060 | 0.00000 | 1.0 | 4.7 |
| the module's | a lit room beside a dark one through a wall of one cell | by marched sight, `MergeByMarchedSight` | lit floor 0.924, lit walls 0.869 | 0.110 | 0.141 | 0.00000 | 1.0 | 4.6 |
| the module's | a room under a lintel | at the ray's end, `MergeAtRayEnd` | by the opening 0.917, between 0.981, deepest 0.917 | 0.064 | 0.087 |  | 2.0 | 18.4 |
| the module's | a room under a lintel | by reach sight, `MergeByReachSight` | by the opening 1.104, between 0.897, deepest 0.623 | 0.227 | 0.473 |  | 1.5 | 20.3 |
| the module's | a room under a lintel | by marched sight, `MergeByMarchedSight` | by the opening 0.334, between 0.187, deepest 0.149 | 1.559 | 1.902 |  | 1.6 | 17.9 |
| the module's | an open box under a sky | at the ray's end, `MergeAtRayEnd` | floor 1.029, walls 1.002 | 0.015 | 0.028 |  | 1.2 | 6.1 |
| the module's | an open box under a sky | by reach sight, `MergeByReachSight` | floor 1.255, walls 1.048 | 0.137 | 0.227 |  | 1.0 | 6.4 |
| the module's | an open box under a sky | by marched sight, `MergeByMarchedSight` | floor 0.271, walls 1.029 | 0.667 | 1.305 |  | 1.1 | 6.0 |
| the module's | a long hall | at the ray's end, `MergeAtRayEnd` | by the opening 0.961, middle 0.993, far end 0.993 | 0.018 | 0.040 |  | 0.4 | 5.2 |
| the module's | a long hall | by reach sight, `MergeByReachSight` | by the opening 1.017, middle 0.994, far end 0.992 | 0.010 | 0.017 |  | 0.7 | 4.4 |
| the module's | a long hall | by marched sight, `MergeByMarchedSight` | by the opening 0.978, middle 0.917, far end 0.893 | 0.074 | 0.113 |  | 0.8 | 4.9 |
| the module's | a vestibule off a sunlit court | at the ray's end, `MergeAtRayEnd` | wall at -z 1.000, wall at +z 1.000, beside the door 1.021, floor 0.962, ceiling 1.030 | 0.018 | 0.039 |  | 0.9 | 4.7 |
| the module's | a vestibule off a sunlit court | by reach sight, `MergeByReachSight` | wall at -z 1.114, wall at +z 1.114, beside the door 1.313, floor 0.979, ceiling 1.053 | 0.112 | 0.273 |  | 0.9 | 4.7 |
| the module's | a vestibule off a sunlit court | by marched sight, `MergeByMarchedSight` | wall at -z 0.810, wall at +z 0.810, beside the door 0.395, floor 0.870, ceiling 0.928 | 0.313 | 0.930 |  | 1.0 | 4.9 |
| the module's | held out, a room with a roof window and a pillar | at the ray's end, `MergeAtRayEnd` | floor 1.052, walls 1.026, ceiling 1.049, pillar 1.030 | 0.038 | 0.050 |  | 0.8 | 4.7 |
| the module's | held out, a room with a roof window and a pillar | by reach sight, `MergeByReachSight` | floor 1.310, walls 0.991, ceiling 1.060, pillar 1.017 | 0.088 | 0.270 |  | 0.9 | 4.6 |
| the module's | held out, a room with a roof window and a pillar | by marched sight, `MergeByMarchedSight` | floor 0.570, walls 0.799, ceiling 1.024, pillar 0.880 | 0.234 | 0.563 |  | 0.9 | 4.8 |
| an engine's | a room with a door | at the ray's end, `MergeAtRayEnd` | floor 1.005, walls 1.075, ceiling 1.006 | 0.028 | 0.072 |  | 0.4 | 15.2 |
| an engine's | a room with a door | by reach sight, `MergeByReachSight` | floor 0.967, walls 1.055, ceiling 1.009 | 0.032 | 0.054 |  | 1.2 | 15.3 |
| an engine's | a room with a door | by marched sight, `MergeByMarchedSight` | floor 0.879, walls 0.929, ceiling 0.936 | 0.089 | 0.129 |  | 1.3 | 12.4 |
| an engine's | a closed box with one lamp | at the ray's end, `MergeAtRayEnd` | floor 0.914, walls 0.899 | 0.098 | 0.106 |  | 0.6 | 11.0 |
| an engine's | a closed box with one lamp | by reach sight, `MergeByReachSight` | floor 0.725, walls 0.859 | 0.236 | 0.321 |  | 1.2 | 13.5 |
| an engine's | a closed box with one lamp | by marched sight, `MergeByMarchedSight` | floor 0.725, walls 0.859 | 0.236 | 0.321 |  | 1.3 | 11.2 |
| an engine's | a lit room beside a dark one through a wall of one cell | at the ray's end, `MergeAtRayEnd` | lit floor 0.957, lit walls 0.941 | 0.052 | 0.061 | 0.00000 | 0.4 | 14.2 |
| an engine's | a lit room beside a dark one through a wall of one cell | by reach sight, `MergeByReachSight` | lit floor 0.962, lit walls 0.941 | 0.050 | 0.061 | 0.00000 | 1.0 | 14.5 |
| an engine's | a lit room beside a dark one through a wall of one cell | by marched sight, `MergeByMarchedSight` | lit floor 0.961, lit walls 0.941 | 0.050 | 0.061 | 0.00000 | 1.2 | 11.7 |
| an engine's | a room under a lintel | at the ray's end, `MergeAtRayEnd` | by the opening 0.892, between 0.870, deepest 0.981 | 0.091 | 0.139 |  | 0.9 | 11.6 |
| an engine's | a room under a lintel | by reach sight, `MergeByReachSight` | by the opening 0.897, between 0.954, deepest 0.966 | 0.063 | 0.109 |  | 1.2 | 13.1 |
| an engine's | a room under a lintel | by marched sight, `MergeByMarchedSight` | by the opening 0.676, between 0.312, deepest 0.362 | 0.857 | 1.163 |  | 1.3 | 11.2 |
| an engine's | an open box under a sky | at the ray's end, `MergeAtRayEnd` | floor 1.054, walls 0.979 | 0.037 | 0.052 |  | 0.7 | 13.4 |
| an engine's | an open box under a sky | by reach sight, `MergeByReachSight` | floor 0.806, walls 0.956 | 0.130 | 0.215 |  | 1.3 | 15.3 |
| an engine's | an open box under a sky | by marched sight, `MergeByMarchedSight` | floor 0.804, walls 0.956 | 0.131 | 0.218 |  | 1.4 | 13.1 |
| an engine's | a long hall | at the ray's end, `MergeAtRayEnd` | by the opening 0.986, middle 0.980, far end 0.990 | 0.015 | 0.020 |  | 0.5 | 20.4 |
| an engine's | a long hall | by reach sight, `MergeByReachSight` | by the opening 0.974, middle 0.971, far end 0.988 | 0.023 | 0.029 |  | 1.5 | 20.0 |
| an engine's | a long hall | by marched sight, `MergeByMarchedSight` | by the opening 0.823, middle 0.936, far end 0.978 | 0.094 | 0.194 |  | 1.6 | 16.9 |
| an engine's | a vestibule off a sunlit court | at the ray's end, `MergeAtRayEnd` | wall at -z 1.042, wall at +z 1.042, beside the door 1.067, floor 0.966, ceiling 0.983 | 0.040 | 0.065 |  | 0.5 | 15.2 |
| an engine's | a vestibule off a sunlit court | by reach sight, `MergeByReachSight` | wall at -z 0.936, wall at +z 0.936, beside the door 0.952, floor 0.709, ceiling 0.956 | 0.114 | 0.343 |  | 1.2 | 15.4 |
| an engine's | a vestibule off a sunlit court | by marched sight, `MergeByMarchedSight` | wall at -z 0.555, wall at +z 0.555, beside the door 0.410, floor 0.535, ceiling 0.721 | 0.604 | 0.891 |  | 1.3 | 12.6 |
| an engine's | held out, a room with a roof window and a pillar | at the ray's end, `MergeAtRayEnd` | floor 1.041, walls 1.194, ceiling 1.012, pillar 1.048 | 0.069 | 0.177 |  | 0.3 | 15.5 |
| an engine's | held out, a room with a roof window and a pillar | by reach sight, `MergeByReachSight` | floor 0.965, walls 1.194, ceiling 1.004, pillar 1.044 | 0.065 | 0.177 |  | 1.2 | 15.5 |
| an engine's | held out, a room with a roof window and a pillar | by marched sight, `MergeByMarchedSight` | floor 0.619, walls 1.019, ceiling 1.002, pillar 0.886 | 0.155 | 0.479 |  | 1.3 | 12.7 |
