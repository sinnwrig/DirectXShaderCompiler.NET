namespace DirectXShaderCompiler.NET;


public enum DenormalType { Any, Preserve, Ftz }

public enum LanguageVersion { _2016, _2017, _2018, _2021 }

public enum Linkage { Internal, External }

public enum FlowControlMode { Avoid, Prefer }

public enum DebugInfoType { Normal, Slim, Random }

public enum OptimizationLevel { O0, O1, O2, O3 }

public enum MatrixPackMode { ColumnMajor, RowMajor }


public partial class CompilerOptions
{   
    /// <summary> Enables agressive flattening </summary>
    [CompilerOption(name:"-all-resources-bound")]
    public bool allResourcesBound = false; 
    
    /// <summary> Set auto binding space - enables auto resource binding in libraries </summary>
    [CompilerOption(name:"-auto-binding-space")]
    public string? autoBindingSpace = null; 
    
    /// <summary> Output color coded assembly listings </summary>
    [CompilerOption(name:"-Cc")]
    public bool outputColorCodedListings = false; 
    
    /// <summary> Set default linkage for non-shader functions when compiling or linking to a library target (internal, external) </summary>
    [CompilerOption(name:"-default-linkage")]
    public Linkage? defaultLinkage = null; 

    /// <summary> Select denormal value options (any, preserve, ftz). any is the default. </summary>
    [CompilerOption(name:"-denorm")]
    public DenormalType? denormalValue = null; 

    /// <summary> Disables support for payload access qualifiers for raytracing payloads in SM 6.7. </summary>
    [CompilerOption(name:"-disable-payload-qualifiers")]
    public bool disablePayloadQualifiers = false; 
    
    private Dictionary<string, string> macros = new();

    /// <summary> Define a macro </summary>
    public void SetMacro(string name, string value) => macros[name] = value; 

    /// <summary> Remove a macro </summary>
    public void RemoveMacro(string name) => macros.Remove(name); 

    private void AddMacros(List<string> args)
    {
        foreach (var macro in macros)
        {
            args.Add("-D");
            args.Add($"{macro.Key}={macro.Value}");
        }
    }


    private HashSet<string> includePaths = new();

    /// <summary> Define an include path </summary>
    public void SetIncludePath(string path) => includePaths.Add(path); 

    /// <summary> Remove an include path </summary>
    public void RemoveIncludePath(string path) => includePaths.Remove(path); 

    private void AddIncludePaths(List<string> args)
    {
        foreach (var path in includePaths)
        {
            args.Add("-I");
            args.Add(path);
        }
    }

    
    /// <summary> Enable 16bit types and disable min precision types. Available in HLSL 2018 and shader model 6.2 </summary>
    [CompilerOption(name:"-enable-16bit-types")]
    public bool enable16BitTypes = false;
    
    /// <summary> Enable generation of lifetime markers </summary>
    [CompilerOption(name:"-enable-lifetime-markers")]
    public bool enableLifetimeMarkers = false; 

    /// <summary> Enables support for payload access qualifiers for raytracing payloads in SM 6.6. </summary>
    [CompilerOption(name:"-enable-payload-qualifiers")]
    public bool enablePayloadQualifiers = false; 

    /// <summary> Set default encoding for source inputs and text outputs (utf8|utf16(win)|utf32(*nix)|wide) default=utf8 </summary>
    /// <remarks> Interop implementation details require encoding to be utf-8 only. As such, this option cannot be changed. </remarks>
    [CompilerOption(name:"-encoding")]
    public readonly string? encoding = "utf8"; 

    /// <summary> Only export shaders when compiling a library </summary>
    [CompilerOption(name:"-export-shaders-only")]
    public bool exportShadersOnly = false; 

    // NOTE: Not too sure how this is supposed to work- when I figure it out I'll make some wrappers to make building an export list easier 
    /// <summary> Specify exports when compiling a library: export1[[,export1_clone,...]=internal_name][;...] </summary>
    [CompilerOption(name:"-exports")]
    public string? exports = null; 
    
