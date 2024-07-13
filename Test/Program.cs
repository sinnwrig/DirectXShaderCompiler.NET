﻿using DirectXShaderCompiler.NET;

namespace Application;

public class Program
{        
    public static string IncludeFile(string filename)
    {
        Console.WriteLine($"Including file: {filename}");

        return "#define INCLUDED_FUNC(x) x * 10 - sin(x)";
    }


    public static void Main(string[] args)
    {
        CompilerOptions options = new CompilerOptions(new ShaderProfile(ShaderType.Pixel, 6, 0))
        {
            entryPoint = "pixel",
            generateAsSpirV = true,
        };

        Console.WriteLine(string.Join(' ', options.GetArgumentsArray()));

        using ShaderCompiler compInstance = new ShaderCompiler();

        CompilationResult result = compInstance.Compile(ShaderCode.HlslCode, options, IncludeFile);

        if (result.compilationErrors != null)
        {
            Console.WriteLine("Errors compiling shader:");
            Console.WriteLine(result.compilationErrors);
            return;
        }

        Console.WriteLine($"Success! {result.objectBytes.Length} bytes generated.");

        Console.WriteLine("Completed all tasks");
    }
}