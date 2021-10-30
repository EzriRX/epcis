﻿using FasTnT.Domain.Commands.Subscribe;
using FasTnT.Formatter.Xml.Formatters;
using System.Xml.Linq;

namespace FasTnT.Formatters.Xml.Tests;

[TestClass]
public class WhenFormattingASubscribeResult
{
    public SubscribeResult Result = new();
    public XElement Formatted { get; set; }

    [TestInitialize]
    public void When()
    {
        Formatted = XmlResponseFormatter.FormatSubscribeResponse(Result);
    }

    [TestMethod]
    public void ItShouldReturnAnXElement()
    {
        Assert.IsNotNull(Formatted);
    }

    [TestMethod]
    public void TheXmlShouldBeCorrectlyFormatter()
    {
        Assert.IsTrue(Formatted.Name == XName.Get("SubscribeResult", "urn:epcglobal:epcis-query:xsd:1"));
        Assert.IsTrue(Formatted.IsEmpty);
    }
}