    /// <summary> Entry point name- defaults to main </summary>
    [CompilerOption(name:"-E")]
    public string entryPoint = "main"; 
    
    /// <summary> Output assembly code listing file </summary>
    [CompilerOption(name:"-Fc")]
    public string? assemblyListingFile = null; 
    
    /// <summary> Print option name with mappable diagnostics </summary>
    [CompilerOption(name:"-fdiagnostics-show-option")]
    public bool diagnosticsShowOption = false; 

    /// <summary> Disable source location tracking in IR. This will break diagnostic generation for late validation. (Ignored if /Zi is passed) </summary>
    [CompilerOption(name:"-fdisable-loc-tracking")]
    public bool disableLocTracking = false; 
    
    /// <summary> Write debug information to the given file, or automatically named file in directory when ending in '\' </summary>
    [CompilerOption(name:"-Fd")]
    public string? debugOutputName = null; 

    /// <summary> Output warnings and errors to the given file </summary>
    [CompilerOption(name:"-Fe")]
    public string? errorOutputName = null;
    
    /// <summary> Output header file containing object code </summary>
    [CompilerOption(name:"-Fh")]
    public string? headerOutputName = null; 

    /// <summary> Set preprocess output file name (with /P) </summary>
    [CompilerOption(name:"-Fi")]
    public string? preprocessOutputName = null; 
    
    /// <summary> Expand the operands before performing token-pasting operation (fxc behavior) </summary>
    [CompilerOption(name:"-flegacy-macro-expansion")]
    public bool legacyExpandMacros = false; 

    /// <summary> Reserve unused explicit register assignments for compatibility with shader model 5.0 and below </summary>
    [CompilerOption(name:"-flegacy-resource-reservation")]
    public bool legacyResourceReservation = false; 

    /// <summary> Experimental option to use heuristics-driven late inlining and disable alwaysinline annotation for library shaders </summary>
    [CompilerOption(name:"-fnew-inlining-behavior")]
    public bool newInliningBehavior = false; 

    /// <summary> Do not print option name with mappable diagnostics </summary>
    [CompilerOption(name:"-fno-diagnostics-show-option")]
    public bool noDiagnosticsShowOption = false; 

    /// <summary> Force root signature version (rootsig_1_1 if omitted) </summary>
    [CompilerOption(name:"-force-rootsig-ver")]
    public string? rootsigVersion = null; 
    
    /// <summary> Output object file </summary>
    [CompilerOption(name:"-Fo")]
    public string? objectOutputName = null; 

    /// <summary> Output reflection to the given file </summary>
    [CompilerOption(name:"-Fre")]
    public string? reflectionOutputName = null; 

    /// <summary> Output root signature to the given file </summary>
    [CompilerOption(name:"-Frs")]
    public string? rootSignatureOutputName = null; 

    /// <summary> Output shader hash to the given file </summary>
    [CompilerOption(name:"-Fsh")]
    public string? hashOutputName = null; 

    /// <summary> Print time report </summary>
    [CompilerOption(name:"-ftime-report")]
    public bool timeReport = false;
    
    /// <summary> Print hierarchial time to file- stdout if no file is specified </summary>
    [CompilerOption(name:"-ftime-trace", Assignment = AssignmentType.Equals)]
    public string? timeTrace = null; 

    /// <summary> Enable backward compatibility mode </summary>
    [CompilerOption(name:"-Gec")]
    public bool backwardCompatibilityMode = false; 

    /// <summary> Enable strict mode </summary>
    [CompilerOption(name:"-Ges")]
    public bool strictMode = false; 

    [CompilerOption(name:"-Gfp", value:(int)FlowControlMode.Prefer)]
    [CompilerOption(name:"-Gfa", value:(int)FlowControlMode.Avoid)]
    public FlowControlMode? flowControlMode;

