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
using Microsoft.Extensions.Primitives;
using NetLoadBalancer.Code.Classes;
using Microsoft.Extensions.Configuration;


namespace NetLoadBalancer.Code.Middleware
{

    /// <summary>
    /// NetLoadBalancer.Code.Middleware.BalancerMiddleware
    /// </summary>
    public class BalancerMiddleware:FilterMiddleware
    {

       
        public override string Name =>  "Balancer";

        int last = 0;

      //  private readonly RequestDelegate _next;

        private ILogger<BalancerMiddleware> logger;

        //public BalancerMiddleware(RequestDelegate next)
        //{
        //    _next = next;
        //}

        public async override Task InvokeImpl(HttpContext context, string host, VHostOptions vhost, IConfigurationSection settings)
        {

            BalancerOptions options= new BalancerOptions();
            settings.Bind("Settings:Balancer", options);
            //Round Robin
            last = (last + 1) % options.Nodes.Length;
            context.Items["bal-destination"] = options.Nodes[last];



            //Requests

            context.Request.Headers["X-Forwarded-For"] = context.Connection.RemoteIpAddress.ToString();
            context.Request.Headers["X-Forwarded-Proto"] = context.Request.Protocol.ToString();
            int port = context.Request.Host.Port ?? (context.Request.IsHttps ? 443 : 80);
            context.Request.Headers["X-Forwarded-Port"] = port.ToString();

        }

     
    }
}
