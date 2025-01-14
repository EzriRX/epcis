﻿using FasTnT.Domain.Exceptions;
using FasTnT.Host.Features.v1_2.Endpoints.Interfaces.Utils;

namespace FasTnT.Host.Features.v1_2.Extensions;

public class SoapActionBuilder
{
    private readonly Dictionary<string, Delegate> _mappedActions = new();

    public void On<TAction>(Delegate requestDelegate) => On(typeof(TAction).Name, requestDelegate);
    public void On(string action, Delegate requestDelegate) => _mappedActions[action] = requestDelegate;

    internal Task<IResult> SoapAction(SoapEnvelope envelope, HttpContext context, CancellationToken cancellationToken)
    {
        return _mappedActions.TryGetValue(envelope.Action, out var handler)
            ? HandleSoapAction(handler, envelope, context, cancellationToken)
            : throw new Exception($"Unknown soap action: '{envelope.Action}'");
    }

    private static async Task<IResult> HandleSoapAction(Delegate handler, SoapEnvelope envelope, HttpContext context, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = handler.Method.GetParameters();
            var paramList = new object[parameters.Length];

            for (int i = 0; i < paramList.Length; i++)
            {
                if (parameters[i].ParameterType.Name == envelope.Action)
                {
                    paramList[i] = envelope.Query;
                }
                else if (parameters[i].ParameterType == typeof(CancellationToken))
                {
                    paramList[i] = cancellationToken;
                }
                else
                {
                    paramList[i] = context.RequestServices.GetService(parameters[i].ParameterType);
                }
            }

            var result = await handler.DynamicInvoke(paramList).CastTask();

            return SoapResults.Create(result);
        }
        catch (Exception ex)
        {
            return SoapResults.Fault(ex is EpcisException epcisException ? epcisException : EpcisException.Default);
        }
    }
}