    /// <summary> Force IEEE strictness </summary>
    [CompilerOption(name:"-Gis")]
    public bool forceIEEEStrictness = false; 

    /// <summary> HLSL version (2016, 2017, 2018, 2021). Default is 2021 </summary>
    [CompilerOption(name:"-HV")]
    public LanguageVersion? languageVersion = null; 

    /// <summary> Show header includes and nesting depth </summary>
    [CompilerOption(name:"-H")]
    public bool showIncludesAndNestingDepth = false; 
    
    /// <summary> Ignore line directives </summary>
    [CompilerOption(name:"-ignore-line-directives")]
    public bool ignoreLineDirectives = false; 
    
    /// <summary> Output hexadecimal literals </summary>
    [CompilerOption(name:"-Lx")]
    public bool outputHexLiterals = false; 

    /// <summary> Output instruction numbers in assembly listings </summary>
    [CompilerOption(name:"-Ni")]
    public bool outputInstructionNumbers = false; 

    /// <summary> Do not use legacy cbuffer load </summary>
    [CompilerOption(name:"-no-legacy-cbuf-layout")]
    public bool noLegacyCbufferLoad = false; 

    /// <summary> Suppress warnings </summary>
    [CompilerOption(name:"-no-warnings")]
    public bool suppressWarnings = false; 

    /// <summary> Output instruction byte offsets in assembly listings </summary>
    [CompilerOption(name:"-No")]
    public bool outputByteOffsets = false; 

    /// <summary> Print the optimizer commands </summary>
    [CompilerOption(name:"-Odump")]
    public bool printOptimizerCommands = false; 
    
    /// <summary> Disable optimizations </summary>
    [CompilerOption(name:"-Od")]
    public bool disableOptimization = false; 

    /// <summary> Optimize signature packing assuming identical signature provided for each connecting stage </summary>
    [CompilerOption(name:"-pack-optimized")]
    public bool optimizeSignaturePacking = false; 
    
    /// <summary> (default) Pack signatures preserving prefix-stable property - appended elements will not disturb placement of prior elements </summary>
    [CompilerOption(name:"-pack-prefix-stable")]
    public bool packPrefixStable = false; 

    /// <summary> recompile from DXIL container with Debug Info or Debug Info bitcode file </summary>
    [CompilerOption(name:"-recompile")]
    public bool recompileFromDXIL = false; 

    /// <summary> Assume that UAVs/SRVs may alias </summary>
    [CompilerOption(name:"-res-may-alias")]
    public bool assumeResAliasing = false; 

    [CompilerOption(name:"-rootsig-define")]
    public string? rootSignatureDefine = null;

    /// <summary> Set target profile </summary>
    [CompilerOption(name:"-T")]
    public ShaderProfile profile; 

    /// <summary> Disable validation </summary>
    [CompilerOption(name:"-Vd")]
    public bool disableValidation = false; 

    /// <summary> Verify diagnostic output using comment directives </summary>
    [CompilerOption(name:"-verify")]
    public string? verificationDirectives = null; 

    /// <summary> Display details about the include process. </summary>
    [CompilerOption(name:"-Vi")]
    public bool displayIncludeDetails = false; 

    /// <summary> Use <name> as variable name in header file </summary>
    [CompilerOption(name:"-Vn")]
    public string? variableName = null; 
    
    /// <summary> Treat warnings as errors </summary>
    [CompilerOption(name:"-WX")]
    public bool warningsAsErrors = false; 

    /// <summary> Debug info type  </summary>
    [CompilerOption(name:"-Zs", value:(int)DebugInfoType.Slim)]
    [CompilerOption(name:"-Zi", value:(int)DebugInfoType.Normal)]
    public DebugInfoType? debugInfo = null; 

