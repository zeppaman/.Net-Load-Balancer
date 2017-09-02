using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Classes
{
    public class BalancerData
    {
        public Dictionary<int, long> Scores { get; set; }
        public long LastServed { get; set; }
    }
}
