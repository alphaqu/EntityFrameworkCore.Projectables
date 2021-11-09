﻿using EntityFrameworkCore.Projectables.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EntityFrameworkCore.Projectables.Generator
{
    [Generator]
    public class ProjectionExpressionGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            {
                return;
            }

            if (receiver.Candidates?.Count > 0)
            {
                var projectables = receiver.Candidates
                    .Select(x => ProjectableInterpreter.GetDescriptor(x, context))
                    .Where(x => x is not null)
                    .Select(x => x!);

                var resultBuilder = new StringBuilder();

                foreach (var projectable in projectables)
                {
                    if (projectable.MemberName is null)
                    {
                        throw new InvalidOperationException("Expected a memberName here");
                    }

                    resultBuilder.Clear();

                    if (projectable.UsingDirectives is not null)
                    {
                        foreach (var usingDirective in projectable.UsingDirectives.Distinct())
                        {
                            resultBuilder.AppendLine(usingDirective);
                        }
                    }

                    if (projectable.TargetClassNamespace is not null)
                    {
                        var targetClassUsingDirective = $"using {projectable.TargetClassNamespace};";

                        if (!projectable.UsingDirectives.Contains(targetClassUsingDirective))
                        {
                            resultBuilder.AppendLine(targetClassUsingDirective);
                        }
                    }

                    if (projectable.ClassNamespace is not null && projectable.ClassNamespace != projectable.TargetClassNamespace)
                    {
                        var classUsingDirective = $"using {projectable.ClassNamespace};";

                        if (!projectable.UsingDirectives.Contains(classUsingDirective))
                        {
                            resultBuilder.AppendLine(classUsingDirective);
                        }
                    }

                    var generatedClassName = ProjectionExpressionClassNameGenerator.GenerateName(projectable.ClassNamespace, projectable.NestedInClassNames, projectable.MemberName);

                    var lambdaTypeArguments = SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SeparatedList(
                            projectable.ParametersList?.Parameters.Where(p => p.Type is not null).Select(p => p.Type!)
                        )
                    );

                    resultBuilder.Append($@"
namespace EntityFrameworkCore.Projectables.Generated
#nullable disable
{{
    public static class {generatedClassName}
    {{
        public static System.Linq.Expressions.Expression<System.Func<{lambdaTypeArguments.Arguments}, {projectable.ReturnTypeName}>> Expression{(projectable.TypeParameterList?.Parameters.Any() == true ? projectable.TypeParameterList.ToString() : string.Empty)}()");

                    if (projectable.ConstraintClauses is not null)
                    {
                        foreach (var constraintClause in projectable.ConstraintClauses)
                        {
                            resultBuilder.Append($@"
            {constraintClause}");
                        }
                    }

                    resultBuilder.Append($@"
        {{
            return {projectable.ParametersList} => 
                {projectable.Body};
        }}
    }}
}}");


                    context.AddSource($"{generatedClassName}_Generated", SourceText.From(resultBuilder.ToString(), Encoding.UTF8));
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context) =>
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }
}
