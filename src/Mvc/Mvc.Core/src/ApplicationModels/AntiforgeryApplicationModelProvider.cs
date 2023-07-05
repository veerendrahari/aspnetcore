// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Linq;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Core.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

internal sealed class AntiforgeryApplicationModelProvider(IOptions<MvcOptions> mvcOptions, ILogger<AntiforgeryMiddlewareAuthorizationFilter> logger) : IApplicationModelProvider
{
    private readonly MvcOptions _mvcOptions = mvcOptions.Value;
    private readonly ILogger<AntiforgeryMiddlewareAuthorizationFilter> _logger = logger;
    public int Order => -1000 + 10;

    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        foreach (var controllerModel in context.Result.Controllers)
        {
            var antiforgeryMetadata = controllerModel.Attributes.OfType<IAntiforgeryMetadata>();
            if (antiforgeryMetadata.Any() && _mvcOptions.EnableEndpointRouting)
            {
                controllerModel.Filters.Add(new AntiforgeryMiddlewareAuthorizationFilter(_logger));
            }

            foreach (var actionModel in controllerModel.Actions)
            {
                var actionAntiforgeryMetadata = actionModel.Attributes.OfType<IAntiforgeryMetadata>();
                if (actionAntiforgeryMetadata.Any() && _mvcOptions.EnableEndpointRouting)
                {
                    actionModel.Filters.Add(new AntiforgeryMiddlewareAuthorizationFilter(_logger));
                }
            }
        }
    }
    public void OnProvidersExecuted(ApplicationModelProviderContext context)
    {
        // Intentionally empty.
    }
}
