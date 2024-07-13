using System.Reflection;
using System.Runtime.InteropServices;

namespace DirectXShaderCompiler.NET;

[StructLayout(LayoutKind.Sequential)]
internal struct NativeDxcCompiler { }

[StructLayout(LayoutKind.Sequential)]
internal struct NativeDxcCompileResult { }

[StructLayout(LayoutKind.Sequential)]
internal struct NativeDxcCompileError { }

[StructLayout(LayoutKind.Sequential)]
internal struct NativeDxcCompileObject { }


[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeDxcIncludeResult
{
    internal IntPtr header_data;
    internal nuint header_length;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeDxcIncludeCallbacks {
    internal IntPtr include_ctx;
    internal IntPtr include_func;
    internal IntPtr free_func;
}


internal unsafe delegate IntPtr NativeDxcIncludeFunction(IntPtr context, byte* headerNameUtf8); 

internal unsafe delegate int NativeDxcFreeIncludeFunction(IntPtr context, IntPtr includeResult);


[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeDxcCompileOptions {
    // Required
    internal IntPtr code; // utf-8 string pointer
    internal nuint code_len;
    internal IntPtr* args; // Array of utf-8 string pointers
    internal nuint args_len;

    // Optional
    internal NativeDxcIncludeCallbacks* include_callbacks; // nullable
}


internal static class DXCNative
{
    const string LibName = "dxcompiler-shared";

    const string WinLib = LibName + ".dll";
    const string OSXLib = "lib" + LibName + ".dylib";
    const string LinuxLib = "lib" + LibName + ".so";

    private static string CreateLibPath(string platform) => Path.Combine("runtimes", platform, "native");

    private static readonly Dictionary<PlatformInfo, (string, string)> LibraryPathDict = new()
    {
        { new PlatformInfo(OSPlatform.Windows, Architecture.X64), (CreateLibPath("win-x64"), WinLib) },
        { new PlatformInfo(OSPlatform.Windows, Architecture.Arm64), (CreateLibPath("win-arm64"), WinLib) },

        { new PlatformInfo(OSPlatform.OSX, Architecture.X64), (CreateLibPath("osx-x64"), OSXLib) },
        { new PlatformInfo(OSPlatform.OSX, Architecture.Arm64), (CreateLibPath("osx-arm64"), OSXLib) },

        { new PlatformInfo(OSPlatform.Linux, Architecture.X64), (CreateLibPath("linux-x64"), LinuxLib) },
        { new PlatformInfo(OSPlatform.Linux, Architecture.Arm64), (CreateLibPath("linux-arm64"), LinuxLib) },
    };


    private static bool _assembliesResolved;
    private static string[]? additionalSearchPaths;


    internal static void ResolveAssemblies(string[]? additionalSearchPaths)
    {
        if (_assembliesResolved)
            return;

        DXCNative.additionalSearchPaths = additionalSearchPaths;

        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);

        _assembliesResolved = true;
    }

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != LibName)
            return IntPtr.Zero;

        PlatformInfo platform = PlatformInfo.GetCurrentPlatform();

        (string, string) libraryPath = LibraryPathDict[platform];

        string applicationPath = AppContext.BaseDirectory;
        string assemblyPath = Path.GetDirectoryName(assembly.Location) ?? applicationPath;

        List<string> searchPaths = new()
        {
            // Possible library locations in release build
            applicationPath, // App path
            assemblyPath,    // Assembly path

            // Possible library locations in debug build
            Path.Join(applicationPath, libraryPath.Item1), 
            Path.Join(assemblyPath, libraryPath.Item1),  
        };
        
        // Add other possible library paths
        if (additionalSearchPaths != null)
        {
            foreach (string path in additionalSearchPaths)
            {
                // Root path, no need to combine
                if (Path.IsPathRooted(path))
                {
                    searchPaths.Add(path);
                }
                else
                {
                    // Add possible application and assembly paths.
                    searchPaths.Add(Path.Join(applicationPath, path));
                    searchPaths.Add(Path.Join(assemblyPath, path));
                }
            }
        }

        string bestPath = "/"; 
        foreach (string path in searchPaths)
        {
            string filePath = Path.Join(path, libraryPath.Item2);

            if (File.Exists(filePath))
                bestPath = filePath;
        }

        return NativeLibrary.Load(bestPath, assembly, DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.ApplicationDirectory);
    }

    const CallingConvention cconv = CallingConvention.Cdecl;

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern NativeDxcCompiler* dxc_initialize();

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern void dxc_finalize(NativeDxcCompiler* compilerPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern NativeDxcCompileResult* dxc_compile(NativeDxcCompiler* compilerPtr, NativeDxcCompileOptions* compileOptions);


    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern NativeDxcCompileError* dxc_compile_result_get_error(NativeDxcCompileResult* resultPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern NativeDxcCompileObject* dxc_compile_result_get_object(NativeDxcCompileResult* resultPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern void dxc_compile_result_deinit(NativeDxcCompileResult* resultPtr);


    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern byte* dxc_compile_object_get_bytes(NativeDxcCompileObject* objectPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern nuint dxc_compile_object_get_bytes_length(NativeDxcCompileObject* objectPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern void dxc_compile_object_deinit(NativeDxcCompileObject* objectPtr);


    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern byte* dxc_compile_error_get_string(NativeDxcCompileError* errorPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern nuint dxc_compile_error_get_string_length(NativeDxcCompileError* errorPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern void dxc_compile_error_deinit(NativeDxcCompileError* errorPtr);
}