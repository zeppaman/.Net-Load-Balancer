using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Options
{
    public class InternalProxyOptions
    {
        private int? _bufferSize;
        public long Score { get; set; }
        public string Scheme { get; set; }
        public string Host { get; set; }
        public int? Port { get; set; }        
        public string UrlHost { get; set; }
        public HttpMessageHandler BackChannelMessageHandler { get; set; }
        public TimeSpan? WebSocketKeepAliveInterval { get; set; }
        public bool SendChunked { get; set; }
        public int? BufferSize
        {
            get
            {
                return _bufferSize;
            }
            set
            {
                if (value.HasValue && value.Value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _bufferSize = value;
            }
        }
    }
}
