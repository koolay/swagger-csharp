//-----------------------------------------------------------------------
// <copyright file="OperationSummaryAndDescriptionProcessor.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using NJsonSchema.Infrastructure;
using SwaggerSharp.CodeGeneration.SwaggerGenerators.WebApi.Processors.Contexts;

namespace SwaggerSharp.CodeGeneration.SwaggerGenerators.WebApi.Processors
{
    /// <summary>Loads the operation summary and description from the DescriptionAttribute and the XML documentation.</summary>
    public class OperationSummaryAndDescriptionProcessor : IOperationProcessor
    {
        /// <summary>Processes the specified method information.</summary>
        /// <param name="context"></param>
        /// <param name="setting"></param>
        /// <returns>true if the operation should be added to the Swagger specification.</returns>
        public bool Process(OperationProcessorContext context, WebApiToSwaggerGeneratorSettings setting)
        {
            var summaryAttrSplits = setting.SummaryAttribute.Split('.');
            var attrName = "";
            var prop = "";
            if (summaryAttrSplits.Length > 1)
            {
                attrName = summaryAttrSplits[0];
                prop = summaryAttrSplits[1];
            }
            else
            {
                attrName = setting.SummaryAttribute;
            }

            dynamic descriptionAttribute = context.MethodInfo.GetCustomAttributes()
                .SingleOrDefault(a => a.GetType().Name == attrName);

            if (descriptionAttribute != null)
            {
                if (!string.IsNullOrEmpty(prop))
                {

                    if (descriptionAttribute.GetType().GetProperty(prop) == null)
                    {
                        throw new Exception($"类{attrName}不存在该属性{prop}");
                    }
                    context.OperationDescription.Operation.Summary = descriptionAttribute.GetType().GetProperty(prop).GetValue(descriptionAttribute, null);
                }

            }
            else
            {
                var summary = context.MethodInfo.GetXmlSummary();
                if (summary != string.Empty)
                    context.OperationDescription.Operation.Summary = summary;
            }

            if (string.IsNullOrEmpty(context.OperationDescription.Operation.Summary))
            {
                context.OperationDescription.Operation.Summary = context.MethodInfo.Name;
            }

            var remarks = context.MethodInfo.GetXmlRemarks();
            if (remarks != string.Empty)
                context.OperationDescription.Operation.Description = remarks;

            return true; 
        }
    }
}