using System.Runtime.InteropServices;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// A kernel of the tests compiled by slangc to a shared library of the CPU, through the system's
/// C++ compiler, and dispatched there over its buffers, so what the library's Slang computes is
/// read with no GPU.
/// </summary>
/// <remarks>
/// slangc writes the compute entry <c>test</c> as a function of three pointers, the first and the
/// end group to run along each axis, the entry's own parameters, and the kernel's globals, which
/// hold each <c>RWStructuredBuffer</c> as a pointer to its data and a count of its elements, in
/// the order the kernel declares them. A kernel run here declares its buffers alone as globals,
/// each an array of the element type it reads, floats or uints.
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

    /// <summary>
    /// Compiles <paramref name="kernel"/> of the tests' kernels for the CPU and runs its groups
    /// from the first to <paramref name="groups"/>, each of <paramref name="buffers"/> bound to its
    /// buffers in order and read back into in place.
    /// </summary>
    internal static void Run(string kernel, (uint X, uint Y, uint Z) groups, params Array[] buffers)
    {
        var folder = Directory.CreateTempSubdirectory("radiancecascades-cpu-");
        var pinned = buffers.Select(buffer => GCHandle.Alloc(buffer, GCHandleType.Pinned)).ToArray();
        var globals = Marshal.AllocHGlobal(Math.Max(1, buffers.Length) * 2 * IntPtr.Size);
        var library = IntPtr.Zero;
        try
        {
            var path = Path.Combine(folder.FullName, "kernel" + Extension);
            var (code, said) = Slangc.Compile(Path.Combine(Repository.TestKernels, kernel), "shader-sharedlib", path);
            Assert.True(code == 0, $"{kernel} for the CPU exited {code}:\n{said}");
            for (int i = 0; i < buffers.Length; i++)
            {
                Marshal.WriteIntPtr(globals, 2 * i * IntPtr.Size, pinned[i].AddrOfPinnedObject());
                Marshal.WriteIntPtr(globals, (2 * i + 1) * IntPtr.Size, buffers[i].Length);
            }
            library = NativeLibrary.Load(path);
            var entry = Marshal.GetDelegateForFunctionPointer<Entry>(NativeLibrary.GetExport(library, "test"));
            var range = new Groups { EndX = groups.X, EndY = groups.Y, EndZ = groups.Z };
            entry(ref range, IntPtr.Zero, globals);
        }
        finally
        {
            if (library != IntPtr.Zero)
                NativeLibrary.Free(library);
            Marshal.FreeHGlobal(globals);
            foreach (var handle in pinned)
                handle.Free();
            folder.Delete(recursive: true);
        }
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
