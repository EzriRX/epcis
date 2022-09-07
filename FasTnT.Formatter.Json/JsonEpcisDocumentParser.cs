﻿using FasTnT.Domain.Model;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FasTnT.Formatter.Json;

public static class JsonEpcisDocumentParser
{
    public static Request Parse(JsonDocument document, IDictionary<string, string> extensions)
    {
        if(document.RootElement.TryGetProperty("@context", out JsonElement context) && context.ValueKind == JsonValueKind.Array)
        {
            foreach (var ctxNamespace in context.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Object).Select(x => x.EnumerateObject().First()))
            {
                extensions[ctxNamespace.Name] = ctxNamespace.Value.GetString();
            }
        }

        var schemaVersion = document.RootElement.GetProperty("schemaVersion").GetString();
        var documentTime = document.RootElement.GetProperty("creationDate").GetDateTime();
        var events = document.RootElement.GetProperty("epcisBody").GetProperty("eventList").EnumerateArray().Select(x => new JsonEventParser(x, extensions).Parse()).ToList();

        return new Request
        {
            SchemaVersion = schemaVersion,
            DocumentTime = documentTime,
            Events = events
        };
    }
}