    /// <summary> Should matrices be packed in column-major or row-major order </summary>
    [CompilerOption(name:"-Zpr", value:(int)MatrixPackMode.RowMajor)]
    [CompilerOption(name:"-Zpc", value:(int)MatrixPackMode.ColumnMajor)]
    public MatrixPackMode? matrixPackMode = null; 
    
    /// <summary> Compute Shader Hash considering only output binary </summary>
    [CompilerOption(name:"-Zsb")]
    public bool computeBinaryHash = false; 
    
    /// <summary> Compute Shader Hash considering source information </summary>
    [CompilerOption(name:"-Zss")]
    public bool computeSourceHash = false; 

    /// <summary> Force finite math only in shader </summary>
    [CompilerOption(name:"-fno-finite-math-only", value:0)]
    [CompilerOption(name:"-ffinite-math-only", value:1)]
    public bool? finiteMathOnly = null; 

    /// <summary> Optimization level </summary>
    [CompilerOption(name:"-O1", value:(int)OptimizationLevel.O1)]
    [CompilerOption(name:"-O0", value:(int)OptimizationLevel.O0)]
    [CompilerOption(name:"-O2", value:(int)OptimizationLevel.O2)]
    [CompilerOption(name:"-O3", value:(int)OptimizationLevel.O3)]
    public OptimizationLevel? optimization = OptimizationLevel.O3; 

    /// <summary> Collect all global constants outside cbuffer declarations into cbuffer GlobalCB { ... }. Still experimental, not all dependency scenarios handled. </summary>
    [CompilerOption(name:"-decl-global-cb")]
    public bool createGlobalCBuffer = false; 

    /// <summary> Move uniform parameters from entry point to global scope </summary>
    [CompilerOption(name:"-extract-entry-uniforms")]
    public bool extractEntryUniforms = false; 
    
    /// <summary> Set extern on non-static globals </summary>
    [CompilerOption(name:"-global-extern-by-default")]
    public bool globalExternByDefault = false; 

    /// <summary> Write out user defines after rewritten HLSL </summary>
    [CompilerOption(name:"-keep-user-macro")]
    public bool keepUserMacro = false; 

    /// <summary> Add line directive </summary>
    [CompilerOption(name:"-line-directive")]
    public bool addLineDirective = false; 

    /// <summary> Remove unused functions and types </summary>
    [CompilerOption(name:"-remove-unused-functions")]
    public bool removeUnusedFunctions = false; 

    /// <summary> Remove unused static globals and functions </summary>
    [CompilerOption(name:"-remove-unused-globals")]
    public bool removeUnusedGlobals = false;
    
    /// <summary> Translate function definitions to declarations </summary>
    [CompilerOption(name:"-skip-fn-body")]
    public bool skipFunctionBody = false; 

    /// <summary> Remove static functions and globals when used with -skip-fn-body </summary>
    [CompilerOption(name:"-skip-static")]
    public bool skipStatic = false;
    
    /// <summary> Rewrite HLSL, without changes. </summary>
    [CompilerOption(name:"-unchanged")]
    public bool rewriteUnchanged = false; 

    /// <summary> Specify whitelist of debug info category (file -> source -> line, tool, vulkan-with-source) </summary>
    [CompilerOption(name:"-fspv-debug", Assignment = AssignmentType.Equals)]
    public string? debugWhitelist = null; 
    
    /// <summary> Specify the SPIR-V entry point name. Defaults to the HLSL entry point name. </summary>
    [CompilerOption(name:"-fspv-entrypoint-name", Assignment = AssignmentType.Equals)]
    public string? entrypointName = null; 
    
    /// <summary> Specify SPIR-V extension permitted to use </summary>
    [CompilerOption(name:"-fspv-extension", Assignment = AssignmentType.Equals)]
    public string? extension = null; 

    /// <summary> Flatten arrays of resources so each array element takes one binding number </summary>
    [CompilerOption(name:"-fspv-flatten-resource-arrays")]               
    public bool flattenResourceArrays = false; 

