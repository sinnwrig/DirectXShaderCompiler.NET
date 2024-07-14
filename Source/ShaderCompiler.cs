using System.Runtime.CompilerServices;
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

    private static unsafe NativeDxcIncludeResult* IncludeFunction(IntPtr nativeContext, byte* headerNameUtf8)
    {
        DxcIncludeCallbackContext context = Marshal.PtrToStructure<DxcIncludeCallbackContext>(nativeContext);

        string? headerName = Marshal.PtrToStringUTF8((IntPtr)headerNameUtf8);

        if (context.includeHandler != null && headerName != null)
        {
            string includeFile = context.includeHandler.Invoke(headerName);

            NativeDxcIncludeResult includeResult = new NativeDxcIncludeResult
            {
                header_data = NativeStringUtility.GetUTF8Ptr(includeFile, out uint len, false),
                header_length = len
            };

            return AllocStruct(includeResult);
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

    private static unsafe T* AllocStruct<T>(T type) where T : unmanaged
    {
        IntPtr memPtr = Marshal.AllocHGlobal(sizeof(T));
        Marshal.StructureToPtr(type, memPtr, false);
        return (T*)memPtr;
    }

    public ShaderCompiler(string[]? searchPaths = null)
    {
        DXCNative.ResolveAssemblies(searchPaths);

        unsafe
        {
            if (nativeCompiler == null)
                nativeCompiler = DXCNative.DxcInitialize(); 
            else
                throw new Exception("Cannot have multiple compiler instances active simultaneously");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CompilationResult Compile(string code, CompilerOptions compilationOptions, FileIncludeHandler? includeHandler = null)
    {
        return Compile(code, compilationOptions.GetArgumentsArray(), includeHandler);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CompilationResult Compile(string code, string[] compilerArgs, FileIncludeHandler? includeHandler = null)
    {
        return Compile(NativeStringUtility.GetUTF8Bytes(code), compilerArgs.Select(x => NativeStringUtility.GetUTF8Bytes(x)).ToArray(), includeHandler);
    }

    public unsafe CompilationResult Compile(byte[] codeUtf8, byte[][] compilerArgs, FileIncludeHandler? includeHandler = null)
    {
        DxcIncludeCallbackContext context = new DxcIncludeCallbackContext()
        {
            includeHandler = includeHandler
        };
    
        GCHandle pinnedContext = GCHandle.Alloc(context, GCHandleType.Pinned); // Allocate context
        GCHandle codePinned = GCHandle.Alloc(codeUtf8, GCHandleType.Pinned); // Pin code

        Span<GCHandle> argsPinned = stackalloc GCHandle[compilerArgs.Length];
        byte** argsUtf8 = stackalloc byte*[compilerArgs.Length];

        for (int i = 0; i < compilerArgs.Length; i++)
        {
            GCHandle pin = GCHandle.Alloc(compilerArgs[i]); // Pin argument

            argsPinned[i] = pin;
            argsUtf8[i] = (byte*)pin.AddrOfPinnedObject();
        }

        NativeDxcIncludeCallbacks* callbacks = stackalloc NativeDxcIncludeCallbacks[1];

        callbacks->include_ctx = (void*)pinnedContext.AddrOfPinnedObject();
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

        pinnedContext.Free();

        return result;
    }

    static unsafe CompilationResult GetResult(NativeDxcCompileResult* resultPtr)
    {
        NativeDxcCompileError* errorPtr = DXCNative.DxcCompileResultGetError(resultPtr);

        byte[] objectBytes = Array.Empty<byte>();
        string? compilationErrors = null;

        if (errorPtr != null)
        {
            byte* errorStringPtr = DXCNative.DxcCompileErrorGetString(errorPtr);
            nuint errorStringLen = DXCNative.DxcCompileErrorGetStringLength(errorPtr);

            compilationErrors = Marshal.PtrToStringUTF8((IntPtr)errorStringPtr, (int)errorStringLen);
            DXCNative.DxcCompileErrorDeinit(errorPtr);
        }
        else
        {
            NativeDxcCompileObject* objectPtr = DXCNative.DxcCompileResultGetObject(resultPtr);
            byte* objectBytesPtr = DXCNative.DxcCompileObjectGetBytes(objectPtr);
            nuint objectBytesLen = DXCNative.DxcCompileObjectGetBytesLength(objectPtr);

            objectBytes = new byte[(int)objectBytesLen];
            Marshal.Copy((IntPtr)objectBytesPtr, objectBytes, 0, (int)objectBytesLen);

            DXCNative.DxcCompileObjectDeinit(objectPtr);
        }

        DXCNative.DxcCompileResultRelease(resultPtr);

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
                DXCNative.DxcFinalize(nativeCompiler);
                
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