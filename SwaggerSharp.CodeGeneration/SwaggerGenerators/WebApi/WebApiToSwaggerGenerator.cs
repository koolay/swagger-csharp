//-----------------------------------------------------------------------
// <copyright file="WebApiToSwaggerGenerator.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NJsonSchema;
using NJsonSchema.Infrastructure;
using NSwag;
using NSwag.CodeGeneration.Infrastructure;
using SwaggerSharp.CodeGeneration.SwaggerGenerators.WebApi.Processors.Contexts;
using SwaggerSharp.Core;

namespace SwaggerSharp.CodeGeneration.SwaggerGenerators.WebApi
{
    /// <summary>Generates a <see cref="SwaggerDocument"/> object for the given Web API class type. </summary>
    public class WebApiToSwaggerGenerator
    {
        private readonly SwaggerJsonSchemaGenerator _schemaGenerator;

        /// <summary>Initializes a new instance of the <see cref="WebApiToSwaggerGenerator" /> class.</summary>
        /// <param name="settings">The settings.</param>
        public WebApiToSwaggerGenerator(WebApiToSwaggerGeneratorSettings settings)
            : this(settings, new SwaggerJsonSchemaGenerator(settings))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="WebApiToSwaggerGenerator" /> class.</summary>
        /// <param name="settings">The settings.</param>
        /// <param name="schemaGenerator">The schema generator.</param>
        public WebApiToSwaggerGenerator(WebApiToSwaggerGeneratorSettings settings,
            SwaggerJsonSchemaGenerator schemaGenerator)
        {
            Settings = settings;
            _schemaGenerator = schemaGenerator;
        }

        /// <summary>Gets all controller class types of the given assembly.</summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The controller classes.</returns>
        public static IEnumerable<Type> GetControllerClasses(Assembly assembly)
        {
            // TODO: Move to IControllerClassLoader interface
            return assembly.ExportedTypes
                    .Where(t => t.GetTypeInfo().IsAbstract == false)
                    .Where(t => t.Name.EndsWith("Controller") ||
                                t.InheritsFrom("ApiController", TypeNameStyle.Name) ||
                                t.InheritsFrom("Controller", TypeNameStyle.Name))
                    // in ASP.NET Core, a Web API controller inherits from Controller
                    .Where(t => t.GetTypeInfo().ImplementedInterfaces.All(i => i.FullName != "System.Web.Mvc.IController"));
                // no MVC controllers (legacy ASP.NET)
        }

        /// <summary>Gets or sets the generator settings.</summary>
        public WebApiToSwaggerGeneratorSettings Settings { get; set; }

        /// <summary>Generates a Swagger specification for the given controller type.</summary>
        /// <typeparam name="TController">The type of the controller.</typeparam>
        /// <returns>The <see cref="SwaggerDocument" />.</returns>
        /// <exception cref="InvalidOperationException">The operation has more than one body parameter.</exception>
        public SwaggerDocument GenerateForController<TController>()
        {
            return GenerateForControllers(new[] {typeof(TController)});
        }

        /// <summary>Generates a Swagger specification for the given controller type.</summary>
        /// <param name="controllerType">The type of the controller.</param>
        /// <returns>The <see cref="SwaggerDocument" />.</returns>
        /// <exception cref="InvalidOperationException">The operation has more than one body parameter.</exception>
        public SwaggerDocument GenerateForController(Type controllerType)
        {
            return GenerateForControllers(new[] {controllerType});
        }

        /// <summary>Generates a Swagger specification for the given controller types.</summary>
        /// <param name="controllerTypes">The types of the controller.</param>
        /// <returns>The <see cref="SwaggerDocument" />.</returns>
        /// <exception cref="InvalidOperationException">The operation has more than one body parameter.</exception>
        public SwaggerDocument GenerateForControllers(IEnumerable<Type> controllerTypes)
        {
            var document = CreateDocument(Settings);

            var schemaResolver = new SchemaResolver();
            var schemaDefinitionAppender = new SwaggerDocumentSchemaDefinitionAppender(document,
                Settings.TypeNameGenerator);

            foreach (var controllerType in controllerTypes)
                GenerateForController(document, controllerType,
                    new SwaggerGenerator(_schemaGenerator, Settings, schemaResolver, schemaDefinitionAppender));

            AppendRequiredSchemasToDefinitions(document, schemaResolver);
            document.GenerateOperationIds();

            foreach (var processor in Settings.DocumentProcessors)
                processor.Process(new DocumentProcessorContext(document, controllerTypes));

            return document;
        }

