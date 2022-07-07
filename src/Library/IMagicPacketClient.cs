using System.Net;
using System.Net.NetworkInformation;

namespace MagicPacket;
public interface IMagicPacketClient
{
    Task BroadcastOnAllInterfacesAsync(PhysicalAddress target, string? password = null, CancellationToken cancellationToken = default);
    Task BroadcastOnAllInterfacesAsync(string target, CancellationToken cancellationToken = default);
    Task BroadcastOnAllInterfacesAsync(string target, string? password, CancellationToken cancellationToken = default);
    Task BroadcastOnSingleInterfaceAsync(PhysicalAddress target, IPAddress broadcast, string? password = null, CancellationToken cancellationToken = default);
    Task BroadcastOnSingleInterfaceAsync(string target, string broadcast, CancellationToken cancellationToken = default);
    Task BroadcastOnSingleInterfaceAsync(string target, string broadcast, string? password, CancellationToken cancellationToken = default);
    Task SendMagicPacketAsync(PhysicalAddress target, IPAddress broadcast, string? password, CancellationToken cancellationToken = default);
}