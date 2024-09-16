﻿using System;
using System.Diagnostics;
using DirectXShaderCompiler.NET;

namespace Application;

public class Program
{
    struct FileIncluder
    {
        int includeCount;

        public string IncludeFile(string filename)
        {
            Console.WriteLine($"\tIncluded file count {includeCount}");

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


    public static void Main(string[] args)
    {
        CompilerOptions options = new CompilerOptions(ShaderType.Fragment.ToProfile(6, 0))
        {
            entryPoint = "pixel",
            generateAsSpirV = true,
        };

        Console.WriteLine("Compilation options:");
        Console.WriteLine('\t' + string.Join(" ", options.GetArgumentsArray()));

        int cont = 10;

        if (args.Length > 0)
            _ = int.TryParse(args[0], out cont);

        FileIncluder includer = new();

        CompilationResult result = ShaderCompiler.Compile(ShaderCode.HlslCode, options, includer.IncludeFile);

        foreach (var message in result.messages)
            Console.WriteLine($"{message.severity} compiling {message.filename}: (at line {message.line}, column {message.column}): {message.message}");

        if (result.objectBytes != null)
            Console.WriteLine($"Generated {result.objectBytes.Length} bytes of shader code");
    }
}