        private SwaggerDocument CreateDocument(WebApiToSwaggerGeneratorSettings settings)
        {
            var document = !string.IsNullOrEmpty(settings.DocumentTemplate)
                ? SwaggerDocument.FromJson(settings.DocumentTemplate)
                : new SwaggerDocument();

            document.Consumes = new List<string> {"application/json"};
            document.Produces = new List<string> {"application/json"};
            document.Info = new SwaggerInfo
            {
                Title = settings.Title,
                Description = settings.Description,
                Version = settings.Version
            };

            return document;
        }

        /// <exception cref="InvalidOperationException">The operation has more than one body parameter.</exception>
        private void GenerateForController(SwaggerDocument document, Type controllerType,
            SwaggerGenerator swaggerGenerator)
        {
            var hasIgnoreAttribute = controllerType.GetTypeInfo()
                .GetCustomAttributes()
                .Any(a => a.GetType().Name == Settings.SwaggerIgnoreAttribute);

            if (hasIgnoreAttribute) return;
            var operations = new List<Tuple<SwaggerOperationDescription, MethodInfo>>();
            foreach (var method in GetActionMethods(controllerType))
            {
                var httpPaths = GetHttpPaths(controllerType, method).ToList();
                var httpMethods = GetSupportedHttpMethods(method).ToList();

                foreach (var httpPath in httpPaths)
                {
                    foreach (var httpMethod in httpMethods)
                    {
                        var operationDescription = new SwaggerOperationDescription
                        {
                            Path = httpPath,
                            Method = httpMethod,
                            Operation = new SwaggerOperation
                            {
                                IsDeprecated = method.GetCustomAttribute<ObsoleteAttribute>() != null,
                                OperationId = GetOperationId(document, controllerType.Name, method)
                            }
                        };

                        operations.Add(new Tuple<SwaggerOperationDescription, MethodInfo>(operationDescription, method));
                    }
                }
            }

            AddOperationDescriptionsToDocument(document, operations, swaggerGenerator);
        }

        private void AddOperationDescriptionsToDocument(SwaggerDocument document,
            List<Tuple<SwaggerOperationDescription, MethodInfo>> operations, SwaggerGenerator swaggerGenerator)
        {
            var allOperation = operations.Select(t => t.Item1).ToList();
            foreach (var tuple in operations)
            {
                var operation = tuple.Item1;
                var method = tuple.Item2;

                var addOperation = RunOperationProcessors(document, method, operation, allOperation, swaggerGenerator);
                if (addOperation)
                {
                    if (!document.Paths.ContainsKey(operation.Path))
                        document.Paths[operation.Path] = new SwaggerOperations();

                    if (document.Paths[operation.Path].ContainsKey(operation.Method))
                        throw new InvalidOperationException("The method '" + operation.Method + "' on path '" +
                                                            operation.Path + "' is registered multiple times.");

                    document.Paths[operation.Path][operation.Method] = operation.Operation;
                }
            }
        }

        private bool RunOperationProcessors(SwaggerDocument document, MethodInfo methodInfo,
            SwaggerOperationDescription operationDescription, List<SwaggerOperationDescription> allOperations,
            SwaggerGenerator swaggerGenerator)
        {
            // 1. Run from settings
            foreach (var operationProcessor in Settings.OperationProcessors)
            {
                if (
                    operationProcessor.Process(new OperationProcessorContext(document, operationDescription, methodInfo,
                        swaggerGenerator, allOperations)) == false)
                    return false;
            }

            // 2. Run from class attributes
            var operationProcessorAttribute = methodInfo.DeclaringType.GetTypeInfo()
                .GetCustomAttributes()
                // 3. Run from method attributes
                .Concat(methodInfo.GetCustomAttributes())
                .Where(a => a.GetType().Name == "SwaggerOperationProcessorAttribute");

            foreach (dynamic attribute in operationProcessorAttribute)
            {
                var operationProcessor = Activator.CreateInstance(attribute.Type);
                if (operationProcessor.Process(methodInfo, operationDescription, swaggerGenerator, allOperations) ==
                    false)
                    return false;
            }

            return true;
        }

        private void AppendRequiredSchemasToDefinitions(SwaggerDocument document, ISchemaResolver schemaResolver)
        {
            foreach (var schema in schemaResolver.Schemas)
            {
                if (!document.Definitions.Values.Contains(schema))
                {
                    var typeName = schema.GetTypeName(Settings.TypeNameGenerator, string.Empty);

                    if (!document.Definitions.ContainsKey(typeName))
                        document.Definitions[typeName] = schema;
                    else
                        document.Definitions["ref_" + Guid.NewGuid().ToString().Replace("-", "_")] = schema;
                }
            }
        }

