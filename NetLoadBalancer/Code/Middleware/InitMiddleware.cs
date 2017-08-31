using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NetLoadBalancer.Code.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NetLoadBalancer.Code.Options;
using static NetLoadBalancer.Code.Options.System;

namespace NetLoadBalancer.Code.Middleware
{
    public class InitMiddleware: FilterMiddleware
    {
       
           // private readonly RequestDelegate _next;

            private ILogger<InitMiddleware> logger;

            //public InitMiddleware(RequestDelegate next)
            //{
              
            //    _next = next;
            //}

        public override string Name => "Init";


        public override async Task Invoke(HttpContext context)
        {
            InvokeImpl(context, null, null, null);
            await NextStep(context);
            
        }

        public async override Task InvokeImpl(HttpContext context, string host, VHostOptions vhost, IConfigurationSection settings)
        {
            host = context.Request.Host.Value;
            vhost = BalancerSettings.Current.GetSettings<VHostOptions>(host);

            context.Items["bal-host"] = host;
            context.Items["bal-vhost"] = vhost;
        }
    }
    
}
