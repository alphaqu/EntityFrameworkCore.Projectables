using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityFrameworkCore.Projectables.Generator;

public partial class ExpressionSyntaxRewriter
{

    public ObjectCreator CreateObjectCreator(
        MemberAccessExpressionSyntax memberAccessExpressionSyntax
    )
    {
        var objectType =
            ((GenericNameSyntax)memberAccessExpressionSyntax.Expression)
            .TypeArgumentList.Arguments[0];
        var objectSymbol = _semanticModel.GetSymbolInfo(objectType).Symbol!;

        return ObjectCreator.ForType(
            (INamedTypeSymbol)objectSymbol,
            (TypeSyntax)Visit(objectType)
        );
    }

    public ExpressionSyntax CompileMethod(ExpressionSyntax syntax)
    {
        // This is for references pointing to methods, where we inline them.
        var function = GetMethodIdentifier(syntax);
        if (function != null)
        {
            var functionSymbol = _semanticModel.GetSymbolInfo(function).Symbol;
            if (functionSymbol is IMethodSymbol methodSymbol)
            {
                // TODO ensure this symbol contains the Projectable attribute.
                var methodExpression = GetMethodExpression(methodSymbol);
                if (methodExpression is not null)
                {
                    var rewriter = new ExpressionSyntaxRewriter(
                        methodSymbol.ContainingType,
                        _nullConditionalRewriteSupport,
                        _semanticModel,
                        _context
                    );

                    // We inline the methods content.
                    return (ExpressionSyntax)rewriter.Visit(methodExpression);
                }
            }
        }

        // This is for everything else
        return (ExpressionSyntax)Visit(syntax);
    }

    private SyntaxNode? ReportError(SyntaxNode node, string reason)
    {
        _context.ReportDiagnostic(
            Diagnostic.Create(
                Diagnostics.ExtendUnsupported,
                node.GetLocation(),
                node,
                reason
            )
        );
        return node;
    }

    public static ExpressionSyntax? GetMethodIdentifier(SyntaxNode syntax)
    {

        if (syntax is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess;
        }
        if (syntax is IdentifierNameSyntax nameSyntax)
        {
            return nameSyntax;
        }

        if (syntax is InvocationExpressionSyntax invocationSyntax)
        {
            return GetMethodIdentifier(invocationSyntax.Expression);
        }

        return null;
    }

    public static IdentifierNameSyntax? GetIdentifier(SyntaxNode syntax)
    {
        if (syntax is IdentifierNameSyntax identifierName)
        {
            return identifierName;
        }
        if (syntax is AnonymousObjectMemberDeclaratorSyntax
            anonymousObjectMember)
        {

            if (anonymousObjectMember.NameEquals is not null)
            {
                return anonymousObjectMember.NameEquals.Name;
            }

            return GetIdentifier(anonymousObjectMember.Expression);
        }
        if (syntax is AssignmentExpressionSyntax assignmentExpressionSyntax)
        {
            return GetIdentifier(assignmentExpressionSyntax.Left);
        }
        if (syntax is MemberAccessExpressionSyntax memberAccess)
        {
            return GetIdentifier(memberAccess.Name);
        }

        return null;
    }


    private static ExpressionSyntax? GetMethodExpression(
        IMethodSymbol methodSymbol
    )
    {
        var syntaxReference =
            methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxReference == null)
            return null;

        var syntaxNode = syntaxReference.GetSyntax();
        if (syntaxNode is MethodDeclarationSyntax declaration)
        {
            syntaxNode = declaration.ExpressionBody;
        }

        if (syntaxNode is ArrowExpressionClauseSyntax methodDeclaration)
        {
            return methodDeclaration.Expression;
        }
        return null;
    }

    public static List<IPropertySymbol> GetAllPropertiesIncludingBase(
        INamedTypeSymbol? classSymbol
    )
    {
        var properties = new List<IPropertySymbol>();

        while (classSymbol != null)
        {
            foreach (var propertySymbol in classSymbol
                         .GetMembers()
                         .OfType<IPropertySymbol>()
                         .Reverse())
            {
                properties.Insert(0, propertySymbol);
            }
            classSymbol = classSymbol.BaseType;
        }

        return properties;
    }


}

