using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using SwaggerSharp.AssemblyLoaderCore.Infrastructure;
using SwaggerSharp.AssemblyLoaderCore.SwaggerGenerators.WebApi;
using SwaggerSharp.CodeGeneration.SwaggerGenerators.WebApi;
using SwaggerSharp.Examples;

namespace SwaggerSharp
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            LoadFromAssembly();
            return;
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
                VerbAttribute = "ActionVerbAttribute"
            };

            var generator = new WebApiToSwaggerGenerator(settings);
            var doc = generator.GenerateForController<ProductController>();
            var swaggerJson = doc.ToJson();
            Console.Write(swaggerJson);

        }

        public static void LoadFromAssembly()
        {
            var settings = new WebApiAssemblyToSwaggerGeneratorSettings();
            settings.AssemblyPaths = new[]{ "/Users/huwl/RiderProjects/SwaggerSharp/bin/Debug/SwaggerSharp.Examples.dll"};

            var assembly = Assembly.LoadFrom(settings.AssemblyPaths[0]);

            foreach (var type in assembly.GetTypes())
            {
                Console.WriteLine(type.FullName);
            }


            //var swaggerJson = document.ToJson();
        }
    }
}