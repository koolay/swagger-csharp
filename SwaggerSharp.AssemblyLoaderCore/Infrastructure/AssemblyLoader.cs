//-----------------------------------------------------------------------
// <copyright file="AssemblyLoader.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;


namespace SwaggerSharp.AssemblyLoaderCore.Infrastructure
{
    internal class AssemblyLoader
    {
        public AssemblyLoadContext Context { get; }

        public AssemblyLoader()
        {
            Context = new CustomAssemblyLoadContext();
        }

        protected void RegisterReferencePaths(IEnumerable<string> referencePaths)
        {
            var allReferencePaths = new List<string>();
            foreach (var path in referencePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                allReferencePaths.Add(path);
                allReferencePaths.AddRange(GetAllDirectories(path));
            }

            // Add path to nswag directory
            allReferencePaths.Add(Path.GetDirectoryName(typeof(AssemblyLoader).GetTypeInfo().Assembly.CodeBase.Replace("file:///", string.Empty)));
            allReferencePaths = allReferencePaths.Distinct().ToList();

            Context.Resolving += (context, args) =>
            {
                var separatorIndex = args.Name.IndexOf(",", StringComparison.Ordinal);
                var assemblyName = separatorIndex > 0 ? args.Name.Substring(0, separatorIndex) : args.Name;

                foreach (var path in allReferencePaths)
                {
                    var files = Directory.GetFiles(path, assemblyName + ".dll", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        try
                        {
                            return Context.LoadFromAssemblyPath(file);
                        }
                        catch (Exception exception)
                        {
                            Debug.WriteLine("AssemblyLoader.AssemblyResolve exception when loading DLL '" + file + "': \n" + exception.ToString());
                        }
                    }
                }

                return null;
            };
        }

        private string[] GetAllDirectories(string rootDirectory)
        {
            return Directory.GetDirectories(rootDirectory, "*", SearchOption.AllDirectories);
        }
    }
}