public class ObjectCreator
{

    public Dictionary<string, ExpressionSyntax>? Initializer
    {
        get;
        private set;
    }
    public List<IPropertySymbol> Properties { get; private set; } = [];
    public INamedTypeSymbol TypeSymbol { get; private set; } = null!;
    public TypeSyntax TypeSyntax { get; private set; } = null!;

    private ObjectCreator() {}

    public static ObjectCreator ForType(
        INamedTypeSymbol typeSymbol,
        TypeSyntax typeSyntax
    )
    {


        var properties =
            ExpressionSyntaxRewriter.GetAllPropertiesIncludingBase(typeSymbol);
        return new ObjectCreator() {
            Properties = properties,
            TypeSymbol = typeSymbol,
            TypeSyntax = typeSyntax
        };
    }



    public bool Append(SyntaxNode syntax)
    {
        if (syntax is ObjectCreationExpressionSyntax objectCreation)
        {
            if (objectCreation.Initializer is not null)
            {
                foreach (var expression in objectCreation.Initializer
                             .Expressions)
                {
                    _addInit(expression);
                }

                return true;
            }

        }
        if (syntax is AnonymousObjectCreationExpressionSyntax
            anonymousObjectCreation)
        {

            foreach (var expression in anonymousObjectCreation.Initializers)
            {
                _addInit(expression);
            }

            return true;
        }

        return false;
    }

    public ExpressionSyntax Generate(
        SourceProductionContext context,
        SyntaxNode node
    )
    {

        var fields = new Dictionary<string, ExpressionSyntax>();

        if (Initializer is not null)
        {
            foreach (var propertySymbol in Properties)
            {
                if (Initializer.TryGetValue(
                        propertySymbol.Name,
                        out var initializer
                    ))
                {
                    fields[propertySymbol.Name] = initializer;
                }
                else
                {
                    if (propertySymbol.IsRequired)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Diagnostics.ExtendUnsupported,
                                node.GetLocation(),
                                node,
                                $"Property '{propertySymbol.Name}' is required, but does not have an assigner."
                            )
                        );
                    }
                }
            }
        }

        return SyntaxFactory.ObjectCreationExpression(
            TypeSyntax,
            null,
            SyntaxFactory.InitializerExpression(
                SyntaxKind.ObjectInitializerExpression,
                SeparatedSyntaxList.Create<ExpressionSyntax>(
                    fields.Values.ToArray()
                )
            )
        );
    }


    private void _addInit(SyntaxNode syntax)
    {
        Initializer ??= new Dictionary<string, ExpressionSyntax>();

        var initializer = this.Initializer!;
        var identifier = ExpressionSyntaxRewriter.GetIdentifier(syntax)!;
        initializer[identifier.ToString()] =
            CreateAssignment(identifier, syntax)!;
    }

    private static ExpressionSyntax GetAssignmentExpression(SyntaxNode syntax)
    {
        if (syntax is AnonymousObjectMemberDeclaratorSyntax
            anonymousObjectMember)
        {
            return anonymousObjectMember.Expression;
        }

        if (syntax is AssignmentExpressionSyntax assignmentExpressionSyntax)
        {
            return assignmentExpressionSyntax.Right;
        }

        if (syntax is ExpressionSyntax expression)
        {
            return expression;
        }

        throw new NotSupportedException(
            $"'{syntax}' ({syntax.GetType()}) is not supported"
        );
    }

    private static ExpressionSyntax? CreateAssignment(
        IdentifierNameSyntax identifier,
        SyntaxNode syntax
    )
    {
        var expression = GetAssignmentExpression(syntax);

        return SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            identifier,
            expression
        );
    }

}