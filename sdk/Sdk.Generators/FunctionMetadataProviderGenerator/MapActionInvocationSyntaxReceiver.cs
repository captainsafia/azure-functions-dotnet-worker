using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators;

public class MapActionInvocationSyntaxReceiver : ISyntaxReceiver
{
    private static readonly List<string> KnownMethods = [
        "MapGet",
        "MapPost",
        "MapPut",
        "MapDelete",
        "MapPatch"
    ];

    public List<InvocationExpressionSyntax> MapActions { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name: IdentifierNameSyntax
                {
                    Identifier: { ValueText: var method }
                }
            },
            ArgumentList: { Arguments: { Count: 2 } args }
        } mapActionCall && KnownMethods.Contains(method))
        {
            MapActions.Add(mapActionCall);
        }
    }
}
