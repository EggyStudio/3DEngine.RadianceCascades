using System.Buffers.Binary;
using System.Globalization;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace RadianceCascades.Tests;

/// <summary>
/// A binding's log of one probe's trace and merge on its device, run again through the library's
/// passes on the CPU with every call answered from the log, so the two part only where the passes'
/// calls or the light they keep part.
/// </summary>
public sealed class ParityReplayTests(ITestOutputHelper output)
{
    // The difference a float argument may lie from the logged one's, over the larger of 1 and its
    // size, a ten-thousandth of a cell of the field's finest cascade at most 1.25e-5 units, so a
    // way or a place the passes reach by another order of the same operations passes and a march
    // that takes another step does not.
    private const float Slack = 1e-4f;

    // The share a kept light may lie from the device's, of the larger of the two.
    private const float LightSlack = 1e-3f;

    // The calls by kind as the log names them.
    private static readonly string[] Kinds =
    [
        "", "hitLight", "throughAt", "cellAt", "secondAt", "between", "cascadeOf", "lightAt", "skyShareAt", "reachAt", "placeOf",
        "skyAlong", "shareAlong", "placeOf above",
    ];

    [CpuKernelFact]
    public void A_Logged_Probe_Replays_Through_The_Passes_On_The_CPU_Where_RC_PARITY_Names_Its_Folder()
    {
        // dotnet test RadianceCascades.Tests with RC_PARITY set to a folder of a binding's frames,
        // each a folder of header.json, texels.json and calls.bin, replays each frame's texels and
        // writes what parts. The test kernel answers each call from the log in its place.
        var folder = Environment.GetEnvironmentVariable("RC_PARITY");
        if (string.IsNullOrEmpty(folder))
            return;
        var parted = new List<string>();
        foreach (var frame in Directory.GetDirectories(folder).Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileName(frame);
            using var header = JsonDocument.Parse(File.ReadAllText(Path.Combine(frame, "header.json")));
            using var records = JsonDocument.Parse(File.ReadAllText(Path.Combine(frame, "texels.json")));
            var bytes = File.ReadAllBytes(Path.Combine(frame, "calls.bin"));
            var merge = header.RootElement.GetProperty("merge");
            var setup = new float[45];
            Cascade(merge.GetProperty("cascade"), setup, 0);
            Cascade(merge.GetProperty("farCascade"), setup, 11);
            setup[22] = merge.GetProperty("marchEnd").GetSingle();
            setup[23] = merge.GetProperty("size").GetSingle();
            var at = 24;
            foreach (var cascade in merge.GetProperty("cornerAndCell").EnumerateArray())
                foreach (var value in cascade.EnumerateArray())
                    setup[at++] = value.GetSingle();
            setup[44] = Slack;
            var traceSetup = (float[])setup.Clone();
            Cascade(header.RootElement.GetProperty("trace").GetProperty("cascadeAsTraced"), traceSetup, 0);

            var slots = header.RootElement.GetProperty("slots").EnumerateArray().ToArray();
            var texels = records.RootElement.EnumerateArray().ToArray();
            var (traced, merged, worstArgument, worstLight) = (0, 0, 0f, 0f);
            var (rayLight, mergedLight, marchedLight, marchedCount) = (0.0, 0.0, 0.0, 0);
            for (int slot = 0; slot < slots.Length; slot++)
            {
                var entry = slots[slot];
                var record = texels[slot];
                Assert.False(entry.GetProperty("overflow").GetBoolean(), $"{name} slot {slot} overflowed its log");
                var offset = entry.GetProperty("calls").GetInt32();
                var count = entry.GetProperty("logged").GetInt32();
                var values = new uint[count * 16];
                for (int w = 0; w < values.Length; w++)
                    values[w] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 4 * w, 4));
                var flags = record.GetProperty("flags").GetInt32();

                // The trace's calls, but the sky the pass reads after the trace returns, which the
                // passes do not call.
                if ((flags & 2) != 0)
                {
                    var calls = Calls(values, 0, kind => kind != 11);
                    var ray = new float[18];
                    var index = record.GetProperty("traceIndex").GetUInt32();
                    var state = Replay(traceSetup, calls, ray, 0, index);
                    var kept = Ray(record.GetProperty("traceKept"));
                    var apart = LightApart(ray.AsSpan(6, 6), kept);
                    (traced, worstArgument, worstLight) = (traced + 1, Math.Max(worstArgument, ray[17]), Math.Max(worstLight, apart));
                    var said = Parted(name, "trace", slot, state, calls, apart);
                    if (said is not null)
                        parted.Add(said);
                }
                if ((flags & 16) != 0)
                {
                    var calls = Calls(values, 1, _ => true);
                    var ray = new float[18];
                    Ray(record.GetProperty("mergeRayAt")).CopyTo(ray, 0);
                    var index = record.GetProperty("mergeIndex").GetUInt32();
                    var state = Replay(setup, calls, ray, 1, index);
                    var light = Floats(record.GetProperty("keepMerged"));
                    float[] device = [.. light, record.GetProperty("keepSkyShare").GetSingle(), record.GetProperty("marched").GetSingle()];
                    var apart = LightApart(ray.AsSpan(12, 5), device);
                    (merged, worstArgument, worstLight) = (merged + 1, Math.Max(worstArgument, ray[17]), Math.Max(worstLight, apart));
                    var said = Parted(name, "merge", slot, state, calls, apart);
                    if (said is not null)
                        parted.Add(said);
                    rayLight += ray[3] + ray[4] + ray[5];
                    mergedLight += light.Sum();
                    if (ray[16] > 0)
                        (marchedLight, marchedCount) = (marchedLight + light.Sum(), marchedCount + 1);
                }
            }
            output.WriteLine($"{name}: {traced} texels traced and {merged} merged on the CPU from the log, the arguments at most "
                + $"{Invariant(worstArgument, "0.###e0")} from the device's and the light kept at most {Invariant(worstLight, "0.###e0")} of "
                + $"it apart. The merge read rays of {Invariant(rayLight / Math.Max(merged, 1), "0.#####e0")} on the mean over its texels and "
                + $"kept {Invariant(mergedLight / Math.Max(merged, 1), "0.#####e0")}, {marchedCount} texels marched on keeping "
                + $"{Invariant(marchedLight / Math.Max(marchedCount, 1), "0.#####e0")} each, a sum of the three channels.");
        }
        foreach (var line in parted.Take(20))
            output.WriteLine(line);
        Assert.True(parted.Count == 0, $"{parted.Count} texels part from the device's, the first {string.Join("; ", parted.Take(3))}");
    }

    // Runs the replay of one texel's pass and gives the replay's state.
    private static uint[] Replay(float[] setup, uint[] calls, float[] ray, uint pass, uint index)
    {
        var state = new uint[9];
        (state[6], state[7], state[8]) = (pass, index, (uint)(calls.Length / 16));
        CpuKernel.Run("parity_replay_cpu.slang", (1, 1, 1), setup, calls.Length == 0 ? new uint[16] : calls, state, ray);
        return state;
    }

    // A texel's calls of one pass, the kinds keep lets through, 16 values each.
    private static uint[] Calls(uint[] values, uint pass, Func<uint, bool> keep) =>
        [.. Enumerable.Range(0, values.Length / 16).Where(c => values[c * 16] >> 8 == pass && keep(values[c * 16] & 255))
            .SelectMany(c => values.AsSpan(c * 16, 16).ToArray())];

    // What part of a texel's replay parts from the device's, or null where none does.
    private static string? Parted(string frame, string pass, int slot, uint[] state, uint[] calls, float apart)
    {
        if (state[1] == 0 && state[5] == 0 && state[0] == calls.Length / 16 && apart <= LightSlack)
            return null;
        var said = $"{frame} {pass} slot {slot}, {state[0]} calls made of {calls.Length / 16} logged, the light {Invariant(apart, "0.###e0")} apart";
        if (state[1] != 0)
        {
            var call = (int)state[1] - 1;
            var kind = (int)(calls[call * 16] & 255);
            said += state[2] == 0
                ? $", call {call} a {Kinds[Math.Min((int)state[3], Kinds.Length - 1)]} where the device made a {Kinds[Math.Min(kind, Kinds.Length - 1)]}"
                : $", call {call} ({Kinds[Math.Min(kind, Kinds.Length - 1)]}) value {state[2]} {Invariant(BitConverter.UInt32BitsToSingle(state[3]), "0.#########")} "
                    + $"(as an int {(int)state[3]}) where the device's is {Invariant(BitConverter.UInt32BitsToSingle(state[4]), "0.#########")} "
                    + $"(as an int {(int)state[4]})";
        }
        return said;
    }

    // The largest difference of the values from the device's, each over the larger of the two.
    private static float LightApart(ReadOnlySpan<float> mine, float[] device)
    {
        var worst = 0f;
        for (int i = 0; i < device.Length; i++)
        {
            var size = Math.Max(Math.Abs(mine[i]), Math.Abs(device[i]));
            if (size > 0)
                worst = Math.Max(worst, Math.Abs(mine[i] - device[i]) / size);
        }
        return worst;
    }

    private static void Cascade(JsonElement cascade, float[] setup, int at)
    {
        var corner = Floats(cascade.GetProperty("corner"));
        var count = Floats(cascade.GetProperty("count"));
        float[] values =
        [
            .. corner, cascade.GetProperty("spacing").GetSingle(), .. count, cascade.GetProperty("texels").GetSingle(),
            cascade.GetProperty("start").GetSingle(), cascade.GetProperty("end").GetSingle(), cascade.GetProperty("stepCap").GetSingle(),
        ];
        values.CopyTo(setup, at);
    }

    private static float[] Ray(JsonElement ray) =>
        [ray.GetProperty("ended").GetSingle(), ray.GetProperty("t").GetSingle(), ray.GetProperty("carried").GetSingle(), .. Floats(ray.GetProperty("light"))];

    private static float[] Floats(JsonElement values) => [.. values.EnumerateArray().Select(v => v.GetSingle())];

    private static string Invariant(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
}
