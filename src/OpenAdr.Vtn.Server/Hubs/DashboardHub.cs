using Microsoft.AspNetCore.SignalR;
using OpenAdr.Vtn.Core.Models;

namespace OpenAdr.Vtn.Server.Hubs;

/// <summary>Messages pushed to every connected dashboard client. There's nothing for clients
/// to call on the server — this hub is push-only.</summary>
public interface IDashboardClient
{
    Task ReportReceived(ReportDto report);
    Task VenConnectionChanged(string venName, bool mqttConnected);
}

public class DashboardHub : Hub<IDashboardClient>;
