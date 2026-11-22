# The ladder of merges

Measured on 2026-11-22 on the tree of e310325 with the changes committed beside it, every merge the
passes take by name (`IMerge`) run over 8 scenes on the CPU at 2 configurations, as `dotnet test
RadianceCascades.Tests` writes it with `RC_LADDER` naming this file. The module's configuration is
its tests' chain of four levels in one cascade of the field of 64 cells of 0.25. An engine's is one
an engine binds the passes at, five levels of 8 probes a side every 8 cells of the field's five
cascades of 64 cells from 0.125, the finest two traced at 16 texels a side and read at 8, every ray
from its probe to four times its spacing but the last two's to the field's diagonal, each point
gathered from the finest level about it.

Each merge runs to a steady state of the bounce. A round runs the whole chain with every hit taking
its surface's own light and what the surface reflects of the light the probes gave it the round
before, gathered at a cell of a grid over the scene's surfaces, every surface reflecting 0.5 of it
but a black one and the lintel's room's 0.352, and nothing kept past that one round. The light
traced to the points follows the same rounds over the same cells by tracing alone. Each loop stops
at the first round that changes its points' light by under a hundredth, or at the 40th.

A part of a scene is read as the light its points take over the light traced to them, after one
gather and at the steady state, and its distance is the steady state's ratio's logarithm taken
whole, 0.105 a tenth over or under. The gain a round is the change a loop's last round makes over
the change of the round before, the factor a bounce multiplies an error by. The leak is the light
the dark room takes through a wall of one cell beside the lit one at the steady state, as a share of
the lit room's traced light. The cost is a texel's steps past its ray's end, the per-probe stage's
steps among them, and its reads of the probes above, a texel's light, its share of the sky or a
reach texel, on the mean over every level's texels. A merge stands above another at a configuration
where it is nearer on the mean and no worse in its worst part, its leak and its steps and reads
together, over the 7 scenes, and the one standing above every other at every configuration is the
default. The last scene is held out of the standing and read apart, since no merge is tuned on it.

## Standing

| Configuration | Merge | Mean distance | Worst part | Leak | Steps a texel | Reads a texel | Most gain a round | Stands above | Held out, mean and worst |
|---|---|---|---|---|---|---|---|---|---|
| the module's | by reach sight, `MergeByReachSight` | 0.083 | 0.487 | 0.00000 | 1.0 | 7.2 | 0.501 | none | 0.065 and 0.143 |
| the module's | at the ray's end, `MergeAtRayEnd` | 0.098 | 0.329 | 0.00000 | 1.1 | 6.8 | 0.501 | by marched sight | 0.030 and 0.041 |
| the module's | by marched sight, `MergeByMarchedSight` | 0.467 | 1.769 | 0.00000 | 1.0 | 6.9 | 0.475 | none | 0.270 and 0.412 |
| an engine's | at the ray's end, `MergeAtRayEnd` | 0.043 | 0.106 | 0.00000 | 0.6 | 14.4 | 0.500 | by reach sight | 0.092 and 0.153 |
| an engine's | by reach sight, `MergeByReachSight` | 0.076 | 0.251 | 0.00000 | 1.2 | 15.3 | 0.500 | none | 0.073 and 0.143 |
| an engine's | by marched sight, `MergeByMarchedSight` | 0.271 | 0.768 | 0.00000 | 1.3 | 12.7 | 0.500 | none | 0.087 and 0.212 |

No merge stands above every other at every configuration, so the default stays the merge at the ray's end, `MergeAtRayEnd`.

## Each scene

