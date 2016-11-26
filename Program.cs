using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CommandLine;
using SwaggerSharp.CodeGeneration.SwaggerGenerators.WebApi;
using SwaggerSharp.Examples;


namespace SwaggerSharp
{
    internal class Options
    {
        [Option('a', "assembly", Required = false,
             HelpText = "Input files to be processed.")]
        public IEnumerable<string> Assembly { get; set; }

        [Option('c', "controller", Required = false,
             HelpText = "Content language.")]
        public IEnumerable<string> Controllers { get; set; }

        [Option('i', "inherit", Required = true, HelpText = "Controller父类, 如:SwaggerSharp.Examples.ControllerBase, SwaggerSharp.Examples.dll")]
        public string InheritFrom { get; set; }

        // Omitting long name, default --verbose
        [Option(
             HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

    }

    internal class APIConfig
    {
        // action方法名提取
        public string ActionPathRegex { get; set; } = @"(?<action>[^\s]+)$";

        // 控制器类名提取
        public string ControllerPathRegex { get; set; } = @"(?<controller>[^\s]+)Controller$";

        // 命名空间提取到module
        public string NameSpacePathRegex { get; set; } = @"\.?(?<namespace>[^\s\.]+)$";

        // 解析时是否忽略
        public string SwaggerIgnoreAttribute { get; set; } = "ForbidHttpAttribute";

        // verb属性: post,get,put等
        public string VerbAttribute { get; set; } = "ActionVerbAttribute.Verb";

        // 根据此参数的属性是否存在, 值判断参数是否可选
        public string RequiredParemeterAttribute { get; set; } = "RequiredAttribute";

    }


    internal class AssemblyConfig
    {

        public string ControllerSuffix { get; set; } = "Controller";

        // Controller父类, 如: SwaggerSharp.Examples.ControllerBase, SwaggerSharp.Examples.dll
        public string InheritFrom { get; set; }

    }


    internal class Program
    {
        public static void Main(string[] args)
        {
            var swaggerJson = string.Empty;
            var result = Parser.Default.ParseArguments<Options>(args);
            if (!result.Errors.Any())
            {
                var controllers = new List<Type>();
                var opts = result.Value;

                if (opts.Assembly != null && opts.Assembly.Any())
                {
                    foreach (var path in opts.Assembly)
                    {
                        controllers.AddRange(LoadControllersFromAssembly(path, GetAssemblyConfig()));
                    }
                }
                if (opts.Controllers != null && opts.Controllers.Any())
                {
                    controllers.Add(Type.GetType(opts.InheritFrom));
                }

                swaggerJson = ExportController(controllers, GetAPIConfig());
                Console.Write(swaggerJson);
            }

            Console.WriteLine("complete");

        }


        public static AssemblyConfig GetAssemblyConfig()
        {
            return new AssemblyConfig();
        }

        public static APIConfig GetAPIConfig()
        {
            return new APIConfig();
        }


        public static string ExportController(IEnumerable<Type> controllers, APIConfig config)
        {
            var settings = new AssemblyTypeToSwaggerGeneratorSettings();
            // dll路径

            settings.ApiSetting = new WebApiToSwaggerGeneratorSettings
            {
                // action方法名提取
                ActionPathRegex = config.ActionPathRegex,
                // 控制器类名提取
                ControllerPathRegex = config.ControllerPathRegex,
                // 命名空间提取到module
                NameSpacePathRegex = config.NameSpacePathRegex,
                // 解析时是否忽略
                SwaggerIgnoreAttribute = config.SwaggerIgnoreAttribute,
                // verb属性: post,get,put等
                VerbAttribute = config.VerbAttribute,
                // 根据此参数的属性是否存在, 值判断参数是否可选
                RequiredParemeterAttribute = config.RequiredParemeterAttribute

            };

            var generator = new WebApiToSwaggerGenerator(settings.ApiSetting);
            var doc = generator.GenerateForControllers(controllers);
            return doc.ToJson();

        }


        public static IEnumerable<Type> LoadControllersFromAssembly(string assemblyPath, AssemblyConfig assemblyConfig)
        {

            var assembly = Assembly.LoadFrom(assemblyPath);
            IList<Type> controllers = new List<Type>();
            foreach (var type in assembly.GetTypes())
            {
                var classTypeIsOk = true;
                var classNameIsOk = true;

                if (!string.IsNullOrEmpty(assemblyConfig.InheritFrom))
                {
                    var parentType = Type.GetType(assemblyConfig.InheritFrom);
                    classTypeIsOk = type.IsSubclassOf(parentType) && !type.IsAbstract;
                }

                if (!string.IsNullOrEmpty(assemblyConfig.ControllerSuffix))
                {
                    var reg = new Regex(@".+" + assemblyConfig.ControllerSuffix);
                    classNameIsOk = reg.Match(type.Name).Success;
                }

                if (classNameIsOk && classTypeIsOk)
                {
                    controllers.Add(type);
                }

            }
            return controllers;

        }

    }
}