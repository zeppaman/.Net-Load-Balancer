using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Options
{
    
        public class ProxyOptionscs
        {
            public Defaultdestination DefaultDestination { get; set; }
        }

        public class Defaultdestination
        {
            public string Host { get; set; }
            public int Port { get; set; }
            public string Scheme { get; set; }
        }

    
}
