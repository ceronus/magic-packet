# ![Logo](https://raw.githubusercontent.com/ceronus/magic-packet/master/icons/icon-64x64.png) Magic Packet

A cross-platform, light-weight implementation of the Magic Packet -- most commonly known for its use in Wake-on-LAN.

[![Continuous Integration](https://github.com/ceronus/magic-packet/actions/workflows/continuous-integration.yml/badge.svg)](https://github.com/ceronus/magic-packet/actions/workflows/continuous-integration.yml)

# Portable Binary Downloads
There are portable binaries for the following platforms:

Windows
- [Windows 64-bit (win-x64)](https://github.com/ceronus/magic-packet/releases/download/v1.0.0/win-x64.zip)
- [Windows 32-bit (win-x86)](https://github.com/ceronus/magic-packet/releases/download/v1.0.0/win-x86.zip)
- [Windows ARM 32-bit (win-arm)](https://github.com/ceronus/magic-packet/releases/download/v1.0.0/win-arm.zip)
- [Windows ARM 64-bit (win-arm64)](https://github.com/ceronus/magic-packet/releases/download/v1.0.0/win-arm64.zip)

Linux
- [Linux 64-bit (linux-x64)](https://github.com/ceronus/magic-packet/releases/download/v1.0.0/linux-x64.zip)
- [Linux ARM 32-bit (linux-arm)](https://github.com/ceronus/magic-packet/releases/download/v1.0.0/linux-arm.zip)

macOS
- [macOS 64-bit (osx-x64)](https://github.com/ceronus/magic-packet/releases/download/v1.0.0/osx-x64.zip)

For previous releases you can find them [here](https://github.com/ceronus/magic-packet/releases).

## Standards and References
This implementation follows the specifications outlined in the 
[Magic Packet Technology, Whitepaper](https://github.com/ceronus/magic-packet/blob/master/docs/20213-amd-magic-packet-technology-whitepaper.pdf) 
which was originally proposed by AMD and Hewlett Packard back in 1995.

In addition to following the specification, the program also has support for Secure-On passwords (which was added later). 
Another [good resource published by Texas Instruments](https://github.com/ceronus/magic-packet/blob/master/docs/dp83822-texas-instruments-wake-on-lan.pdf) 
covers the entire specification and was also referenced.


## What is Wake-on-LAN?
Wake-on-LAN (WOL) is an Ethernet or Token Ring computer networking standard that allows a computer to be turned on or awakened by a network message.

The message is usually sent to the target computer by a program executed on a device connected to the same local area network. It is also possible to initiate the message from another network by using subnet directed broadcasts or a WoL gateway service.

Equivalent terms include wake on WAN, remote wake-up, power on by LAN, power up by LAN, resume by LAN, resume on LAN and wake up on LAN.


## What is a Magic Packet?
The magic packet is a frame that is most often sent as a broadcast and that contains anywhere within its payload 6 bytes 
of all 255 (e.g., `FF FF FF FF FF FF` in hexadecimal), followed by 16 repetitions of the target computer's 48-bit MAC 
address, for a total of 102 bytes.

Since the magic packet is only scanned for the string above, and not actually parsed by a full protocol stack, it could be
sent as payload of any network- and transport-layer protocol, although it is typically sent as a UDP datagram to port 0 
(reserved port number), 7 (Echo Protocol) or 9 (Discard Protocol), or directly over Ethernet as EtherType `0x0842`.

A connection-oriented transport-layer protocol like TCP is less suited for this task as it requires establishing an active 
connection before sending user data.

A standard magic packet has the following basic limitations:

- Requires destination computer MAC address (also may require a Secure-On password).
- Does not provide a delivery confirmation.
- May not work outside of the local network.
- Requires hardware support of Wake-on-LAN on destination machine.
- Most 802.11 wireless interfaces do not maintain a link in low power states and cannot receive a magic packet.

The Wake-on-LAN implementation is designed to be very simple and to be quickly processed by the circuitry present on 
the network interface card with minimal power requirement. Because Wake-on-LAN operates below the IP protocol layer, 
IP addresses and DNS names are meaningless and so the MAC address is required.
