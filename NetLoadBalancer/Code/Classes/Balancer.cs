using Microsoft.Extensions.Options;
using NetLoadBalancer.Code.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Classes
{
    public class Balancer
    {
        BalancerOptions options;
        public Balancer(IOptions<BalancerOptions> settings)
        {
            options = settings.Value;
        }
    }
}
