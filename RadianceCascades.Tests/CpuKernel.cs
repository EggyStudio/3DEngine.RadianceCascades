using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// A kernel of the tests compiled by slangc to a shared library of the CPU, through the system's
/// C++ compiler, and dispatched there over its buffers, so what the library's Slang computes is
/// read with no GPU.
/// </summary>
/// <remarks>
/// <para>
/// slangc writes the compute entry <c>test</c> as a function of three pointers, the first and the
/// end group to run along each axis, the entry's own parameters, and the kernel's globals, which
/// hold each <c>RWStructuredBuffer</c> as a pointer to its data and a count of its elements, in
/// the order the kernel declares them. A kernel run here declares its buffers alone as globals,
/// each an array of the element type it reads, floats or uints.
/// </para>
/// <para>
/// Each kernel is compiled the first time a test runs it and kept loaded until the run ends, since
/// a test that runs one kernel's passes one after another would otherwise compile it for each. A
/// loaded library is entered by one run at a time, since nothing says the code slangc writes may be
/// entered by two threads at once, and a run that finds every loaded library of its kernel in use
/// loads one more from a copy of the compiled file under a name of its own, which each system loads
/// apart from the first, so that runs on several threads at once each enter a library of their own.
/// </para>
/// </remarks>
internal static class CpuKernel
{
    /// <summary>
    /// Why a kernel cannot run on the CPU here, no slangc or no C++ compiler it calls, or null
    /// where one can, found once by compiling a kernel of nothing.
    /// </summary>
    internal static string? Missing { get; } = Probe();

    private static string Extension =>
        OperatingSystem.IsWindows() ? ".dll" : OperatingSystem.IsMacOS() ? ".dylib" : ".so";

    private static readonly ConcurrentDictionary<string, Lazy<Compiled>> Kernels = new();

    // Each kernel's loaded libraries no run is in, and every copy loaded beside a first, which the
    // run's end frees with the first.
    private static readonly ConcurrentDictionary<string, ConcurrentBag<Compiled>> Idle = new();
    private static readonly ConcurrentBag<Compiled> Copies = new();
    private static int copied;

