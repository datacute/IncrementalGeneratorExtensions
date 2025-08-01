﻿// <auto-generated>
// This file is part of the Datacute.IncrementalGeneratorExtensions package.
// It is included as a source file and should not be modified.
// </auto-generated>

#if !DATACUTE_EXCLUDE_SOURCETEXTGENERATORBASE && !DATACUTE_EXCLUDE_ATTRIBUTECONTEXTANDDATA && !DATACUTE_EXCLUDE_TYPECONTEXT && !DATACUTE_EXCLUDE_INDENTINGLINEAPPENDER
using System;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Datacute.IncrementalGeneratorExtensions
{
    /// <summary>
    /// Base class for source text generators that produce source code based on a marker attribute.
    /// </summary>
    /// <typeparam name="T">Type of the data associated with the attribute context, which must implement <see cref="IEquatable{T}"/>.</typeparam>
    public class SourceTextGeneratorBase<T> 
        where T : IEquatable<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SourceTextGeneratorBase{T}"/> class with the provided context and cancellation token.
        /// </summary>
        /// <param name="contextAndData">The context and data associated with the type, including namespace and containing types.</param>
        /// <param name="cancellationToken">The cancellation token to observe for cancellation requests.</param>
        protected SourceTextGeneratorBase(
            in AttributeContextAndData<T> contextAndData,
            in CancellationToken cancellationToken)
        {
            ContainingNamespaceIsGlobalNamespace = contextAndData.ContainingNamespaceIsGlobalNamespace;
            ContainingNamespaceDisplayString = contextAndData.ContainingNamespaceDisplayString;
            Context = contextAndData.Context;
            HasContainingTypes = contextAndData.HasContainingTypes;
            ContainingTypes = contextAndData.ContainingTypes;
            NullableEnabled = contextAndData.IsNullableContextEnabled;
            FileScopedNamespace = contextAndData.IsInFileScopedNamespace;
            Token = cancellationToken;
            Buffer = new IndentingLineAppender();
        }

        /// <summary>
        /// Indicates whether the containing namespace is the global namespace.
        /// </summary>
        protected virtual bool ContainingNamespaceIsGlobalNamespace { get; }
        /// <summary>
        /// The display string of the containing namespace.
        /// </summary>
        protected virtual string ContainingNamespaceDisplayString { get; }
        /// <summary>
        /// The information about the type being generated.
        /// </summary>
        protected virtual TypeContext Context { get; }
        /// <summary>
        /// Indicates whether the type has containing types (e.g., nested classes).
        /// </summary>
        protected virtual bool HasContainingTypes { get; }
        /// <summary>
        /// The collection of information about containing types, if any.
        /// </summary>
        protected virtual EquatableImmutableArray<TypeContext> ContainingTypes { get; }
        /// <summary>
        /// The auto-indenting string builder wrapper used to build the source text.
        /// </summary>
        protected virtual IndentingLineAppender Buffer { get; }
        /// <summary>
        /// Indicates whether nullable reference types are enabled in the context.
        /// </summary>
        protected virtual bool NullableEnabled { get; }
        /// <summary>
        /// Indicates whether the namespace is file-scoped.
        /// </summary>
        protected virtual bool FileScopedNamespace { get; }
        /// <summary>
        /// The cancellation token to observe for cancellation requests during source generation.
        /// </summary>
        protected virtual CancellationToken Token { get; }
        
        /// <summary>
        /// The comment that will be used at the top of the source file to indicate that this is an automatically generated file.
        /// </summary>
        protected virtual string AutoGeneratedComment => /* language=c# */
            @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a source code generator.
// </auto-generated>
//------------------------------------------------------------------------------";
        
        /// <summary>
        /// Gets the generated source text as a <see cref="SourceText"/> object.
        /// </summary>
        /// <returns>A <see cref="SourceText"/> object containing the generated source code.</returns>
        public virtual SourceText GetSourceText()
        {
            AppendSource();
            return SourceText.From(Buffer.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Generates all the source code, appending it to the internal buffer.
        /// </summary>
        protected virtual void AppendSource()
        {
            PrepareForGeneration();

            AppendAutoGeneratedComment();
            AppendNullableEnable();
            AppendStartNamespace();
            AppendContainingTypes();
            AppendDocComments();
            AppendTypeDeclaration();
            AppendStartBlock();

            AppendCustomMembers();

            AppendEndBlock();
            AppendContainingTypesEndBlock();
            AppendEndNamespace();

            AppendDiagnosticLogs();
        }
        
        /// <summary>
        /// Prepares the generator for source code generation.
        /// </summary>
        /// <remarks>
        /// This method is called before any source code is generated.
        /// Override this method to perform any additional setup required before the source is generated.
        /// Overriding implementations may call the base implementation to check for cancellation, and clears the internal buffer,
        /// </remarks>
        protected virtual void PrepareForGeneration()
        {
            Token.ThrowIfCancellationRequested();
            Buffer.Clear();
        }

        /// <summary>
        /// Appends the auto-generated comment to the source code.
        /// </summary>
        protected virtual void AppendAutoGeneratedComment()
        {
            Buffer.AppendLine(AutoGeneratedComment);
            Buffer.AppendLine();
        }

        /// <summary>
        /// Appends the nullable enable directive if nullable reference types are enabled in the context.
        /// </summary>
        protected virtual void AppendNullableEnable()
        {
            if (NullableEnabled)
            {
                Buffer.AppendLine("#nullable enable");
                Buffer.AppendLine();
            }
        }

        /// <summary>
        /// Appends the namespace declaration to the source code.
        /// </summary>
        protected virtual void AppendStartNamespace()
        {
            if (ContainingNamespaceIsGlobalNamespace) return;

            Buffer.Append("namespace ");
            Buffer.Append(ContainingNamespaceDisplayString);
            if (FileScopedNamespace)
            {
                Buffer.Append(';').AppendLine();
                Buffer.AppendLine();
            }
            else
            {
                AppendStartBlock();
            }
        }

        /// <summary>
        /// Appends the documentation comments for the type being generated.
        /// </summary>
        /// <remarks>
        /// Override this method to append custom documentation comments for the type.
        /// By default, this method does nothing, so overriding implementations do not need to call the base method.
        /// </remarks>
        protected virtual void AppendDocComments()
        {
        }

        /// <summary>
        /// Appends the declarations for the containing types and opens their code blocks.
        /// </summary>
        protected virtual void AppendContainingTypes()
        {
            if (!HasContainingTypes) return;

            foreach (var containingType in ContainingTypes)
            {
                Buffer.AppendLine(containingType.TypeDeclaration());
                AppendStartBlock();
            }
        }

        /// <summary>
        /// Appends the type declaration for the main type being generated.
        /// </summary>
        protected virtual void AppendTypeDeclaration()
        {
            Buffer.AppendLine(Context.TypeDeclaration());
        }

        /// <summary>
        /// Appends a start block for the type being generated, typically just an opening brace `{`.
        /// </summary>
        protected virtual void AppendStartBlock()
        {
            Buffer.AppendStartBlock();
        }

        /// <summary>
        /// Appends the custom members for the type being generated.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is intended to be overridden by subclasses to add the body of the generated type,
        /// such as properties, methods, and fields. It is called after the type declaration and
        /// the opening brace have been appended to the source.
        /// </para>
        /// <para>
        /// By default, this method does nothing, so overriding implementations do not need to call the base method.
        /// </para>
        /// </remarks>
        protected virtual void AppendCustomMembers()
        {
        }

        /// <summary>
        /// Appends the end block for the type being generated, typically just a closing brace `}`.
        /// </summary>
        protected virtual void AppendEndBlock()
        {
            Buffer.AppendEndBlock();
        }

        /// <summary>
        /// Appends the end block for all containing types, closing their code blocks.
        /// </summary>
        protected virtual void AppendContainingTypesEndBlock()
        {
            // Close parent classes if any
            if (!HasContainingTypes) return;

            for (int i = 0; i < ContainingTypes.Count; i++)
            {
                AppendEndBlock();
            }
        }

        /// <summary>
        /// Appends the end of the namespace declaration, closing the namespace block if it is not the global namespace or file-scoped.
        /// </summary>
        protected virtual void AppendEndNamespace()
        {
            if (ContainingNamespaceIsGlobalNamespace) return;

            if (FileScopedNamespace) return;

            AppendEndBlock();
        }

        /// <summary>
        /// Appends any diagnostic logs or additional information to the generated source code.
        /// </summary>
        /// <remarks>
        /// This method is intended to be overridden by subclasses to append any diagnostic logs or additional information.
        /// It is called at the end of the source generation process, after all other content has been appended.
        /// By default, this method does nothing.
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void AppendDiagnosticLogs()
        /// {
        ///     if (_contextData.OutputDiagnosticTraceLog)
        ///     {
        ///         Buffer.AppendLine();
        ///         Buffer.Direct.AppendDiagnosticsComment(CustomTrackingNameDescriptions.EventNameMap);
        ///         LightweightTrace.Add(CustomTrackingNames.DiagnosticTraceLogWritten);
        ///     }
        /// }
        /// </code>
        /// </example>
        protected virtual void AppendDiagnosticLogs()
        {
        }
    }
}
#endif