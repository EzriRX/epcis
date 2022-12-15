﻿using FasTnT.Application.UseCases.Subscriptions;
using FasTnT.Host.Features.v2_0.Endpoints.Interfaces;
using FasTnT.Host.Features.v2_0.Endpoints.Interfaces.Utils;
using FasTnT.Host.Features.v2_0.Subscriptions;

namespace FasTnT.Host.Features.v2_0.Endpoints;

public static class SubscriptionEndpoints
{
    public static IEndpointRouteBuilder AddRoutes(IEndpointRouteBuilder app)
    {
        app.Get("v2_0/queries/{query}/subscriptions", HandleSubscriptionQuery).RequireAuthorization("query");
        app.Get("v2_0/queries/{query}/subscriptions/{name}", HandleSubscriptionDetailQuery).RequireAuthorization("query").WithName("SubscriptionDetail");
        app.MapDelete("v2_0/queries/{query}/subscriptions/{name}", HandleDeleteSubscription).RequireAuthorization("query");
        app.Post("v2_0/queries/{query}/subscriptions", HandleSubscribeRequest).RequireAuthorization("query");

        return app;
    }

    private static async Task<IResult> HandleSubscriptionQuery(string query, IListSubscriptionsHandler handler, CancellationToken cancellationToken)
    {
        var response = await handler.ListSubscriptionsAsync(query, cancellationToken);

        return EpcisResults.Ok(new ListSubscriptionsResult(response));
    }

    private static async Task<IResult> HandleSubscriptionDetailQuery(string name, IGetSubscriptionDetailsHandler handler, CancellationToken cancellationToken)
    {
        var response = await handler.GetSubscriptionDetailsAsync(name, cancellationToken);

        return EpcisResults.Ok(new SubscriptionDetailsResult(response));
    }

    private static async Task<IResult> HandleDeleteSubscription(string name, IDeleteSubscriptionHandler handler, CancellationToken cancellationToken)
    {
        await handler.DeleteSubscriptionAsync(name, cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> HandleSubscribeRequest(string query, SubscriptionRequest request, IRegisterSubscriptionHandler handler, CancellationToken cancellationToken)
    {
        request.Subscription.QueryName = query;
        request.Subscription.Name = Guid.NewGuid().ToString();

        var response = await handler.RegisterSubscriptionAsync(request.Subscription, JsonResultSender.Instance, cancellationToken);

        return Results.Created($"v2_0/queries/{query}/subscriptions/{response.Name}", null);
    }
}