using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text;

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
public static partial class ShaderCompiler
{
    private static unsafe NativeDxcCompiler* nativeCompiler = null;

    /// <summary>
    /// Initializes the native compiler library immediately. 
    /// This method is implicitly called when using any of the compilation methods.
    /// </summary>
    public static unsafe void Initialize()
    {
        if (nativeCompiler != null)
            return;

        nativeCompiler = DXCNative.DxcInitialize();
    }

    private static readonly unsafe void* includePtr = (void*)Marshal.GetFunctionPointerForDelegate<NativeDxcIncludeFunction>(Include);
    private static unsafe NativeDxcIncludeResult* Include(IntPtr nativeContext, byte* headerUtf8)
    {
        if (nativeContext == IntPtr.Zero)
            return null;

        string? headerName = Marshal.PtrToStringUTF8((IntPtr)headerUtf8);

        if (headerName != null)
        {
            FileIncludeHandler handler = Marshal.GetDelegateForFunctionPointer<FileIncludeHandler>(nativeContext);
            string includeFile = handler.Invoke(headerName);

            NativeDxcIncludeResult includeResult = new NativeDxcIncludeResult
            {
                headerData = GetUTF8Ptr(includeFile, out uint len, false),
                headerLength = len
            };

            IntPtr nativeIncludeResult = Marshal.AllocHGlobal(sizeof(NativeDxcIncludeResult));
            Marshal.StructureToPtr(includeResult, nativeIncludeResult, false);

            return (NativeDxcIncludeResult*)nativeIncludeResult;
        }

        return null;
    }

    private static readonly unsafe void* freeIncludePtr = (void*)Marshal.GetFunctionPointerForDelegate<NativeDxcFreeIncludeFunction>(FreeInclude);
    private static unsafe int FreeInclude(IntPtr nativeContext, NativeDxcIncludeResult* includeResult)
    {
        if (includeResult == null)
            return 0;

        NativeDxcIncludeResult result = Marshal.PtrToStructure<NativeDxcIncludeResult>((IntPtr)includeResult);

        Marshal.FreeHGlobal((IntPtr)result.headerData);
        Marshal.FreeHGlobal((IntPtr)includeResult);

        return 0;
    }


    private static byte[] GetUTF8Bytes(string stringValue, bool nullTerminate = true)
    {
        if (nullTerminate)
            stringValue = stringValue[^1] == '\0' ? stringValue : stringValue + '\0';

        return Encoding.UTF8.GetBytes(stringValue);
    }


    private static unsafe byte* GetUTF8Ptr(string stringValue, out uint len, bool nullTerminate = true)
    {
        byte[] bytes = GetUTF8Bytes(stringValue, nullTerminate);

        len = (uint)bytes.Length;

        IntPtr nativePtr = Marshal.AllocHGlobal((int)len);
        Marshal.Copy(bytes, 0, nativePtr, (int)len);

        return (byte*)nativePtr;
    }

    /// <summary>
    /// Compiles a string of shader code.
    /// </summary>
    /// <param name="code">The code to compile.</param>
    /// <param name="compilationOptions">The options to compile with.</param>
    /// <param name="includeHandler">The include handler to use for header inclusion.</param>
    /// <returns>A CompilationResult structure containing the resulting bytecode and possible errors.</returns>
    public static CompilationResult Compile(string code, CompilerOptions compilationOptions, FileIncludeHandler? includeHandler = null)
    {
        return Compile(code, compilationOptions.GetArgumentsArray(), includeHandler);
    }

    /// <summary>
    /// Compiles a string of shader code.
    /// </summary>
    /// <param name="code">The code to compile.</param>
    /// <param name="compilerArgs">The array of string arguments to compile the shader with.</param>
    /// <param name="includeHandler">The include handler to use for header inclusion.</param>
    /// <returns>A CompilationResult structure containing the resulting bytecode and possible errors.</returns>
    public static CompilationResult Compile(string code, string[] compilerArgs, FileIncludeHandler? includeHandler = null)
    {
        byte[] codeUtf8 = GetUTF8Bytes(code, true);
        byte[][] argsUtf8 = new byte[compilerArgs.Length][];

        for (int i = 0; i < compilerArgs.Length; i++)
            argsUtf8[i] = GetUTF8Bytes(compilerArgs[i], true);

        return Compile(codeUtf8, argsUtf8, includeHandler);
    }

