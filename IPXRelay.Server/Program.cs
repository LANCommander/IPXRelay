using IPXRelayDotNet;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddFilter("IPXRelayDotNet", LogLevel.Trace)
        .AddConsole();
});

var logger = loggerFactory.CreateLogger<Program>();

// Starts a new IPX relay on port 213 by default
using (var relay = new IPXRelay(logger))
{
    Console.WriteLine($"Listening for UDP packets on port {relay.Port}...");

    relay.OnClientConnected += (sender, e) =>
    {
        Console.WriteLine($"Client connected from {e.RemoteEndPoint}!");
    };

    relay.OnReceivePacket += (sender, e) =>
    {
        Console.WriteLine($"Received packet from {e.RemoteEndPoint} | Network: {e.Packet.Header.DestinationAddress.Network}, Node: {e.Packet.Header.DestinationAddress.Node}, Socket: {e.Packet.Header.DestinationAddress.Socket}");
    };

    relay.OnReceivePacketError += (sender, e) =>
    {
        Console.WriteLine($"Error receiving packet from {e.RemoteEndPoint} | Message: {e.Exception.Message}");
    };

    relay.StartAsync().Wait();
}