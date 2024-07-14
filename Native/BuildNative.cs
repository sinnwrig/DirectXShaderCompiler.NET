using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CommandLine;

static class Build
{
    struct PlatformAlias(OSPlatform platform, string commandLineName, string zigAlias, string executableExtension)
    {
        public OSPlatform platform = platform;
        public string commandLineName = commandLineName;
        public string zigAlias = zigAlias;
        public string executableExtension = executableExtension;

        public override readonly bool Equals([NotNullWhen(true)] object obj)
        {
            if (obj is not PlatformAlias alias)
                return false;

            return alias.platform == platform && alias.zigAlias == zigAlias && alias.executableExtension == executableExtension;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(platform, zigAlias, executableExtension);
        }
    }

    struct ArchAlias(Architecture architecture, string commandLineName, string zigAlias)
    {
        public Architecture architecture = architecture;
        public string commandLineName = commandLineName;
        public string zigAlias = zigAlias;

        public override readonly bool Equals([NotNullWhen(true)] object obj)
        {
            if (obj is not ArchAlias alias)
                return false;

            return alias.architecture == architecture && alias.zigAlias == zigAlias;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(architecture, zigAlias);
        }
    }

    static List<PlatformAlias> platformAliases = new()
    {
        new(OSPlatform.Windows, "windows", "windows-gnu", ".exe"),
        new(OSPlatform.Linux, "linux", "linux-gnu", ""),
        new(OSPlatform.OSX, "macos", "macos-none", ""),
    };

    static List<ArchAlias> architectureAliases = new()
    {
        new(Architecture.X64, "x64", "x86_64"),
        new(Architecture.Arm64, "arm64", "aarch64"),
    };

    static void Compile(
        string srcPath, 
        string outputPath,  
        ArchAlias architecture, 
        PlatformAlias platform, 
        bool isShared = true,
        bool skipExecutable = true,
        bool skipTests = true,
        bool clearCache = false, 
        bool debugSymbols = false, 
        string cpuSpecific = null)
    {
        string zigCacheDir = Path.Combine(srcPath, ".zig-cache");

        if (clearCache && Directory.Exists(zigCacheDir))
            Directory.Delete(zigCacheDir, true);

        if (!platformAliases.Contains(platform))
            throw new Exception($"Invalid platform '{platform}'. Must be one of the following: {string.Join(", ", platformAliases.Select(x => x.platform))}.");

        if (!architectureAliases.Contains(architecture))
            throw new Exception($"Invalid architecture '{architecture}'. Must be one of the following: {string.Join(", ", architectureAliases.Select(x => x.architecture))}.");

        Console.WriteLine($"Compiling for {architecture.zigAlias}-{platform.zigAlias}. Shared Library: {isShared}. Output directory: {outputPath}");

        string zigArgs = $"build -p \"{outputPath}\" -Dshared -Dspirv -Doptimize=ReleaseFast -Dtarget={architecture.zigAlias}-{platform.zigAlias}";

        if (debugSymbols)
            zigArgs += " -Ddebug";

        if (cpuSpecific != null)
            zigArgs += $" -Dcpu={cpuSpecific}";

        if (skipExecutable)
            zigArgs += " -Dskip_executables";
        
        if (skipTests)
            zigArgs += " -Dskip_tests";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "zig",
                Arguments = zigArgs,
                WorkingDirectory = srcPath,
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"Zig build failed with exit code {process.ExitCode}");
    }

    static bool EnsureZig()
    {
        try
        {
            var sysResult = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "zig",
                    Arguments = "version",
                    CreateNoWindow = true
                }
            };

            sysResult.Start();
            sysResult.WaitForExit();

            return true;
        }
        catch
        {
            return false;
        }
    }

    static OSPlatform GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return OSPlatform.Linux;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OSPlatform.OSX;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OSPlatform.Windows;

        throw new Exception("Could not determine operating system");
    }

    static void Main(string[] args)
    {
        if (!EnsureZig())
            throw new Exception("Failed to find zig installation on the system. Make sure zig is installed and matches the zig version expected by the source project.");

        string cwd = Directory.GetCurrentDirectory();

        var parserResult = Parser.Default.ParseArguments<Options>(args);

        parserResult.WithParsed(options =>
        {
            List<PlatformAlias> platforms = [ platformAliases.Find(x => x.platform == GetPlatform()) ];
            List<ArchAlias> architectures = [ architectureAliases.Find(x => x.architecture == RuntimeInformation.OSArchitecture )];

            var platformStr = options.Platform?.Select(x => x.ToLower()).ToArray();
            var architectureStr = options.Architecture?.Select(x => x.ToLower()).ToArray();

            if (platformStr?.Length != 0)
            {
                if (platformStr.Length == 1 && platformStr[0] == "all")
                {
                    platforms = platformAliases;
                }
                else
                {
                    platforms = options.Platform?.Select(x => {
                        var value = platformAliases.Find(y => y.commandLineName.Equals(x, StringComparison.OrdinalIgnoreCase));

                        if (value.Equals(default(PlatformAlias)))
                            throw new Exception($"Invalid platform: {x}");

                        return value;
                    }).ToList();
                }
            }

            if (architectureStr?.Length != 0)
            {
                if (architectureStr.Length == 1 && architectureStr[0] == "all")
                {
                    architectures = architectureAliases;
                }
                else
                {
                    architectures = options.Architecture?.Select(x => {
                        var value = architectureAliases.Find(y => y.commandLineName.Equals(x, StringComparison.OrdinalIgnoreCase));

                        if (value.Equals(default(ArchAlias)))
                            throw new Exception($"Invalid platform: {x}");

                        return value;
                    }).ToList();
                }
            }

            bool debugSymbols = options.Debug?.ToLower() == "on";

            Console.WriteLine("Compiling for platforms:");
            foreach (var pAlias in platforms)
            {
                foreach (var aAlias in architectures)
                {
                    Console.WriteLine($"\t {pAlias.commandLineName}-{aAlias.commandLineName}");
                }
            }

            foreach (var pAlias in platforms)
            {
                foreach (var aAlias in architectures)
                {
                    string pname = pAlias.zigAlias;
                    string aname = aAlias.zigAlias;

                    string outputPath = Path.Combine(cwd, "lib", $"{pAlias.commandLineName}-{aAlias.commandLineName}");

                    Compile(Path.Combine(cwd, "DirectXShaderCompiler-zig"), outputPath, aAlias, pAlias, isShared:true);
                }
            }
        });
    }


    class Options
    {
        [Option('P', "platform", Required = false, HelpText = "Target platform(s).")]
        public IEnumerable<string> Platform { get; set; }

        [Option('A', "architecture", Required = false, HelpText = "Target architecture(s).")]
        public IEnumerable<string> Architecture { get; set; }

        [Option('D', "debug", Required = false, HelpText = "Enable debug symbols.")]
        public string Debug { get; set; }
    }
}