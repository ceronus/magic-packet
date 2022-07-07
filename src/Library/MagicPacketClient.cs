using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MagicPacket;

public class MagicPacketClient : IMagicPacketClient
{
    private const int ReservedPortNumber = 0;
    private const int EchoProtocolPortNumber = 7;
    private const int DiscardProtocolPortNumber = 9;

    private readonly UdpClient _client;

    public MagicPacketClient() : this(new()) { }

    public MagicPacketClient(UdpClient client)
    {
        _client = client ?? new();
    }

    public Task BroadcastOnSingleInterfaceAsync(string target, string broadcast, CancellationToken cancellationToken = default)
        => BroadcastOnSingleInterfaceAsync(ConvertMacAddressStringToPhysicalAddress(target), IPAddress.Parse(broadcast), null, cancellationToken);

    public Task BroadcastOnSingleInterfaceAsync(string target, string broadcast, string? password, CancellationToken cancellationToken = default)
        => BroadcastOnSingleInterfaceAsync(ConvertMacAddressStringToPhysicalAddress(target), IPAddress.Parse(broadcast), password, cancellationToken);

    public async Task BroadcastOnSingleInterfaceAsync(PhysicalAddress target, IPAddress broadcast, string? password = null, CancellationToken cancellationToken = default)
        => await SendMagicPacketAsync(target, broadcast, password, cancellationToken).ConfigureAwait(false);

    public Task BroadcastOnAllInterfacesAsync(string target, CancellationToken cancellationToken = default)
    => BroadcastOnAllInterfacesAsync(ConvertMacAddressStringToPhysicalAddress(target), null, cancellationToken);

    public Task BroadcastOnAllInterfacesAsync(string target, string? password, CancellationToken cancellationToken = default)
        => BroadcastOnAllInterfacesAsync(ConvertMacAddressStringToPhysicalAddress(target), password, cancellationToken);

    public async Task BroadcastOnAllInterfacesAsync(PhysicalAddress target, string? password = null, CancellationToken cancellationToken = default)
    {
        foreach (IPAddress broadcast in GetIPv4BroadcastAddresses())
        {
            await BroadcastOnSingleInterfaceAsync(target, broadcast, password, cancellationToken).ConfigureAwait(false);
        }
    }

