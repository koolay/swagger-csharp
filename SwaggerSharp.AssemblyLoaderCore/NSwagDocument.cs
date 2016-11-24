//-----------------------------------------------------------------------
// <copyright file="NSwagDocument.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using NSwag.Commands;
using SwaggerSharp.AssemblyLoaderCore.Utilities;

namespace SwaggerSharp.AssemblyLoaderCore
{
    /// <summary>The NSwagDocument implementation.</summary>
    /// <seealso cref="NSwag.Commands.NSwagDocumentBase" />
    public class NSwagDocument : NSwagDocumentBase
    {
        /// <summary>Initializes a new instance of the <see cref="NSwagDocument"/> class.</summary>
        public NSwagDocument()
        {
        }

        /// <summary>Creates a new NSwagDocument.</summary>
        /// <returns>The document.</returns>
        public static NSwagDocument Create()
        {
            return Create<NSwagDocument>();
        }

        /// <summary>Converts to absolute path.</summary>
        /// <param name="pathToConvert">The path to convert.</param>
        /// <returns></returns>
        protected override string ConvertToAbsolutePath(string pathToConvert)
        {
            if (!string.IsNullOrEmpty(pathToConvert) && !System.IO.Path.IsPathRooted(pathToConvert))
                return PathUtilities.MakeAbsolutePath(pathToConvert, GetDocumentDirectory());
            return pathToConvert;
        }

        /// <summary>Converts a path to an relative path.</summary>
        /// <param name="pathToConvert">The path to convert.</param>
        /// <returns>The relative path.</returns>
        protected override string ConvertToRelativePath(string pathToConvert)
        {
            if (!string.IsNullOrEmpty(pathToConvert) && !pathToConvert.Contains("C:\\Program Files\\"))
                return PathUtilities.MakeRelativePath(pathToConvert, GetDocumentDirectory())?.Replace("\\", "/");
            return pathToConvert?.Replace("\\", "/");
        }

        private string GetDocumentDirectory()
        {
            var absoluteDocumentPath = PathUtilities.MakeAbsolutePath(Path, System.IO.Directory.GetCurrentDirectory());
            return System.IO.Path.GetDirectoryName(absoluteDocumentPath);
        }
    }
}