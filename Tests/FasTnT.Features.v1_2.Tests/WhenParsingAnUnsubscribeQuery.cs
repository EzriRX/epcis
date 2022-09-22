﻿using FasTnT.Features.v1_2.Communication.Parsers;
using FasTnT.Features.v1_2.Endpoints.Interfaces;

namespace FasTnT.Features.v1_2.Tests;

[TestClass]
public class WhenParsingAnUnsubscribeQuery : XmlParsingTestCase
{
    public static readonly string ResourceName = "FasTnT.Features.v1_2.Tests.Resources.Queries.Unsubscribe.xml";

    public object Query { get; set; }

    [TestInitialize]
    public void When()
    {
        Query = XmlQueryParser.Parse(ParseResource(ResourceName).Root);
    }

    [TestMethod]
    public void ItShouldReturnAnUnsubscribeObject()
    {
        Assert.IsInstanceOfType(Query, typeof(Unsubscribe));
    }

    [TestMethod]
    public void TheUnsubscribeCommandShouldHaveTheCorrectSubscriptionId()
    {
        Assert.AreEqual("TestSubscription", (Query as Unsubscribe).SubscriptionId);
    }
}
