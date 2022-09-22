﻿using FasTnT.Domain.Enumerations;
using FasTnT.Domain.Infrastructure.Exceptions;
using FasTnT.Domain.Model.Events;
using System.Globalization;

namespace FasTnT.Features.v1_2.Communication.Parsers;

public static class XmlEventParser
{
    private readonly static Dictionary<string, Func<XElement, Event>> RootParsers = new()
    {
        { "ObjectEvent", ParseObjectEvent },
        { "TransactionEvent", ParseTransactionEvent },
        { "AggregationEvent", ParseAggregationEvent },
        { "QuantityEvent", ParseQuantityEvent },
        { "extension", ParseEventListExtension }
    };

    private readonly static Dictionary<string, Func<XElement, Event>> ExtensionParsers = new()
    {
        { "TransformationEvent", ParseTransformationEvent },
        { "extension", ParseEventListSubExtension }
    };

    private readonly static Dictionary<string, Func<XElement, Event>> SubExtensionParsers = new()
    {
        { "AssociationEvent", ParseAssociationEvent }
    };

    public static IEnumerable<Event> ParseEvents(XElement root)
    {
        foreach (var element in root.Elements())
        {
            if (!RootParsers.TryGetValue(element.Name.LocalName, out Func<XElement, Event> parser))
            {
                throw new ArgumentException($"Element '{element.Name.LocalName}' not expected in this context");
            }

            yield return parser(element);
        }
    }

    private static Event ParseEventListExtension(XElement element)
    {
        var eventElement = element.Elements().First();

        if (!ExtensionParsers.TryGetValue(eventElement.Name.LocalName, out Func<XElement, Event> parser))
        {
            throw new ArgumentException($"Element '{eventElement.Name.LocalName}' not expected in this context");
        }

        return parser(eventElement);
    }

    private static Event ParseEventListSubExtension(XElement element)
    {
        var eventElement = element.Elements().First();

        if (!SubExtensionParsers.TryGetValue(eventElement.Name.LocalName, out Func<XElement, Event> parser))
        {
            throw new ArgumentException($"Element '{eventElement.Name.LocalName}' not expected in this context");
        }

        return parser(eventElement);
    }

    public static Event ParseObjectEvent(XElement eventRoot)
    {
        var evt = ParseBase(eventRoot, EventType.ObjectEvent);

        evt.Action = Enum.Parse<EventAction>(eventRoot.Element("action").Value, true);
        ParseTransactions(eventRoot.Element("bizTransactionList"), evt);
        ParseEpcList(eventRoot.Element("epcList"), evt, EpcType.List);
        ParseObjectExtension(eventRoot.Element("extension"), evt);

        return evt;
    }

