// Copyright 2020 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XmlSchemaClassGenerator;

namespace NodaTime.Demo
{
    public partial class XmlSchemaDemo
    {
        private class MemoryOutputWriter : OutputWriter
        {
            private readonly List<(string namespaceName, string source)> content = new List<(string namespaceName, string source)>();

            public IEnumerable<(string namespaceName, string source)> Content => content;

            public override void Write(CodeNamespace codeNamespace)
            {
                var compileUnit = new CodeCompileUnit();
                compileUnit.Namespaces.Add(codeNamespace);

                using var writer = new StringWriter();
                Write(writer, compileUnit);
                content.Add((codeNamespace.Name, writer.ToString()));
            }
        }

        private class NodaTimeNamingProvider : NamingProvider
        {
            public NodaTimeNamingProvider(NamingScheme namingScheme) : base(namingScheme)
            {
            }

            protected override string QualifiedNameToTitleCase(XmlQualifiedName qualifiedName)
            {
                if (qualifiedName.Namespace == Xml.XmlSchemaDefinition.NodaTimeXmlNamespace.Namespace)
                {
                    return qualifiedName.Name;
                }
                return base.QualifiedNameToTitleCase(qualifiedName);
            }
        }

        /// <summary>
        /// An implementation of the <see cref="IXmlSchemaAssemblyCreator"/> interface using the open source
        /// <see cref="XmlSchemaClassGenerator"/> library and Roslyn (<see cref="Microsoft.CodeAnalysis"/>).
        /// </summary>
        private class XmlSchemaClassGeneratorAssemblyCreator : IXmlSchemaAssemblyCreator
        {
            public Assembly CreateAssembly(string namespaceName, XmlSchemaSet schemaSet)
            {
                var writer = new MemoryOutputWriter();
                var namespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        [new NamespaceKey(Xml.XmlSchemaDefinition.NodaTimeXmlNamespace.Namespace)] = nameof(NodaTime),
                    }
                    .ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = namespaceName }.NamespaceProvider.GenerateNamespace);
                var generator = new Generator
                {
                    OutputWriter = writer,
                    GenerateNullables = true,
                    NamespaceProvider = namespaceProvider,
                    NamingProvider = new NodaTimeNamingProvider(NamingScheme.PascalCase)
                };

                generator.Generate(schemaSet);

                var systemDependencies = new List<string>
                {
#if NETFRAMEWORK
                    "netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
                    "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                    "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                    "System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
#else
                    "netstandard",
                    "System.ComponentModel.Primitives",
                    "System.Diagnostics.Tools",
                    "System.Private.CoreLib",
                    "System.Private.Xml",
                    "System.Runtime",
                    "System.Xml.XmlSerializer",
#endif
                };
                var references = systemDependencies.Select(e => Assembly.Load(e).Location)
                    .Append(typeof(Instant).Assembly.Location)
                    .Select(p => MetadataReference.CreateFromFile(p));
                var options = new CSharpParseOptions(kind: SourceCodeKind.Regular, languageVersion: LanguageVersion.Latest);
                var syntaxTrees = writer.Content.Where(e => e.namespaceName != nameof(NodaTime))
                    .Select(e => CSharpSyntaxTree.ParseText(e.source, options));
                var compilation = CSharpCompilation.Create(namespaceName, syntaxTrees)
                    .AddReferences(references)
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                using var dllStream = new MemoryStream();
                var result = compilation.Emit(dllStream);
                if (result.Success)
                    return Assembly.Load(dllStream.ToArray());
                throw new AggregateException(result.Diagnostics.Select(e => new Exception(e.ToString())));
            }
        }
    }
}