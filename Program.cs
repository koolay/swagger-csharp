using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NJsonSchema.Infrastructure;
using SwaggerSharp.CodeGeneration.SwaggerGenerators.WebApi;
using SwaggerSharp.Examples;

namespace SwaggerSharp
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            //var jsonStr = LoadFromAssembly();
            //Console.Write(jsonStr);
            //return;
            /*
            1. 如果controller类有指定的属性，则忽略该controller.通过SwaggerIgnoreAttribute配置
            2. 如果action有指定的属性，则忽略该action. 通过SwaggerIgnoreAttribute配置
            */

            var settings = new WebApiToSwaggerGeneratorSettings
            {
                ActionPathRegex = @"(?<action>[^\s]+)$",
                ControllerPathRegex = @"(?<controller>[^\s]+)Controller$",
                NameSpacePathRegex = @"\.?(?<namespace>[^\s\.]+)$",
                SwaggerIgnoreAttribute = "ForbidHttpAttribute",
                VerbAttribute = "ActionVerbAttribute.Verb"
            };

            var generator = new WebApiToSwaggerGenerator(settings);
            var doc = generator.GenerateForController<ProductController>();
            var swaggerJson = doc.ToJson();
            Console.Write(swaggerJson);

        }

        public static string LoadFromAssembly()
        {
            var settings = new AssemblyTypeToSwaggerGeneratorSettings();
            // dll路径
            settings.AssemblyPaths = new[]{ "/Users/huwl/RiderProjects/SwaggerSharp/bin/Debug/SwaggerSharp.Examples.dll"};
            // controller所在dll
            settings.InheritFrom = "SwaggerSharp.Examples.ControllerBase, SwaggerSharp.Examples.dll";
            // 设置类的后缀
            settings.ControllerSuffix = "Controller";

            settings.ApiSetting = new WebApiToSwaggerGeneratorSettings
            {
                ActionPathRegex = @"(?<action>[^\s]+)$",
                ControllerPathRegex = @"(?<controller>[^\s]+)Controller$",
                NameSpacePathRegex = @"\.?(?<namespace>[^\s\.]+)$",
                SwaggerIgnoreAttribute = "ForbidHttpAttribute",
                VerbAttribute = "ActionVerbAttribute.Verb"

            };

            var assembly = Assembly.LoadFrom(settings.AssemblyPaths[0]);
            IList<Type> controllers = new List<Type>();
            foreach (var type in assembly.GetTypes())
            {
                var classTypeIsOk = true;
                var classNameIsOk = true;

                if (!string.IsNullOrEmpty(settings.InheritFrom))
                {
                    var parentType = Type.GetType(settings.InheritFrom);
                    classTypeIsOk = type.IsSubclassOf(parentType) && !type.IsAbstract;
                }

                if (!string.IsNullOrEmpty(settings.ControllerSuffix))
                {
                    var reg = new Regex(@".+" + settings.ControllerSuffix);
                    classNameIsOk = reg.Match(type.Name).Success;

                }

                if (classNameIsOk && classTypeIsOk)
                {
                    Console.WriteLine($"controller: {type.FullName}:");
                    controllers.Add(type);
                }
                else
                {
                    Console.WriteLine(type.FullName);
                }


            }

            var generator = new WebApiToSwaggerGenerator(settings.ApiSetting);

            //GenerateForControllers(new[] {typeof(TController)});

            var doc = generator.GenerateForControllers(controllers);
            return doc.ToJson();

        }
    }
}