    /// <summary> Preserves all bindings declared within the module, even when those bindings are unused </summary>
    [CompilerOption(name:"-fspv-preserve-bindings")]                     
    public bool preserveBindings = false; 

    /// <summary> Preserves all interface variables in the entry point, even when those variables are unused </summary>
    [CompilerOption(name:"-fspv-preserve-interface")]                    
    public bool preserveInterface = false; 

    /// <summary> Print the SPIR-V module before each pass and after the last one. Useful for debugging SPIR-V legalization and optimization passes. </summary>
    [CompilerOption(name:"-fspv-print-all")]                             
    public bool printAll = false; 

    /// <summary> Replaces loads of composite objects to reduce memory pressure for the loads </summary>
    [CompilerOption(name:"-fspv-reduce-load-size")]                      
    public bool reduceLoadSize = false; 

    /// <summary> Emit additional SPIR-V instructions to aid reflection </summary>
    [CompilerOption(name:"-fspv-reflect")]                               
    public bool addReflectionAid = false; 

    /// <summary> Specify the target environment: vulkan1.0 (default), vulkan1.1, vulkan1.1spirv1.4, vulkan1.2, vulkan1.3, or universal1.5 </summary>
    [CompilerOption(name:"-fspv-target-env", Assignment = AssignmentType.Equals)]
    public string? targetEnvironment = null; 
    
    /// <summary> Assume the legacy matrix order (row major) when accessing raw buffers (e.g., ByteAdddressBuffer) </summary>
    [CompilerOption(name:"-fspv-use-legacy-buffer-matrix-order")]        
    public bool useLegacyBufferMatrixOrder = false; 

    /// <summary> Apply fvk-*-shift to resources without an explicit register assignment. </summary>
    [CompilerOption(name:"-fvk-auto-shift-bindings")]                    
    public bool autoShiftBindings = false; 

    /// <summary> Specify Vulkan binding number shift for b-type register: <shift> <space>  </summary>
    [CompilerOption(name:"-fvk-b-shift")]                                
    public string? bindingNumberShift = null; 

    /// <summary> Specify Vulkan binding number and set number for the $Globals cbuffer: <binding> <set> </summary>
    [CompilerOption(name:"-fvk-bind-globals")]                           
    public string? globalBindingNumber = null; 

    /// <summary> Specify Vulkan descriptor set and binding for a specific register: <type-number> <space> <binding> <set> </summary>
    [CompilerOption(name:"-fvk-bind-register")]                          
    public string? registerBindings = null; 

    /// <summary> Negate SV_Position.y before writing to stage output in VS/DS/GS to accommodate Vulkan's coordinate system </summary>
    [CompilerOption(name:"-fvk-invert-y")]                               
    public bool invertY = false; 

    /// <summary> Specify Vulkan binding number shift for s-type register: <shift> <space>  </summary>
    [CompilerOption(name:"-fvk-s-shift")]                                
    public string? SRegisterBindingShift = null; 

    /// <summary> Follow Vulkan spec to use gl_BaseInstance as the first vertex instance, which makes SV_InstanceID = gl_InstanceIndex - gl_BaseInstance (without this option, SV_InstanceID = gl_InstanceIndex) </summary>
    [CompilerOption(name:"-fvk-support-nonzero-base-instance")]          
    public bool nonzeroBaseInstance = false; 
    
    /// <summary> Specify Vulkan binding number shift for t-type register: <shift> <space>  </summary>
    [CompilerOption(name:"-fvk-t-shift")]                                
    public string? TRegisterBindingShift = null; 

    /// <summary> Specify Vulkan binding number shift for u-type register: <shift> <space>  </summary>
    [CompilerOption(name:"-fvk-u-shift")]                                
    public string? URegisterBindingShift = null; 

