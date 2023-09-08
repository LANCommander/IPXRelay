using System.Net.Sockets;
using System.Net;
using IPXRelayDotNet;
using Microsoft.Extensions.Logging;

// Starts a new IPX relay on port 213 by default
using (var relay = new IPXRelay())
{
    Console.WriteLine($"Listening for UDP packets on port {relay.Port}...");

    relay.OnClientConnected += (sender, e) =>
    {
        Console.WriteLine($"Client connected from {e.RemoteEndPoint}!");
    };

    relay.OnPacketReceiveError += (sender, e) =>
    {
        Console.WriteLine($"Error receiving packet from {e.RemoteEndPoint} | Message: {e.Exception.Message}");
    };

    relay.StartAsync().Wait();
}