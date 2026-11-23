# The ladder of merges

Measured on 2026-11-23 on the tree of 151689d with the changes committed beside it, every merge the
passes take by name (`IMerge`) run over 9 scenes on the CPU at 2 configurations, as `dotnet test
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
together, over the 8 scenes, and the one standing above every other at every configuration is the
default. The last scene is held out of the standing and read apart, since no merge is tuned on it.

## Standing

| Configuration | Merge | Mean distance | Worst part | Leak | Steps a texel | Reads a texel | Most gain a round | Stands above | Held out, mean and worst |
|---|---|---|---|---|---|---|---|---|---|
| the module's | by reach sight, `MergeByReachSight` | 0.095 | 0.487 | 0.00000 | 1.0 | 7.2 | 0.501 | none | 0.065 and 0.143 |
| the module's | at the ray's end, `MergeAtRayEnd` | 0.099 | 0.329 | 0.00000 | 1.1 | 6.7 | 0.501 | about its own probe, by marched sight | 0.030 and 0.041 |
| the module's | about its own probe, `MergeAboutProbe` | 0.130 | 0.720 | 0.00000 | 0.0 | 7.9 | 0.501 | none | 0.073 and 0.170 |
| the module's | by marched sight, `MergeByMarchedSight` | 0.501 | 1.769 | 0.00000 | 1.1 | 6.8 | 0.475 | none | 0.270 and 0.412 |
| an engine's | at the ray's end, `MergeAtRayEnd` | 0.072 | 0.653 | 0.00000 | 0.6 | 14.3 | 0.500 | by reach sight, about its own probe | 0.092 and 0.153 |
| an engine's | by reach sight, `MergeByReachSight` | 0.107 | 0.724 | 0.00000 | 1.2 | 15.3 | 0.500 | none | 0.073 and 0.143 |
| an engine's | about its own probe, `MergeAboutProbe` | 0.227 | 0.750 | 0.00000 | 0.0 | 16.2 | 0.500 | none | 0.071 and 0.186 |
| an engine's | by marched sight, `MergeByMarchedSight` | 0.283 | 0.768 | 0.00000 | 1.3 | 12.9 | 0.500 | none | 0.087 and 0.212 |

No merge stands above every other at every configuration, so the default stays the merge at the ray's end, `MergeAtRayEnd`.

## Each scene

| Configuration | Scene | Merge | Each part's ratio after one gather | Each part's ratio at the steady state | Rounds | Gain a round | Mean distance | Worst part | Leak | Steps a texel | Reads a texel |
|---|---|---|---|---|---|---|---|---|---|---|---|
| the module's | a room with a door | at the ray's end, `MergeAtRayEnd` | floor 1.002, walls 1.002, ceiling 1.013 | floor 0.978, walls 0.975, ceiling 0.954 | 7 | 0.495 | 0.032 | 0.048 |  | 0.8 | 4.6 |
| the module's | a room with a door | by reach sight, `MergeByReachSight` | floor 0.992, walls 1.053, ceiling 0.996 | floor 0.980, walls 1.007, ceiling 0.956 | 7 | 0.494 | 0.024 | 0.045 |  | 0.9 | 4.6 |
| the module's | a room with a door | by marched sight, `MergeByMarchedSight` | floor 0.920, walls 0.872, ceiling 0.892 | floor 0.853, walls 0.812, ceiling 0.819 | 7 | 0.471 | 0.189 | 0.208 |  | 0.9 | 4.8 |
| the module's | a room with a door | about its own probe, `MergeAboutProbe` | floor 0.961, walls 1.080, ceiling 1.049 | floor 0.974, walls 1.022, ceiling 0.982 | 7 | 0.493 | 0.022 | 0.026 |  | 0.0 | 5.1 |
| the module's | a closed box with one lamp | at the ray's end, `MergeAtRayEnd` | floor 0.944, walls 0.777 | floor 0.886, walls 0.808 | 7 | 0.501 | 0.167 | 0.213 |  | 1.2 | 4.7 |
| the module's | a closed box with one lamp | by reach sight, `MergeByReachSight` | floor 1.161, walls 1.032 | floor 1.130, walls 1.073 | 7 | 0.501 | 0.096 | 0.122 |  | 1.0 | 5.5 |
| the module's | a closed box with one lamp | by marched sight, `MergeByMarchedSight` | floor 1.161, walls 0.986 | floor 1.010, walls 0.897 | 6 | 0.415 | 0.059 | 0.108 |  | 1.0 | 5.2 |
| the module's | a closed box with one lamp | about its own probe, `MergeAboutProbe` | floor 1.161, walls 0.820 | floor 1.043, walls 0.867 | 7 | 0.501 | 0.093 | 0.143 |  | 0.0 | 6.2 |
| the module's | a lit room beside a dark one through a wall of one cell | at the ray's end, `MergeAtRayEnd` | lit floor 0.960, lit walls 0.919 | lit floor 0.958, lit walls 0.941 | 7 | 0.500 | 0.052 | 0.061 | 0.00000 | 1.0 | 3.9 |
| the module's | a lit room beside a dark one through a wall of one cell | by reach sight, `MergeByReachSight` | lit floor 0.962, lit walls 0.942 | lit floor 0.962, lit walls 0.955 | 7 | 0.500 | 0.042 | 0.046 | 0.00000 | 1.0 | 4.7 |
| the module's | a lit room beside a dark one through a wall of one cell | by marched sight, `MergeByMarchedSight` | lit floor 0.924, lit walls 0.869 | lit floor 0.883, lit walls 0.836 | 7 | 0.475 | 0.152 | 0.180 | 0.00000 | 1.0 | 4.6 |
| the module's | a lit room beside a dark one through a wall of one cell | about its own probe, `MergeAboutProbe` | lit floor 0.953, lit walls 0.907 | lit floor 0.953, lit walls 0.933 | 7 | 0.500 | 0.059 | 0.069 | 0.00000 | 0.0 | 5.2 |
| the module's | a room under a lintel | at the ray's end, `MergeAtRayEnd` | by the opening 0.917, between 0.981, deepest 0.917 | by the opening 0.719, between 0.780, deepest 0.735 | 4 | 0.300 | 0.295 | 0.329 |  | 2.0 | 18.4 |
| the module's | a room under a lintel | by reach sight, `MergeByReachSight` | by the opening 1.104, between 0.897, deepest 0.623 | by the opening 0.880, between 0.766, deepest 0.615 | 4 | 0.292 | 0.294 | 0.487 |  | 1.5 | 20.3 |
| the module's | a room under a lintel | by marched sight, `MergeByMarchedSight` | by the opening 0.334, between 0.187, deepest 0.149 | by the opening 0.284, between 0.192, deepest 0.170 | 5 | 0.278 | 1.559 | 1.769 |  | 1.6 | 17.9 |
| the module's | a room under a lintel | about its own probe, `MergeAboutProbe` | by the opening 0.912, between 1.219, deepest 1.111 | by the opening 0.770, between 1.045, deepest 1.041 | 5 | 0.279 | 0.115 | 0.262 |  | 0.0 | 22.5 |
| the module's | an open box under a sky | at the ray's end, `MergeAtRayEnd` | floor 1.029, walls 1.002 | floor 1.012, walls 1.002 | 6 | 0.405 | 0.007 | 0.011 |  | 1.2 | 6.1 |
| the module's | an open box under a sky | by reach sight, `MergeByReachSight` | floor 1.255, walls 1.048 | floor 1.114, walls 1.036 | 6 | 0.393 | 0.071 | 0.108 |  | 1.0 | 6.4 |
| the module's | an open box under a sky | by marched sight, `MergeByMarchedSight` | floor 0.271, walls 1.029 | floor 0.378, walls 0.841 | 5 | 0.327 | 0.573 | 0.972 |  | 1.1 | 6.0 |
| the module's | an open box under a sky | about its own probe, `MergeAboutProbe` | floor 1.396, walls 1.110 | floor 1.182, walls 1.074 | 6 | 0.381 | 0.120 | 0.168 |  | 0.0 | 7.1 |
| the module's | a long hall | at the ray's end, `MergeAtRayEnd` | by the opening 0.961, middle 0.993, far end 0.993 | by the opening 0.954, middle 0.988, far end 0.995 | 7 | 0.489 | 0.021 | 0.047 |  | 0.4 | 5.2 |
| the module's | a long hall | by reach sight, `MergeByReachSight` | by the opening 1.017, middle 0.994, far end 0.992 | by the opening 0.990, middle 0.993, far end 0.998 | 7 | 0.487 | 0.007 | 0.010 |  | 0.7 | 4.4 |
| the module's | a long hall | by marched sight, `MergeByMarchedSight` | by the opening 0.978, middle 0.917, far end 0.893 | by the opening 0.909, middle 0.867, far end 0.838 | 7 | 0.458 | 0.138 | 0.177 |  | 0.8 | 4.9 |
| the module's | a long hall | about its own probe, `MergeAboutProbe` | by the opening 1.181, middle 1.184, far end 1.034 | by the opening 1.097, middle 1.122, far end 1.041 | 7 | 0.482 | 0.083 | 0.115 |  | 0.0 | 4.7 |
| the module's | a vestibule off a sunlit court | at the ray's end, `MergeAtRayEnd` | wall at -z 1.000, wall at +z 1.000, beside the door 1.021, floor 0.962, ceiling 1.030 | wall at -z 0.913, wall at +z 0.913, beside the door 0.946, floor 0.922, ceiling 0.865 | 6 | 0.445 | 0.093 | 0.145 |  | 0.9 | 4.7 |
| the module's | a vestibule off a sunlit court | by reach sight, `MergeByReachSight` | wall at -z 1.114, wall at +z 1.114, beside the door 1.313, floor 0.979, ceiling 1.053 | wall at -z 0.994, wall at +z 0.994, beside the door 1.113, floor 0.962, ceiling 0.901 | 6 | 0.441 | 0.052 | 0.107 |  | 0.9 | 4.7 |
| the module's | a vestibule off a sunlit court | by marched sight, `MergeByMarchedSight` | wall at -z 0.810, wall at +z 0.810, beside the door 0.395, floor 0.870, ceiling 0.928 | wall at -z 0.693, wall at +z 0.693, beside the door 0.447, floor 0.775, ceiling 0.722 | 6 | 0.415 | 0.424 | 0.805 |  | 1.0 | 4.9 |
| the module's | a vestibule off a sunlit court | about its own probe, `MergeAboutProbe` | wall at -z 1.364, wall at +z 1.364, beside the door 1.623, floor 1.120, ceiling 1.316 | wall at -z 1.176, wall at +z 1.175, beside the door 1.337, floor 1.109, ceiling 1.072 | 6 | 0.426 | 0.157 | 0.290 |  | 0.0 | 5.3 |
| the module's | a court lit in a strip between arcades | at the ray's end, `MergeAtRayEnd` | low wall 1.073, high wall 1.212 | low wall 0.915, high wall 0.883 | 5 | 0.441 | 0.107 | 0.125 |  | 1.2 | 6.1 |
| the module's | a court lit in a strip between arcades | by reach sight, `MergeByReachSight` | low wall 0.982, high wall 1.068 | low wall 0.846, high wall 0.764 | 5 | 0.454 | 0.218 | 0.269 |  | 1.1 | 6.7 |
| the module's | a court lit in a strip between arcades | by marched sight, `MergeByMarchedSight` | low wall 0.444, high wall 0.763 | low wall 0.377, high wall 0.492 | 5 | 0.406 | 0.843 | 0.977 |  | 1.1 | 6.2 |
| the module's | a court lit in a strip between arcades | about its own probe, `MergeAboutProbe` | low wall 0.575, high wall 1.264 | low wall 0.487, high wall 0.854 | 5 | 0.388 | 0.439 | 0.720 |  | 0.0 | 7.5 |
| the module's | held out, a room with a roof window and a pillar | at the ray's end, `MergeAtRayEnd` | floor 1.052, walls 1.026, ceiling 1.049, pillar 1.030 | floor 1.033, walls 1.021, ceiling 1.042, pillar 1.023 | 7 | 0.492 | 0.030 | 0.041 |  | 0.8 | 4.7 |
| the module's | held out, a room with a roof window and a pillar | by reach sight, `MergeByReachSight` | floor 1.310, walls 0.991, ceiling 1.060, pillar 1.017 | floor 1.154, walls 1.018, ceiling 1.070, pillar 1.032 | 7 | 0.490 | 0.065 | 0.143 |  | 0.9 | 4.6 |
| the module's | held out, a room with a roof window and a pillar | by marched sight, `MergeByMarchedSight` | floor 0.570, walls 0.799, ceiling 1.024, pillar 0.880 | floor 0.662, walls 0.742, ceiling 0.867, pillar 0.795 | 7 | 0.459 | 0.270 | 0.412 |  | 0.9 | 4.8 |
| the module's | held out, a room with a roof window and a pillar | about its own probe, `MergeAboutProbe` | floor 1.375, walls 1.064, ceiling 0.999, pillar 0.914 | floor 1.185, walls 1.060, ceiling 1.046, pillar 0.980 | 7 | 0.488 | 0.073 | 0.170 |  | 0.0 | 5.1 |
| an engine's | a room with a door | at the ray's end, `MergeAtRayEnd` | floor 1.005, walls 1.075, ceiling 1.006 | floor 1.025, walls 1.079, ceiling 1.025 | 7 | 0.492 | 0.042 | 0.076 |  | 0.4 | 15.2 |
| an engine's | a room with a door | by reach sight, `MergeByReachSight` | floor 0.967, walls 1.055, ceiling 1.009 | floor 0.996, walls 1.055, ceiling 1.001 | 7 | 0.492 | 0.020 | 0.054 |  | 1.2 | 15.3 |
| an engine's | a room with a door | by marched sight, `MergeByMarchedSight` | floor 0.879, walls 0.929, ceiling 0.936 | floor 0.913, walls 0.963, ceiling 0.928 | 7 | 0.494 | 0.068 | 0.091 |  | 1.3 | 12.4 |
| an engine's | a room with a door | about its own probe, `MergeAboutProbe` | floor 0.850, walls 0.942, ceiling 0.928 | floor 0.899, walls 0.958, ceiling 0.913 | 7 | 0.496 | 0.080 | 0.107 |  | 0.0 | 15.9 |
| an engine's | a closed box with one lamp | at the ray's end, `MergeAtRayEnd` | floor 0.914, walls 0.899 | floor 0.907, walls 0.919 | 7 | 0.500 | 0.091 | 0.097 |  | 0.6 | 11.0 |
| an engine's | a closed box with one lamp | by reach sight, `MergeByReachSight` | floor 0.725, walls 0.859 | floor 0.778, walls 0.863 | 7 | 0.500 | 0.199 | 0.251 |  | 1.2 | 13.5 |
| an engine's | a closed box with one lamp | by marched sight, `MergeByMarchedSight` | floor 0.725, walls 0.859 | floor 0.778, walls 0.863 | 7 | 0.500 | 0.199 | 0.251 |  | 1.3 | 11.2 |
| an engine's | a closed box with one lamp | about its own probe, `MergeAboutProbe` | floor 0.725, walls 0.859 | floor 0.776, walls 0.863 | 7 | 0.500 | 0.201 | 0.254 |  | 0.0 | 14.4 |
| an engine's | a lit room beside a dark one through a wall of one cell | at the ray's end, `MergeAtRayEnd` | lit floor 0.957, lit walls 0.941 | lit floor 0.955, lit walls 0.949 | 7 | 0.500 | 0.049 | 0.053 | 0.00000 | 0.4 | 14.2 |
| an engine's | a lit room beside a dark one through a wall of one cell | by reach sight, `MergeByReachSight` | lit floor 0.962, lit walls 0.941 | lit floor 0.959, lit walls 0.951 | 7 | 0.500 | 0.046 | 0.051 | 0.00000 | 1.0 | 14.5 |
| an engine's | a lit room beside a dark one through a wall of one cell | by marched sight, `MergeByMarchedSight` | lit floor 0.961, lit walls 0.941 | lit floor 0.957, lit walls 0.949 | 7 | 0.499 | 0.048 | 0.053 | 0.00000 | 1.2 | 11.7 |
| an engine's | a lit room beside a dark one through a wall of one cell | about its own probe, `MergeAboutProbe` | lit floor 0.963, lit walls 0.941 | lit floor 0.960, lit walls 0.951 | 7 | 0.500 | 0.046 | 0.050 | 0.00000 | 0.0 | 14.9 |
| an engine's | a room under a lintel | at the ray's end, `MergeAtRayEnd` | by the opening 0.892, between 0.870, deepest 0.981 | by the opening 0.900, between 0.926, deepest 1.028 | 5 | 0.286 | 0.070 | 0.106 |  | 0.9 | 11.6 |
| an engine's | a room under a lintel | by reach sight, `MergeByReachSight` | by the opening 0.897, between 0.954, deepest 0.966 | by the opening 0.892, between 0.975, deepest 0.949 | 5 | 0.287 | 0.064 | 0.114 |  | 1.2 | 13.1 |
| an engine's | a room under a lintel | by marched sight, `MergeByMarchedSight` | by the opening 0.676, between 0.312, deepest 0.362 | by the opening 0.675, between 0.464, deepest 0.548 | 5 | 0.278 | 0.587 | 0.768 |  | 1.3 | 11.2 |
| an engine's | a room under a lintel | about its own probe, `MergeAboutProbe` | by the opening 0.463, between 0.390, deepest 0.434 | by the opening 0.548, between 0.539, deepest 0.636 | 5 | 0.290 | 0.557 | 0.619 |  | 0.0 | 14.2 |
| an engine's | an open box under a sky | at the ray's end, `MergeAtRayEnd` | floor 1.054, walls 0.979 | floor 1.023, walls 0.994 | 6 | 0.401 | 0.014 | 0.023 |  | 0.7 | 13.4 |
| an engine's | an open box under a sky | by reach sight, `MergeByReachSight` | floor 0.806, walls 0.956 | floor 0.909, walls 0.964 | 6 | 0.414 | 0.066 | 0.096 |  | 1.3 | 15.3 |
| an engine's | an open box under a sky | by marched sight, `MergeByMarchedSight` | floor 0.804, walls 0.956 | floor 0.903, walls 0.963 | 6 | 0.414 | 0.069 | 0.102 |  | 1.4 | 13.1 |
| an engine's | an open box under a sky | about its own probe, `MergeAboutProbe` | floor 0.809, walls 0.953 | floor 0.908, walls 0.961 | 6 | 0.415 | 0.068 | 0.097 |  | 0.0 | 16.5 |
| an engine's | a long hall | at the ray's end, `MergeAtRayEnd` | by the opening 0.986, middle 0.980, far end 0.990 | by the opening 1.043, middle 0.982, far end 0.983 | 7 | 0.484 | 0.026 | 0.042 |  | 0.5 | 20.4 |
| an engine's | a long hall | by reach sight, `MergeByReachSight` | by the opening 0.974, middle 0.971, far end 0.988 | by the opening 1.032, middle 0.968, far end 0.979 | 7 | 0.485 | 0.029 | 0.033 |  | 1.5 | 20.0 |
| an engine's | a long hall | by marched sight, `MergeByMarchedSight` | by the opening 0.823, middle 0.936, far end 0.978 | by the opening 0.891, middle 0.928, far end 0.961 | 7 | 0.484 | 0.077 | 0.116 |  | 1.6 | 16.9 |
| an engine's | a long hall | about its own probe, `MergeAboutProbe` | by the opening 0.974, middle 0.972, far end 0.988 | by the opening 1.032, middle 0.967, far end 0.979 | 7 | 0.485 | 0.029 | 0.034 |  | 0.0 | 20.8 |
| an engine's | a vestibule off a sunlit court | at the ray's end, `MergeAtRayEnd` | wall at -z 1.042, wall at +z 1.042, beside the door 1.067, floor 0.966, ceiling 0.983 | wall at -z 1.033, wall at +z 1.033, beside the door 1.058, floor 0.994, ceiling 0.996 | 7 | 0.443 | 0.026 | 0.056 |  | 0.5 | 15.2 |
| an engine's | a vestibule off a sunlit court | by reach sight, `MergeByReachSight` | wall at -z 0.936, wall at +z 0.936, beside the door 0.952, floor 0.709, ceiling 0.956 | wall at -z 0.914, wall at +z 0.914, beside the door 0.940, floor 0.815, ceiling 0.895 | 7 | 0.442 | 0.111 | 0.204 |  | 1.2 | 15.4 |
| an engine's | a vestibule off a sunlit court | by marched sight, `MergeByMarchedSight` | wall at -z 0.555, wall at +z 0.555, beside the door 0.410, floor 0.535, ceiling 0.721 | wall at -z 0.599, wall at +z 0.600, beside the door 0.505, floor 0.600, ceiling 0.685 | 7 | 0.443 | 0.519 | 0.682 |  | 1.3 | 12.6 |
| an engine's | a vestibule off a sunlit court | about its own probe, `MergeAboutProbe` | wall at -z 0.708, wall at +z 0.708, beside the door 0.631, floor 0.634, ceiling 0.820 | wall at -z 0.747, wall at +z 0.747, beside the door 0.699, floor 0.719, ceiling 0.779 | 7 | 0.450 | 0.304 | 0.358 |  | 0.0 | 16.1 |
| an engine's | a court lit in a strip between arcades | at the ray's end, `MergeAtRayEnd` | low wall 1.163, high wall 2.367 | low wall 1.082, high wall 1.922 | 6 | 0.394 | 0.366 | 0.653 |  | 0.9 | 13.1 |
| an engine's | a court lit in a strip between arcades | by reach sight, `MergeByReachSight` | low wall 1.183, high wall 2.573 | low wall 1.118, high wall 2.063 | 6 | 0.405 | 0.418 | 0.724 |  | 1.3 | 15.5 |
| an engine's | a court lit in a strip between arcades | by marched sight, `MergeByMarchedSight` | low wall 1.177, high wall 2.573 | low wall 1.079, high wall 2.045 | 6 | 0.388 | 0.396 | 0.715 |  | 1.4 | 13.8 |
| an engine's | a court lit in a strip between arcades | about its own probe, `MergeAboutProbe` | low wall 1.195, high wall 2.727 | low wall 1.094, high wall 2.117 | 6 | 0.390 | 0.420 | 0.750 |  | 0.0 | 17.2 |
| an engine's | held out, a room with a roof window and a pillar | at the ray's end, `MergeAtRayEnd` | floor 1.041, walls 1.194, ceiling 1.012, pillar 1.048 | floor 1.076, walls 1.166, ceiling 1.067, pillar 1.078 | 7 | 0.490 | 0.092 | 0.153 |  | 0.3 | 15.5 |
| an engine's | held out, a room with a roof window and a pillar | by reach sight, `MergeByReachSight` | floor 0.965, walls 1.194, ceiling 1.004, pillar 1.044 | floor 1.031, walls 1.154, ceiling 1.054, pillar 1.068 | 7 | 0.491 | 0.073 | 0.143 |  | 1.2 | 15.5 |
| an engine's | held out, a room with a roof window and a pillar | by marched sight, `MergeByMarchedSight` | floor 0.619, walls 1.019, ceiling 1.002, pillar 0.886 | floor 0.809, walls 0.995, ceiling 0.967, pillar 0.908 | 7 | 0.494 | 0.087 | 0.212 |  | 1.3 | 12.7 |
| an engine's | held out, a room with a roof window and a pillar | about its own probe, `MergeAboutProbe` | floor 0.637, walls 1.035, ceiling 1.025, pillar 0.895 | floor 0.830, walls 1.012, ceiling 0.991, pillar 0.925 | 7 | 0.497 | 0.071 | 0.186 |  | 0.0 | 16.1 |

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
| a court lit in a strip between arcades | 9012 | 6 | 0.404 |
| held out, a room with a roof window and a pillar | 4052 | 7 | 0.493 |

## The vestibule round by round

Its two walls' light each round over the first round's, traced and by each merge.

| Configuration | Merge | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|---|---|---|---|---|---|---|---|---|
| every one | traced | 1.000 | 1.504 | 1.741 | 1.848 | 1.896 | 1.918 | 1.927 |
| the module's | at the ray's end, `MergeAtRayEnd` | 1.000 | 1.428 | 1.620 | 1.706 | 1.743 | 1.760 |  |
| the module's | by reach sight, `MergeByReachSight` | 1.000 | 1.408 | 1.590 | 1.670 | 1.705 | 1.721 |  |
| the module's | by marched sight, `MergeByMarchedSight` | 1.000 | 1.383 | 1.544 | 1.610 | 1.637 | 1.648 |  |
| the module's | about its own probe, `MergeAboutProbe` | 1.000 | 1.384 | 1.549 | 1.619 | 1.648 | 1.661 |  |
| an engine's | at the ray's end, `MergeAtRayEnd` | 1.000 | 1.500 | 1.732 | 1.836 | 1.881 | 1.902 | 1.911 |
| an engine's | by reach sight, `MergeByReachSight` | 1.000 | 1.484 | 1.709 | 1.810 | 1.854 | 1.874 | 1.882 |
| an engine's | by marched sight, `MergeByMarchedSight` | 1.000 | 1.578 | 1.861 | 1.989 | 2.046 | 2.071 | 2.082 |
| an engine's | about its own probe, `MergeAboutProbe` | 1.000 | 1.555 | 1.821 | 1.942 | 1.997 | 2.022 | 2.033 |

## The cones by roughness

Measured on 2026-11-23 on the tree of 151689d with the changes committed beside it, the cone a rough
surface reflects read from the cascades by `ConeAtProbes` (`IConeRead`) over 11 scenes, the ladder's
and two of their own, on the CPU at 2 configurations after one gather, as `dotnet test
RadianceCascades.Tests` writes it with `RC_LADDER` naming this file. Each point of a scene reflects
toward an eye 45 degrees off its normal, at each roughness of 0.1, 0.2, 0.3, 0.5, 0.7, 1.0, the lobe
of a GGX surface whose alpha is the roughness squared, and the trace draws the same lobe's ways with
4,096 rays a cone, each taking the light of the surface it meets or the sky's.

A part's difference is the sum over its points of the reading less the trace, taken whole, over the
count of its points times the scene's light, its traced light on the mean over its points at
roughness 1.0, the channels of the red wall's scene summed alike, so a scene whose narrow cones
mostly see nothing is read against the light it holds. The median scene is the median over the
scenes of each one's mean over its parts. The reading less the trace is the same sum signed, over a
scene's points. The trace's own noise is the same difference of half the two halves of its rays,
those of a checkerboard's black squares against its white ones, about how far the trace lies from
the lobe's light. The cone widened is the difference the trace makes at the alpha the read takes,
never under half the width of a texel of the level it reads, against the trace at its own alpha, the
part of the difference the cascades' count of directions makes alone. The leak is the light the dark
room's cones take through the wall of one cell, as a share of the lit room's traced cones. The reads
are a cone's reads of a texel or a reach texel. A mark is the least roughness from which on every
roughness reads within a tenth, and the leak is the dark room's cones' light over the lit room's
light.

| Configuration | Roughness | Mean difference | Median scene | Worst part | Of it the cone widened | The trace's own noise | Reading less the trace | Leak | Reads a point |
|---|---|---|---|---|---|---|---|---|---|
| the module's | 0.1 | 0.703 | 0.336 | 4.785 | 0.457 | 0.000 | +0.163 | 0.00000 | 162 |
| the module's | 0.2 | 0.552 | 0.220 | 4.092 | 0.302 | 0.004 | +0.140 | 0.00000 | 162 |
| the module's | 0.3 | 0.362 | 0.134 | 2.770 | 0.081 | 0.006 | +0.150 | 0.00000 | 162 |
| the module's | 0.5 | 0.205 | 0.069 | 1.899 | 0.000 | 0.010 | -0.016 | 0.00000 | 314 |
| the module's | 0.7 | 0.140 | 0.060 | 1.349 | 0.000 | 0.012 | -0.031 | 0.00000 | 314 |
| the module's | 1.0 | 0.114 | 0.049 | 0.986 | 0.000 | 0.017 | -0.029 | 0.00000 | 314 |
| an engine's | 0.1 | 0.741 | 0.250 | 7.885 | 0.457 | 0.000 | -0.413 | 0.02060 | 167 |
| an engine's | 0.2 | 0.706 | 0.290 | 7.199 | 0.302 | 0.004 | -0.436 | 0.02060 | 167 |
| an engine's | 0.3 | 0.635 | 0.341 | 5.820 | 0.081 | 0.006 | -0.426 | 0.02060 | 167 |
| an engine's | 0.5 | 0.247 | 0.153 | 1.084 | 0.000 | 0.010 | +0.018 | 0.00000 | 86 |
| an engine's | 0.7 | 0.224 | 0.095 | 1.160 | 0.000 | 0.012 | +0.056 | 0.00000 | 86 |
| an engine's | 1.0 | 0.218 | 0.082 | 1.211 | 0.000 | 0.017 | +0.099 | 0.00000 | 86 |

The cones read within a tenth of their trace on the median scene from roughness 0.5 up at the module's configuration and from roughness 0.7 up at an engine's, and on the mean over every part at no roughness of the six and at no roughness of the six.

### Each scene's mark

| Scene | From the module's configuration | From an engine's configuration |
|---|---|---|
| a room with a door | 0.3 | 0.5 |
| a closed box with one lamp | none | none |
| a lit room beside a dark one through a wall of one cell | 0.1 | 0.1 |
| a room under a lintel | 1.0 | 1.0 |
| an open box under a sky | 0.3 | 0.5 |
| a long hall | 0.1 | 0.5 |
| a vestibule off a sunlit court | 0.5 | 0.7 |
| a court lit in a strip between arcades | none | none |
| held out, a room with a roof window and a pillar | 1.0 | 1.0 |
| a low room lit by a small bright light | none | none |
| a red wall beside a gray floor | 0.3 | 0.5 |

### Each scene's cones

| Configuration | Scene | Roughness | Each part's difference | Each part's reading less its trace | The scene's reading less its trace |
|---|---|---|---|---|---|
| the module's | a room with a door | 0.1 | 0.007, 0.471, 0.006 | +0.007, -0.023, +0.006 | -0.009 |
| the module's | a room with a door | 0.2 | 0.006, 0.366, 0.005 | +0.006, -0.006, +0.005 | -0.001 |
| the module's | a room with a door | 0.3 | 0.005, 0.206, 0.005 | -0.002, +0.037, +0.001 | +0.019 |
| the module's | a room with a door | 0.5 | 0.012, 0.099, 0.006 | +0.001, +0.041, +0.003 | +0.023 |
| the module's | a room with a door | 0.7 | 0.026, 0.064, 0.013 | +0.004, +0.021, +0.008 | +0.014 |
| the module's | a room with a door | 1.0 | 0.051, 0.043, 0.032 | -0.001, +0.015, +0.021 | +0.013 |
| the module's | a closed box with one lamp | 0.1 | 0.364, 0.796 | +0.364, +0.796 | +0.665 |
| the module's | a closed box with one lamp | 0.2 | 0.291, 0.731 | +0.291, +0.731 | +0.597 |
| the module's | a closed box with one lamp | 0.3 | 0.189, 0.627 | +0.066, +0.564 | +0.412 |
| the module's | a closed box with one lamp | 0.5 | 0.256, 0.394 | -0.081, +0.133 | +0.068 |
| the module's | a closed box with one lamp | 0.7 | 0.206, 0.390 | -0.070, -0.086 | -0.081 |
| the module's | a closed box with one lamp | 1.0 | 0.123, 0.385 | -0.032, -0.186 | -0.139 |
| the module's | a lit room beside a dark one through a wall of one cell | 0.1 | 0.002, 0.088 | -0.002, -0.088 | -0.036 |
| the module's | a lit room beside a dark one through a wall of one cell | 0.2 | 0.002, 0.088 | -0.002, -0.088 | -0.036 |
| the module's | a lit room beside a dark one through a wall of one cell | 0.3 | 0.002, 0.088 | -0.002, -0.088 | -0.036 |
| the module's | a lit room beside a dark one through a wall of one cell | 0.5 | 0.010, 0.088 | -0.010, -0.088 | -0.041 |
| the module's | a lit room beside a dark one through a wall of one cell | 0.7 | 0.027, 0.068 | -0.027, -0.068 | -0.044 |
| the module's | a lit room beside a dark one through a wall of one cell | 1.0 | 0.050, 0.048 | -0.050, -0.048 | -0.049 |
| the module's | a room under a lintel | 0.1 | 4.785, 3.214, 1.114 | -2.232, -1.263, +1.114 | -1.032 |
| the module's | a room under a lintel | 0.2 | 4.092, 2.371, 1.020 | -1.546, -0.601, +0.893 | -0.582 |
| the module's | a room under a lintel | 0.3 | 2.770, 1.204, 0.818 | -0.166, +0.338, +0.811 | +0.267 |
| the module's | a room under a lintel | 0.5 | 0.823, 0.450, 0.209 | -0.818, -0.435, -0.208 | -0.522 |
| the module's | a room under a lintel | 0.7 | 0.250, 0.125, 0.094 | -0.250, -0.113, -0.094 | -0.160 |
| the module's | a room under a lintel | 1.0 | 0.113, 0.042, 0.052 | -0.111, -0.033, -0.052 | -0.067 |
| the module's | an open box under a sky | 0.1 | 0.303, 0.128 | +0.303, +0.128 | +0.182 |
| the module's | an open box under a sky | 0.2 | 0.197, 0.104 | +0.197, +0.104 | +0.132 |
| the module's | an open box under a sky | 0.3 | 0.085, 0.040 | +0.078, +0.039 | +0.051 |
| the module's | an open box under a sky | 0.5 | 0.034, 0.021 | +0.032, +0.010 | +0.017 |
| the module's | an open box under a sky | 0.7 | 0.025, 0.019 | +0.022, +0.009 | +0.013 |
| the module's | an open box under a sky | 1.0 | 0.023, 0.016 | +0.023, +0.003 | +0.009 |
| the module's | a long hall | 0.1 | 0.239, 0.025, 0.003 | +0.124, +0.025, +0.003 | +0.052 |
| the module's | a long hall | 0.2 | 0.152, 0.022, 0.002 | +0.051, +0.022, +0.002 | +0.025 |
| the module's | a long hall | 0.3 | 0.053, 0.006, 0.006 | -0.035, +0.002, -0.006 | -0.014 |
| the module's | a long hall | 0.5 | 0.096, 0.023, 0.037 | -0.093, -0.023, -0.036 | -0.053 |
| the module's | a long hall | 0.7 | 0.097, 0.033, 0.050 | -0.097, -0.032, -0.049 | -0.061 |
| the module's | a long hall | 1.0 | 0.073, 0.029, 0.041 | -0.071, -0.025, -0.038 | -0.046 |
| the module's | a vestibule off a sunlit court | 0.1 | 2.424, 0.078, 0.523, 0.041, 0.038 | -1.870, +0.010, +0.523, +0.039, +0.033 | -0.236 |
| the module's | a vestibule off a sunlit court | 0.2 | 1.666, 0.051, 0.343, 0.035, 0.033 | -1.304, +0.007, +0.343, +0.034, +0.029 | -0.163 |
| the module's | a vestibule off a sunlit court | 0.3 | 0.763, 0.025, 0.161, 0.020, 0.019 | -0.614, -0.010, +0.135, +0.008, +0.008 | -0.086 |
| the module's | a vestibule off a sunlit court | 0.5 | 0.220, 0.009, 0.077, 0.021, 0.019 | -0.144, 0.000, +0.053, +0.006, +0.017 | -0.012 |
| the module's | a vestibule off a sunlit court | 0.7 | 0.116, 0.011, 0.051, 0.024, 0.036 | -0.081, +0.005, +0.024, -0.011, +0.029 | -0.005 |
| the module's | a vestibule off a sunlit court | 1.0 | 0.058, 0.025, 0.032, 0.045, 0.076 | -0.039, +0.004, +0.012, -0.042, +0.055 | -0.001 |
| the module's | a court lit in a strip between arcades | 0.1 | 2.541, 0.023 | +2.541, +0.023 | +1.282 |
| the module's | a court lit in a strip between arcades | 0.2 | 1.894, 0.020 | +1.894, +0.020 | +0.957 |
| the module's | a court lit in a strip between arcades | 0.3 | 1.331, 0.004 | +1.236, +0.001 | +0.618 |
| the module's | a court lit in a strip between arcades | 0.5 | 0.787, 0.021 | +0.356, +0.019 | +0.187 |
| the module's | a court lit in a strip between arcades | 0.7 | 0.349, 0.081 | +0.113, +0.081 | +0.097 |
| the module's | a court lit in a strip between arcades | 1.0 | 0.154, 0.198 | +0.126, +0.198 | +0.162 |
| the module's | held out, a room with a roof window and a pillar | 0.1 | 0.238, 0.145, 0.732, 0.230 | +0.238, +0.145, +0.015, +0.230 | +0.140 |
| the module's | held out, a room with a roof window and a pillar | 0.2 | 0.149, 0.125, 0.462, 0.142 | +0.094, +0.116, +0.078, +0.142 | +0.104 |
| the module's | held out, a room with a roof window and a pillar | 0.3 | 0.093, 0.102, 0.188, 0.153 | +0.018, +0.070, +0.108, -0.091 | +0.061 |
| the module's | held out, a room with a roof window and a pillar | 0.5 | 0.052, 0.085, 0.114, 0.223 | +0.032, +0.021, +0.107, -0.181 | +0.035 |
| the module's | held out, a room with a roof window and a pillar | 0.7 | 0.055, 0.080, 0.089, 0.189 | +0.041, +0.021, +0.064, -0.131 | +0.030 |
| the module's | held out, a room with a roof window and a pillar | 1.0 | 0.045, 0.078, 0.066, 0.175 | +0.039, +0.041, +0.037, -0.125 | +0.034 |
| the module's | a low room lit by a small bright light | 0.1 | 3.217, 0.018, 0.000 | +3.217, +0.006, 0.000 | +0.754 |
| the module's | a low room lit by a small bright light | 0.2 | 2.730, 0.075, 0.000 | +2.217, -0.063, 0.000 | +0.483 |
| the module's | a low room lit by a small bright light | 0.3 | 2.231, 0.191, 0.000 | +1.910, -0.179, 0.000 | +0.350 |
| the module's | a low room lit by a small bright light | 0.5 | 1.899, 0.395, 0.000 | +1.298, -0.347, 0.000 | +0.118 |
| the module's | a low room lit by a small bright light | 0.7 | 1.349, 0.518, 0.000 | +0.408, -0.449, 0.000 | -0.144 |
| the module's | a low room lit by a small bright light | 1.0 | 0.986, 0.530, 0.000 | +0.106, -0.483, 0.000 | -0.233 |
| the module's | a red wall beside a gray floor | 0.1 | 0.337, 0.227, 0.133 | -0.109, +0.119, 0.000 | +0.038 |
| the module's | a red wall beside a gray floor | 0.2 | 0.242, 0.153, 0.100 | -0.055, +0.072, +0.005 | +0.027 |
| the module's | a red wall beside a gray floor | 0.3 | 0.115, 0.054, 0.046 | -0.012, +0.007, +0.011 | +0.003 |
| the module's | a red wall beside a gray floor | 0.5 | 0.038, 0.030, 0.021 | +0.018, -0.013, +0.009 | -0.001 |
| the module's | a red wall beside a gray floor | 0.7 | 0.020, 0.020, 0.013 | +0.012, -0.010, +0.006 | -0.001 |
| the module's | a red wall beside a gray floor | 1.0 | 0.019, 0.017, 0.011 | +0.010, -0.011, +0.004 | -0.002 |
| an engine's | a room with a door | 0.1 | 0.059, 0.504, 0.007 | +0.059, -0.109, +0.007 | -0.043 |
| an engine's | a room with a door | 0.2 | 0.059, 0.436, 0.006 | +0.058, -0.092, +0.006 | -0.034 |
| an engine's | a room with a door | 0.3 | 0.057, 0.351, 0.004 | +0.051, -0.049, +0.001 | -0.014 |
| an engine's | a room with a door | 0.5 | 0.012, 0.104, 0.010 | 0.000, +0.097, +0.005 | +0.053 |
| an engine's | a room with a door | 0.7 | 0.026, 0.131, 0.016 | +0.005, +0.129, +0.012 | +0.073 |
| an engine's | a room with a door | 1.0 | 0.053, 0.111, 0.036 | -0.001, +0.110, +0.032 | +0.066 |
| an engine's | a closed box with one lamp | 0.1 | 0.182, 0.189 | +0.182, +0.189 | +0.187 |
| an engine's | a closed box with one lamp | 0.2 | 0.123, 0.135 | +0.108, +0.124 | +0.119 |
| an engine's | a closed box with one lamp | 0.3 | 0.163, 0.192 | -0.117, -0.044 | -0.066 |
| an engine's | a closed box with one lamp | 0.5 | 0.573, 0.883 | -0.053, +0.391 | +0.256 |
| an engine's | a closed box with one lamp | 0.7 | 0.459, 0.758 | -0.114, +0.130 | +0.056 |
| an engine's | a closed box with one lamp | 1.0 | 0.314, 0.752 | -0.039, +0.025 | +0.005 |
| an engine's | a lit room beside a dark one through a wall of one cell | 0.1 | 0.001, 0.053 | -0.001, -0.053 | -0.021 |
| an engine's | a lit room beside a dark one through a wall of one cell | 0.2 | 0.001, 0.053 | -0.001, -0.053 | -0.021 |
| an engine's | a lit room beside a dark one through a wall of one cell | 0.3 | 0.001, 0.053 | -0.001, -0.053 | -0.021 |
| an engine's | a lit room beside a dark one through a wall of one cell | 0.5 | 0.011, 0.050 | -0.011, -0.050 | -0.026 |
| an engine's | a lit room beside a dark one through a wall of one cell | 0.7 | 0.028, 0.046 | -0.028, -0.046 | -0.036 |
| an engine's | a lit room beside a dark one through a wall of one cell | 1.0 | 0.051, 0.032 | -0.051, -0.032 | -0.044 |
| an engine's | a room under a lintel | 0.1 | 7.885, 3.735, 0.436 | -7.885, -3.446, +0.112 | -4.221 |
| an engine's | a room under a lintel | 0.2 | 7.199, 3.058, 0.532 | -7.199, -2.784, -0.110 | -3.771 |
| an engine's | a room under a lintel | 0.3 | 5.820, 2.109, 0.591 | -5.820, -1.845, -0.192 | -2.922 |
| an engine's | a room under a lintel | 0.5 | 1.084, 0.446, 0.163 | -1.084, -0.438, -0.155 | -0.610 |
| an engine's | a room under a lintel | 0.7 | 0.396, 0.164, 0.062 | -0.396, -0.163, -0.060 | -0.224 |
| an engine's | a room under a lintel | 1.0 | 0.141, 0.084, 0.034 | -0.140, -0.075, -0.032 | -0.089 |
| an engine's | an open box under a sky | 0.1 | 0.395, 0.230 | +0.395, +0.230 | +0.280 |
| an engine's | an open box under a sky | 0.2 | 0.289, 0.206 | +0.289, +0.206 | +0.231 |
| an engine's | an open box under a sky | 0.3 | 0.184, 0.161 | +0.170, +0.141 | +0.150 |
| an engine's | an open box under a sky | 0.5 | 0.061, 0.031 | +0.057, +0.019 | +0.031 |
| an engine's | an open box under a sky | 0.7 | 0.055, 0.020 | +0.051, -0.007 | +0.011 |
| an engine's | an open box under a sky | 1.0 | 0.048, 0.022 | +0.048, -0.015 | +0.004 |
| an engine's | a long hall | 0.1 | 1.282, 0.638, 0.638 | +0.243, -0.638, -0.638 | -0.324 |
| an engine's | a long hall | 0.2 | 1.364, 0.642, 0.640 | +0.171, -0.642, -0.640 | -0.351 |
| an engine's | a long hall | 0.3 | 1.483, 0.661, 0.648 | +0.084, -0.661, -0.648 | -0.390 |
| an engine's | a long hall | 0.5 | 0.162, 0.058, 0.027 | -0.023, -0.058, -0.027 | -0.034 |
| an engine's | a long hall | 0.7 | 0.144, 0.092, 0.049 | -0.083, -0.091, -0.049 | -0.073 |
| an engine's | a long hall | 1.0 | 0.118, 0.078, 0.049 | -0.110, -0.077, -0.049 | -0.079 |
| an engine's | a vestibule off a sunlit court | 0.1 | 4.106, 0.139, 0.346, 0.265, 0.031 | -3.872, +0.103, +0.346, +0.265, +0.031 | -0.503 |
| an engine's | a vestibule off a sunlit court | 0.2 | 3.410, 0.130, 0.181, 0.260, 0.027 | -3.306, +0.099, +0.166, +0.260, +0.027 | -0.430 |
| an engine's | a vestibule off a sunlit court | 0.3 | 2.667, 0.108, 0.135, 0.238, 0.009 | -2.616, +0.082, -0.041, +0.234, +0.006 | -0.352 |
| an engine's | a vestibule off a sunlit court | 0.5 | 0.756, 0.019, 0.116, 0.042, 0.036 | -0.248, +0.015, -0.055, +0.029, +0.034 | -0.025 |
| an engine's | a vestibule off a sunlit court | 0.7 | 0.331, 0.027, 0.051, 0.022, 0.036 | -0.017, +0.019, +0.027, +0.001, +0.035 | +0.013 |
| an engine's | a vestibule off a sunlit court | 1.0 | 0.127, 0.062, 0.033, 0.038, 0.087 | -0.021, +0.036, +0.027, -0.033, +0.086 | +0.020 |
| an engine's | a court lit in a strip between arcades | 0.1 | 0.314, 0.020 | +0.288, +0.020 | +0.154 |
| an engine's | a court lit in a strip between arcades | 0.2 | 0.625, 0.018 | -0.359, +0.018 | -0.171 |
| an engine's | a court lit in a strip between arcades | 0.3 | 1.041, 0.016 | -1.017, -0.002 | -0.510 |
| an engine's | a court lit in a strip between arcades | 0.5 | 0.738, 0.228 | +0.558, +0.228 | +0.393 |
| an engine's | a court lit in a strip between arcades | 0.7 | 0.891, 0.632 | +0.345, +0.632 | +0.489 |
| an engine's | a court lit in a strip between arcades | 1.0 | 0.837, 1.211 | +0.410, +1.211 | +0.811 |
| an engine's | held out, a room with a roof window and a pillar | 0.1 | 0.113, 0.142, 0.737, 0.006 | -0.045, -0.034, -0.584, -0.006 | -0.157 |
| an engine's | held out, a room with a roof window and a pillar | 0.2 | 0.226, 0.163, 0.620, 0.094 | -0.188, -0.062, -0.520, -0.094 | -0.193 |
| an engine's | held out, a room with a roof window and a pillar | 0.3 | 0.282, 0.206, 0.547, 0.327 | -0.264, -0.108, -0.490, -0.327 | -0.236 |
| an engine's | held out, a room with a roof window and a pillar | 0.5 | 0.124, 0.252, 0.097, 0.141 | +0.029, +0.248, +0.032, +0.006 | +0.143 |
| an engine's | held out, a room with a roof window and a pillar | 0.7 | 0.067, 0.204, 0.062, 0.134 | +0.039, +0.199, +0.034, -0.042 | +0.118 |
| an engine's | held out, a room with a roof window and a pillar | 1.0 | 0.045, 0.207, 0.053, 0.081 | +0.037, +0.204, +0.024, -0.012 | +0.119 |
| an engine's | a low room lit by a small bright light | 0.1 | 0.165, 0.007, 0.000 | -0.165, -0.007, 0.000 | -0.042 |
| an engine's | a low room lit by a small bright light | 0.2 | 1.166, 0.076, 0.000 | -1.166, -0.076, 0.000 | -0.313 |
| an engine's | a low room lit by a small bright light | 0.3 | 1.472, 0.192, 0.000 | -1.472, -0.192, 0.000 | -0.446 |
| an engine's | a low room lit by a small bright light | 0.5 | 0.929, 0.585, 0.000 | +0.084, -0.025, 0.000 | +0.006 |
| an engine's | a low room lit by a small bright light | 0.7 | 1.160, 1.011, 0.000 | +0.132, +0.282, 0.000 | +0.181 |
| an engine's | a low room lit by a small bright light | 1.0 | 0.988, 1.207, 0.000 | +0.036, +0.479, 0.000 | +0.264 |
| an engine's | a red wall beside a gray floor | 0.1 | 0.350, 0.594, 0.131 | -0.109, +0.349, -0.038 | +0.152 |
| an engine's | a red wall beside a gray floor | 0.2 | 0.252, 0.519, 0.099 | -0.054, +0.302, -0.033 | +0.141 |
| an engine's | a red wall beside a gray floor | 0.3 | 0.124, 0.412, 0.058 | -0.012, +0.237, -0.027 | +0.117 |
| an engine's | a red wall beside a gray floor | 0.5 | 0.047, 0.077, 0.034 | -0.018, +0.017, +0.027 | +0.011 |
| an engine's | a red wall beside a gray floor | 0.7 | 0.016, 0.039, 0.038 | +0.004, -0.007, +0.038 | +0.006 |
| an engine's | a red wall beside a gray floor | 1.0 | 0.015, 0.024, 0.045 | +0.006, -0.011, +0.044 | +0.006 |
