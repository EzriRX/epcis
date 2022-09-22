﻿using FasTnT.Features.v1_2.Communication.Formatters;
using System.Xml.Linq;

namespace FasTnT.Features.v1_2.Tests;

[TestClass]
public class WhenFormattingASubscribeResult
{
    public XElement Formatted { get; set; }

    [TestInitialize]
    public void When()
    {
        Formatted = XmlResponseFormatter.FormatSubscribeResponse();
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
