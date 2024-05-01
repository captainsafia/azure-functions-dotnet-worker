using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.AspNetMiddleware;

namespace Microsoft.Azure.Functions.OpenApi;

internal class FunctionsMetadataProvider(FunctionsEndpointDataSource endpointDataSource) : IApiDescriptionProvider
{
    public int Order => 100;

    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
        var endpoints = endpointDataSource.Endpoints;
        foreach (var endpoint in endpoints)
        {
            if (endpoint is not RouteEndpoint)
            {
                continue;
            }

            var httpMethods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;
            if (httpMethods == null)
            {
                continue;
            }
            foreach (var httpMethod in httpMethods)
            {
                context.Results.Add(new ApiDescription
                {
                    ActionDescriptor = new ActionDescriptor
                    {
                        RouteValues = new Dictionary<string, string?>(),
                        DisplayName = endpoint.DisplayName,
                    },
                    HttpMethod = httpMethod,
                    RelativePath = ((RouteEndpoint)endpoint).RoutePattern.RawText,
                });
            }
        }
    }
}
