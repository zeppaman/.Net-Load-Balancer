using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetLoadBalancer.Code.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Middleware
{ 

    public class BalancerMiddleware
    {

        public static List<ProxyOptions> Nodes { get; set; }
        int last = 0;

        private readonly RequestDelegate _next;

        private ILogger<BalancerMiddleware> logger;

        public BalancerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {

            BalanceRequest(context);

            if (_next != null)
            {
                await _next(context);
            }
        }

        private void BalanceRequest(HttpContext context)
        {
            //Round Robin
            last = (last + 1) % Nodes.Count;
            context.Items["proxy-options"] = Nodes[last];

        }
    }
}
