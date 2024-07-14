namespace DirectXShaderCompiler.NET;

public partial class CompilerOptions
{   
    /// <summary> Specify whitelist of debug info category (file -> source -> line, tool, vulkan-with-source) </summary>
    [CompilerOption(name:"-fspv-debug", Assignment = AssignmentType.Equals)]
    public string? debugWhitelist = null; 

    /// <summary> Enables the MaximallyReconvergesKHR execution mode for this module. </summary>
    [CompilerOption(name:"-fspv-enable-maximal-reconvergence")]
    public bool? enableMaximalReconvergence = null; 
    
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
}