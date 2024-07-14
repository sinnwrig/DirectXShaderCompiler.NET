namespace DirectXShaderCompiler.NET;


/// <summary>
/// The result structure returned by <see cref="ShaderCompiler.Compile"/> 
/// </summary>
public struct CompilationResult
{
    /// <summary> The compiled binary returned by the operation. </summary>
    /// <remarks> This field will be <see cref="null"/> if an error was encountered. </remarks>
    public byte[] objectBytes;

    /// <summary> Errors returned by compilation. </summary>
    /// <remarks> If compilation was successful, this field will be null. </remarks>
    public string? compilationErrors;
}