    private static void ParseObjectExtension(XElement element, Event evt)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        ParseEpcQuantityList(element.Element("quantityList"), evt, EpcType.Quantity);
        ParseSources(element.Element("sourceList"), evt);
        ParseDestinations(element.Element("destinationList"), evt);
        ParseIlmd(element.Element("ilmd"), evt);
        ParseV2Extensions(element.Element("extension"), evt);
    }

    public static Event ParseAggregationEvent(XElement eventRoot)
    {
        var evt = ParseBase(eventRoot, EventType.AggregationEvent);

        evt.Action = Enum.Parse<EventAction>(eventRoot.Element("action").Value, true);
        ParseParentId(eventRoot.Element("parentID"), evt);
        ParseEpcList(eventRoot.Element("childEPCs"), evt, EpcType.ChildEpc);
        ParseTransactions(eventRoot.Element("bizTransactionList"), evt);
        ParseAggregationExtension(eventRoot.Element("extension"), evt);

        return evt;
    }

    private static void ParseAggregationExtension(XElement element, Event evt)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        ParseEpcQuantityList(element.Element("childQuantityList"), evt, EpcType.ChildQuantity);
        ParseSources(element.Element("sourceList"), evt);
        ParseDestinations(element.Element("destinationList"), evt);
        ParseV2Extensions(element.Element("extension"), evt);
    }

    public static Event ParseTransactionEvent(XElement eventRoot)
    {
        var evt = ParseBase(eventRoot, EventType.TransactionEvent);

        evt.Action = Enum.Parse<EventAction>(eventRoot.Element("action").Value, true);
        ParseParentId(eventRoot.Element("parentID"), evt);
        ParseTransactions(eventRoot.Element("bizTransactionList"), evt);
        ParseEpcList(eventRoot.Element("epcList"), evt, EpcType.List);
        ParseTransactionExtension(eventRoot.Element("extension"), evt);

        return evt;
    }

    private static void ParseTransactionExtension(XElement element, Event evt)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        ParseEpcQuantityList(element.Element("quantityList"), evt, EpcType.Quantity);
        ParseSources(element.Element("sourceList"), evt);
        ParseDestinations(element.Element("destinationList"), evt);
        ParseV2Extensions(element.Element("extension"), evt);
    }

    public static Event ParseQuantityEvent(XElement eventRoot)
    {
        var evt = ParseBase(eventRoot, EventType.QuantityEvent);
        var epcQuantity = new Epc
        {
            Id = eventRoot.Element("epcClass").Value,
            Quantity = float.Parse(eventRoot.Element("quantity").Value, NumberStyles.AllowDecimalPoint, new CultureInfo("en-GB")),
            IsQuantity = true,
            Type = EpcType.Quantity
        };

        evt.Epcs.Add(epcQuantity);
        ParseExtension(eventRoot.Element("extension"), evt, FieldType.Extension);

        return evt;
    }

    public static Event ParseTransformationEvent(XElement eventRoot)
    {
        var evt = ParseBase(eventRoot, EventType.TransformationEvent);

        evt.TransformationId = eventRoot.Element("transformationID")?.Value;
        ParseTransactions(eventRoot.Element("bizTransactionList"), evt);
        ParseSources(eventRoot.Element("sourceList"), evt);
        ParseDestinations(eventRoot.Element("destinationList"), evt);
        ParseIlmd(eventRoot.Element("ilmd"), evt);
        ParseEpcList(eventRoot.Element("inputEPCList"), evt, EpcType.InputEpc);
        ParseEpcQuantityList(eventRoot.Element("inputQuantityList"), evt, EpcType.InputQuantity);
        ParseEpcList(eventRoot.Element("outputEPCList"), evt, EpcType.OutputEpc);
        ParseEpcQuantityList(eventRoot.Element("outputQuantityList"), evt, EpcType.OutputQuantity);
        ParseV2Extensions(eventRoot.Element("extension"), evt);

        return evt;
    }

    public static Event ParseAssociationEvent(XElement eventRoot)
    {
        var evt = ParseBase(eventRoot, EventType.AssociationEvent);

        evt.Action = Enum.Parse<EventAction>(eventRoot.Element("action").Value, true);
        ParseParentId(eventRoot.Element("parentID"), evt);
        ParseEpcList(eventRoot.Element("childEPCs"), evt, EpcType.ChildEpc);
        ParseTransactions(eventRoot.Element("bizTransactionList"), evt);
        ParseAssociationExtension(eventRoot.Element("extension"), evt);

        return evt;
    }

    private static void ParseAssociationExtension(XElement element, Event evt)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        ParseEpcQuantityList(element.Element("childQuantityList"), evt, EpcType.ChildQuantity);
        ParseSources(element.Element("sourceList"), evt);
        ParseDestinations(element.Element("destinationList"), evt);
        ParseV2Extensions(element.Element("extension"), evt);
    }

    public static Event ParseBase(XElement eventRoot, EventType eventType)
    {
        var Event = new Event
        {
            Type = eventType,
            EventTime = DateTime.Parse(eventRoot.Element("eventTime").Value, null, DateTimeStyles.AdjustToUniversal),
            EventTimeZoneOffset = eventRoot.Element("eventTimeZoneOffset").Value,
            BusinessStep = eventRoot.Element("bizStep")?.Value,
            Disposition = eventRoot.Element("disposition")?.Value,
        };

        ParseReadPoint(eventRoot.Element("readPoint"), Event);
        ParseBusinessLocation(eventRoot.Element("bizLocation"), Event);
        ParseBaseExtension(eventRoot.Element("baseExtension"), Event);
        ParseFields(eventRoot, Event, FieldType.CustomField);

        return Event;
    }

    private static void ParseReadPoint(XElement readPoint, Event evt)
    {
        if (readPoint == null || readPoint.IsEmpty)
        {
            return;
        }

        evt.ReadPoint = readPoint.Element("id")?.Value;
        ParseExtension(readPoint.Element("extension"), evt, FieldType.ReadPointExtension);
        ParseFields(readPoint, evt, FieldType.ReadPointCustomField);
    }

    private static void ParseBusinessLocation(XElement bizLocation, Event evt)
    {
        if (bizLocation == null || bizLocation.IsEmpty)
        {
            return;
        }

        evt.BusinessLocation = bizLocation.Element("id")?.Value;
        ParseExtension(bizLocation.Element("extension"), evt, FieldType.BusinessLocationExtension);
        ParseFields(bizLocation, evt, FieldType.BusinessLocationCustomField);
    }

    private static void ParseParentId(XElement element, Event evt)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        evt.Epcs.Add(new Epc { Id = element.Value, Type = EpcType.ParentId });
    }

    private static void ParseIlmd(XElement element, Event evt)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        ParseFields(element, evt, FieldType.Ilmd);
        ParseExtension(element.Element("extension"), evt, FieldType.IlmdExtension);
    }

    private static void ParseEpcList(XElement element, Event evt, EpcType type)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        evt.Epcs.AddRange(element.Elements("epc").Select(x => new Epc { Id = x.Value, Type = type }));
    }

    private static void ParseEpcQuantityList(XElement element, Event evt, EpcType type)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        evt.Epcs.AddRange(element.Elements("quantityElement").Select(x => new Epc
        {
            Id = x.Element("epcClass").Value,
            IsQuantity = true,
            Quantity = float.TryParse(x.Element("quantity")?.Value, NumberStyles.AllowDecimalPoint, new CultureInfo("en-GB"), out float quantity) ? quantity : default(float?),
            UnitOfMeasure = x.Element("uom")?.Value,
            Type = type
        }));
    }

    private static void ParseBaseExtension(XElement element, Event evt)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        evt.EventId = element.Element("eventID")?.Value;
        ParseErrorDeclaration(element.Element("errorDeclaration"), evt);
        ParseExtension(element.Element("extension"), evt, FieldType.BaseExtension);
    }

    private static void ParseV2Extensions(XElement element, Event evt)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        foreach (var field in element.Elements())
        {
            switch (field.Name.LocalName)
            {
                case "persistentDisposition":
                    evt.PersistentDispositions.AddRange(XmlExtensionParser.ParsePersistentDisposition(field)); break;
                case "sensorElementList":
                    evt.SensorElements.AddRange(XmlExtensionParser.ParseSensorList(field)); break;
                default:
                    evt.Fields.Add(XmlCustomFieldParser.ParseCustomFields(field, FieldType.Extension)); break;
            }
        }

        ParseExtension(element.Element("extension"), evt, FieldType.Extension);
    }

    private static void ParseErrorDeclaration(XElement element, Event evt)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        evt.CorrectiveDeclarationTime = DateTime.Parse(element.Element("declarationTime").Value);
        evt.CorrectiveReason = element.Element("reason")?.Value;
        evt.CorrectiveEventIds.AddRange(element.Element("correctiveEventIDs")?.Elements("correctiveEventID")?.Select(x => new CorrectiveEventId { CorrectiveId = x.Value }));
        ParseExtension(element.Element("extension"), evt, FieldType.ErrorDeclarationExtension);
        ParseFields(element, evt, FieldType.ErrorDeclarationCustomField);
    }

    internal static void ParseFields(XElement element, Event Event, FieldType type)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        var customFields = element.Elements().Where(x => !string.IsNullOrEmpty(x.Name.NamespaceName));
        Event.Fields.AddRange(customFields.Select(x => XmlCustomFieldParser.ParseCustomFields(x, type)));
    }

    internal static void ParseExtension(XElement element, Event evt, FieldType type)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        var customFields = element.Elements().Where(x => string.IsNullOrEmpty(x.Name.NamespaceName));
        evt.Fields.AddRange(customFields.Select(x => XmlCustomFieldParser.ParseCustomFields(x, type)));
    }

    internal static void ParseSources(XElement element, Event evt)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        evt.Sources.AddRange(element.Elements("source").Select(CreateSource));
    }

    internal static void ParseDestinations(XElement element, Event evt)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        evt.Destinations.AddRange(element.Elements("destination").Select(CreateDest));
    }

    internal static void ParseTransactions(XElement element, Event evt)
    {
        if (element == null || element.IsEmpty)
        {
            return;
        }

        evt.Transactions.AddRange(element.Elements("bizTransaction").Select(CreateBusinessTransaction));
    }

    private static BusinessTransaction CreateBusinessTransaction(XElement element)
    {
        return new()
        {
            Id = element.Value,
            Type = element.Attribute("type").Value
        };
    }

    private static Source CreateSource(XElement element)
    {
        return new() { Type = element.Attribute("type").Value, Id = element.Value };
    }

    private static Destination CreateDest(XElement element)
    {
        return new() { Type = element.Attribute("type").Value, Id = element.Value };
    }
}

