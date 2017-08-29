using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Interfaces
{
    interface IScore
    {
         long GetScore(ProxyOptions proxyOptions);

        
    }
}
