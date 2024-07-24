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
            // Shows persistent property values even when being invoked from an (indirect) unmanaged context
            Console.WriteLine($"\tIncluded file count {includeCount}");
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
        CompilerOptions options = new CompilerOptions(ShaderProfile.Fragment_6_0)
        {
            entryPoint = "pixel",
            generateAsSpirV = true,
        };

        Console.WriteLine("Compilation options:");
        Console.WriteLine('\t' + string.Join(" ", options.GetArgumentsArray()));

        int cont = 10;

        if (args.Length > 0)
            _ = int.TryParse(args[0], out cont);

        Console.WriteLine($"\nCompiling {cont} shaders\n");
        Stopwatch watch = Stopwatch.StartNew();
        for (int i = 0; i < cont; i++)
        {
            FileIncluder finc = new();

            Console.WriteLine($"Compiling shader {i}");
            CompilationResult result = ShaderCompiler.Compile(ShaderCode.HlslCode, options, finc.IncludeFile);

            if (result.compilationErrors != null)
            {
                Console.WriteLine("Errors compiling shader:");
                Console.WriteLine(result.compilationErrors);
                return;
            } 
        }

        watch.Stop();

        Console.WriteLine($"\nCompiled {cont} shaders in {watch.Elapsed.TotalSeconds} seconds. Average time: {watch.Elapsed.TotalMilliseconds / cont} ms");
    }
}