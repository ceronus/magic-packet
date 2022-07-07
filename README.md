![Logo](https://raw.githubusercontent.com/ceronus/magic-packet/master/icons/icon-64x64.png)
# Magic Packet

[![Continuous Integration](https://github.com/ceronus/magic-packet/actions/workflows/continuous-integration.yml/badge.svg)](https://github.com/ceronus/magic-packet/actions/workflows/continuous-integration.yml)

The magic packet is a frame that is most often sent as a broadcast and that contains anywhere within its payload 6 bytes 
of all 255 (`FF FF FF FF FF FF` in hexadecimal), followed by 16 repetitions of the target computer's 48-bit MAC 
address, for a total of 102 bytes.

Since the magic packet is only scanned for the string above, and not actually parsed by a full protocol stack, it could be
sent as payload of any network- and transport-layer protocol, although it is typically sent as a UDP datagram to port 0 
(reserved port number), 7 (Echo Protocol) or 9 (Discard Protocol), or directly over Ethernet as EtherType 0x0842.

A connection-oriented transport-layer protocol like TCP is less suited for this task as it requires establishing an active 
connection before sending user data.

A standard magic packet has the following basic limitations:

- Requires destination computer MAC address (also may require a SecureOn password)
- Does not provide a delivery confirmation
- May not work outside of the local network
- Requires hardware support of Wake-on-LAN on destination computer
- Most 802.11 wireless interfaces do not maintain a link in low power states and cannot receive a magic packet

The Wake-on-LAN implementation is designed to be very simple and to be quickly processed by the circuitry present on 
the network interface card with minimal power requirement. Because Wake-on-LAN operates below the IP protocol layer, 
IP addresses and DNS names are meaningless and so the MAC address is required.