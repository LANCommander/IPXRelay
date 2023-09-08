using System.Diagnostics;
using System.Net;

namespace IPXRelay
{
    [DebuggerDisplay("{Endpoint}, Connected = {Connected}")]
    public class IPXClientConnection
    {
        public IPEndPoint Endpoint { get; set; }
        public bool Connected { get; set; }
    }
}