    private static IEnumerable<IPAddress> GetIPv4BroadcastAddresses()
    {
        // Iterate over every available network interface
        foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Ignore interfaces that are loopbacks or in the down status (not up)
            if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            if (networkInterface.OperationalStatus != OperationalStatus.Up) continue;

            // Get the unicast information to extract the address and mask
            IPInterfaceProperties interfaceProperties = networkInterface.GetIPProperties();
            foreach (UnicastIPAddressInformation unicast in interfaceProperties.UnicastAddresses)
            {
                // Ignore any invalid masks (e.g., IPv6 addresses)
                if (Equals(unicast.IPv4Mask, IPAddress.Any)) continue;

                // Calculate the broadcast address using the unicast address and mask
                yield return CalculateIPv4BroadcastAddress(unicast);
            }
        }
    }

    private static IPAddress CalculateIPv4BroadcastAddress(UnicastIPAddressInformation unicast)
        => CalculateIPv4BroadcastAddress(unicast.Address, unicast.IPv4Mask);

    public static IPAddress CalculateIPv4BroadcastAddress(IPAddress address, IPAddress subnet)
        => CalculateIPv4BroadcastAddress(ConvertToUInt32BigEndian(address), ConvertToUInt32BigEndian(subnet));

    public static IPAddress CalculateIPv4BroadcastAddress(uint address, uint subnet)
    {
        Span<byte> broadcast = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(broadcast, address | ~subnet);
        return new IPAddress(broadcast);
    }

    internal static uint ConvertToUInt32BigEndian(IPAddress address)
        => ConvertToUInt32BigEndian(address.GetAddressBytes());

    internal static uint ConvertToUInt32BigEndian(ReadOnlySpan<byte> address)
        => BinaryPrimitives.ReadUInt32BigEndian(address);

    public async Task SendMagicPacketAsync(PhysicalAddress target, IPAddress broadcast, string? password, CancellationToken cancellationToken = default)
    {
        byte[] frame = CreateMagicPacketFrame(target, password);

        Debug.Assert(string.IsNullOrWhiteSpace(password)
            ? frame.Length == 102
            : (frame.Length is 106 or 108));

        // Reserved
        await SendDatagramAsync(frame, broadcast, ReservedPortNumber, cancellationToken).ConfigureAwait(false);

        // Echo Protocol
        await SendDatagramAsync(frame, broadcast, EchoProtocolPortNumber, cancellationToken).ConfigureAwait(false);

        // Discard Protocol
        await SendDatagramAsync(frame, broadcast, DiscardProtocolPortNumber, cancellationToken).ConfigureAwait(false);

        // TODO: Directly over Ethernet as EtherType 0x0842
    }

    private static byte[] CreateMagicPacketFrame(PhysicalAddress target, string? password)
    {
        // First 6 bytes of all 255 (FF FF FF FF FF FF in hexadecimal)
        IEnumerable<byte>? synchronizationStream = Enumerable.Repeat(byte.MaxValue, 6);

        // Followed by sixteen repetitions of the target computer's 48-bit MAC address
        IEnumerable<byte>? targetMac = Enumerable.Repeat(target.GetAddressBytes(), 16).SelectMany(_ => _);

        // The Password field is optional, but if present, contains either 4 bytes or 6 bytes. 
        IEnumerable<byte>? passwordBytes = ConvertSecureOnPassword(password).ToArray();

        return passwordBytes == null
            ? synchronizationStream.Concat(targetMac).ToArray()
            : synchronizationStream.Concat(targetMac).Concat(passwordBytes).ToArray();
    }

    private async Task SendDatagramAsync(byte[] datagram, IPAddress address, int port, CancellationToken cancellationToken = default)
    {
        try
        {
            IPEndPoint endpoint = new(address, port);
            await _client.SendAsync(datagram, endpoint, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Broadcast failed.{Environment.NewLine}" +
                $"   {ex.Message} ({address}:{port})");
        }
    }

    private static Span<byte> ConvertSecureOnPassword(ReadOnlySpan<char> password)
    {
        if (password == null || password.Length == 0) return null;
        if (password.Length is not 8 and not 12) throw new ArgumentException($"Invalid input. The password must either be 4 or 6 bytes.");

        return ToByteSpan(password);
    }

    public static PhysicalAddress ConvertMacAddressStringToPhysicalAddress(ReadOnlySpan<char> address)
        => new(ToByteSpan(CleanMacAddressString(address)).ToArray());

    public static string CleanMacAddressString(ReadOnlySpan<char> value)
    {
        if (value == null || value.Length == 0) throw new ArgumentException("The value is not defined.", nameof(value));
        if (value.Length < 12) throw new ArgumentException($"The length of the MAC address is too short.");

        List<char> clean = new();

        for (int i = 0; i < value.Length; i++)
        {
            switch (value[i])
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case 'A':
                case 'a':
                case 'B':
                case 'b':
                case 'C':
                case 'c':
                case 'D':
                case 'd':
                case 'E':
                case 'e':
                case 'F':
                case 'f':
                    clean.Add(value[i]);
                    break;

                case ' ':
                case '-':
                case ':':
                    break;

                default:
                    throw new ArgumentException($"The character '{value[i]}' is not a valid hexadecimal or separator value.");
            }
        }

        if (clean.Count != 12) throw new ArgumentException($"The length of the MAC address is invalid.");

        return new string(clean.ToArray());
    }

    private static Span<byte> ToByteSpan(ReadOnlySpan<char> value)
    {
        Debug.Assert(value != null);
        Debug.Assert(value.Length % 2 == 0);

        Span<byte> result = new byte[value.Length / 2];
        for (int i = 0; i < value.Length; i += 2)
        {
            result[i / 2] = Convert.ToByte(new string(value[i..(i + 2)]), fromBase: 16); // base 16 for hexadecimal representation
        }

        return result;
    }

    #region IDisposable
    private bool _isDisposed;
    void IDisposable.Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
            // Free managed resources
            _client?.Close();

        _isDisposed = true;
    }
    #endregion
}