public static class XmlExtensionParser
{
    public static IEnumerable<PersistentDisposition> ParsePersistentDisposition(XElement field)
    {
        return field.Elements().Select(x => new PersistentDisposition
        {
            Id = x.Value,
            Type = Enum.Parse<PersistentDispositionType>(x.Name.LocalName, true)
        });
    }

    public static IEnumerable<SensorElement> ParseSensorList(XElement field)
    {
        return field.Elements().Select(ParseSensorElement);
    }

    private static SensorElement ParseSensorElement(XElement element)
    {
        var sensorElement = new SensorElement();

        foreach (var field in element.Elements())
        {
            if (string.IsNullOrEmpty(field.Name.NamespaceName))
            {
                switch (field.Name.LocalName)
                {
                    case "sensorMetadata":
                        ParseSensorMetadata(sensorElement, field); break;
                    case "sensorReport":
                        ParseSensorReport(sensorElement, field); break;
                }
            }
            else
            {
                sensorElement.Fields.Add(XmlCustomFieldParser.ParseCustomFields(field, FieldType.Sensor));
            }
        }

        return sensorElement;
    }

    private static void ParseSensorReport(SensorElement sensorElement, XElement element)
    {
        var report = new SensorReport();

        foreach (var field in element.Attributes())
        {
            if (string.IsNullOrEmpty(field.Name.NamespaceName))
            {
                switch (field.Name.LocalName)
                {
                    case "value":
                        report.Value = float.Parse(field.Value); break;
                    case "type":
                        report.Type = field.Value; break;
                    case "component":
                        report.Component = field.Value; break;
                    case "stringValue":
                        report.StringValue = field.Value; break;
                    case "booleanValue":
                        report.BooleanValue = bool.Parse(field.Value); break;
                    case "hexBinaryValue":
                        report.HexBinaryValue = field.Value; break;
                    case "uriValue":
                        report.UriValue = field.Value; break;
                    case "uom":
                        report.UnitOfMeasure = field.Value; break;
                    case "minValue":
                        report.MinValue = float.Parse(field.Value); break;
                    case "maxValue":
                        report.MaxValue = float.Parse(field.Value); break;
                    case "sDev":
                        report.SDev = float.Parse(field.Value); break;
                    case "chemicalSubstance":
                        report.ChemicalSubstance = field.Value; break;
                    case "microorganism":
                        report.Microorganism = field.Value; break;
                    case "deviceID":
                        report.DeviceId = field.Value; break;
                    case "deviceMetadata":
                        report.DeviceMetadata = field.Value; break;
                    case "rawData":
                        report.RawData = field.Value; break;
                    case "time":
                        report.Time = DateTime.Parse(field.Value); break;
                    case "meanValue":
                        report.MeanValue = float.Parse(field.Value); break;
                    case "percRank":
                        report.PercRank = float.Parse(field.Value); break;
                    case "percValue":
                        report.PercValue = float.Parse(field.Value); break;
                    case "dataProcessingMethod":
                        report.DataProcessingMethod = field.Value; break;
                }
            }
            else
            {
                report.Fields.Add(XmlCustomFieldParser.ParseCustomFields(field, FieldType.SensorReport));
            }
        }

        sensorElement.Reports.Add(report);
    }