    // The folder the kernels are compiled into, and the libraries loaded from it, freed and the
    // folder deleted as the run ends.
    private static readonly Lazy<DirectoryInfo> Folder = new(() =>
    {
        var folder = Directory.CreateTempSubdirectory("radiancecascades-cpu-");
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            foreach (var kernel in Kernels.Values.Where(kernel => kernel.IsValueCreated))
                NativeLibrary.Free(kernel.Value.Library);
            foreach (var copy in Copies)
                NativeLibrary.Free(copy.Library);
            try { folder.Delete(recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        };
        return folder;
    });

    private readonly record struct Compiled(IntPtr Library, Entry Entry);

    /// <summary>
    /// Compiles <paramref name="kernel"/> of the tests' kernels for the CPU, the first time it is
    /// run, and runs its groups from the first to <paramref name="groups"/>, each of
    /// <paramref name="buffers"/> bound to its buffers in order and read back into in place.
    /// </summary>
    internal static void Run(string kernel, (uint X, uint Y, uint Z) groups, params Array[] buffers)
    {
        var first = Kernels.GetOrAdd(kernel, name => new Lazy<Compiled>(() => Compile(name))).Value;
        var idle = Idle.GetOrAdd(kernel, _ => new ConcurrentBag<Compiled>([first]));
        if (!idle.TryTake(out var compiled))
            compiled = Copy(kernel);
        var pinned = buffers.Select(buffer => GCHandle.Alloc(buffer, GCHandleType.Pinned)).ToArray();
        var globals = Marshal.AllocHGlobal(Math.Max(1, buffers.Length) * 2 * IntPtr.Size);
        try
        {
            for (int i = 0; i < buffers.Length; i++)
            {
                Marshal.WriteIntPtr(globals, 2 * i * IntPtr.Size, pinned[i].AddrOfPinnedObject());
                Marshal.WriteIntPtr(globals, (2 * i + 1) * IntPtr.Size, buffers[i].Length);
            }
            var range = new Groups { EndX = groups.X, EndY = groups.Y, EndZ = groups.Z };
            compiled.Entry(ref range, IntPtr.Zero, globals);
        }
        finally
        {
            idle.Add(compiled);
            Marshal.FreeHGlobal(globals);
            foreach (var handle in pinned)
                handle.Free();
        }
    }

    // A library of the compiled kernel loaded from a copy of its file, for a run that finds every
    // loaded one in use.
    private static Compiled Copy(string kernel)
    {
        var name = Path.GetFileNameWithoutExtension(kernel);
        var copy = Path.Combine(Folder.Value.FullName, $"{name}-{Interlocked.Increment(ref copied)}{Extension}");
        File.Copy(Path.Combine(Folder.Value.FullName, name + Extension), copy);
        var library = NativeLibrary.Load(copy);
        var loaded = new Compiled(library, Marshal.GetDelegateForFunctionPointer<Entry>(NativeLibrary.GetExport(library, "test")));
        Copies.Add(loaded);
        return loaded;
    }

    private static Compiled Compile(string kernel)
    {
        var path = Path.Combine(Folder.Value.FullName, Path.GetFileNameWithoutExtension(kernel) + Extension);
        // At slangc's own level the C++ compiler it hands a kernel to is given -Os by gcc's and
        // clang's rules and no level by Visual Studio's, whose cl then builds it unoptimized, which
        // made the tests several times slower on Windows than elsewhere. -O2 gives cl /O2 and the
        // others -O2, and the heaviest tests take under half their time at -Os and a sixth at -O0.
        var (code, said) = Slangc.Compile(Path.Combine(Repository.TestKernels, kernel), "shader-sharedlib", path, "-O2");
        Assert.True(code == 0, $"{kernel} for the CPU exited {code}:\n{said}");
        var library = NativeLibrary.Load(path);
        return new(library, Marshal.GetDelegateForFunctionPointer<Entry>(NativeLibrary.GetExport(library, "test")));
    }

    private static string? Probe()
    {
        if (Slangc.Path is null)
            return Slangc.Missing;
        var folder = Directory.CreateTempSubdirectory("radiancecascades-cpu-");
        try
        {
            var source = Path.Combine(folder.FullName, "nothing.slang");
            File.WriteAllText(source, "[shader(\"compute\")]\n[numthreads(1, 1, 1)]\nvoid test(uint3 id : SV_DispatchThreadID) {}\n");
            var (code, said) = Slangc.Compile(source, "shader-sharedlib", Path.Combine(folder.FullName, "nothing" + Extension));
            return code == 0 ? null : "needs a C++ compiler slangc calls to build a kernel for the CPU, where slangc said " + said.Split('\n')[0];
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Entry(ref Groups groups, IntPtr entryParameters, IntPtr globals);

    // The first group to run along each axis and the end, one past the last.
    [StructLayout(LayoutKind.Sequential)]
    private struct Groups
    {
        public uint StartX, StartY, StartZ, EndX, EndY, EndZ;
    }
}

/// <summary>
/// A test that runs a kernel on the CPU, skipped with its reason where <see cref="CpuKernel"/>
/// cannot build one.
/// </summary>
internal sealed class CpuKernelFactAttribute : FactAttribute
{
    /// <summary>
    /// Marks the test skipped where <see cref="CpuKernel.Missing"/> says why it cannot run.
    /// </summary>
    public CpuKernelFactAttribute()
    {
        if (CpuKernel.Missing is { } missing)
            Skip = missing;
    }
}

/// <summary>
/// A test over cases that runs a kernel on the CPU, skipped with its reason where
/// <see cref="CpuKernel"/> cannot build one.
/// </summary>
internal sealed class CpuKernelTheoryAttribute : TheoryAttribute
{
    /// <summary>
    /// Marks the test skipped where <see cref="CpuKernel.Missing"/> says why it cannot run.
    /// </summary>
    public CpuKernelTheoryAttribute()
    {
        if (CpuKernel.Missing is { } missing)
            Skip = missing;
    }
}
