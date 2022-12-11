﻿using FasTnT.Domain.Model.Events;
using FasTnT.Domain.Model.Masterdata;
using FasTnT.Domain.Model.Subscriptions;

namespace FasTnT.Domain.Model;

public class Request
{
    public int Id { get; set; }
    public string CaptureId { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; }
    public StandardBusinessHeader StandardBusinessHeader { get; set; }
    public DateTime CaptureTime { get; set; } = DateTime.UtcNow;
    public DateTime DocumentTime { get; set; }
    public string SchemaVersion { get; set; }
    public SubscriptionCallback SubscriptionCallback { get; set; }
    public List<Event> Events { get; set; } = new();
    public List<MasterData> Masterdata { get; set; } = new();
}
