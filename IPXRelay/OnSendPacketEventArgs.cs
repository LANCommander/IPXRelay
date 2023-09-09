using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IPXRelayDotNet
{
    public class OnSendPacketEventArgs : EventArgs
    {
        public IPEndPoint SourceEndPoint { get; set; }
        public IPEndPoint DestinationEndPoint { get; set; }
        public IPXPacket Packet { get; set; }
    }
}