    /// <summary> Use DirectX memory layout for Vulkan resources </summary>
    [CompilerOption(name:"-fvk-use-dx-layout")]                          
    public bool useDirectXMemoryLayout = false; 

    /// <summary> Reciprocate SV_Position.w after reading from stage input in PS to accommodate the difference between Vulkan and DirectX </summary>
    [CompilerOption(name:"-fvk-use-dx-position-w")]                      
    public bool useDirectXPositionW = false; 

    /// <summary> Use strict OpenGL std140/std430 memory layout for Vulkan resources </summary>
    [CompilerOption(name:"-fvk-use-gl-layout")]                          
    public bool useOpenGLMemoryLayout = false; 

    /// <summary> Use scalar memory layout for Vulkan resources </summary>
    [CompilerOption(name:"-fvk-use-scalar-layout")]                      
    public bool useScalarMemoryLayout = false; 

    /// <summary> Specify a comma-separated list of SPIRV-Tools passes to customize optimization configuration (see https://khr.io/hlsl2spirv#optimization) </summary>
    [CompilerOption(name:"-Oconfig", Assignment = AssignmentType.Equals)]
    public string? spirVOptimizationConfig = null; 

    /// <summary> Generate SPIR-V code </summary>
    [CompilerOption(name:"-spirv")]                                      
    public bool generateAsSpirV = false; 

    /// <summary> Load a binary file rather than compiling </summary>
    [CompilerOption(name:"-dumpbin")]
    public bool dumpBin = false; 

    /// <summary> Extract root signature from shader bytecode (must be used with /Fo <file>) </summary>
    [CompilerOption(name:"-extractrootsignature")]                       
    public bool extractRootSignature = false; 
    
    /// <summary>  Save private data from shader blob </summary>
    [CompilerOption(name:"-getprivate")] 
    public string? privateDataFile = null; 
    
    /// <summary> Preprocess to file </summary>
    [CompilerOption(name:"-P")]                                          
    public bool outputPreprocessedCode = false; 

    /// <summary> Embed PDB in shader container (must be used with /Zi) </summary>
    [CompilerOption(name:"-Qembed_debug")]                               
    public bool embedDebugPDB = false; 
    
    /// <summary> Strip debug information from 4_0+ shader bytecode  (must be used with /Fo <file>) </summary>
    [CompilerOption(name:"-Qstrip_debug")]                               
    public bool stripDebugPDB = false; 
    
    /// <summary> Strip private data from shader bytecode  (must be used with /Fo <file>) </summary>
    [CompilerOption(name:"-Qstrip_priv")]                                
    public bool stripPrivate = false; 
    
    /// <summary> Strip reflection data from shader bytecode  (must be used with /Fo <file>) </summary>
    [CompilerOption(name:"-Qstrip_reflect")]                             
    public bool stripReflection = false; 
    
    /// <summary> Strip root signature data from shader bytecode  (must be used with /Fo <file>) </summary>
    [CompilerOption(name:"-Qstrip_rootsignature")]                       
    public bool stripRootSignature = false; 
    
    /// <summary> Private data to add to compiled shader blob </summary>
    [CompilerOption(name:"-setprivate")]                    
    public string? additionalPrivateData = null; 
    
    /// <summary> Attach root signature to shader bytecode </summary>
    [CompilerOption(name:"-setrootsignature")]                   
    public string? attachedRootSignature = null; 
    
    /// <summary> Verify shader bytecode with root signature </summary>
    [CompilerOption(name:"-verifyrootsignature")]                 
    public string? verifyRootSignature = null; 

    private Dictionary<string, bool> warnings = new();
    
    /// <summary> Enable/disable a warning </summary>
    public void SetWarning(string name, bool value) => warnings[name] = value; 

    private void AddWarnings(List<string> args)
    {
        foreach (var warning in warnings)
        {
            string warnVal = warning.Value ? string.Empty : "no-";
            args.Add($"-W{warnVal}{warning.Key}");
        }
    }
}