
namespace SwaggerSharp.CodeGeneration.SwaggerGenerators.WebApi
{
    public class AssemblyTypeToSwaggerGeneratorSettings
    {
        public WebApiToSwaggerGeneratorSettings ApiSetting { get; set; }

        public string[] AssemblyPaths { get; set; }

        public string InheritFrom { get; set; }

        public string ControllerSuffix { get; set; }
    }
}