        private IEnumerable<MethodInfo> GetActionMethods(Type controllerType)
        {
            var methods = controllerType.GetRuntimeMethods().Where(m => m.IsPublic);
            return methods.Where(m =>
                m.IsSpecialName == false && // avoid property methods
                m.DeclaringType != null &&
                m.DeclaringType != typeof(object) &&
                m.GetCustomAttributes().All(a => a.GetType().Name != Settings.SwaggerIgnoreAttribute) &&
                m.DeclaringType.FullName.StartsWith("Microsoft.AspNet") == false && // .NET Core (Web API & MVC)
                m.DeclaringType.FullName != "System.Web.Http.ApiController" &&
                m.DeclaringType.FullName != "System.Web.Mvc.Controller");
        }

        private string GetOperationId(SwaggerDocument document, string controllerName, MethodInfo method)
        {
            string operationId;

            dynamic swaggerOperationAttribute =
                method.GetCustomAttributes().FirstOrDefault(a => a.GetType().Name == "SwaggerOperationAttribute");
            if (swaggerOperationAttribute != null && !string.IsNullOrEmpty(swaggerOperationAttribute.OperationId))
                operationId = swaggerOperationAttribute.OperationId;
            else
            {
                if (controllerName.EndsWith("Controller"))
                    controllerName = controllerName.Substring(0, controllerName.Length - 10);

                var methodName = method.Name;
                if (methodName.EndsWith("Async"))
                    methodName = methodName.Substring(0, methodName.Length - 5);

                operationId = controllerName + "_" + methodName;
            }

            var number = 1;
            while (
                document.Operations.Any(
                    o => o.Operation.OperationId == operationId + (number > 1 ? "_" + number : string.Empty)))
                number++;

            return operationId + (number > 1 ? number.ToString() : string.Empty);
        }

        private IEnumerable<string> GetHttpPaths(Type controllerType, MethodInfo method)
        {
            var httpPaths = new List<string>();

            var routeAttributes = GetRouteAttributes(method.GetCustomAttributes()).ToList();

            // .NET Core: RouteAttribute on class level
            dynamic routeAttributeOnClass =
                GetRouteAttributes(controllerType.GetTypeInfo().GetCustomAttributes()).SingleOrDefault();
            dynamic routePrefixAttribute =
                GetRoutePrefixAttributes(controllerType.GetTypeInfo().GetCustomAttributes()).SingleOrDefault();

            if (routeAttributes.Any())
            {
                foreach (dynamic attribute in routeAttributes)
                {
                    if (attribute.Template.StartsWith("~/")) // ignore route prefixes
                        httpPaths.Add(attribute.Template.Substring(1));
                    else if (routePrefixAttribute != null)
                        httpPaths.Add(routePrefixAttribute.Prefix + "/" + attribute.Template);
                    else if (routeAttributeOnClass != null)
                        httpPaths.Add(routeAttributeOnClass.Template + "/" + attribute.Template);
                    else
                        httpPaths.Add(attribute.Template);
                }
            }
            else
            {
                var actionPathName = "";
                var controllerPathName = "";
                var namespacePathName = "";

                if (!string.IsNullOrEmpty(Settings.ControllerPathRegex))
                {
                    Match controllerNameMatch = (new Regex(Settings.ControllerPathRegex)).Match(controllerType.Name);
                    controllerPathName = controllerNameMatch.Groups["controller"].Value;
                }
                if (!string.IsNullOrEmpty(Settings.ActionPathRegex))
                {
                    var actionPathMatch = (new Regex(Settings.ActionPathRegex)).Match(method.Name);
                    actionPathName = actionPathMatch.Groups.Count > 0 ? actionPathMatch.Groups["action"].Value : "";
                }
                if (!string.IsNullOrEmpty(Settings.NameSpacePathRegex))
                {
                    if (controllerType.Namespace != null)
                    {
                        var namespacePathMatch = (new Regex(Settings.NameSpacePathRegex)).Match(controllerType.Namespace);
                        namespacePathName = namespacePathMatch.Groups.Count > 0
                            ? namespacePathMatch.Groups["namespace"].Value
                            : "";
                    }
                }

                var path = $"{namespacePathName}/{controllerPathName}/{actionPathName}";
                path = path.Replace("//", "/");
                httpPaths.Add(path);
            }

            return httpPaths
                .Select(p =>
                    "/" + p
                        .Replace("[", "{")
                        .Replace("]", "}")
                        .Trim('/'))
                .SelectMany(p => ExpandOptionalHttpPathParameters(p, method))
                .Distinct()
                .ToList();
        }

