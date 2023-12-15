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
                functionMetadataInfo.RawBindings = new List<IDictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "name", "req" },
                        { "type", "httpTrigger" },
                        { "direction", "in" },
                        { "authLevel", "anonymous" },
                        { "methods", new List<string> { TryGetHttpMethod(invocationExpressionSyntax, out var httpMethod) ? httpMethod : "" } }
                    },
                    new Dictionary<string, object>
                    {
                        { "name", "$return" },
                        { "type", "http" },
                        { "direction", "out" }
                    }
                };
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

        public static bool TryGetHttpMethod(InvocationExpressionSyntax invocation, out string? httpMethod)
        {
            var expression = (MemberAccessExpressionSyntax)invocation.Expression;
            var name = (IdentifierNameSyntax)expression.Name;
            var identifier = name.Identifier;
            httpMethod = MapHttpMethod(identifier.ValueText);
            return httpMethod != null;

            static string? MapHttpMethod(string mapMethodName) => mapMethodName switch
            {
                "MapGet" => "GET",
                "MapPost" => "POST",
                "MapPut" => "PUT",
                "MapDelete" => "DELETE",
                "MapPatch" => "PATCH",
                _ => null
            };
        }
    }
}
