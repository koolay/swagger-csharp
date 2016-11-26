//-----------------------------------------------------------------------
// <copyright file="WebApiToSwaggerGeneratorSettings.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Newtonsoft.Json;
using NJsonSchema;
using NJsonSchema.Generation;
using SwaggerSharp.CodeGeneration.SwaggerGenerators.WebApi.Processors;

namespace SwaggerSharp.CodeGeneration.SwaggerGenerators.WebApi
{
    /// <summary>Settings for the <see cref="WebApiToSwaggerGenerator"/>.</summary>
    public class WebApiToSwaggerGeneratorSettings : JsonSchemaGeneratorSettings
    {
        /// <summary>Initializes a new instance of the <see cref="WebApiToSwaggerGeneratorSettings"/> class.</summary>
        public WebApiToSwaggerGeneratorSettings()
        {
            NullHandling = NullHandling.Swagger;

            OperationProcessors.Add(new OperationParameterProcessor(this));
            OperationProcessors.Add(new OperationResponseProcessor(this));
        }

        /// <summary>
        /// 如果有该属性，则忽略该controller/action
        /// </summary>
        public string SwaggerIgnoreAttribute { get; set; } = "SwaggerIgnoreAttribute";

        /// <summary>
        /// 路由属性，以些提取path
        /// </summary>
        public string RouteAttribute { get; set; } = "RouteAttribute";

        /// <summary>
        /// 根据此参数的属性是否存在, 值判断参数是否可选
        /// </summary>
        public string RequiredParemeterAttribute { get; set; } = "RequiredAttribute";

        /// <summary>
        /// verb属性
        /// </summary>
        public string VerbAttribute { get; set; } = "Action.Verb";

        /// <summary>
        /// 路由前缀属性提取
        /// </summary>
        public string RoutePrefixAttribute { get; set; } = "RoutePrefixAttribute";

        /// <summary>
        /// 正则表达式匹配出controller部分的path
        /// </summary>
        public string ControllerPathRegex { get; set; } = @"(?<controller>[a-zA-Z]+)Controller";

        /// <summary>
        /// 正则表达式匹配出controller部分的path
        /// </summary>
        public string ActionPathRegex { get; set; } = @"(?<action>[^\s]+)";

        /// <summary>
        /// {namespace}/{controller}/{action}
        /// 正则表达式匹配出namespace部分作为path的前缀,等于空,则命名空间不作path的一部分
        /// 例如: @"(?<namespace>[a-zA-Z]+)";
        /// </summary>
        public string NameSpacePathRegex { get; set; }


        /// <summary>Gets or sets the default Web API URL template.</summary>
        public string DefaultUrlTemplate { get; set; } = "api/{controller}/{id}";

        /// <summary>Gets or sets the Swagger specification title.</summary>
        public string Title { get; set; } = "Web API Swagger specification";

        /// <summary>Gets or sets the Swagger specification description.</summary>
        public string Description { get; set; }

        /// <summary>Gets or sets the Swagger specification version.</summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>Gets the operation processor.</summary>
        [JsonIgnore]
        public IList<IOperationProcessor> OperationProcessors { get; } = new List<IOperationProcessor>
        {
            new OperationSummaryAndDescriptionProcessor(),
            new OperationTagsProcessor()
        };

        /// <summary>Gets the operation processor.</summary>
        [JsonIgnore]
        public IList<IDocumentProcessor> DocumentProcessors { get; } = new List<IDocumentProcessor>
        {
            new DocumentTagsProcessor()
        };

        /// <summary>Gets or sets the document template representing the initial Swagger specification (JSON data).</summary>
        public string DocumentTemplate { get; set; }

        /// <summary>Gets or sets a value indicating whether the controllers are hosted by ASP.NET Core.</summary>
        public bool IsAspNetCore { get; set; }

        /// <summary>Gets or sets a value indicating whether to add path parameters which are missing in the action method.</summary>
        public bool AddMissingPathParameters { get; set; }
    }
}