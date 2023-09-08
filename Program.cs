using System.Net.Sockets;
using System.Net;
using IPXRelay;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;

int port = 33213; // Specify the port you want to listen on

var logger = LoggerFactory.Create(builder => builder.AddNLog()).CreateLogger<Program>();

using (var server = new IPXServer())
{
    Console.WriteLine($"Listening for UDP packets on port {port}...");

    server.Start();
}