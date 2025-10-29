/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.Parser.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using log4net;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.VisualBasic;

    public class Compile
    {
        public bool Success;
        public Assembly CompiledAssembly { get; set; }
        // ReSharper disable once MemberCanBePrivate.Global
        public IEnumerable<Diagnostic> Errors { get; set; }

        public void CompileCode(string code, ILog log, string[] refs)
        {
            var assemblyGuid = Guid.NewGuid().ToString();

            if (log.IsInfoEnabled)
            {
                log.Info("Roslyn Compilation in VB.net: Is about to compile the code " + code +
                         " with the assembly GUID of " + assemblyGuid + ".");   
            }

            var compilation = VisualBasicCompilationConfig(code, log, refs, assemblyGuid);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    "Roslyn Compilation in VB.net: Has configured the compiler and will now proceed to compile the code.");
            }

            var peStream = new MemoryStream();
            var result = compilation.Emit(peStream);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    "Roslyn Compilation in VB.net: Code compilation process has concluded.  Will now inspect any errors.");
            }

            if (!result.Success)
            {
                var failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
                IEnumerable<Diagnostic> diagnostics = failures as Diagnostic[] ?? failures.ToArray();
                Errors = diagnostics;
                Success = false;
            }
            else
            {
                Success = true;
                HandleCompile(log, peStream);
            }
        }

        private void HandleCompile(ILog log, Stream peStream)
        {
            peStream.Position = 0;
            if (log.IsInfoEnabled)
            {
                log.Info("Roslyn Compilation in VB.net: Is about to load the assembly from a stream of " +
                         peStream.Length + ".");
            }

            var assemblyLoadContext = new SimpleUnloadableAssemblyLoadContext();
            CompiledAssembly = assemblyLoadContext.LoadFromStream(peStream);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    "Roslyn Compilation in VB.net: Loaded compiled assembly.  Will now proceed to unload the assembly context.");
            }

            assemblyLoadContext.Unload();

            if (log.IsInfoEnabled)
            {
                log.Info("Roslyn Compilation in VB.net: Unloaded assembly context.");
            }
        }

        private static VisualBasicCompilation VisualBasicCompilationConfig(string code, ILog log,
            IReadOnlyList<string> refs,
            string assemblyGuid)
        {
            var parseOptions = VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
            var compileOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release, embedVbCoreRuntime: true);

            var references = MetadataReferences(log, refs);

            var compilation = VisualBasicCompilation.Create(assemblyGuid)
                .AddSyntaxTrees(VisualBasicSyntaxTree.ParseText(code, parseOptions))
                .WithReferences(references).WithOptions(compileOptions);
            return compilation;
        }

        private static MetadataReference[] MetadataReferences(ILog log, IReadOnlyList<string> refs)
        {
            var references = new MetadataReference[refs.Count + 3];
            int i;
            for (i = 0; i < refs.Count; i++)
            {
                references[i] = MetadataReference.CreateFromFile(refs[i]);

                if (log.IsInfoEnabled)
                {
                    log.Info("Roslyn Compilation in VB.net: Included custom reference " + refs[i] +
                             ".  Will now add the mandated reference.");
                }
            }

            var directoryForDll = Path.GetDirectoryName(typeof(object).Assembly.Location);
            references[i] = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);                    //Dummy
            references[i + 1] = MetadataReference.CreateFromFile(Path.Join(directoryForDll, "System.Runtime.dll"));//Dummy
            references[i + 2] = MetadataReference.CreateFromFile(Path.Join(directoryForDll, "netstandard.dll"));   //Dummy

            if (log.IsInfoEnabled)
            {
                log.Info("Roslyn Compilation in VB.net: Included mandated reference " + references[i] +
                         ".  Will now configure the compiler.");
            }

            return references;
        }
    }
}