| Configuration | Scene | Merge | Each part's ratio after one gather | Each part's ratio at the steady state | Rounds | Gain a round | Mean distance | Worst part | Leak | Steps a texel | Reads a texel |
|---|---|---|---|---|---|---|---|---|---|---|---|
| the module's | a room with a door | at the ray's end, `MergeAtRayEnd` | floor 1.002, walls 1.002, ceiling 1.013 | floor 0.978, walls 0.975, ceiling 0.954 | 7 | 0.495 | 0.032 | 0.048 |  | 0.8 | 4.6 |
| the module's | a room with a door | by reach sight, `MergeByReachSight` | floor 0.992, walls 1.053, ceiling 0.996 | floor 0.980, walls 1.007, ceiling 0.956 | 7 | 0.494 | 0.024 | 0.045 |  | 0.9 | 4.6 |
| the module's | a room with a door | by marched sight, `MergeByMarchedSight` | floor 0.920, walls 0.872, ceiling 0.892 | floor 0.853, walls 0.812, ceiling 0.819 | 7 | 0.471 | 0.189 | 0.208 |  | 0.9 | 4.8 |
| the module's | a closed box with one lamp | at the ray's end, `MergeAtRayEnd` | floor 0.944, walls 0.777 | floor 0.886, walls 0.808 | 7 | 0.501 | 0.167 | 0.213 |  | 1.2 | 4.7 |
| the module's | a closed box with one lamp | by reach sight, `MergeByReachSight` | floor 1.161, walls 1.032 | floor 1.130, walls 1.073 | 7 | 0.501 | 0.096 | 0.122 |  | 1.0 | 5.5 |
| the module's | a closed box with one lamp | by marched sight, `MergeByMarchedSight` | floor 1.161, walls 0.986 | floor 1.010, walls 0.897 | 6 | 0.415 | 0.059 | 0.108 |  | 1.0 | 5.2 |
| the module's | a lit room beside a dark one through a wall of one cell | at the ray's end, `MergeAtRayEnd` | lit floor 0.960, lit walls 0.919 | lit floor 0.958, lit walls 0.941 | 7 | 0.500 | 0.052 | 0.061 | 0.00000 | 1.0 | 3.9 |
| the module's | a lit room beside a dark one through a wall of one cell | by reach sight, `MergeByReachSight` | lit floor 0.962, lit walls 0.942 | lit floor 0.962, lit walls 0.955 | 7 | 0.500 | 0.042 | 0.046 | 0.00000 | 1.0 | 4.7 |
| the module's | a lit room beside a dark one through a wall of one cell | by marched sight, `MergeByMarchedSight` | lit floor 0.924, lit walls 0.869 | lit floor 0.883, lit walls 0.836 | 7 | 0.475 | 0.152 | 0.180 | 0.00000 | 1.0 | 4.6 |
| the module's | a room under a lintel | at the ray's end, `MergeAtRayEnd` | by the opening 0.917, between 0.981, deepest 0.917 | by the opening 0.719, between 0.780, deepest 0.735 | 4 | 0.300 | 0.295 | 0.329 |  | 2.0 | 18.4 |
| the module's | a room under a lintel | by reach sight, `MergeByReachSight` | by the opening 1.104, between 0.897, deepest 0.623 | by the opening 0.880, between 0.766, deepest 0.615 | 4 | 0.292 | 0.294 | 0.487 |  | 1.5 | 20.3 |
| the module's | a room under a lintel | by marched sight, `MergeByMarchedSight` | by the opening 0.334, between 0.187, deepest 0.149 | by the opening 0.284, between 0.192, deepest 0.170 | 5 | 0.278 | 1.559 | 1.769 |  | 1.6 | 17.9 |
| the module's | an open box under a sky | at the ray's end, `MergeAtRayEnd` | floor 1.029, walls 1.002 | floor 1.012, walls 1.002 | 6 | 0.405 | 0.007 | 0.011 |  | 1.2 | 6.1 |
| the module's | an open box under a sky | by reach sight, `MergeByReachSight` | floor 1.255, walls 1.048 | floor 1.114, walls 1.036 | 6 | 0.393 | 0.071 | 0.108 |  | 1.0 | 6.4 |
| the module's | an open box under a sky | by marched sight, `MergeByMarchedSight` | floor 0.271, walls 1.029 | floor 0.378, walls 0.841 | 5 | 0.327 | 0.573 | 0.972 |  | 1.1 | 6.0 |
| the module's | a long hall | at the ray's end, `MergeAtRayEnd` | by the opening 0.961, middle 0.993, far end 0.993 | by the opening 0.954, middle 0.988, far end 0.995 | 7 | 0.489 | 0.021 | 0.047 |  | 0.4 | 5.2 |
| the module's | a long hall | by reach sight, `MergeByReachSight` | by the opening 1.017, middle 0.994, far end 0.992 | by the opening 0.990, middle 0.993, far end 0.998 | 7 | 0.487 | 0.007 | 0.010 |  | 0.7 | 4.4 |
| the module's | a long hall | by marched sight, `MergeByMarchedSight` | by the opening 0.978, middle 0.917, far end 0.893 | by the opening 0.909, middle 0.867, far end 0.838 | 7 | 0.458 | 0.138 | 0.177 |  | 0.8 | 4.9 |
| the module's | a vestibule off a sunlit court | at the ray's end, `MergeAtRayEnd` | wall at -z 1.000, wall at +z 1.000, beside the door 1.021, floor 0.962, ceiling 1.030 | wall at -z 0.913, wall at +z 0.913, beside the door 0.946, floor 0.922, ceiling 0.865 | 6 | 0.445 | 0.093 | 0.145 |  | 0.9 | 4.7 |
| the module's | a vestibule off a sunlit court | by reach sight, `MergeByReachSight` | wall at -z 1.114, wall at +z 1.114, beside the door 1.313, floor 0.979, ceiling 1.053 | wall at -z 0.994, wall at +z 0.994, beside the door 1.113, floor 0.962, ceiling 0.901 | 6 | 0.441 | 0.052 | 0.107 |  | 0.9 | 4.7 |
| the module's | a vestibule off a sunlit court | by marched sight, `MergeByMarchedSight` | wall at -z 0.810, wall at +z 0.810, beside the door 0.395, floor 0.870, ceiling 0.928 | wall at -z 0.693, wall at +z 0.693, beside the door 0.447, floor 0.775, ceiling 0.722 | 6 | 0.415 | 0.424 | 0.805 |  | 1.0 | 4.9 |
| the module's | held out, a room with a roof window and a pillar | at the ray's end, `MergeAtRayEnd` | floor 1.052, walls 1.026, ceiling 1.049, pillar 1.030 | floor 1.033, walls 1.021, ceiling 1.042, pillar 1.023 | 7 | 0.492 | 0.030 | 0.041 |  | 0.8 | 4.7 |
| the module's | held out, a room with a roof window and a pillar | by reach sight, `MergeByReachSight` | floor 1.310, walls 0.991, ceiling 1.060, pillar 1.017 | floor 1.154, walls 1.018, ceiling 1.070, pillar 1.032 | 7 | 0.490 | 0.065 | 0.143 |  | 0.9 | 4.6 |
| the module's | held out, a room with a roof window and a pillar | by marched sight, `MergeByMarchedSight` | floor 0.570, walls 0.799, ceiling 1.024, pillar 0.880 | floor 0.662, walls 0.742, ceiling 0.867, pillar 0.795 | 7 | 0.459 | 0.270 | 0.412 |  | 0.9 | 4.8 |
| an engine's | a room with a door | at the ray's end, `MergeAtRayEnd` | floor 1.005, walls 1.075, ceiling 1.006 | floor 1.025, walls 1.079, ceiling 1.025 | 7 | 0.492 | 0.042 | 0.076 |  | 0.4 | 15.2 |
| an engine's | a room with a door | by reach sight, `MergeByReachSight` | floor 0.967, walls 1.055, ceiling 1.009 | floor 0.996, walls 1.055, ceiling 1.001 | 7 | 0.492 | 0.020 | 0.054 |  | 1.2 | 15.3 |
| an engine's | a room with a door | by marched sight, `MergeByMarchedSight` | floor 0.879, walls 0.929, ceiling 0.936 | floor 0.913, walls 0.963, ceiling 0.928 | 7 | 0.494 | 0.068 | 0.091 |  | 1.3 | 12.4 |
| an engine's | a closed box with one lamp | at the ray's end, `MergeAtRayEnd` | floor 0.914, walls 0.899 | floor 0.907, walls 0.919 | 7 | 0.500 | 0.091 | 0.097 |  | 0.6 | 11.0 |
| an engine's | a closed box with one lamp | by reach sight, `MergeByReachSight` | floor 0.725, walls 0.859 | floor 0.778, walls 0.863 | 7 | 0.500 | 0.199 | 0.251 |  | 1.2 | 13.5 |
| an engine's | a closed box with one lamp | by marched sight, `MergeByMarchedSight` | floor 0.725, walls 0.859 | floor 0.778, walls 0.863 | 7 | 0.500 | 0.199 | 0.251 |  | 1.3 | 11.2 |
| an engine's | a lit room beside a dark one through a wall of one cell | at the ray's end, `MergeAtRayEnd` | lit floor 0.957, lit walls 0.941 | lit floor 0.955, lit walls 0.949 | 7 | 0.500 | 0.049 | 0.053 | 0.00000 | 0.4 | 14.2 |
| an engine's | a lit room beside a dark one through a wall of one cell | by reach sight, `MergeByReachSight` | lit floor 0.962, lit walls 0.941 | lit floor 0.959, lit walls 0.951 | 7 | 0.500 | 0.046 | 0.051 | 0.00000 | 1.0 | 14.5 |
| an engine's | a lit room beside a dark one through a wall of one cell | by marched sight, `MergeByMarchedSight` | lit floor 0.961, lit walls 0.941 | lit floor 0.957, lit walls 0.949 | 7 | 0.499 | 0.048 | 0.053 | 0.00000 | 1.2 | 11.7 |
| an engine's | a room under a lintel | at the ray's end, `MergeAtRayEnd` | by the opening 0.892, between 0.870, deepest 0.981 | by the opening 0.900, between 0.926, deepest 1.028 | 5 | 0.286 | 0.070 | 0.106 |  | 0.9 | 11.6 |
| an engine's | a room under a lintel | by reach sight, `MergeByReachSight` | by the opening 0.897, between 0.954, deepest 0.966 | by the opening 0.892, between 0.975, deepest 0.949 | 5 | 0.287 | 0.064 | 0.114 |  | 1.2 | 13.1 |
| an engine's | a room under a lintel | by marched sight, `MergeByMarchedSight` | by the opening 0.676, between 0.312, deepest 0.362 | by the opening 0.675, between 0.464, deepest 0.548 | 5 | 0.278 | 0.587 | 0.768 |  | 1.3 | 11.2 |
| an engine's | an open box under a sky | at the ray's end, `MergeAtRayEnd` | floor 1.054, walls 0.979 | floor 1.023, walls 0.994 | 6 | 0.401 | 0.014 | 0.023 |  | 0.7 | 13.4 |
| an engine's | an open box under a sky | by reach sight, `MergeByReachSight` | floor 0.806, walls 0.956 | floor 0.909, walls 0.964 | 6 | 0.414 | 0.066 | 0.096 |  | 1.3 | 15.3 |
| an engine's | an open box under a sky | by marched sight, `MergeByMarchedSight` | floor 0.804, walls 0.956 | floor 0.903, walls 0.963 | 6 | 0.414 | 0.069 | 0.102 |  | 1.4 | 13.1 |
| an engine's | a long hall | at the ray's end, `MergeAtRayEnd` | by the opening 0.986, middle 0.980, far end 0.990 | by the opening 1.043, middle 0.982, far end 0.983 | 7 | 0.484 | 0.026 | 0.042 |  | 0.5 | 20.4 |
| an engine's | a long hall | by reach sight, `MergeByReachSight` | by the opening 0.974, middle 0.971, far end 0.988 | by the opening 1.032, middle 0.968, far end 0.979 | 7 | 0.485 | 0.029 | 0.033 |  | 1.5 | 20.0 |
| an engine's | a long hall | by marched sight, `MergeByMarchedSight` | by the opening 0.823, middle 0.936, far end 0.978 | by the opening 0.891, middle 0.928, far end 0.961 | 7 | 0.484 | 0.077 | 0.116 |  | 1.6 | 16.9 |
| an engine's | a vestibule off a sunlit court | at the ray's end, `MergeAtRayEnd` | wall at -z 1.042, wall at +z 1.042, beside the door 1.067, floor 0.966, ceiling 0.983 | wall at -z 1.033, wall at +z 1.033, beside the door 1.058, floor 0.994, ceiling 0.996 | 7 | 0.443 | 0.026 | 0.056 |  | 0.5 | 15.2 |
| an engine's | a vestibule off a sunlit court | by reach sight, `MergeByReachSight` | wall at -z 0.936, wall at +z 0.936, beside the door 0.952, floor 0.709, ceiling 0.956 | wall at -z 0.914, wall at +z 0.914, beside the door 0.940, floor 0.815, ceiling 0.895 | 7 | 0.442 | 0.111 | 0.204 |  | 1.2 | 15.4 |
| an engine's | a vestibule off a sunlit court | by marched sight, `MergeByMarchedSight` | wall at -z 0.555, wall at +z 0.555, beside the door 0.410, floor 0.535, ceiling 0.721 | wall at -z 0.599, wall at +z 0.600, beside the door 0.505, floor 0.600, ceiling 0.685 | 7 | 0.443 | 0.519 | 0.682 |  | 1.3 | 12.6 |
| an engine's | held out, a room with a roof window and a pillar | at the ray's end, `MergeAtRayEnd` | floor 1.041, walls 1.194, ceiling 1.012, pillar 1.048 | floor 1.076, walls 1.166, ceiling 1.067, pillar 1.078 | 7 | 0.490 | 0.092 | 0.153 |  | 0.3 | 15.5 |
| an engine's | held out, a room with a roof window and a pillar | by reach sight, `MergeByReachSight` | floor 0.965, walls 1.194, ceiling 1.004, pillar 1.044 | floor 1.031, walls 1.154, ceiling 1.054, pillar 1.068 | 7 | 0.491 | 0.073 | 0.143 |  | 1.2 | 15.5 |
| an engine's | held out, a room with a roof window and a pillar | by marched sight, `MergeByMarchedSight` | floor 0.619, walls 1.019, ceiling 1.002, pillar 0.886 | floor 0.809, walls 0.995, ceiling 0.967, pillar 0.908 | 7 | 0.494 | 0.087 | 0.212 |  | 1.3 | 12.7 |