    /// <summary>
    /// Compiles a string of shader code.
    /// </summary>
    /// <param name="code">The UTF-8 encoded code to compile.</param>
    /// <param name="compilerArgs">
    /// <para>The array of UTF-8 encoded arguments to compile the shader with.</para>
    /// <para>These arguments must be provided in the format consumed by DXC, the spec of which can be found here: https://github.com/microsoft/DirectXShaderCompiler/wiki/Using-dxc.exe-and-dxcompiler.dll</para>
    /// </param>
    /// <param name="includeHandler">The include handler to use for header inclusion.</param>
    /// <returns>A CompilationResult structure containing the resulting bytecode and possible errors.</returns>
    public unsafe static CompilationResult Compile(byte[] code, byte[][] compilerArgs, FileIncludeHandler? includeHandler = null)
    {
        Initialize();

        // Ideally the pins would use the fixed() keyword, but nested arrays can only be pinned with GCHandles, so for consistency, we use those.
        GCHandle codePinned = GCHandle.Alloc(code, GCHandleType.Pinned);

        Span<GCHandle> argsHandles = stackalloc GCHandle[compilerArgs.Length];
        byte** argsUtf8 = stackalloc byte*[compilerArgs.Length];

        for (int i = 0; i < compilerArgs.Length; i++)
        {
            argsHandles[i] = GCHandle.Alloc(compilerArgs[i], GCHandleType.Pinned);
            argsUtf8[i] = (byte*)argsHandles[i].AddrOfPinnedObject();
        }

        NativeDxcIncludeCallbacks* callbacks = stackalloc NativeDxcIncludeCallbacks[1];

        if (includeHandler != null)
            callbacks->includeContext = (void*)Marshal.GetFunctionPointerForDelegate(includeHandler);
        else
            callbacks->includeContext = null;

        callbacks->includeFunction = includePtr;
        callbacks->freeIncludeFunction = freeIncludePtr;

        NativeDxcCompileOptions* options = stackalloc NativeDxcCompileOptions[1];

        options->code = (byte*)codePinned.AddrOfPinnedObject();
        options->codeLength = (nuint)code.Length;
        options->arguments = argsUtf8;
        options->argumentsLength = (nuint)compilerArgs.Length;
        options->includeCallbacks = callbacks;

        CompilationResult result = GetResult(DXCNative.DxcCompile(nativeCompiler, options));

        codePinned.Free();

        for (int i = 0; i < compilerArgs.Length; i++)
            argsHandles[i].Free();

        return result;
    }

    private static unsafe CompilationResult GetResult(NativeDxcCompileResult* resultPtr)
    {
        NativeDxcCompileError* messagesPtr = DXCNative.DxcCompileResultGetError(resultPtr);

        byte[]? objectBytes = null;
        CompilationMessage[] messages = [];

        if (messagesPtr != null)
        {
            byte* messageStringPtr = DXCNative.DxcCompileErrorGetString(messagesPtr);
            nuint messageStringLen = DXCNative.DxcCompileErrorGetStringLength(messagesPtr);

            string? compilationMessage = Marshal.PtrToStringUTF8((IntPtr)messageStringPtr, (int)messageStringLen);
            DXCNative.DxcCompileErrorRelease(messagesPtr);
            messages = ParseMessages(compilationMessage);
        }

        NativeDxcCompileObject* objectPtr = DXCNative.DxcCompileResultGetObject(resultPtr);

        if (objectPtr != null)
        {
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
            messages = messages
        };
    }



    private static bool IsStackTrace(string line, out string lineParsed, out CompilationFile file)
    {
        file = default;
        lineParsed = line;

        // Found message start
        if (line.StartsWith("hlsl.hlsl") || line.StartsWith("./"))
        {
            int lIndex = Math.Max(line.LastIndexOf(": error:"), Math.Max(line.LastIndexOf(": warning:"), line.LastIndexOf(": note:")));

            if (lIndex == -1)
                return false;

            lineParsed = line.Substring(lIndex + 2);

            int colIndex = line.LastIndexOf(':', lIndex - 1);
            int rowIndex = line.LastIndexOf(':', colIndex - 1);

            file.filename = line.Substring(0, rowIndex);

            file.line = int.Parse(line.AsSpan(rowIndex + 1, colIndex - (rowIndex + 1)));
            file.column = int.Parse(line.AsSpan(colIndex + 1, lIndex - (colIndex + 1)));

            return true;
        }

        const string includedFromText = "In file included from ";

        if (line.StartsWith(includedFromText))
        {
            lineParsed = "";

            file.filename = line.Substring(includedFromText.Length, line.Length - (includedFromText.Length + 3));

            file.line = 1;
            file.column = 1;

            return true;
        }

        return false;
    }


    private static bool IsMessageLine(string line, out string lineParsed, out MessageSeverity severity)
    {
        severity = MessageSeverity.Info;
        lineParsed = line;

        if (line.StartsWith("error:"))
        {
            severity = MessageSeverity.Error;
            lineParsed = line.Substring("error: ".Length);
            return true;
        }

        if (line.StartsWith("warning:"))
        {
            severity = MessageSeverity.Warning;
            lineParsed = line.Substring("warning: ".Length);
            return true;
        }

        if (line.StartsWith("note:"))
        {
            lineParsed = line.Substring("note: ".Length);
            return true;
        }

        return false;
    }


    private static CompilationMessage[] ParseMessages(string fullMessage)
    {
        string[] lines = fullMessage.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        List<CompilationMessage> messages = [];
        List<CompilationFile> stackTrace = [];

        foreach (string line in lines)
        {
            bool isTrace = false;
            if (IsStackTrace(line, out string lineParsed, out CompilationFile file))
            {
                stackTrace.Add(file);
                isTrace = true;
            }

            if (IsMessageLine(lineParsed, out string messageParsed, out MessageSeverity severity))
            {
                CompilationMessage message = new CompilationMessage();
                message.message = messageParsed;
                message.severity = severity;

                List<CompilationFile> trace = new List<CompilationFile>(stackTrace);
                trace.Reverse();
                message.stackTrace = trace;

                stackTrace.Clear();

                messages.Add(message);

                continue;
            }

            if (!isTrace && messages.Count > 0)
            {
                CompilationMessage last = messages[^1];
                last.message += "\n" + line;
                messages[^1] = last;
            }
        }

        return [.. messages];
    }
}