using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IPXRelay
{
    public class PacketReceiveErrorArgs : EventArgs
    {
        public IPEndPoint RemoteEndPoint { get; set; }
        public IPEndPoint LocalEndPoint { get; set; }
        public byte[] Data { get; set; }
        public Exception Exception { get; set; }
    }
}
