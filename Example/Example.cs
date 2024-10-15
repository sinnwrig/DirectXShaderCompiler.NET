﻿using System;
using DirectXShaderCompiler.NET;

namespace Application;

public class Example
{
    class FileIncluder
    {
        public int includeCount;

        public string IncludeFile(string filename)
        {
            // Can be used to track total includes, but falls apart when files have nested includes. 
            // Still useful to avoid recursive includes past certain counts.
            includeCount++;

            if (filename == "./FileA.hlsl")
                return "#include \"FileB.hlsl\"\n#include \"FileC.hlsl\"";

            if (filename == "./FileB.hlsl")
                return "#define INCLUDED_B_FUNC(x) x * 10 - sin(x)";

            if (filename == "./FileC.hlsl")
                return "#define INCLUDED_C_FUNC(x) x * 10 - sin(x)";

            return "// Nothing to see here";
        }
    }


    public static void Main()
    {
        // Certain combinations of options are known to break DXC or be invalid, but most of the important ones work.
        CompilerOptions options = new CompilerOptions(ShaderType.Fragment.ToProfile(6, 0))
        {
            entryPoint = "pixel",
            generateAsSpirV = true,
        };

        Console.WriteLine("Compilation options:");
        Console.WriteLine('\t' + string.Join(" ", options.GetArgumentsArray()));

        FileIncluder includer = new();

        CompilationResult result = ShaderCompiler.Compile(ShaderCode.HlslCode, options, includer.IncludeFile);

        Console.WriteLine($"\tIncluded ({includer.includeCount}) total files");

        foreach (var message in result.messages)
        {
            Console.WriteLine($"{message.severity}: {message.message}");

            foreach (var file in message.stackTrace)
            {
                Console.WriteLine($"\tAt {file.filename}:{file.line}:{file.column}");
            }
        }

        if (result.objectBytes != null)
            Console.WriteLine($"Generated {result.objectBytes.Length} bytes of shader code");
    }
}