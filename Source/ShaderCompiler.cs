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
    private static readonly unsafe NativeDxcCompiler* nativeCompiler = DXCNative.DxcInitialize();

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
        string? compilationMessage = null;
        CompilationMessage[] messages = [];

        if (messagesPtr != null)
        {
            byte* messageStringPtr = DXCNative.DxcCompileErrorGetString(messagesPtr);
            nuint messageStringLen = DXCNative.DxcCompileErrorGetStringLength(messagesPtr);

            compilationMessage = Marshal.PtrToStringUTF8((IntPtr)messageStringPtr, (int)messageStringLen);
            DXCNative.DxcCompileErrorRelease(messagesPtr);

            messages = ParseMessages(compilationMessage);
        }

        // No error messages exist, only warnings - there are bytes for us to get at.
        if (!Array.Exists(messages, x => x.severity == CompilationMessage.MessageSeverity.Error))
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
            messages = messages
        };
    }


    // Regex might fail with certain message contents. However, it has not as of yet.
    [GeneratedRegex(@"^(?<filename>[\w\.]+):(?<line>\d+):(?<column>\d+):\s(?<messageType>\w+):\s(?<message>.*?)(?=\n[\w\.]+:\d+:\d+:|\Z)", RegexOptions.Singleline | RegexOptions.Multiline)]
    private static partial Regex MessageRegex();

    private static CompilationMessage[] ParseMessages(string fullMessage)
    {
        MatchCollection matches = MessageRegex().Matches(fullMessage);

        CompilationMessage[] messages = new CompilationMessage[matches.Count];

        for (int i = 0; i < messages.Length; i++)
        {
            Match match = matches[i];

            messages[i] = new CompilationMessage()
            {
                filename = match.Groups["filename"].Value,
                line = int.Parse(match.Groups["line"].Value),
                column = int.Parse(match.Groups["column"].Value),

                severity = match.Groups["messageType"].Value switch
                {
                    "note" => CompilationMessage.MessageSeverity.Info,
                    "warning" => CompilationMessage.MessageSeverity.Warning,
                    "error" => CompilationMessage.MessageSeverity.Error,
                    _ => throw new Exception("Unknown compilation error"),
                },

                message = match.Groups["message"].Value,
            };
        }

        return messages;
    }
}