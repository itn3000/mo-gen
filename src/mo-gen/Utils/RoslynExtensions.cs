using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Buildalyzer.Workspaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MagicOnion.Utils;
using Buildalyzer;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace MagicOnion
{
    // Utility and Extension methods for Roslyn
    internal static class RoslynExtensions
    {
        static LoggerVerbosity ToLoggerVerbosity(this int value)
        {
            switch (value)
            {
                case 1:
                    return LoggerVerbosity.Minimal;
                case 2:
                    return LoggerVerbosity.Normal;
                case 3:
                    return LoggerVerbosity.Detailed;
                case 0:
                default:
                    return LoggerVerbosity.Quiet;
            }
        }
        static LanguageVersion ConvertLanguageVersion(string versionString)

        {
            if (string.IsNullOrEmpty(versionString))
            {
                return LanguageVersion.Default;
            }
            if (Version.TryParse(versionString, out var semver))
            {
                if (semver.Major < 7)
                {
                    return (LanguageVersion)semver.Major;
                }
                else if (semver.Major == 7)
                {
                    switch (semver.Minor)
                    {
                        case 0:
                            return LanguageVersion.CSharp7;
                        case 1:
                            return LanguageVersion.CSharp7_1;
                        case 2:
                            return LanguageVersion.CSharp7_2;
                        case 3:
                            return LanguageVersion.CSharp7_3;
                        default:
                            return LanguageVersion.Latest;
                    }
                }
                else
                {
                    return LanguageVersion.Latest;
                }
            }
            else if (versionString == "latest")
            {
                return LanguageVersion.Latest;
            }
            else
            {
                return LanguageVersion.Default;
            }
        }
        public static async Task<Compilation> GetCompilationFromProject(string csprojPath, int verbosityLevel,
            Dictionary<string, string> additionalProperties,
            IEnumerable<string> conditionalSymbols)
        {
            // fucking workaround of resolve reference...
            var externalReferences = new List<PortableExecutableReference>();
            {
                var locations = new List<string>();
                locations.Add(typeof(object).Assembly.Location); // mscorlib
                locations.Add(typeof(System.Linq.Enumerable).Assembly.Location); // core

                var xElem = XElement.Load(csprojPath);
                var ns = xElem.Name.Namespace;

                var csProjRoot = Path.GetDirectoryName(csprojPath);
                var framworkRoot = Path.GetDirectoryName(typeof(object).Assembly.Location);

                foreach (var item in xElem.Descendants(ns + "Reference"))
                {
                    var hintPath = item.Element(ns + "HintPath")?.Value;
                    if (hintPath == null)
                    {
                        var path = Path.Combine(framworkRoot, item.Attribute("Include").Value + ".dll");
                        locations.Add(path);
                    }
                    else
                    {
                        locations.Add(Path.Combine(csProjRoot, hintPath));
                    }
                }

                foreach (var item in locations.Distinct())
                {
                    if (File.Exists(item))
                    {
                        externalReferences.Add(MetadataReference.CreateFromFile(item));
                    }
                }
            }

            EnvironmentHelper.Setup();
            var analyzerOptions = new AnalyzerManagerOptions();
            analyzerOptions.LoggerVerbosity = verbosityLevel.ToLoggerVerbosity();
            if (verbosityLevel > 0)
            {
                analyzerOptions.LogWriter = Console.Out;
            }
            var manager = new AnalyzerManager(analyzerOptions);
            var projectAnalyzer = manager.GetProject(csprojPath);
            if (additionalProperties != null)
            {
                foreach (var kv in additionalProperties)
                {
                    projectAnalyzer.SetGlobalProperty(kv.Key, kv.Value);
                }
            }
            var compiledProject = projectAnalyzer.Build().Project;
            var ws = new AdhocWorkspace();
            if (compiledProject == null)
            {
                throw new Exception("project compilation failed");
            }

            var workspace = projectAnalyzer.GetWorkspace();
            workspace.WorkspaceFailed += WorkSpaceFailed;
            var project = workspace.CurrentSolution.Projects.First();
            project = project.AddMetadataReferences(externalReferences);
            var opt = project.ParseOptions as CSharpParseOptions;
            if (opt != null)
            {
                var defineconstants = compiledProject.GetPropertyValue("DefineConstants").Split(';');
                var langVersion = compiledProject.GetPropertyValue("LangVersion");
                var features = compiledProject.GetPropertyValue("Features").Split(';').Select(x =>
                {
                    var kv = x.Split('=', 2);
                    if (kv.Length == 1)
                    {
                        return new KeyValuePair<string, string>(kv[0], "true");
                    }
                    else if (kv.Length > 1)
                    {
                        return new KeyValuePair<string, string>(kv[0], kv[1]);
                    }
                    else
                    {
                        return new KeyValuePair<string, string>(null, null);
                    }
                }).Where(x => x.Key != null).ToArray();
                opt = opt.WithLanguageVersion(ConvertLanguageVersion(langVersion))
                    .WithPreprocessorSymbols(defineconstants.Concat(conditionalSymbols))
                    .WithFeatures(features)
                    ;
                project = project.WithParseOptions(opt);
            }
            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
            return compilation;
        }

        private static void WorkSpaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            Console.WriteLine(e);
        }

        public static IEnumerable<INamedTypeSymbol> GetNamedTypeSymbols(this Compilation compilation)
        {
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semModel = compilation.GetSemanticModel(syntaxTree);

                foreach (var item in syntaxTree.GetRoot()
                    .DescendantNodes()
                    .Select(x => semModel.GetDeclaredSymbol(x))
                    .Where(x => x != null))
                {
                    var namedType = item as INamedTypeSymbol;
                    if (namedType != null)
                    {
                        yield return namedType;
                    }
                }
            }
        }

        public static IEnumerable<INamedTypeSymbol> EnumerateBaseType(this ITypeSymbol symbol)
        {
            var t = symbol.BaseType;
            while (t != null)
            {
                yield return t;
                t = t.BaseType;
            }
        }

        public static AttributeData FindAttribute(this IEnumerable<AttributeData> attributeDataList, string typeName)
        {
            return attributeDataList
                .Where(x => x.AttributeClass.ToDisplayString() == typeName)
                .FirstOrDefault();
        }

        public static AttributeData FindAttributeShortName(this IEnumerable<AttributeData> attributeDataList,
            string typeName)
        {
            return attributeDataList
                .Where(x => x.AttributeClass.Name == typeName)
                .FirstOrDefault();
        }

        public static AttributeData FindAttributeIncludeBasePropertyShortName(this IPropertySymbol property,
            string typeName)
        {
            do
            {
                var data = FindAttributeShortName(property.GetAttributes(), typeName);
                if (data != null) return data;
                property = property.OverriddenProperty;
            } while (property != null);

            return null;
        }

        public static AttributeSyntax FindAttribute(this BaseTypeDeclarationSyntax typeDeclaration, SemanticModel model,
            string typeName)
        {
            return typeDeclaration.AttributeLists
                .SelectMany(x => x.Attributes)
                .Where(x => model.GetTypeInfo(x).Type?.ToDisplayString() == typeName)
                .FirstOrDefault();
        }

        public static INamedTypeSymbol FindBaseTargetType(this ITypeSymbol symbol, string typeName)
        {
            return symbol.EnumerateBaseType()
                .Where(x => x.OriginalDefinition?.ToDisplayString() == typeName)
                .FirstOrDefault();
        }

        public static object GetSingleNamedArgumentValue(this AttributeData attribute, string key)
        {
            foreach (var item in attribute.NamedArguments)
            {
                if (item.Key == key)
                {
                    return item.Value.Value;
                }
            }

            return null;
        }

        public static bool IsNullable(this INamedTypeSymbol symbol)
        {
            if (symbol.IsGenericType)
            {
                if (symbol.ConstructUnboundGenericType().ToDisplayString() == "T?")
                {
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol symbol)
        {
            var t = symbol;
            while (t != null)
            {
                foreach (var item in t.GetMembers())
                {
                    yield return item;
                }
                t = t.BaseType;
            }
        }

        public static IEnumerable<ISymbol> GetAllInterfaceMembers(this ITypeSymbol symbol)
        {
            return symbol.GetMembers()
                .Concat(symbol.AllInterfaces.SelectMany(x => x.GetMembers()));
        }
    }
}