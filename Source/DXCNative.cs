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
    internal byte* header_data;
    internal nuint header_length;
}

internal unsafe delegate NativeDxcIncludeResult* NativeDxcIncludeFunction(IntPtr context, byte* headerNameUtf8); 
internal unsafe delegate int NativeDxcFreeIncludeFunction(IntPtr context, NativeDxcIncludeResult* includeResult);


[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeDxcIncludeCallbacks {
    internal void* include_ctx;
    internal void* include_func;
    internal void* free_func;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeDxcCompileOptions {
    // Required
    internal byte* code; // utf-8 string pointer
    internal nuint code_len;
    internal byte** args; // Array of utf-8 string pointers
    internal nuint args_len;

    // Optional
    internal NativeDxcIncludeCallbacks* include_callbacks; // nullable
}


internal static class DXCNative
{
    const string LibName = "dxcompiler";

    /*

    internal struct PlatformInfo
    {
        internal OSPlatform platform;
        internal Architecture architecture;
    
        internal PlatformInfo(OSPlatform platform, Architecture architecture)
        {
            this.platform = platform;
            this.architecture = architecture;
        }
    
        internal static OSPlatform GetPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OSPlatform.OSX;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) 
                return OSPlatform.Linux;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
                return OSPlatform.Windows;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                return OSPlatform.FreeBSD;
            throw new Exception("Cannot determine operating system.");
        }
    
        internal static PlatformInfo GetCurrentPlatform()
        {
            return new PlatformInfo(GetPlatform(), RuntimeInformation.ProcessArchitecture);
        }
    
        public override readonly string ToString()
        {
            return $"({platform}, {architecture})";
        }
    }

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

        //NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);

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
    */

    const CallingConvention cconv = CallingConvention.Cdecl;


    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern NativeDxcCompiler* DxcInitialize();

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern void DxcFinalize(NativeDxcCompiler* compilerPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern NativeDxcCompileResult* DxcCompile(NativeDxcCompiler* compilerPtr, NativeDxcCompileOptions* compileOptions);


    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern NativeDxcCompileError* DxcCompileResultGetError(NativeDxcCompileResult* resultPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern NativeDxcCompileObject* DxcCompileResultGetObject(NativeDxcCompileResult* resultPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern void DxcCompileResultRelease(NativeDxcCompileResult* resultPtr);


    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern byte* DxcCompileObjectGetBytes(NativeDxcCompileObject* objectPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern nuint DxcCompileObjectGetBytesLength(NativeDxcCompileObject* objectPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern void DxcCompileObjectRelease(NativeDxcCompileObject* objectPtr);


    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern byte* DxcCompileErrorGetString(NativeDxcCompileError* errorPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern nuint DxcCompileErrorGetStringLength(NativeDxcCompileError* errorPtr);

    [DllImport(LibName, CallingConvention = cconv)]
    unsafe internal static extern void DxcCompileErrorRelease(NativeDxcCompileError* errorPtr);
}