    private static void ParseSensorMetadata(SensorElement sensorElement, XElement metadata)
    {
        foreach (var field in metadata.Attributes())
        {
            if (string.IsNullOrEmpty(field.Name.NamespaceName))
            {
                switch (field.Name.LocalName)
                {
                    case "time":
                        sensorElement.Time = DateTime.Parse(field.Value); break;
                    case "bizRules":
                        sensorElement.BizRules = field.Value; break;
                    case "deviceID":
                        sensorElement.DeviceId = field.Value; break;
                    case "deviceMetadata":
                        sensorElement.DeviceMetadata = field.Value; break;
                    case "rawData":
                        sensorElement.RawData = field.Value; break;
                    case "startTime":
                        sensorElement.StartTime = DateTime.Parse(field.Value); break;
                    case "endTime":
                        sensorElement.EndTime = DateTime.Parse(field.Value); break;
                    case "dataProcessingMethod":
                        sensorElement.DataProcessingMethod = field.Value; break;
                    default:
                        throw new EpcisException(ExceptionType.ImplementationException, $"Unexpected event field: {field.Name}");
                }
            }
            else
            {
                sensorElement.Fields.Add(XmlCustomFieldParser.ParseCustomFields(field, FieldType.SensorMetadata));
            }
        }
    }
}

