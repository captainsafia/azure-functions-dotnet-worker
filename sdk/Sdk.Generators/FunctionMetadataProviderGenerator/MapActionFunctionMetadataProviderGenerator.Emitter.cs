﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators;

public partial class MapActionFunctionMetadataProviderGenerator
{
    internal sealed class Emitter
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        };

        public string Emit(GeneratorExecutionContext context, IReadOnlyList<GeneratorFunctionMetadata> funcMetadata, bool includeAutoRegistrationCode)
        {
            string functionMetadataInfo = AddFunctionMetadataInfo(funcMetadata, context.CancellationToken);

            return $$"""
                        // <auto-generated/>
                        using System;
                        using System.Collections.Generic;
                        using System.Collections.Immutable;
                        using System.Text.Json;
                        using System.Threading.Tasks;
                        using Microsoft.Azure.Functions.Worker;
                        using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
                        using Microsoft.Extensions.DependencyInjection;
                        using Microsoft.Extensions.Hosting;

                        namespace {{FunctionsUtil.GetNamespaceForGeneratedCode(context)}}
                        {
                            /// <summary>
                            /// Custom <see cref="IFunctionMetadataProvider"/> implementation that returns function metadata definitions for the current worker."/>
                            /// </summary>
                            [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                            [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
                            public class GeneratedMapActionFunctionMetadataProvider : IFunctionMetadataProvider
                            {
                                /// <inheritdoc/>
                                public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
                                {
                                    var metadataList = new List<IFunctionMetadata>();
                        {{functionMetadataInfo}}
                                    return Task.FromResult(metadataList.ToImmutableArray());
                                }
                            }

                            /// <summary>
                            /// Extension methods to enable registration of the custom <see cref="IFunctionMetadataProvider"/> implementation generated for the current worker.
                            /// </summary>
                            public static class WorkerHostBuilderMapActionFunctionMetadataProviderExtension
                            {
                                ///<summary>
                                /// Adds the GeneratedMapActionFunctionMetadataProvider to the service collection.
                                /// During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
                                ///</summary>
                                public static IHostBuilder ConfigureGeneratedMapActionFunctionMetadataProvider(this IHostBuilder builder)
                                {
                                    builder.ConfigureServices(s => 
                                    {
                                        s.AddSingleton<IFunctionMetadataProvider, GeneratedMapActionFunctionMetadataProvider>();
                                    });
                                    return builder;
                                }
                            }{{GetAutoConfigureStartupClass(includeAutoRegistrationCode)}}
                        }
                        """;
        }

        private static string GetAutoConfigureStartupClass(bool includeAutoRegistrationCode)
        {
            if (includeAutoRegistrationCode)
            {
                string result = $$"""

                                    /// <summary>
                                    /// Auto startup class to register the custom <see cref="IFunctionMetadataProvider"/> implementation generated for the current worker.
                                    /// </summary>
                                    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                                    public class MapActionFunctionMetadataProviderAutoStartup : IAutoConfigureStartup
                                    {
                                        /// <summary>
                                        /// Configures the <see cref="IHostBuilder"/> to use the custom <see cref="IFunctionMetadataProvider"/> implementation generated for the current worker.
                                        /// </summary>
                                        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> instance to use for service registration.</param>
                                        public void Configure(IHostBuilder hostBuilder)
                                        {
                                            hostBuilder.ConfigureGeneratedMapActionFunctionMetadataProvider();
                                        }
                                    }
                                """;

                return result;
            }
            return "";
        }
        private string AddFunctionMetadataInfo(IReadOnlyList<GeneratorFunctionMetadata> functionMetadata, CancellationToken cancellationToken)
        {
            var functionCount = 0;
            var builder = new StringBuilder();

            foreach (var function in functionMetadata)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // we're going to base variable names on Function[Num] because some function names have characters we can't use for a dotnet variable
                var functionVariableName = "Function" + functionCount.ToString();
                var functionBindingsListVarName = functionVariableName + "RawBindings";
                var bindingInfo = BuildBindingInfo(functionBindingsListVarName, function.RawBindings);

                builder.AppendLine(
                $$"""
                            var {{functionBindingsListVarName}} = new List<string>();
                {{bindingInfo}}
                            var {{functionVariableName}} = new DefaultFunctionMetadata
                            {
                                Language = "{{Constants.Languages.DotnetIsolated}}",
                                Name = {{function.Name.Replace("/", "")}},
                                EntryPoint = "AspNetIntegration.NoOpTrigger.Run",
                                RawBindings = {{functionBindingsListVarName}},
                """);

                builder.Append(BuildRetryOptions(function.Retry));

                builder.AppendLine($$"""                
                                ScriptFile = "{{function.ScriptFile}}"
                """);
                builder.AppendLine($$"""            };""");
                builder.AppendLine($$"""            metadataList.Add({{functionVariableName}});""");

                functionCount++;
            }

            return builder.ToString();
        }

        private string BuildBindingInfo(string bindingListVariableName, IList<IDictionary<string, object>> bindings)
        {
            var builder = new StringBuilder();
            foreach (var binding in bindings)
            {
                var jsonBinding = JsonSerializer.Serialize(binding, _jsonOptions);
                builder.AppendLine($"""            {bindingListVariableName}.Add(@"{jsonBinding.Replace("\"", "\"\"")}");""");
            }

            return builder.ToString();
        }

        private StringBuilder BuildRetryOptions(GeneratorRetryOptions? retry)
        {
            var builder = new StringBuilder();

            if (retry?.Strategy is RetryStrategy.FixedDelay)
            {
                builder.AppendLine($$"""
                                Retry = new DefaultRetryOptions
                                {
                                    MaxRetryCount = {{retry!.MaxRetryCount}},
                                    DelayInterval = TimeSpan.Parse("{{retry.DelayInterval}}")
                                },
                """);
            }
            else if (retry?.Strategy is RetryStrategy.ExponentialBackoff)
            {
                builder.AppendLine($$"""
                                    Retry = new DefaultRetryOptions
                                    {
                                        MaxRetryCount = {{retry!.MaxRetryCount}},
                                        MinimumInterval = TimeSpan.Parse("{{retry.MinimumInterval}}"),
                                        MaximumInterval = TimeSpan.Parse("{{retry.MaximumInterval}}")
                                    },
                    """);
            }

            return builder;
        }
    }

}