## The traced loop

| Scene | Cells | Rounds | Gain a round |
|---|---|---|---|
| a room with a door | 4830 | 7 | 0.494 |
| a closed box with one lamp | 5768 | 7 | 0.500 |
| a lit room beside a dark one through a wall of one cell | 6372 | 7 | 0.500 |
| a room under a lintel | 4468 | 5 | 0.272 |
| an open box under a sky | 4868 | 6 | 0.406 |
| a long hall | 2392 | 7 | 0.485 |
| a vestibule off a sunlit court | 6192 | 7 | 0.447 |
| held out, a room with a roof window and a pillar | 4052 | 7 | 0.493 |

## The vestibule round by round

Its two walls' light each round over the first round's, traced and by each merge.

| Configuration | Merge | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|---|---|---|---|---|---|---|---|---|
| every one | traced | 1.000 | 1.504 | 1.741 | 1.848 | 1.896 | 1.918 | 1.927 |
| the module's | at the ray's end, `MergeAtRayEnd` | 1.000 | 1.428 | 1.620 | 1.706 | 1.743 | 1.760 |  |
| the module's | by reach sight, `MergeByReachSight` | 1.000 | 1.408 | 1.590 | 1.670 | 1.705 | 1.721 |  |
| the module's | by marched sight, `MergeByMarchedSight` | 1.000 | 1.383 | 1.544 | 1.610 | 1.637 | 1.648 |  |
| an engine's | at the ray's end, `MergeAtRayEnd` | 1.000 | 1.500 | 1.732 | 1.836 | 1.881 | 1.902 | 1.911 |
| an engine's | by reach sight, `MergeByReachSight` | 1.000 | 1.484 | 1.709 | 1.810 | 1.854 | 1.874 | 1.882 |
| an engine's | by marched sight, `MergeByMarchedSight` | 1.000 | 1.578 | 1.861 | 1.989 | 2.046 | 2.071 | 2.082 |
