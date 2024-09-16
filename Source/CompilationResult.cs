namespace DirectXShaderCompiler.NET;

/// <summary>
/// The severity of the message
/// </summary>
public enum MessageSeverity
{
    /// <summary>
    /// Info message (coupled after a warning/error).
    /// </summary>
    Info,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warning,

    /// <summary>
    /// Error message.
    /// </summary>
    Error
}

/// <summary>
/// Parsed compilation messages
/// </summary>
public struct CompilationMessage
{
    /// <summary>
    /// The name of the file the message belongs to. 
    /// </summary>
    public string filename;

    /// <summary>
    /// The line of the message.
    /// </summary>
    public int line;

    /// <summary>
    /// The column or character of the message.
    /// </summary>
    public int column;

    /// <summary>
    /// The severity of the message.
    /// </summary>
    public MessageSeverity severity;

    /// <summary>
    /// The content of the message.
    /// </summary>
    public string message;
}

/// <summary>
/// The result structure returned by <see cref="ShaderCompiler"/> compilation methods 
/// </summary>
public struct CompilationResult
{
    /// <summary> The compiled binary returned by the operation. </summary>
    /// <remarks> This field will be null if an error was encountered. </remarks>
    public byte[]? objectBytes;

    /// <summary>
    /// The messages returned by compilation.
    /// </summary>
    public CompilationMessage[] messages;
}