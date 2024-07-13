using System.Runtime.InteropServices;

namespace DirectXShaderCompiler.NET;

public delegate string FileIncludeHandler(string includeName); 

public class ShaderCompiler : IDisposable
{
    private static void Main() { }

    private static unsafe NativeDxcCompiler* nativeCompiler = null;


    [StructLayout(LayoutKind.Sequential)]
    internal struct DxcIncludeCallbackContext
    {
        internal FileIncludeHandler? includeHandler;
    } 

    private static unsafe IntPtr IncludeFunction(IntPtr nativeContext, byte* headerNameUtf8)
    {
        DxcIncludeCallbackContext context = Marshal.PtrToStructure<DxcIncludeCallbackContext>(nativeContext);

        string? headerName = Marshal.PtrToStringUTF8((IntPtr)headerNameUtf8);

        if (context.includeHandler != null && headerName != null)
        {
            string includeFile = context.includeHandler.Invoke(headerName);

            NativeDxcIncludeResult includeResult = new()
            {
                header_data = NativeStringUtility.GetUTF8Ptr(includeFile, out uint len, false),
                header_length = len
            };

            return AllocStruct(includeResult);
        }

        return IntPtr.Zero;
    }

    private static int FreeFunction(IntPtr nativeContext, IntPtr includeResult)
    {
        if (includeResult == IntPtr.Zero)
            return 0;

        NativeDxcIncludeResult result = Marshal.PtrToStructure<NativeDxcIncludeResult>(includeResult);

        Marshal.FreeHGlobal(result.header_data);
        Marshal.FreeHGlobal(includeResult);

        return 0;
    }

    private static IntPtr AllocStruct<T>(T structure) where T : struct
    {
        IntPtr memPtr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
        Marshal.StructureToPtr(structure, memPtr, false);
        return memPtr;
    }

    public ShaderCompiler(string[]? searchPaths = null)
    {
        DXCNative.ResolveAssemblies(searchPaths);

        unsafe
        {
            if (nativeCompiler == null)
                nativeCompiler = DXCNative.dxc_initialize(); 
            else
                throw new Exception("Cannot have multiple compiler instances active simultaneously");
        }
    }

    public CompilationResult Compile(string code, CompilerOptions compilationOptions, FileIncludeHandler? includeHandler = null)
    {
        return Compile(code, compilationOptions.GetArgumentsArray(), includeHandler);
    }

    public CompilationResult Compile(string code, string[] compilerArgs, FileIncludeHandler? includeHandler = null)
    {
        unsafe
        {
            DxcIncludeCallbackContext context = new DxcIncludeCallbackContext()
            {
                includeHandler = includeHandler
            };
    
            IntPtr contextPtr = AllocStruct(context); // Allocate context
            IntPtr codeUtf8 = NativeStringUtility.GetUTF8Ptr(code, out uint codeLen, true); // Allocate code

            IntPtr* argsUtf8 = stackalloc IntPtr[compilerArgs.Length];

            for (int i = 0; i < compilerArgs.Length; i++)
                argsUtf8[i] = NativeStringUtility.GetUTF8Ptr(compilerArgs[i], out _, true); // Allocate argument

            NativeDxcIncludeCallbacks* callbacks = stackalloc NativeDxcIncludeCallbacks[1];

            callbacks->include_ctx = contextPtr;
            callbacks->include_func = Marshal.GetFunctionPointerForDelegate<NativeDxcIncludeFunction>(IncludeFunction);
            callbacks->free_func = Marshal.GetFunctionPointerForDelegate<NativeDxcFreeIncludeFunction>(FreeFunction);

            NativeDxcCompileOptions* options = stackalloc NativeDxcCompileOptions[1];

            options->code = codeUtf8;
            options->code_len = codeLen;
            options->args = argsUtf8;
            options->args_len = (uint)compilerArgs.Length;
            options->include_callbacks = callbacks;
    
            CompilationResult result = GetResult(DXCNative.dxc_compile(nativeCompiler, options));

            Marshal.FreeHGlobal(codeUtf8); // Free code

            for (int i = 0; i < compilerArgs.Length; i++)
                Marshal.FreeHGlobal(argsUtf8[i]); // Free argument

            Marshal.FreeHGlobal(contextPtr); // Free context
    
            return result;
        }
    }

    static unsafe CompilationResult GetResult(NativeDxcCompileResult* resultPtr)
    {
        NativeDxcCompileError* errorPtr = DXCNative.dxc_compile_result_get_error(resultPtr);

        byte[] objectBytes = Array.Empty<byte>();
        string? compilationErrors = null;

        if (errorPtr != null)
        {
            byte* errorStringPtr = DXCNative.dxc_compile_error_get_string(errorPtr);
            nuint errorStringLen = DXCNative.dxc_compile_error_get_string_length(errorPtr);

            compilationErrors = Marshal.PtrToStringUTF8((IntPtr)errorStringPtr, (int)errorStringLen);
            DXCNative.dxc_compile_error_deinit(errorPtr);
        }
        else
        {
            NativeDxcCompileObject* objectPtr = DXCNative.dxc_compile_result_get_object(resultPtr);
            byte* objectBytesPtr = DXCNative.dxc_compile_object_get_bytes(objectPtr);
            nuint objectBytesLen = DXCNative.dxc_compile_object_get_bytes_length(objectPtr);

            objectBytes = new byte[(int)objectBytesLen];
            Marshal.Copy((IntPtr)objectBytesPtr, objectBytes, 0, (int)objectBytesLen);

            DXCNative.dxc_compile_object_deinit(objectPtr);
        }

        DXCNative.dxc_compile_result_deinit(resultPtr);

        return new CompilationResult()
        {
            objectBytes = objectBytes,
            compilationErrors = compilationErrors
        };
    }

    public void Dispose()
    {
        unsafe
        {
            if (nativeCompiler != null)
                DXCNative.dxc_finalize(nativeCompiler);
                
            nativeCompiler = null;
        }

        GC.SuppressFinalize(this);
    }

    ~ShaderCompiler()
    {
        ConsoleColor prev = Console.ForegroundColor;
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Warning: Native handle for {GetType().Name} was not properly deallocated. Ensure object is disposed by manually calling Dispose() or with a using statement.");
        Console.ForegroundColor = prev;

        Dispose();
    }
}