using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MagicPacket.UserInterface;

public class WakeOnLan
{
    private const int ReservedPortNumber = 0;
    private const int EchoProtocolPortNumber = 7;
    private const int DiscardProtocolPortNumber = 7;

    public static Task SendWakeOnLanAsync(string target, string? password = null)
    {
        PhysicalAddress phyiscalAddress = ConvertMacAddressStringToPhysicalAddress(target);
        return SendWakeOnLanAsync(phyiscalAddress, password);
    }

    public static async Task SendWakeOnLanAsync(PhysicalAddress target, string? password = null)
    {
        // Iterate over every available network interface
        foreach (NetworkInterface networkInterface in GetNetworkInterfaces())
        {
            // Get the unicast information to extract the address and mask
            IPInterfaceProperties interfaceProperties = networkInterface.GetIPProperties();
            foreach (UnicastIPAddressInformation unicast in interfaceProperties.UnicastAddresses)
            {
                // Ignore any invalid masks (e.g., IPv6 addresses)
                if (Equals(unicast.IPv4Mask, IPAddress.Any)) continue;

                // Calculate the broadcast address using the unicast address and mask
                IPAddress broadcast = CalculateIPv4BroadcastAddress(unicast);
                await SendMagicPacketAsync(broadcast, target, password).ConfigureAwait(true);
            }
        }
    }

    private static IEnumerable<NetworkInterface> GetNetworkInterfaces()
        => NetworkInterface.GetAllNetworkInterfaces()
            .Where(_ => _.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Where(_ => _.OperationalStatus == OperationalStatus.Up);

    private static IPAddress CalculateIPv4BroadcastAddress(UnicastIPAddressInformation unicast)
    {
        uint address = ConvertToUInt32(unicast.Address);
        uint mask = ConvertToUInt32(unicast.IPv4Mask);
        Span<byte> broadcast = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(broadcast, address | ~mask);
        return new IPAddress(broadcast);
    }

    private static uint ConvertToUInt32(IPAddress address)
        => BinaryPrimitives.ReadUInt32BigEndian(address.GetAddressBytes());

    private static async Task SendMagicPacketAsync(IPAddress address, PhysicalAddress target, string? password)
    {
        byte[] magicPacket = CreateMagicPacket(target, password);
        Debug.Assert(magicPacket.Length == 102);

        // Reserved
        await SendAsync(magicPacket, address, ReservedPortNumber).ConfigureAwait(false);

        // Echo Protocol
        await SendAsync(magicPacket, address, EchoProtocolPortNumber).ConfigureAwait(false);

        // Discard Protocol
        await SendAsync(magicPacket, address, DiscardProtocolPortNumber).ConfigureAwait(false);

        // TODO: Directly over Ethernet as EtherType 0x0842
    }

    private static byte[] CreateMagicPacket(PhysicalAddress target, string? password)
    {
        // First 6 bytes of all 255 (FF FF FF FF FF FF in hexadecimal)
        IEnumerable<byte>? synchronizationStream = Enumerable.Repeat(byte.MaxValue, 6);

        // Followed by sixteen repetitions of the target computer's 48-bit MAC address
        IEnumerable<byte>? targetMac = Enumerable.Repeat(target.GetAddressBytes(), 16).SelectMany(_ => _);

        // The Password field is optional, but if present, contains either 4 bytes or 6 bytes. 
        IEnumerable<byte>? passwordBytes = null; // TODO: Password

        return passwordBytes == null
            ? synchronizationStream.Concat(targetMac).ToArray()
            : synchronizationStream.Concat(targetMac).Concat(passwordBytes).ToArray();
    }

    private static async Task SendAsync(byte[] payload, IPAddress address, int port)
    {
        try
        {
            using UdpClient? client = new();
            IPEndPoint endpoint = new(address, port);
            await client.SendAsync(payload, payload.Length, endpoint).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{address,15}] {ex.Message}");
        }
    }

    private static PhysicalAddress ConvertMacAddressStringToPhysicalAddress(ReadOnlySpan<char> address)
        => new(ToByteSpan(CleanMacAddressString(address)).ToArray());

    private static ReadOnlySpan<char> CleanMacAddressString(ReadOnlySpan<char> address)
    {
        Debug.Assert(address != null);

        List<char> clean = new();

        for (int i = 0; i < address.Length; i++)
        {
            switch (address[i])
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
                    clean.Add(address[i]);
                    break;

                case ' ':
                case '-':
                case ':':
                    break;

                default:
                    throw new ArgumentException($"Invalid input. The character '{address[i]}' is not a valid value.");
            }
        }

        if (clean.Count != 12) throw new ArgumentException($"Invalid input. The length of the MAC address must be 12.");

        return clean.ToArray();
    }

    private static Span<byte> ToByteSpan(ReadOnlySpan<char> value)
    {
        if (value == null) return null;
        if (value.Length % 2 != 0) throw new ArgumentException("Invalid of malformed hexadecimal string.", nameof(value));

        Span<byte> result = new byte[value.Length / 2];
        for (int i = 0; i < value.Length; i += 2)
        {
            result[i / 2] = Convert.ToByte(new string(value[i..(i + 2)]), fromBase: 16); // base 16 for hexadecimal representation
        }

        return result;
    }
}