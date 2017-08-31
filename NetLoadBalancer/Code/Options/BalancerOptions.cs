using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Options
{

    public class BalancerOptions
    {
        public Node[] Nodes { get; set; }
        public string Policy { get; set; }
    }

    public class Node
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Scheme { get; set; }
    }

}
