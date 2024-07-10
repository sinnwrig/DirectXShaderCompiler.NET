using System.Runtime.InteropServices;

namespace DirectXShaderCompiler.NET;


public delegate string FileIncludeHandler(string includeName); 

public class DirectXShaderCompiler : NativeResourceHandle
{
    private static bool isInitialized = false;

    internal delegate IntPtr DxcIncludeFunction(IntPtr context, IntPtr headerNameUtf8); 

    internal delegate int DxcFreeIncludeFunction(IntPtr context, IntPtr includeResult);


    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeDxcIncludeCallbacks 
    {
        internal IntPtr includeContext;
        internal IntPtr includeFunction;
        internal IntPtr freeFunction;
    }


    [StructLayout(LayoutKind.Sequential)]
    internal struct DxcIncludeCallbackContext
    {
        internal FileIncludeHandler? includeHandler;
    } 


    [StructLayout(LayoutKind.Sequential)]
    internal struct DxcIncludeResult
    {
        internal IntPtr headerData;
        internal nuint headerLength;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CompileOptions 
    {
        // Required
        internal IntPtr code;
        internal nuint codeLen;
        internal IntPtr args;
        internal nuint argsLen;

        // Optional
        internal IntPtr includeCallbacks; // nullable
    }

    private delegate IntPtr NativeIncludeHandler(IntPtr ctx, IntPtr headerUtf8);
    private static IntPtr IncludeFunction(IntPtr nativeContext, IntPtr headerNameUtf8)
    {
        DxcIncludeCallbackContext context = Marshal.PtrToStructure<DxcIncludeCallbackContext>(nativeContext);

        string? headerName = Marshal.PtrToStringUTF8(headerNameUtf8);

        if (context.includeHandler != null && headerName != null)
        {
            string includeFile = context.includeHandler.Invoke(headerName);

            DxcIncludeResult includeResult = new()
            {
                headerData = NativeStringUtility.GetUTF8Ptr(includeFile, out uint len, false),
                headerLength = len
            };

            return AllocStruct(includeResult);
        }

        return IntPtr.Zero;
    }

    private delegate int NativeFreeHandler(IntPtr ctx, IntPtr resultStructure);    
    private static int FreeFunction(IntPtr nativeContext, IntPtr includeResult)
    {
        if (includeResult == IntPtr.Zero)
            return 0;

        DxcIncludeResult result = Marshal.PtrToStructure<DxcIncludeResult>(includeResult);

        Marshal.FreeHGlobal(result.headerData);
        Marshal.FreeHGlobal(includeResult);

        return 0;
    }

    private static IntPtr AllocStruct<T>(T structure) where T : struct
    {
        IntPtr memPtr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
        Marshal.StructureToPtr(structure, memPtr, false);
        return memPtr;
    }

    private static IntPtr AllocArray(IntPtr[] arr, out uint byteSize)
    {
        byteSize = (uint)(arr.Length * IntPtr.Size);
        IntPtr allocation = Marshal.AllocHGlobal((int)byteSize);

        for (int i = 0, offset = 0; i < arr.Length; i++, offset += IntPtr.Size)
            Marshal.WriteIntPtr(allocation, offset, arr[i]);
        
        return allocation;
    }

    public DirectXShaderCompiler()
    {
        if (!isInitialized)
            isInitialized = true;
        else
            throw new Exception("Cannot have multiple compiler instances active simultaneously");

        DXCNative.ResolveAssemblies();

        handle = DXCNative.machDxcInit();
    }

    public CompilationResult Compile(string code, CompilerOptions compilationOptions, FileIncludeHandler? includeHandler = null)
    {
        return Compile(code, compilationOptions.GetArgumentsArray(), includeHandler);
    }

    public CompilationResult Compile(string code, string[] compilerArgs, FileIncludeHandler? includeHandler = null)
    {
        DxcIncludeCallbackContext context = new()
        {
            includeHandler = includeHandler
        };

        IntPtr contextPtr = AllocStruct(context); // Allocate context (1)

        NativeDxcIncludeCallbacks callbacks = new()
        {
            includeContext = contextPtr,
            includeFunction = Marshal.GetFunctionPointerForDelegate<NativeIncludeHandler>(IncludeFunction),
            freeFunction = Marshal.GetFunctionPointerForDelegate<NativeFreeHandler>(FreeFunction)
        };

        IntPtr callbacksPtr = AllocStruct(callbacks); // Allocate callbacks (2)

        IntPtr codeUtf8 = NativeStringUtility.GetUTF8Ptr(code, out uint codeUtf8Len, true); // Allocate code (3)

        IntPtr[] argsUtf8 = new IntPtr[compilerArgs.Length];

        for (int i = 0; i < argsUtf8.Length; i++)
            argsUtf8[i] = NativeStringUtility.GetUTF8Ptr(compilerArgs[i], out _, true); // Allocate arguments elements (4)

        IntPtr argsUtf8Ptr = AllocArray(argsUtf8, out _); // Allocate arguments array (5)

        CompileOptions options;
        options.code = codeUtf8;
        options.codeLen = codeUtf8Len;
        options.args = argsUtf8Ptr;
        options.argsLen = (uint)argsUtf8.Length;
        options.includeCallbacks = callbacksPtr;

        IntPtr optionsPtr = AllocStruct(options); // Allocate compilation options (6)

        CompilationResult result = GetResult(DXCNative.machDxcCompile(handle, optionsPtr));

        Marshal.FreeHGlobal(optionsPtr); // Free compilation options (6)
        Marshal.FreeHGlobal(argsUtf8Ptr); // Free arguments array (5)

        for (int i = 0; i < argsUtf8.Length; i++)
            Marshal.FreeHGlobal(argsUtf8[i]); // Free arguments elements (4)

        Marshal.FreeHGlobal(codeUtf8); // Free code (3)
        Marshal.FreeHGlobal(callbacksPtr); // Free callbacks (2)
        Marshal.FreeHGlobal(contextPtr); // Free context (1)

        return result;
    }

    static CompilationResult GetResult(IntPtr resultPtr)
    {
        IntPtr errorPtr = DXCNative.machDxcCompileResultGetError(resultPtr);

        byte[] objectBytes = Array.Empty<byte>();
        string? compilationErrors = null;

        if (errorPtr != IntPtr.Zero)
        {
            IntPtr errorStringPtr = DXCNative.machDxcCompileErrorGetString(errorPtr);
            nuint errorStringLen = DXCNative.machDxcCompileErrorGetStringLength(errorPtr);

            compilationErrors = Marshal.PtrToStringUTF8(errorStringPtr, (int)errorStringLen);
            DXCNative.machDxcCompileErrorDeinit(errorPtr);
        }
        else
        {
            IntPtr objectPtr = DXCNative.machDxcCompileResultGetObject(resultPtr);
            IntPtr objectBytesPtr = DXCNative.machDxcCompileObjectGetBytes(objectPtr);
            nuint objectBytesLen = DXCNative.machDxcCompileObjectGetBytesLength(objectPtr);

            objectBytes = new byte[(int)objectBytesLen];
            Marshal.Copy(objectBytesPtr, objectBytes, 0, (int)objectBytesLen);

            DXCNative.machDxcCompileObjectDeinit(objectPtr);
        }

        DXCNative.machDxcCompileResultDeinit(resultPtr);

        return new CompilationResult()
        {
            objectBytes = objectBytes,
            compilationErrors = compilationErrors
        };
    }

    protected override void ReleaseHandle()
    {
        isInitialized = false;
        DXCNative.machDxcDeinit(handle);
    }
}