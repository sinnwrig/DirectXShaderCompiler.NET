namespace DirectXShaderCompiler.NET;


/// <summary>
/// The result structure returned by <see cref="ShaderCompiler"/> compilation methods 
/// </summary>
public struct CompilationResult
{
    /// <summary> The compiled binary returned by the operation. </summary>
    /// <remarks> This field will be null if an error was encountered. </remarks>
    public byte[] objectBytes;

    /// <summary>
    /// The messages returned by compilation.
    /// </summary>
    public CompilationMessage[] messages;
}