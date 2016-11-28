using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CommandLine;
using SwaggerSharp.CodeGeneration.SwaggerGenerators.WebApi;


namespace SwaggerSharp
{

    internal class Options
    {
        [Option('a', "assembly", Required = false,
             HelpText = @"程序集dll完整路径.如:c:\assembly\App.Controllers.dll")]
        public IEnumerable<string> Assembly { get; set; }

        [Option('c', "controller", Required = false,
             HelpText = @"控制器controller名称.如:App.Controllers.ProjectController, App.Controllers.dll")]
        public IEnumerable<string> Controllers { get; set; }

        [Option('i', "inherit", Required = false,
             HelpText = @"Controller父类, 如:SwaggerSharp.Examples.ControllerBase, SwaggerSharp.Examples.dll")]
        public string InheritFrom { get; set; }

        [Option('o', "output", Required = true,
             HelpText = @"输出位置, 如:c:\s.json; stdout; http://myapp.com/api")]
        public IEnumerable<string> Output { get; set; } = new [] {"stdout"};

        [Option('H', "header", Required = false,
             HelpText = @"输出为api时调用api请求的header, 如:x-ticket=xxxx")]
        public IEnumerable<string> Header { get; set; }


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

        public string ControllerSuffix { get; set; }// = "Controller";

        // Controller父类, 如: SwaggerSharp.Examples.ControllerBase, SwaggerSharp.Examples.dll
        public string InheritFrom { get; set; }

    }


    internal class Program
    {
        public static void Main(string[] args)
        {
            string swaggerJson;
            var result = Parser.Default.ParseArguments<Options>(args);
            result.WithParsed(opts =>
            {
                var controllers = new List<Type>();
                if (!opts.Assembly.Any() && !opts.Controllers.Any())
                {
                    Console.WriteLine("至少请指定一个程序集或者控制器类");
                    return;
                }

                if (opts.Assembly != null && opts.Assembly.Any())
                {
                    foreach (var path in opts.Assembly)
                    {
                        var assemblyConfig = GetAssemblyConfig();
                        if (string.IsNullOrEmpty(assemblyConfig.InheritFrom))
                            assemblyConfig.InheritFrom = opts.InheritFrom;

                        controllers.AddRange(LoadControllersFromAssembly(path, assemblyConfig));
                    }
                }

                if (opts.Controllers != null && opts.Controllers.Any())
                {
                    controllers.AddRange(opts.Controllers.Select(c => { return Type.GetType(c); }));
                }

                swaggerJson = ExportController(controllers, GetAPIConfig());
                foreach (var outPutStr in opts.Output)
                {
                    IOutputer outPuter;
                    if (outPutStr == "stdout")
                    {
                        outPuter = new Stdoutputer();
                    }
                    else if (outPutStr.EndsWith(".json"))
                    {
                        outPuter = new JsonOutputer(outPutStr);
                    }
                    else if (outPutStr.StartsWith("http"))
                    {
                        outPuter = new APIOutputer(outPutStr, opts.Header);
                    }
                    else
                    {
                        outPuter = new Stdoutputer();
                    }

                    outPuter.Output(swaggerJson);
                }


            }).WithNotParsed(errs =>
            {
                Console.WriteLine("Failed");
                foreach (var err in errs)
                {
                    Console.WriteLine(err.Tag.ToString());
                }
            });


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