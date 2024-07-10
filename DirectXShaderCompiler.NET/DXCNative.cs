using System.Reflection;
using System.Runtime.InteropServices;

namespace DirectXShaderCompiler.NET;


internal static class DXCNative
{
    const string LibName = "machdxcompiler";

    const string WinLib = LibName + ".dll";
    const string OSXLib = "lib" + LibName + ".dylib";
    const string LinuxLib = "lib" + LibName + ".so";


    private static readonly Dictionary<PlatformInfo, string> LibraryNames = new()
    {
        { new PlatformInfo(OSPlatform.Windows, Architecture.X64), WinLib },
        { new PlatformInfo(OSPlatform.Windows, Architecture.Arm64), WinLib },

        { new PlatformInfo(OSPlatform.OSX, Architecture.X64), OSXLib },
        { new PlatformInfo(OSPlatform.OSX, Architecture.Arm64), OSXLib },

        { new PlatformInfo(OSPlatform.Linux, Architecture.X64), LinuxLib },
        { new PlatformInfo(OSPlatform.Linux, Architecture.Arm64), LinuxLib },
    };


    private static bool _assembliesResolved = false;

    internal static void ResolveAssemblies()
    {
        if (_assembliesResolved)
            return;

        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);

        _assembliesResolved = true;
    }

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != LibName)
            return IntPtr.Zero;

        PlatformInfo platform = PlatformInfo.GetCurrentPlatform();

        string libraryPath = LibraryNames[platform];

        IntPtr library = NativeLibrary.Load(libraryPath, assembly, DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.ApplicationDirectory);

        if (library == IntPtr.Zero)
            throw new DllNotFoundException($"Could not find {libraryPath} shared library");

        return library;
    }

    const CallingConvention cconv = CallingConvention.Cdecl;


    [DllImport(LibName, CallingConvention = cconv)]
    internal static extern IntPtr machDxcInit();

    [DllImport(LibName, CallingConvention = cconv)]
    internal static extern void machDxcDeinit(IntPtr compiler);

    [DllImport(LibName, CallingConvention = cconv)]
    internal static extern IntPtr machDxcCompile(IntPtr compilerPtr, IntPtr compilerArgsPtr);


    [DllImport(LibName, CallingConvention = cconv)]
    internal static extern IntPtr machDxcCompileResultGetError(IntPtr resultPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    internal static extern IntPtr machDxcCompileResultGetObject(IntPtr resultPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    internal static extern void machDxcCompileResultDeinit(IntPtr resultPtr);


    [DllImport(LibName, CallingConvention = cconv)]
    internal static extern IntPtr machDxcCompileObjectGetBytes(IntPtr objectPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    internal static extern nuint machDxcCompileObjectGetBytesLength(IntPtr objectPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    internal static extern void machDxcCompileObjectDeinit(IntPtr objectPtr);


    [DllImport(LibName, CallingConvention = cconv)]
    internal static extern IntPtr machDxcCompileErrorGetString(IntPtr errorPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    internal static extern nuint machDxcCompileErrorGetStringLength(IntPtr errorPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    internal static extern void machDxcCompileErrorDeinit(IntPtr errorPtr);
}