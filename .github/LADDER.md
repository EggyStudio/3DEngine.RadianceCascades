# The ladder of merges

Measured on 2026-11-27 on the tree of 7770020 with the changes committed beside it, every merge the
passes take by name (`IMerge`) run over 10 scenes on the CPU at 2 configurations, as `dotnet test
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
together, over the 9 scenes, and the one standing above every other at every configuration is the
default. The last scene is held out of the standing and read apart, since no merge is tuned on it.

## Standing

| Configuration | Merge | Mean distance | Worst part | Leak | Steps a texel | Reads a texel | Most gain a round | Stands above | Held out, mean and worst |
|---|---|---|---|---|---|---|---|---|---|
| the module's | at the ray's end, `MergeAtRayEnd` | 0.096 | 0.329 | 0.00000 | 1.0 | 6.5 | 0.501 | by reach sight, about its own probe, by marched sight | 0.030 and 0.041 |
| the module's | by reach sight, `MergeByReachSight` | 0.103 | 0.467 | 0.00000 | 1.1 | 6.9 | 0.501 | none | 0.065 and 0.143 |
| the module's | about its own probe, `MergeAboutProbe` | 0.161 | 0.720 | 0.00000 | 0.0 | 7.6 | 0.501 | none | 0.073 and 0.170 |
| the module's | by marched sight, `MergeByMarchedSight` | 0.430 | 1.769 | 0.00000 | 1.0 | 6.6 | 0.475 | none | 0.270 and 0.412 |
| an engine's | at the ray's end, `MergeAtRayEnd` | 0.094 | 0.446 | 0.00000 | 0.6 | 14.4 | 0.501 | about its own probe | 0.032 and 0.040 |
| an engine's | by reach sight, `MergeByReachSight` | 0.095 | 0.367 | 0.00000 | 2.1 | 15.2 | 0.501 | none | 0.041 and 0.055 |
| an engine's | about its own probe, `MergeAboutProbe` | 0.216 | 0.655 | 0.00000 | 0.0 | 16.2 | 0.501 | none | 0.109 and 0.219 |
| an engine's | by marched sight, `MergeByMarchedSight` | 0.274 | 0.810 | 0.00000 | 1.3 | 12.8 | 0.497 | none | 0.154 and 0.262 |

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
| the module's | a room under a lintel | by reach sight, `MergeByReachSight` | by the opening 1.125, between 0.976, deepest 0.669 | by the opening 0.885, between 0.800, deepest 0.627 | 4 | 0.292 | 0.270 | 0.467 |  | 2.3 | 20.1 |
| the module's | a room under a lintel | by marched sight, `MergeByMarchedSight` | by the opening 0.334, between 0.187, deepest 0.149 | by the opening 0.284, between 0.192, deepest 0.170 | 5 | 0.278 | 1.559 | 1.769 |  | 1.6 | 17.9 |
| the module's | a room under a lintel | about its own probe, `MergeAboutProbe` | by the opening 0.912, between 1.219, deepest 1.111 | by the opening 0.770, between 1.045, deepest 1.041 | 5 | 0.279 | 0.115 | 0.262 |  | 0.0 | 22.5 |
| the module's | an open box under a sky | at the ray's end, `MergeAtRayEnd` | floor 1.029, walls 1.002 | floor 1.012, walls 1.002 | 6 | 0.405 | 0.007 | 0.011 |  | 1.2 | 6.1 |
| the module's | an open box under a sky | by reach sight, `MergeByReachSight` | floor 1.255, walls 1.048 | floor 1.114, walls 1.036 | 6 | 0.393 | 0.071 | 0.108 |  | 1.0 | 6.4 |
| the module's | an open box under a sky | by marched sight, `MergeByMarchedSight` | floor 0.271, walls 1.029 | floor 0.378, walls 0.841 | 5 | 0.327 | 0.573 | 0.972 |  | 1.1 | 6.0 |
| the module's | an open box under a sky | about its own probe, `MergeAboutProbe` | floor 1.396, walls 1.110 | floor 1.182, walls 1.074 | 6 | 0.381 | 0.120 | 0.168 |  | 0.0 | 7.1 |
| the module's | a long hall | at the ray's end, `MergeAtRayEnd` | by the opening 0.961, middle 0.993, far end 0.993 | by the opening 0.954, middle 0.988, far end 0.995 | 7 | 0.489 | 0.021 | 0.047 |  | 0.4 | 5.2 |
| the module's | a long hall | by reach sight, `MergeByReachSight` | by the opening 1.017, middle 0.994, far end 0.992 | by the opening 0.990, middle 0.993, far end 0.998 | 7 | 0.487 | 0.007 | 0.010 |  | 0.8 | 4.4 |
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
| the module's | a wall under a lamp | at the ray's end, `MergeAtRayEnd` | top left 0.990, top middle 1.429, top right 0.990, bottom left 0.850, bottom middle 0.840, bottom right 0.850 | top left 0.993, top middle 1.350, top right 0.997, bottom left 0.950, bottom middle 0.893, bottom right 0.956 | 6 | 0.489 | 0.086 | 0.300 |  | 0.8 | 4.8 |
| the module's | a wall under a lamp | by reach sight, `MergeByReachSight` | top left 1.091, top middle 1.336, top right 1.095, bottom left 0.571, bottom middle 0.884, bottom right 0.817 | top left 1.000, top middle 1.243, top right 1.020, bottom left 0.690, bottom middle 0.898, bottom right 0.871 | 6 | 0.485 | 0.142 | 0.371 |  | 0.9 | 4.7 |
| the module's | a wall under a lamp | by marched sight, `MergeByMarchedSight` | top left 1.079, top middle 1.294, top right 1.083, bottom left 0.569, bottom middle 0.884, bottom right 0.814 | top left 0.956, top middle 1.194, top right 0.975, bottom left 0.645, bottom middle 0.870, bottom right 0.823 | 6 | 0.458 | 0.170 | 0.438 |  | 0.9 | 4.9 |
| the module's | a wall under a lamp | about its own probe, `MergeAboutProbe` | top left 0.869, top middle 1.200, top right 0.871, bottom left 0.431, bottom middle 0.775, bottom right 0.579 | top left 0.816, top middle 1.113, top right 0.830, bottom left 0.574, bottom middle 0.793, bottom right 0.692 | 6 | 0.488 | 0.275 | 0.556 |  | 0.0 | 5.2 |
| the module's | held out, a room with a roof window and a pillar | at the ray's end, `MergeAtRayEnd` | floor 1.052, walls 1.026, ceiling 1.049, pillar 1.030 | floor 1.033, walls 1.021, ceiling 1.042, pillar 1.023 | 7 | 0.492 | 0.030 | 0.041 |  | 0.8 | 4.7 |
| the module's | held out, a room with a roof window and a pillar | by reach sight, `MergeByReachSight` | floor 1.310, walls 0.991, ceiling 1.060, pillar 1.017 | floor 1.154, walls 1.018, ceiling 1.070, pillar 1.032 | 7 | 0.490 | 0.065 | 0.143 |  | 0.9 | 4.6 |
| the module's | held out, a room with a roof window and a pillar | by marched sight, `MergeByMarchedSight` | floor 0.570, walls 0.799, ceiling 1.024, pillar 0.880 | floor 0.662, walls 0.742, ceiling 0.867, pillar 0.795 | 7 | 0.459 | 0.270 | 0.412 |  | 0.9 | 4.8 |
| the module's | held out, a room with a roof window and a pillar | about its own probe, `MergeAboutProbe` | floor 1.375, walls 1.064, ceiling 0.999, pillar 0.914 | floor 1.185, walls 1.060, ceiling 1.046, pillar 0.980 | 7 | 0.488 | 0.073 | 0.170 |  | 0.0 | 5.1 |
| an engine's | a room with a door | at the ray's end, `MergeAtRayEnd` | floor 1.005, walls 1.030, ceiling 1.006 | floor 1.009, walls 1.020, ceiling 1.009 | 7 | 0.494 | 0.013 | 0.020 |  | 0.4 | 15.2 |
| an engine's | a room with a door | by reach sight, `MergeByReachSight` | floor 0.990, walls 1.086, ceiling 1.009 | floor 1.015, walls 1.055, ceiling 1.011 | 7 | 0.493 | 0.026 | 0.053 |  | 2.1 | 15.2 |
| an engine's | a room with a door | by marched sight, `MergeByMarchedSight` | floor 0.879, walls 0.893, ceiling 0.936 | floor 0.894, walls 0.890, ceiling 0.907 | 7 | 0.489 | 0.109 | 0.116 |  | 1.3 | 12.4 |
| an engine's | a room with a door | about its own probe, `MergeAboutProbe` | floor 0.850, walls 0.942, ceiling 0.928 | floor 0.895, walls 0.942, ceiling 0.908 | 7 | 0.496 | 0.089 | 0.111 |  | 0.0 | 15.9 |
| an engine's | a closed box with one lamp | at the ray's end, `MergeAtRayEnd` | floor 0.629, walls 0.662 | floor 0.640, walls 0.672 | 7 | 0.501 | 0.422 | 0.446 |  | 0.6 | 11.0 |
| an engine's | a closed box with one lamp | by reach sight, `MergeByReachSight` | floor 0.707, walls 0.673 | floor 0.701, walls 0.693 | 7 | 0.501 | 0.361 | 0.367 |  | 2.0 | 13.4 |
| an engine's | a closed box with one lamp | by marched sight, `MergeByMarchedSight` | floor 0.707, walls 0.608 | floor 0.677, walls 0.627 | 7 | 0.493 | 0.428 | 0.467 |  | 1.3 | 11.2 |
| an engine's | a closed box with one lamp | about its own probe, `MergeAboutProbe` | floor 0.738, walls 0.667 | floor 0.716, walls 0.686 | 7 | 0.501 | 0.355 | 0.377 |  | 0.0 | 14.4 |
| an engine's | a lit room beside a dark one through a wall of one cell | at the ray's end, `MergeAtRayEnd` | lit floor 0.953, lit walls 0.941 | lit floor 0.955, lit walls 0.950 | 7 | 0.500 | 0.049 | 0.051 | 0.00000 | 0.4 | 14.2 |
| an engine's | a lit room beside a dark one through a wall of one cell | by reach sight, `MergeByReachSight` | lit floor 0.961, lit walls 0.941 | lit floor 0.962, lit walls 0.953 | 7 | 0.500 | 0.044 | 0.048 | 0.00000 | 1.9 | 14.4 |
| an engine's | a lit room beside a dark one through a wall of one cell | by marched sight, `MergeByMarchedSight` | lit floor 0.961, lit walls 0.941 | lit floor 0.956, lit walls 0.948 | 7 | 0.497 | 0.049 | 0.054 | 0.00000 | 1.2 | 11.7 |
| an engine's | a lit room beside a dark one through a wall of one cell | about its own probe, `MergeAboutProbe` | lit floor 0.962, lit walls 0.941 | lit floor 0.963, lit walls 0.953 | 7 | 0.500 | 0.043 | 0.048 | 0.00000 | 0.0 | 14.9 |
| an engine's | a room under a lintel | at the ray's end, `MergeAtRayEnd` | by the opening 0.892, between 0.870, deepest 0.981 | by the opening 0.887, between 0.914, deepest 1.021 | 5 | 0.287 | 0.077 | 0.120 |  | 0.9 | 11.6 |
| an engine's | a room under a lintel | by reach sight, `MergeByReachSight` | by the opening 0.897, between 0.954, deepest 0.966 | by the opening 0.879, between 0.960, deepest 0.937 | 5 | 0.288 | 0.079 | 0.129 |  | 2.0 | 12.9 |
| an engine's | a room under a lintel | by marched sight, `MergeByMarchedSight` | by the opening 0.676, between 0.312, deepest 0.362 | by the opening 0.660, between 0.445, deepest 0.531 | 5 | 0.277 | 0.619 | 0.810 |  | 1.3 | 11.2 |
| an engine's | a room under a lintel | about its own probe, `MergeAboutProbe` | by the opening 0.463, between 0.390, deepest 0.434 | by the opening 0.534, between 0.519, deepest 0.617 | 5 | 0.291 | 0.588 | 0.655 |  | 0.0 | 14.2 |
| an engine's | an open box under a sky | at the ray's end, `MergeAtRayEnd` | floor 0.990, walls 1.002 | floor 0.992, walls 0.999 | 6 | 0.407 | 0.005 | 0.008 |  | 0.7 | 13.4 |
| an engine's | an open box under a sky | by reach sight, `MergeByReachSight` | floor 1.054, walls 1.011 | floor 1.024, walls 1.007 | 6 | 0.403 | 0.016 | 0.024 |  | 2.2 | 15.1 |
| an engine's | an open box under a sky | by marched sight, `MergeByMarchedSight` | floor 0.786, walls 0.963 | floor 0.871, walls 0.949 | 6 | 0.412 | 0.095 | 0.138 |  | 1.4 | 13.1 |
| an engine's | an open box under a sky | about its own probe, `MergeAboutProbe` | floor 0.824, walls 0.968 | floor 0.910, walls 0.966 | 6 | 0.418 | 0.064 | 0.094 |  | 0.0 | 16.5 |
| an engine's | a long hall | at the ray's end, `MergeAtRayEnd` | by the opening 0.986, middle 0.980, far end 0.990 | by the opening 0.987, middle 0.979, far end 0.983 | 7 | 0.487 | 0.018 | 0.022 |  | 0.5 | 20.4 |
| an engine's | a long hall | by reach sight, `MergeByReachSight` | by the opening 0.974, middle 0.971, far end 0.988 | by the opening 0.975, middle 0.965, far end 0.978 | 7 | 0.488 | 0.028 | 0.036 |  | 2.5 | 20.0 |
| an engine's | a long hall | by marched sight, `MergeByMarchedSight` | by the opening 0.823, middle 0.936, far end 0.978 | by the opening 0.837, middle 0.926, far end 0.961 | 7 | 0.486 | 0.098 | 0.178 |  | 1.6 | 16.9 |
| an engine's | a long hall | about its own probe, `MergeAboutProbe` | by the opening 0.974, middle 0.972, far end 0.988 | by the opening 0.975, middle 0.963, far end 0.978 | 7 | 0.488 | 0.028 | 0.037 |  | 0.0 | 20.8 |
| an engine's | a vestibule off a sunlit court | at the ray's end, `MergeAtRayEnd` | wall at -z 0.981, wall at +z 0.981, beside the door 0.996, floor 0.966, ceiling 0.983 | wall at -z 0.995, wall at +z 0.995, beside the door 0.984, floor 0.977, ceiling 0.978 | 7 | 0.448 | 0.014 | 0.024 |  | 0.5 | 15.2 |
| an engine's | a vestibule off a sunlit court | by reach sight, `MergeByReachSight` | wall at -z 1.044, wall at +z 1.044, beside the door 1.059, floor 0.933, ceiling 0.999 | wall at -z 1.022, wall at +z 1.022, beside the door 1.027, floor 0.977, ceiling 0.983 | 7 | 0.445 | 0.022 | 0.027 |  | 2.1 | 15.3 |
| an engine's | a vestibule off a sunlit court | by marched sight, `MergeByMarchedSight` | wall at -z 0.546, wall at +z 0.546, beside the door 0.391, floor 0.535, ceiling 0.721 | wall at -z 0.574, wall at +z 0.575, beside the door 0.463, floor 0.589, ceiling 0.672 | 7 | 0.440 | 0.561 | 0.770 |  | 1.3 | 12.6 |
| an engine's | a vestibule off a sunlit court | about its own probe, `MergeAboutProbe` | wall at -z 0.742, wall at +z 0.742, beside the door 0.644, floor 0.634, ceiling 0.820 | wall at -z 0.761, wall at +z 0.762, beside the door 0.695, floor 0.718, ceiling 0.777 | 7 | 0.450 | 0.298 | 0.364 |  | 0.0 | 16.1 |
| an engine's | a court lit in a strip between arcades | at the ray's end, `MergeAtRayEnd` | low wall 1.175, high wall 1.240 | low wall 1.137, high wall 1.177 | 6 | 0.392 | 0.146 | 0.163 |  | 0.9 | 13.1 |
| an engine's | a court lit in a strip between arcades | by reach sight, `MergeByReachSight` | low wall 1.324, high wall 1.668 | low wall 1.240, high wall 1.440 | 6 | 0.382 | 0.290 | 0.365 |  | 2.2 | 15.4 |
| an engine's | a court lit in a strip between arcades | by marched sight, `MergeByMarchedSight` | low wall 0.871, high wall 1.681 | low wall 0.871, high wall 1.454 | 6 | 0.395 | 0.256 | 0.374 |  | 1.4 | 13.8 |
| an engine's | a court lit in a strip between arcades | about its own probe, `MergeAboutProbe` | low wall 0.899, high wall 1.780 | low wall 0.899, high wall 1.484 | 6 | 0.403 | 0.251 | 0.395 |  | 0.0 | 17.2 |
| an engine's | a wall under a lamp | at the ray's end, `MergeAtRayEnd` | top left 1.041, top middle 1.505, top right 1.041, bottom left 0.609, bottom middle 0.766, bottom right 0.609 | top left 1.024, top middle 1.410, top right 1.025, bottom left 0.800, bottom middle 0.841, bottom right 0.802 | 6 | 0.489 | 0.168 | 0.344 |  | 0.3 | 15.4 |
| an engine's | a wall under a lamp | by reach sight, `MergeByReachSight` | top left 1.096, top middle 1.507, top right 1.096, bottom left 0.844, bottom middle 0.795, bottom right 0.882 | top left 1.115, top middle 1.432, top right 1.120, bottom left 0.987, bottom middle 0.896, bottom right 1.017 | 6 | 0.486 | 0.120 | 0.359 |  | 2.1 | 15.5 |
| an engine's | a wall under a lamp | by marched sight, `MergeByMarchedSight` | top left 1.096, top middle 1.507, top right 1.096, bottom left 0.839, bottom middle 0.795, bottom right 0.877 | top left 1.097, top middle 1.425, top right 1.102, bottom left 0.957, bottom middle 0.885, bottom right 0.988 | 6 | 0.479 | 0.120 | 0.354 |  | 1.3 | 12.6 |
| an engine's | a wall under a lamp | about its own probe, `MergeAboutProbe` | top left 1.335, top middle 1.599, top right 1.335, bottom left 0.787, bottom middle 0.838, bottom right 0.825 | top left 1.293, top middle 1.515, top right 1.298, bottom left 0.974, bottom middle 0.942, bottom right 1.006 | 6 | 0.485 | 0.171 | 0.416 |  | 0.0 | 16.1 |
| an engine's | held out, a room with a roof window and a pillar | at the ray's end, `MergeAtRayEnd` | floor 1.041, walls 1.017, ceiling 1.012, pillar 1.048 | floor 1.034, walls 1.026, ceiling 1.030, pillar 1.041 | 7 | 0.491 | 0.032 | 0.040 |  | 0.3 | 15.5 |
| an engine's | held out, a room with a roof window and a pillar | by reach sight, `MergeByReachSight` | floor 1.013, walls 1.072, ceiling 1.004, pillar 1.045 | floor 1.029, walls 1.057, ceiling 1.036, pillar 1.048 | 7 | 0.491 | 0.041 | 0.055 |  | 2.1 | 15.4 |
| an engine's | held out, a room with a roof window and a pillar | by marched sight, `MergeByMarchedSight` | floor 0.619, walls 0.876, ceiling 1.002, pillar 0.886 | floor 0.769, walls 0.860, ceiling 0.933, pillar 0.875 | 7 | 0.487 | 0.154 | 0.262 |  | 1.3 | 12.7 |
| an engine's | held out, a room with a roof window and a pillar | about its own probe, `MergeAboutProbe` | floor 0.637, walls 0.928, ceiling 1.025, pillar 0.895 | floor 0.803, walls 0.921, ceiling 0.968, pillar 0.903 | 7 | 0.497 | 0.109 | 0.219 |  | 0.0 | 16.1 |

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
| a wall under a lamp | 3784 | 6 | 0.489 |
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
| an engine's | at the ray's end, `MergeAtRayEnd` | 1.000 | 1.523 | 1.765 | 1.875 | 1.924 | 1.946 | 1.956 |
| an engine's | by reach sight, `MergeByReachSight` | 1.000 | 1.483 | 1.709 | 1.812 | 1.857 | 1.877 | 1.886 |
| an engine's | by marched sight, `MergeByMarchedSight` | 1.000 | 1.549 | 1.818 | 1.939 | 1.992 | 2.016 | 2.026 |
| an engine's | about its own probe, `MergeAboutProbe` | 1.000 | 1.524 | 1.776 | 1.892 | 1.944 | 1.967 | 1.978 |

## On the split probes

The split store, `ProbeLayout.Split`, runs over each scene at each configuration on the field, the
light and the bounce the merges run on, at its default shape with 8,192 slots to its finest pool and
6 cascades on grids fixed in the world, the finest 0.25 apart and each above twice as far, in 78.6
MB. Each round a store anew sees the scene's points and the places of the bounce's cells, runs 64
frames with each hit taking its surface's own light and what the surface reflects of the light
gathered at its cell the round before, and gathers at the same points, the irradiance the dense
passes' gather keeps. Its parts are read as the merges' are, the dense grid's by the default merge,
at the ray's end, beside them, and the store's probes standing in each cascade at the last round's
last frame, the finest asked for at that frame and the rays a frame each of those takes, a ray a
seen point, at the mean.

| Configuration | Merge | Mean distance | Worst part | Leak | Most gain a round | Held out, mean and worst |
|---|---|---|---|---|---|---|
| the module's | the split store's, `mergeSplitCascade` | 0.127 | 0.430 | 0.00000 | 0.508 | 0.018 and 0.062 |
| the module's | at the ray's end, `MergeAtRayEnd` | 0.096 | 0.329 | 0.00000 | 0.501 | 0.030 and 0.041 |
| an engine's | the split store's, `mergeSplitCascade` | 0.125 | 0.421 | 0.00000 | 0.500 | 0.027 and 0.087 |
| an engine's | at the ray's end, `MergeAtRayEnd` | 0.094 | 0.446 | 0.00000 | 0.501 | 0.032 and 0.040 |

| Configuration | Scene | Each part's ratio after one gather | Each part's ratio at the steady state | Rounds | Gain a round | Mean distance | Worst part | Leak | The dense grid's mean and worst | Probes standing by cascade | The finest asked | Rays a probe |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| the module's | a room with a door | floor 0.981, walls 1.003, ceiling 0.997 | floor 0.985, walls 0.995, ceiling 0.983 | 7 | 0.495 | 0.012 | 0.017 |  | 0.032 and 0.048 | 4737, 1142, 264, 56, 12, 4 | 4737 | 1.1 |
| the module's | a closed box with one lamp | floor 0.668, walls 0.681 | floor 0.651, walls 0.666 | 7 | 0.508 | 0.418 | 0.430 |  | 0.167 and 0.213 | 5644, 1352, 296, 56, 8, 4 | 5644 | 1.1 |
| the module's | a lit room beside a dark one through a wall of one cell | lit floor 0.960, lit walls 0.950 | lit floor 0.961, lit walls 0.958 | 7 | 0.500 | 0.041 | 0.043 | 0.00000 | 0.052 and 0.061 | 6094, 1480, 328, 64, 8, 4 | 6094 | 1.1 |
| the module's | a room under a lintel | by the opening 1.389, between 1.688, deepest 1.459 | by the opening 1.308, between 1.524, deepest 1.395 | 4 | 0.300 | 0.341 | 0.421 |  | 0.295 and 0.329 | 4532, 2048, 512, 113, 28, 8 | 4532 | 1.0 |
| the module's | an open box under a sky | floor 0.999, walls 0.971 | floor 0.991, walls 0.980 | 6 | 0.415 | 0.014 | 0.020 |  | 0.007 and 0.011 | 4805, 1156, 260, 52, 8, 4 | 4805 | 1.0 |
| the module's | a long hall | by the opening 1.145, middle 1.067, far end 1.030 | by the opening 1.136, middle 1.083, far end 1.044 | 7 | 0.480 | 0.083 | 0.128 |  | 0.021 and 0.047 | 2257, 532, 152, 32, 4, 2 | 2257 | 1.1 |
| the module's | a vestibule off a sunlit court | wall at -z 1.180, wall at +z 1.124, beside the door 1.037, floor 1.233, ceiling 1.123 | wall at -z 1.163, wall at +z 1.141, beside the door 1.067, floor 1.175, ceiling 1.161 | 7 | 0.441 | 0.132 | 0.162 |  | 0.093 and 0.145 | 6047, 1538, 392, 104, 32, 8 | 6047 | 1.1 |
| the module's | a court lit in a strip between arcades | low wall 0.896, high wall 1.028 | low wall 0.919, high wall 1.019 | 6 | 0.418 | 0.052 | 0.084 |  | 0.107 and 0.125 | 8192, 2048, 488, 92, 16, 4 | 8192 | 1.1 |
| the module's | a wall under a lamp | top left 1.298, top middle 1.214, top right 1.165, bottom left 0.916, bottom middle 0.978, bottom right 0.961 | top left 1.238, top middle 1.178, top right 1.113, bottom left 1.001, bottom middle 0.989, bottom right 0.960 | 6 | 0.493 | 0.089 | 0.213 |  | 0.086 and 0.300 | 3994, 968, 208, 40, 6, 4 | 3994 | 1.0 |
| the module's | held out, a room with a roof window and a pillar | floor 0.990, walls 0.998, ceiling 0.978, pillar 1.111 | floor 0.999, walls 1.002, ceiling 0.994, pillar 1.064 | 7 | 0.491 | 0.018 | 0.062 |  | 0.030 and 0.041 | 3918, 931, 196, 36, 8, 4 | 3918 | 1.1 |
| an engine's | a room with a door | floor 0.981, walls 1.003, ceiling 0.995 | floor 0.984, walls 0.995, ceiling 0.983 | 7 | 0.493 | 0.013 | 0.018 |  | 0.013 and 0.020 | 4737, 1142, 264, 56, 12, 4 | 4737 | 1.1 |
| an engine's | a closed box with one lamp | floor 0.748, walls 0.680 | floor 0.701, walls 0.673 | 7 | 0.494 | 0.376 | 0.397 |  | 0.422 and 0.446 | 5644, 1352, 296, 56, 8, 4 | 5644 | 1.1 |
| an engine's | a lit room beside a dark one through a wall of one cell | lit floor 0.958, lit walls 0.953 | lit floor 0.960, lit walls 0.960 | 7 | 0.500 | 0.041 | 0.041 | 0.00000 | 0.049 and 0.051 | 6094, 1480, 328, 64, 8, 4 | 6094 | 1.1 |
| an engine's | a room under a lintel | by the opening 1.389, between 1.688, deepest 1.459 | by the opening 1.308, between 1.524, deepest 1.395 | 4 | 0.300 | 0.341 | 0.421 |  | 0.077 and 0.120 | 4532, 2048, 512, 113, 28, 8 | 4532 | 1.0 |
| an engine's | an open box under a sky | floor 0.999, walls 0.971 | floor 0.991, walls 0.980 | 6 | 0.415 | 0.015 | 0.020 |  | 0.005 and 0.008 | 4805, 1156, 260, 52, 8, 4 | 4805 | 1.0 |
| an engine's | a long hall | by the opening 1.152, middle 1.069, far end 1.030 | by the opening 1.139, middle 1.086, far end 1.044 | 7 | 0.477 | 0.085 | 0.131 |  | 0.018 and 0.022 | 2257, 532, 152, 32, 4, 2 | 2257 | 1.1 |
| an engine's | a vestibule off a sunlit court | wall at -z 1.182, wall at +z 1.125, beside the door 1.038, floor 1.233, ceiling 1.123 | wall at -z 1.163, wall at +z 1.143, beside the door 1.067, floor 1.176, ceiling 1.161 | 7 | 0.435 | 0.132 | 0.162 |  | 0.014 and 0.024 | 6047, 1538, 392, 104, 32, 8 | 6047 | 1.1 |
| an engine's | a court lit in a strip between arcades | low wall 0.897, high wall 0.998 | low wall 0.922, high wall 0.998 | 6 | 0.408 | 0.042 | 0.082 |  | 0.146 and 0.163 | 8192, 2048, 488, 92, 16, 4 | 8192 | 1.1 |
| an engine's | a wall under a lamp | top left 1.334, top middle 1.211, top right 1.178, bottom left 0.927, bottom middle 0.971, bottom right 0.967 | top left 1.264, top middle 1.175, top right 1.120, bottom left 1.007, bottom middle 0.984, bottom right 0.963 | 6 | 0.490 | 0.095 | 0.234 |  | 0.168 and 0.344 | 3994, 968, 208, 40, 6, 4 | 3994 | 1.0 |
| an engine's | held out, a room with a roof window and a pillar | floor 0.981, walls 0.990, ceiling 0.975, pillar 1.162 | floor 0.994, walls 0.996, ceiling 0.990, pillar 1.091 | 7 | 0.493 | 0.027 | 0.087 |  | 0.032 and 0.040 | 3918, 931, 196, 36, 8, 4 | 3918 | 1.1 |

## The corridor walked

A corridor along x, 2.5 wide and 3.5 high inside, its walls fixed, under a sun through a gap 2 wide
across its roof and a lamp in the roof beside it, its points on the floor and on one wall from -6 to
6. The eye walks along it in quarter steps, from -8 to 8 at an engine's configuration, the field's
cascades and the levels snapped to their spacing about it, and from -4 to 4 at the module's, its
levels snapped about it in the field's one cascade, which holds still. Each point is gathered from
the finest level about it within half its spacing past its edge probes. The swing is each point's
light at every eye over its mean over the walk, taken whole, and the step each step's change over
that mean, each the worst and the 99th part. At an engine's configuration by reach sight the walk is
read again with the levels and then the field held still about its middle. On the split probes the
store runs 256 frames at the first eye and 4 at each step after, its cascades on grids fixed in the
world, the points it sees the corridor's faces within 16 of the eye along it.

| Configuration | Merge | Held still | Worst swing | Swing at the 99th part | Worst step | Step at the 99th part |
|---|---|---|---|---|---|---|
| the module's | at the ray's end, `MergeAtRayEnd` | nothing | 3.141 | 2.938 | 3.702 | 0.503 |
| the module's | by reach sight, `MergeByReachSight` | nothing | 3.141 | 2.884 | 3.702 | 0.558 |
| the module's | by marched sight, `MergeByMarchedSight` | nothing | 3.637 | 3.299 | 4.304 | 0.656 |
| the module's | about its own probe, `MergeAboutProbe` | nothing | 3.141 | 2.635 | 3.702 | 0.620 |
| an engine's | at the ray's end, `MergeAtRayEnd` | nothing | 3.496 | 2.297 | 3.534 | 1.305 |
| an engine's | by reach sight, `MergeByReachSight` | nothing | 3.594 | 2.466 | 3.558 | 1.299 |
| an engine's | by marched sight, `MergeByMarchedSight` | nothing | 4.870 | 2.934 | 5.190 | 1.348 |
| an engine's | about its own probe, `MergeAboutProbe` | nothing | 3.653 | 2.628 | 3.617 | 1.377 |
| an engine's | by reach sight, `MergeByReachSight` | the levels | 0.631 | 0.246 | 0.692 | 0.084 |
| an engine's | by reach sight, `MergeByReachSight` | the field | 3.633 | 2.437 | 3.601 | 1.311 |
| the module's | the split store's, `mergeSplitCascade` | nothing | 0.032 | 0.016 | 0.007 | 0.004 |
| an engine's | the split store's, `mergeSplitCascade` | nothing | 0.058 | 0.044 | 0.076 | 0.005 |

## The cones by roughness

Measured on 2026-11-27 on the tree of 7770020 with the changes committed beside it, the cone a rough
surface reflects read from the cascades by `ConeAtProbes` (`IConeRead`) over 12 scenes, the ladder's
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
| the module's | 0.1 | 0.625 | 0.284 | 4.785 | 0.436 | 0.000 | +0.167 | 0.00000 | 166 |
| the module's | 0.2 | 0.493 | 0.200 | 4.092 | 0.294 | 0.004 | +0.140 | 0.00000 | 166 |
| the module's | 0.3 | 0.335 | 0.160 | 2.770 | 0.079 | 0.007 | +0.133 | 0.00000 | 166 |
| the module's | 0.5 | 0.212 | 0.094 | 1.899 | 0.000 | 0.011 | -0.017 | 0.00000 | 327 |
| the module's | 0.7 | 0.159 | 0.082 | 1.349 | 0.000 | 0.012 | -0.021 | 0.00000 | 327 |
| the module's | 1.0 | 0.144 | 0.059 | 1.036 | 0.000 | 0.017 | -0.008 | 0.00000 | 327 |
| an engine's | 0.1 | 0.635 | 0.220 | 7.885 | 0.436 | 0.000 | -0.372 | 0.02060 | 172 |
| an engine's | 0.2 | 0.608 | 0.283 | 7.199 | 0.294 | 0.004 | -0.400 | 0.02060 | 172 |
| an engine's | 0.3 | 0.567 | 0.273 | 5.820 | 0.079 | 0.007 | -0.406 | 0.02060 | 172 |
| an engine's | 0.5 | 0.246 | 0.149 | 1.328 | 0.000 | 0.011 | -0.018 | 0.00000 | 96 |
| an engine's | 0.7 | 0.198 | 0.090 | 1.189 | 0.000 | 0.012 | -0.022 | 0.00000 | 96 |
| an engine's | 1.0 | 0.191 | 0.075 | 1.393 | 0.000 | 0.017 | +0.001 | 0.00000 | 96 |

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
| a wall under a lamp | none | none |
| held out, a room with a roof window and a pillar | 1.0 | 0.7 |
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
| the module's | a wall under a lamp | 0.1 | 0.011, 0.632, 0.547, 0.000, 0.011, 0.059 | +0.011, +0.632, +0.533, 0.000, +0.011, +0.059 | +0.208 |
| the module's | a wall under a lamp | 0.2 | 0.011, 0.544, 0.466, 0.000, 0.011, 0.050 | +0.011, +0.544, +0.163, 0.000, +0.011, +0.050 | +0.130 |
| the module's | a wall under a lamp | 0.3 | 0.018, 0.321, 0.717, 0.017, 0.024, 0.022 | -0.006, +0.305, -0.590, -0.017, -0.024, +0.015 | -0.053 |
| the module's | a wall under a lamp | 0.5 | 0.038, 0.604, 0.755, 0.009, 0.029, 0.045 | +0.022, +0.604, -0.697, -0.009, -0.026, -0.020 | -0.021 |
| the module's | a wall under a lamp | 0.7 | 0.079, 0.856, 0.469, 0.025, 0.065, 0.075 | +0.035, +0.856, -0.202, -0.024, -0.045, -0.048 | +0.095 |
| the module's | a wall under a lamp | 1.0 | 0.166, 1.036, 0.322, 0.042, 0.176, 0.081 | +0.086, +1.036, +0.138, -0.003, +0.057, +0.027 | +0.224 |
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
| an engine's | a room with a door | 0.5 | 0.012, 0.105, 0.010 | 0.000, +0.017, +0.005 | +0.010 |
| an engine's | a room with a door | 0.7 | 0.026, 0.063, 0.016 | +0.005, +0.046, +0.012 | +0.028 |
| an engine's | a room with a door | 1.0 | 0.053, 0.049, 0.036 | -0.001, +0.041, +0.032 | +0.029 |
| an engine's | a closed box with one lamp | 0.1 | 0.182, 0.189 | +0.182, +0.189 | +0.187 |
| an engine's | a closed box with one lamp | 0.2 | 0.123, 0.135 | +0.108, +0.124 | +0.119 |
| an engine's | a closed box with one lamp | 0.3 | 0.163, 0.192 | -0.117, -0.044 | -0.066 |
| an engine's | a closed box with one lamp | 0.5 | 0.456, 0.447 | -0.160, +0.113 | +0.030 |
| an engine's | a closed box with one lamp | 0.7 | 0.514, 0.504 | -0.370, -0.153 | -0.219 |
| an engine's | a closed box with one lamp | 1.0 | 0.382, 0.511 | -0.304, -0.283 | -0.290 |
| an engine's | a lit room beside a dark one through a wall of one cell | 0.1 | 0.001, 0.053 | -0.001, -0.053 | -0.021 |
| an engine's | a lit room beside a dark one through a wall of one cell | 0.2 | 0.001, 0.053 | -0.001, -0.053 | -0.021 |
| an engine's | a lit room beside a dark one through a wall of one cell | 0.3 | 0.001, 0.053 | -0.001, -0.053 | -0.021 |
| an engine's | a lit room beside a dark one through a wall of one cell | 0.5 | 0.012, 0.050 | -0.012, -0.050 | -0.027 |
| an engine's | a lit room beside a dark one through a wall of one cell | 0.7 | 0.031, 0.046 | -0.031, -0.046 | -0.037 |
| an engine's | a lit room beside a dark one through a wall of one cell | 1.0 | 0.055, 0.032 | -0.055, -0.032 | -0.046 |
| an engine's | a room under a lintel | 0.1 | 7.885, 3.735, 0.436 | -7.885, -3.446, +0.112 | -4.221 |
| an engine's | a room under a lintel | 0.2 | 7.199, 3.058, 0.532 | -7.199, -2.784, -0.110 | -3.771 |
| an engine's | a room under a lintel | 0.3 | 5.820, 2.109, 0.591 | -5.820, -1.845, -0.192 | -2.922 |
| an engine's | a room under a lintel | 0.5 | 1.084, 0.446, 0.163 | -1.084, -0.438, -0.155 | -0.610 |
| an engine's | a room under a lintel | 0.7 | 0.396, 0.164, 0.062 | -0.396, -0.163, -0.060 | -0.224 |
| an engine's | a room under a lintel | 1.0 | 0.141, 0.084, 0.034 | -0.140, -0.075, -0.032 | -0.089 |
| an engine's | an open box under a sky | 0.1 | 0.395, 0.230 | +0.395, +0.230 | +0.280 |
| an engine's | an open box under a sky | 0.2 | 0.289, 0.206 | +0.289, +0.206 | +0.231 |
| an engine's | an open box under a sky | 0.3 | 0.184, 0.161 | +0.170, +0.141 | +0.150 |
| an engine's | an open box under a sky | 0.5 | 0.040, 0.061 | -0.001, +0.060 | +0.042 |
| an engine's | an open box under a sky | 0.7 | 0.026, 0.027 | -0.002, +0.021 | +0.014 |
| an engine's | an open box under a sky | 1.0 | 0.015, 0.022 | +0.003, -0.002 | 0.000 |
| an engine's | a long hall | 0.1 | 1.282, 0.638, 0.638 | +0.243, -0.638, -0.638 | -0.324 |
| an engine's | a long hall | 0.2 | 1.364, 0.642, 0.640 | +0.171, -0.642, -0.640 | -0.351 |
| an engine's | a long hall | 0.3 | 1.483, 0.661, 0.648 | +0.084, -0.661, -0.648 | -0.390 |
| an engine's | a long hall | 0.5 | 0.162, 0.058, 0.027 | -0.023, -0.058, -0.027 | -0.034 |
| an engine's | a long hall | 0.7 | 0.144, 0.092, 0.049 | -0.083, -0.091, -0.049 | -0.073 |
| an engine's | a long hall | 1.0 | 0.118, 0.078, 0.049 | -0.110, -0.077, -0.049 | -0.079 |
| an engine's | a vestibule off a sunlit court | 0.1 | 4.106, 0.139, 0.346, 0.265, 0.031 | -3.872, +0.103, +0.346, +0.265, +0.031 | -0.503 |
| an engine's | a vestibule off a sunlit court | 0.2 | 3.410, 0.130, 0.181, 0.260, 0.027 | -3.306, +0.099, +0.166, +0.260, +0.027 | -0.430 |
| an engine's | a vestibule off a sunlit court | 0.3 | 2.667, 0.108, 0.135, 0.238, 0.009 | -2.616, +0.082, -0.041, +0.234, +0.006 | -0.352 |
| an engine's | a vestibule off a sunlit court | 0.5 | 0.656, 0.014, 0.166, 0.042, 0.036 | -0.504, +0.008, -0.097, +0.029, +0.034 | -0.073 |
| an engine's | a vestibule off a sunlit court | 0.7 | 0.245, 0.015, 0.084, 0.022, 0.036 | -0.189, +0.005, -0.020, +0.001, +0.035 | -0.022 |
| an engine's | a vestibule off a sunlit court | 1.0 | 0.112, 0.032, 0.048, 0.038, 0.087 | -0.102, +0.002, -0.007, -0.033, +0.086 | -0.002 |
| an engine's | a court lit in a strip between arcades | 0.1 | 0.314, 0.020 | +0.288, +0.020 | +0.154 |
| an engine's | a court lit in a strip between arcades | 0.2 | 0.625, 0.018 | -0.359, +0.018 | -0.171 |
| an engine's | a court lit in a strip between arcades | 0.3 | 1.041, 0.016 | -1.017, -0.002 | -0.510 |
| an engine's | a court lit in a strip between arcades | 0.5 | 0.794, 0.063 | +0.770, +0.059 | +0.414 |
| an engine's | a court lit in a strip between arcades | 0.7 | 0.494, 0.173 | +0.494, +0.172 | +0.333 |
| an engine's | a court lit in a strip between arcades | 1.0 | 0.529, 0.360 | +0.529, +0.360 | +0.444 |
| an engine's | a wall under a lamp | 0.1 | 0.014, 0.124, 0.187, 0.002, 0.035, 0.077 | +0.014, +0.124, +0.187, +0.002, +0.035, +0.077 | +0.073 |
| an engine's | a wall under a lamp | 0.2 | 0.014, 0.084, 0.309, 0.002, 0.035, 0.067 | +0.014, +0.036, -0.184, +0.002, +0.035, +0.067 | -0.005 |
| an engine's | a wall under a lamp | 0.3 | 0.012, 0.215, 0.936, 0.014, 0.021, 0.034 | -0.003, -0.203, -0.936, -0.014, 0.000, +0.033 | -0.187 |
| an engine's | a wall under a lamp | 0.5 | 0.048, 1.328, 0.991, 0.016, 0.033, 0.091 | +0.040, +1.328, -0.195, -0.015, -0.014, -0.085 | +0.176 |
| an engine's | a wall under a lamp | 0.7 | 0.094, 1.189, 0.605, 0.053, 0.082, 0.172 | +0.071, +1.189, -0.125, -0.052, -0.046, -0.154 | +0.147 |
| an engine's | a wall under a lamp | 1.0 | 0.228, 1.393, 0.443, 0.110, 0.182, 0.141 | +0.166, +1.393, +0.263, -0.092, +0.003, -0.062 | +0.279 |
| an engine's | held out, a room with a roof window and a pillar | 0.1 | 0.113, 0.142, 0.737, 0.006 | -0.045, -0.034, -0.584, -0.006 | -0.157 |
| an engine's | held out, a room with a roof window and a pillar | 0.2 | 0.226, 0.163, 0.620, 0.094 | -0.188, -0.062, -0.520, -0.094 | -0.193 |
| an engine's | held out, a room with a roof window and a pillar | 0.3 | 0.282, 0.206, 0.547, 0.327 | -0.264, -0.108, -0.490, -0.327 | -0.236 |
| an engine's | held out, a room with a roof window and a pillar | 0.5 | 0.124, 0.100, 0.097, 0.141 | +0.029, +0.023, +0.032, +0.006 | +0.026 |
| an engine's | held out, a room with a roof window and a pillar | 0.7 | 0.067, 0.079, 0.062, 0.134 | +0.039, +0.025, +0.034, -0.042 | +0.028 |
| an engine's | held out, a room with a roof window and a pillar | 1.0 | 0.045, 0.091, 0.053, 0.081 | +0.037, +0.059, +0.024, -0.012 | +0.044 |
| an engine's | a low room lit by a small bright light | 0.1 | 0.165, 0.007, 0.000 | -0.165, -0.007, 0.000 | -0.042 |
| an engine's | a low room lit by a small bright light | 0.2 | 1.166, 0.076, 0.000 | -1.166, -0.076, 0.000 | -0.313 |
| an engine's | a low room lit by a small bright light | 0.3 | 1.472, 0.192, 0.000 | -1.472, -0.192, 0.000 | -0.446 |
| an engine's | a low room lit by a small bright light | 0.5 | 0.929, 0.392, 0.000 | +0.084, -0.392, 0.000 | -0.189 |
| an engine's | a low room lit by a small bright light | 0.7 | 1.160, 0.526, 0.000 | +0.132, -0.526, 0.000 | -0.250 |
| an engine's | a low room lit by a small bright light | 1.0 | 0.988, 0.565, 0.000 | +0.036, -0.565, 0.000 | -0.293 |
| an engine's | a red wall beside a gray floor | 0.1 | 0.350, 0.594, 0.131 | -0.109, +0.349, -0.038 | +0.152 |
| an engine's | a red wall beside a gray floor | 0.2 | 0.252, 0.519, 0.099 | -0.054, +0.302, -0.033 | +0.141 |
| an engine's | a red wall beside a gray floor | 0.3 | 0.124, 0.412, 0.058 | -0.012, +0.237, -0.027 | +0.117 |
| an engine's | a red wall beside a gray floor | 0.5 | 0.047, 0.061, 0.034 | -0.018, +0.035, +0.027 | +0.020 |
| an engine's | a red wall beside a gray floor | 0.7 | 0.016, 0.022, 0.038 | +0.004, +0.005, +0.038 | +0.012 |
| an engine's | a red wall beside a gray floor | 1.0 | 0.015, 0.017, 0.045 | +0.006, -0.004, +0.044 | +0.009 |
