# IPXRelayDotNet

IPXRelayDotNet is a .NET library to relay UDP-encapsulated IPX packets from DOSBox to other connected DOSBox clients. It can replace the need for one client to act as the server. The library utilizes asynchronous sockets, and thus .NET 7 is required.

## Quick Start

Implementation is easy. Included in this repository is the project IPXRelay.Server that is a very basic relay server. The most minimal usage of the library in a console application may look like this:
```csharp
using IPXRelayDotNet;
...
using (var relay = new IPXRelay())
{
	relay.StartAsync().Wait();
}
```
This will start the relay using a default port of 213.

### Configurable Options
The only option that really might need to be configured with the relay is the port in which it will listen. This can be customized when setting up the relay:
```csharp
using IPXRelayDotNet;
...
var port = 33213;
using (var relay = new IPXRelay(port))
{
	relay.StartAsync().Wait();
}
```

### Logging
Some basic logging has been implemented using the abstraction `Microsoft.Extensions.Logging` so it can be used with loggers such as nlog or Serilog.

Logging can be manually disabled in cases where dependency injection may get in the way by using the method `IPXRelay.DisableLogging();`

### Event Handlers
Event handlers exist for the following:
- `OnClientConnected`: Fires whenever a client connects
- `OnReceivePacketError`: Fires when an exception occurs while trying to receive a packet
- `OnReceivePacket`: Fires when a packet has been received and deserialized into an `IPXPacket`
- `OnSendPacketHandler`: Fires after a packet has been sent to a client