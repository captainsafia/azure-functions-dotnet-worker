using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators;

[Generator]
public partial class MapActionFunctionMetadataProviderGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new MapActionInvocationSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not MapActionInvocationSyntaxReceiver mapActionInvocationSyntaxReceiver || mapActionInvocationSyntaxReceiver.MapActions.Count == 0)
        {
            return;
        }

        context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(Constants.BuildProperties.EnableSourceGen, out var sourceGenSwitch);

        bool.TryParse(sourceGenSwitch, out bool enableSourceGen);

        if (!enableSourceGen)
        {
            return;
        }

        var p = new Parser(context);
        var functionMetadataInfo = p.GetFunctionMetadataInfo(mapActionInvocationSyntaxReceiver.MapActions);

        if (functionMetadataInfo.Count > 0)
        {
            FunctionMetadataProviderGenerator.Emitter e = new FunctionMetadataProviderGenerator.Emitter();

            string result = e.Emit(context, functionMetadataInfo, false);

            context.AddSource("MapAction" + Constants.FileNames.GeneratedFunctionMetadata, SourceText.From(result, Encoding.UTF8));
        }
    }
}
