# DirectXShaderCompiler.NET: cross-platform C# wrapper for DirectXShaderCompiler 

A cross-platform .NET 8.0 wrapper for Microsoft's DirectX Shader Compiler, written in C#. 

[![NuGet](https://img.shields.io/nuget/v/DirectXShaderCompiler.NET.svg)](https://www.nuget.org/packages/DirectXShaderCompiler.NET)

# Usage

This project wraps functionality from DXC into a managed DirectXShaderCompiler class, which can be used to easily compile shader code with various options.
 
 
The following is a short example showcasing how a shader can be compiled using this wrapper, along with how source file inclusion can be overridden from C#.

```cs
using DirectXShaderCompiler.NET;

public class Program
{
    const string HlslCode = @"
#include ""IncludedFuncFile.hlsl""

struct PixelInput {
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
};

float4 pixel(VertexOutput input) : SV_Target { 
    input.Color *= 10;
    input.Color /= INCLUDED_FUNC(input.Color.r);
    return input.Color;
}
";

    public static string IncludeFile(string filename)
    {
        Console.WriteLine($"Including file: {filename}");

        // Instead of loading a file, add a placeholder preprocessor macro.
        return "#define INCLUDED_FUNC(x) x * 10 - sin(x)";
    }

    public static void Main(string[] args)
    {
        // Define the compilation options
        CompilerOptions options = new CompilerOptions(ShaderType.Fragment.ToProfile(6, 0))
        {
            entryPoint = "pixel", // The entry-point function. Entry point defaults to 'main' when not specified.
            generateAsSpirV = true, // Generate SPIR-V bytecode instead of DXIL.
        };

        // Log the options as the command-line arguments that DXC consumes.  
        Console.WriteLine(string.Join(' ', options.GetArgumentsArray()));

        // Compile the shader with our shader code, options, and a custom include handler.
        CompilationResult result = ShaderCompiler.Compile(HlslCode, options, IncludeFile);

        // Compilation errors exist, log them and exit.
        if (result.compilationErrors != null)
        {
            Console.WriteLine("Errors compiling shader:");
            Console.WriteLine(result.compilationErrors);
            return;
        }

        // Compilation succeded, log how many bytes were generated.
        Console.WriteLine($"Success! {result.objectBytes.Length} bytes generated.");
    }
}
```

# Native Details
 
Due to the nature of the official DirectXShaderCompiler repository's build system, it is difficult to easily acquire or build binaries of DXC that meets the following criteria:
 
- Exposes a cross-platform c-style interface for DXC.
- Runs on all major desktop operating systems.
- Can be compiled for all major desktop operating systems from any system.
 
As such, DirectXShaderCompiler.NET uses a fork of [DXC, built with zig](https://github.com/sinnwrig/DirectXShaderCompiler-zig) instead of CMake. As Zig's compiler supports cross-compilation out of the box, it allows DXC to build easily from most desktop platforms, for most platforms. The libraries produced by building this repository are what DirectXShaderCompiler.NET uses in its releases.
 
## Building Native Libraries
 
To build native libraries, run the `BuildNative.cs` file inside the _Native_ folder, specicying your target architecture [x64, arm64, all] with -A and your target platform [windows, linux, macos, all] with -P.
 
Native build requirements:
- Zig compiler version of at least 0.14.0
 
Pre-built binaries are bundled in the NuGet package for the following operating systems:
- Windows x64
- Windows arm64
- OSX x64
- OSX arm64
- Linux x64
- Linux arm64
