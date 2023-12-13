using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators;

public partial class MapActionFunctionMetadataProviderGenerator
{
    internal sealed class Parser
    {
        private readonly GeneratorExecutionContext _context;

        public Parser(GeneratorExecutionContext context)
        {
            _context = context;
        }

        public IReadOnlyList<GeneratorFunctionMetadata> GetFunctionMetadataInfo(List<InvocationExpressionSyntax> invocationExpressionSyntaxes)
        {
            var functionMetadataInfos = new List<GeneratorFunctionMetadata>();

            foreach (var invocationExpressionSyntax in invocationExpressionSyntaxes)
            {
                var functionMetadataInfo = new GeneratorFunctionMetadata();

                functionMetadataInfo.IsHttpTrigger = true;
                functionMetadataInfo.Name = TryGetRoutePatternMethod(invocationExpressionSyntax, out var routePattern) ? routePattern : null;
                functionMetadataInfo.ScriptFile = _context.Compilation.AssemblyName + ".dll";

                functionMetadataInfos.Add(functionMetadataInfo);
            }

            return functionMetadataInfos;
        }

        public static bool TryGetRoutePatternMethod(InvocationExpressionSyntax invocation, out string? routePattern)
        {
            routePattern = null;
            var argument = invocation.ArgumentList.Arguments.FirstOrDefault();
            routePattern = ((LiteralExpressionSyntax)argument?.Expression)?.Token.Text;
            return true;
        }
    }
}
