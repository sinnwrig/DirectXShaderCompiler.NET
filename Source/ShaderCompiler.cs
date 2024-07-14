using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DirectXShaderCompiler.NET;

/// <summary>
/// A delegate for shader header inclusion.
/// </summary>
/// <param name="includeName">The name of the header file being included.</param>
/// <returns>Contents of the included header file.</returns>
public delegate string FileIncludeHandler(string includeName);

/// <summary>
/// A static class that allows accessing shader compilation functionality found in the DirectXShaderCompiler. 
/// </summary>
public static class ShaderCompiler
{
    private static void Main() { }

    private static unsafe NativeDxcCompiler* nativeCompiler = null;

    static ShaderCompiler()
    {
        unsafe
        {
            nativeCompiler = DXCNative.DxcInitialize(); 
        }
    }

    private static unsafe NativeDxcIncludeResult* IncludeFunction(IntPtr nativeContext, byte* headerNameUtf8)
    {
        if (nativeContext == IntPtr.Zero)
            return null;

        string? headerName = Marshal.PtrToStringUTF8((IntPtr)headerNameUtf8);

        if (headerName != null)
        {
            FileIncludeHandler handler = Marshal.GetDelegateForFunctionPointer<FileIncludeHandler>(nativeContext);
            string includeFile = handler.Invoke(headerName);

            NativeDxcIncludeResult includeResult = new NativeDxcIncludeResult
            {
                header_data = NativeStringUtility.GetUTF8Ptr(includeFile, out uint len, false),
                header_length = len
            };

            return (NativeDxcIncludeResult*)AllocStruct(includeResult);
        }

        return null;
    }

    private static unsafe int FreeFunction(IntPtr nativeContext, NativeDxcIncludeResult* includeResult)
    {
        if (includeResult == null)
            return 0;

        NativeDxcIncludeResult result = Marshal.PtrToStructure<NativeDxcIncludeResult>((IntPtr)includeResult);

        Marshal.FreeHGlobal((IntPtr)result.header_data);
        Marshal.FreeHGlobal((IntPtr)includeResult);

        return 0;
    }

    private static unsafe IntPtr AllocStruct<T>(T type) where T : struct
    {
        IntPtr memPtr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
        Marshal.StructureToPtr(type, memPtr, false);
        return memPtr;
    }

    /// <summary>
    /// Compiles a string of shader code.
    /// </summary>
    /// <param name="code">The code to compile.</param>
    /// <param name="compilationOptions">The options to compile with.</param>
    /// <param name="includeHandler">The include handler to use for header inclusion.</param>
    /// <returns>A CompilationResult structure containing the resulting bytecode and possible errors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CompilationResult Compile(string code, CompilerOptions compilationOptions, FileIncludeHandler? includeHandler = null)
    {
        return Compile(code, compilationOptions.GetArgumentsArray(), includeHandler);
    }

    /// <summary>
    /// Compiles a string of shader code.
    /// </summary>
    /// <param name="code">The code to compile.</param>
    /// <param name="compilerArgs">The arguments to compile the shader with.</param>
    /// <param name="includeHandler">The include handler to use for header inclusion.</param>
    /// <returns>A CompilationResult structure containing the resulting bytecode and possible errors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CompilationResult Compile(string code, string[] compilerArgs, FileIncludeHandler? includeHandler = null)
    {
        return Compile(NativeStringUtility.GetUTF8Bytes(code), compilerArgs.Select(x => NativeStringUtility.GetUTF8Bytes(x)).ToArray(), includeHandler);
    }

    /// <summary>
    /// Compiles a string of shader code.
    /// </summary>
    /// <param name="codeUtf8">The utf-8 encoded string to compile.</param>
    /// <param name="compilerArgs">The array of utf-8 encoded string arguments to compile the shader with.</param>
    /// <param name="includeHandler">The include handler to use for header inclusion.</param>
    /// <returns>A CompilationResult structure containing the resulting bytecode and possible errors.</returns>
    public unsafe static CompilationResult Compile(byte[] codeUtf8, byte[][] compilerArgs, FileIncludeHandler? includeHandler = null)
    {   
        GCHandle codePinned = GCHandle.Alloc(codeUtf8, GCHandleType.Pinned); // Pin code

        Span<GCHandle> argsPinned = stackalloc GCHandle[compilerArgs.Length];
        byte** argsUtf8 = stackalloc byte*[compilerArgs.Length];

        for (int i = 0; i < compilerArgs.Length; i++)
        {
            GCHandle pin = GCHandle.Alloc(compilerArgs[i], GCHandleType.Pinned); // Pin argument

            argsPinned[i] = pin;
            argsUtf8[i] = (byte*)pin.AddrOfPinnedObject();
        }

        NativeDxcIncludeCallbacks* callbacks = stackalloc NativeDxcIncludeCallbacks[1];

        if (includeHandler != null)
            callbacks->include_ctx = (void*)Marshal.GetFunctionPointerForDelegate(includeHandler);
        else
            callbacks->include_ctx = null;

        callbacks->include_func = (void*)Marshal.GetFunctionPointerForDelegate<NativeDxcIncludeFunction>(IncludeFunction);
        callbacks->free_func = (void*)Marshal.GetFunctionPointerForDelegate<NativeDxcFreeIncludeFunction>(FreeFunction);

        NativeDxcCompileOptions* options = stackalloc NativeDxcCompileOptions[1];

        options->code = (byte*)codePinned.AddrOfPinnedObject();
        options->code_len = (nuint)codeUtf8.Length;
        options->args = argsUtf8;
        options->args_len = (nuint)compilerArgs.Length;
        options->include_callbacks = callbacks;
    
        CompilationResult result = GetResult(DXCNative.DxcCompile(nativeCompiler, options));

        codePinned.Free();

        for (int i = 0; i < compilerArgs.Length; i++)
            argsPinned[i].Free(); // Free argument

        return result;
    }

    private static unsafe CompilationResult GetResult(NativeDxcCompileResult* resultPtr)
    {
        NativeDxcCompileError* errorPtr = DXCNative.DxcCompileResultGetError(resultPtr);

        byte[] objectBytes = Array.Empty<byte>();
        string? compilationErrors = null;

        if (errorPtr != null)
        {
            byte* errorStringPtr = DXCNative.DxcCompileErrorGetString(errorPtr);
            nuint errorStringLen = DXCNative.DxcCompileErrorGetStringLength(errorPtr);

            compilationErrors = Marshal.PtrToStringUTF8((IntPtr)errorStringPtr, (int)errorStringLen);
            DXCNative.DxcCompileErrorRelease(errorPtr);
        }
        else
        {
            NativeDxcCompileObject* objectPtr = DXCNative.DxcCompileResultGetObject(resultPtr);
            byte* objectBytesPtr = DXCNative.DxcCompileObjectGetBytes(objectPtr);
            nuint objectBytesLen = DXCNative.DxcCompileObjectGetBytesLength(objectPtr);

            objectBytes = new byte[(int)objectBytesLen];
            Marshal.Copy((IntPtr)objectBytesPtr, objectBytes, 0, (int)objectBytesLen);

            DXCNative.DxcCompileObjectRelease(objectPtr);
        }

        DXCNative.DxcCompileResultRelease(resultPtr);

        return new CompilationResult()
        {
            objectBytes = objectBytes,
            compilationErrors = compilationErrors
        };
    }
}