        private IEnumerable<string> ExpandOptionalHttpPathParameters(string path, MethodInfo method)
        {
            var segments = path.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (!segment.EndsWith("?}")) continue;
                foreach (
                    var p in
                    ExpandOptionalHttpPathParameters(
                        string.Join("/", segments.Take(i).Concat(segments.Skip(i + 1))), method))
                    yield return p;

                // Only expand if optional parameter is available in action method
                if (!method.GetParameters()
                    .Any(p => segment.StartsWith("{" + p.Name + ":") || segment.StartsWith("{" + p.Name + "?")))
                    yield break;
                {
                    foreach (
                        var p in
                        ExpandOptionalHttpPathParameters(
                            string.Join("/",
                                segments.Take(i)
                                    .Concat(new[] {segment.Replace("?", "")})
                                    .Concat(segments.Skip(i + 1))), method))
                        yield return p;
                }

                yield break;
            }
            yield return path;
        }

        private IEnumerable<Attribute> GetRouteAttributes(IEnumerable<Attribute> attributes)
        {
            return attributes.Where(a => a.GetType().Name == Settings.RouteAttribute ||
                                         a.GetType()
                                             .GetTypeInfo()
                                             .ImplementedInterfaces.Any(t => t.Name == "IHttpRouteInfoProvider") ||
                                         a.GetType()
                                             .GetTypeInfo()
                                             .ImplementedInterfaces.Any(t => t.Name == "IRouteTemplateProvider"))
                // .NET Core
                .Where((dynamic a) => a.Template != null)
                .OfType<Attribute>();
        }

        private IEnumerable<Attribute> GetRoutePrefixAttributes(IEnumerable<Attribute> attributes)
        {
            return attributes.Where(a => a.GetType().Name == "RoutePrefixAttribute" ||
                                         a.GetType()
                                             .GetTypeInfo()
                                             .ImplementedInterfaces.Any(t => t.Name == "IRoutePrefix"));
        }

        private string GetActionName(MethodInfo method)
        {
            dynamic actionNameAttribute = method.GetCustomAttributes()
                .SingleOrDefault(a => a.GetType().Name == "ActionNameAttribute");

            if (actionNameAttribute != null)
                return actionNameAttribute.Name;

            return method.Name;
        }

        private IEnumerable<SwaggerOperationMethod> GetSupportedHttpMethods(MethodInfo method)
        {
            // See http://www.asp.net/web-api/overview/web-api-routing-and-actions/routing-in-aspnet-web-api

            var httpMethods = GetSupportedHttpMethodsFromAttributes(method).ToArray();
            foreach (var httpMethod in httpMethods)
                yield return httpMethod;

            if (httpMethods.Length == 0)
            {
                yield return SwaggerOperationMethod.Post;
            }
        }



        private IEnumerable<SwaggerOperationMethod> GetSupportedHttpMethodsFromAttributes(MethodInfo method)
        {
            // 从自定义的VerbAttribute获取method
            if (string.IsNullOrEmpty(Settings.VerbAttribute))
            {
                yield return SwaggerOperationMethod.Post;
            }

            var classNameSplits = Settings.VerbAttribute.Split('.');
            var className = "";
            var prop = "";
            if (classNameSplits.Length > 1)
            {
                className = classNameSplits[0];
                prop = classNameSplits[1];
            }
            else
            {
                className = Settings.VerbAttribute;
            }

            var verbAttr =
                method.GetCustomAttributes().FirstOrDefault(o => o.GetType().Name == className);

            var verb = "post";
            if (verbAttr != null)
            {
                if (!string.IsNullOrEmpty(prop))
                {
                    var a = (dynamic) verbAttr;
                    if (a.GetType().GetProperty(prop) == null)
                    {
                        throw new Exception($"类{className}不存在该属性{prop}");
                    }
                    verb = a.GetType().GetProperty(prop).GetValue(a, null);
                }
            }
            else
            {
                //根据类名提取verb
                var reg =
                    new Regex(@"[a-zA-Z]*(?<verb>(get)|(post)|(put)|(delete)|(patch)|(head)|(options))[a-zA-Z]*");
                var match = reg.Match(className.ToLower());
                if (match.Success && match.Groups.Count > 0)
                {
                    verb = match.Groups["verb"].Value;
                }

            }
            switch (verb)
            {
                case "get":
                {
                    yield return SwaggerOperationMethod.Get;
                    break;
                }
                case "post":
                {
                    yield return SwaggerOperationMethod.Post;
                    break;
                }
                case "put":
                {
                    yield return SwaggerOperationMethod.Put;
                    break;
                }
                case "delete":
                {
                    yield return SwaggerOperationMethod.Delete;
                    break;
                }
                case "patch":
                {
                    yield return SwaggerOperationMethod.Patch;
                    break;
                }
                case "head":
                {
                    yield return SwaggerOperationMethod.Head;
                    break;
                }
                case "options":
                {
                    yield return SwaggerOperationMethod.Options;
                    break;
                }
                default:
                {
                    yield return SwaggerOperationMethod.Post;
                    break;
                }
            }


        }

    }
}