public static class XmlCustomFieldParser
{
    public static Field ParseCustomFields(XElement element, FieldType fieldType)
    {
        var field = new Field
        {
            Type = fieldType,
            Name = element.Name.LocalName,
            Namespace = string.IsNullOrWhiteSpace(element.Name.NamespaceName) ? default : element.Name.NamespaceName,
            TextValue = element.HasElements ? default : element.Value,
            NumericValue = element.HasElements ? default : float.TryParse(element.Value, NumberStyles.AllowDecimalPoint, new CultureInfo("en-GB"), out float floatValue) ? floatValue : default(float?),
            DateValue = element.HasElements ? default : DateTime.TryParse(element.Value, out DateTime dateValue) ? dateValue : default(DateTime?)
        };

        field.Children.AddRange(element.Elements().Select(x => ParseCustomFields(x, fieldType)));
        field.Children.AddRange(element.Attributes().Where(x => !x.IsNamespaceDeclaration).Select(ParseAttribute));

        return field;
    }

    public static Field ParseCustomFields(XAttribute element, FieldType fieldType)
    {
        return new()
        {
            Type = fieldType,
            Name = element.Name.LocalName,
            Namespace = string.IsNullOrWhiteSpace(element.Name.NamespaceName) ? default : element.Name.NamespaceName,
            TextValue = element.Value,
            NumericValue = float.TryParse(element.Value, NumberStyles.AllowDecimalPoint, new CultureInfo("en-GB"), out float floatValue) ? floatValue : default(float?),
            DateValue = DateTime.TryParse(element.Value, out DateTime dateValue) ? dateValue : default(DateTime?)
        };
    }

    public static Field ParseAttribute(XAttribute element)
    {
        return new()
        {
            Type = FieldType.Attribute,
            Name = element.Name.LocalName,
            Namespace = element.Name.NamespaceName,
            TextValue = element.Value,
            NumericValue = float.TryParse(element.Value, NumberStyles.AllowDecimalPoint, new CultureInfo("en-GB"), out float floatValue) ? floatValue : default(float?),
            DateValue = DateTime.TryParse(element.Value, out DateTime dateValue) ? dateValue : default(DateTime?)
        };
    }
}
