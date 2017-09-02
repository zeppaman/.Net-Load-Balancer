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

        Dictionary<string, BalancerData> data = new Dictionary<string, BalancerData>();
       
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

            BalancerData balancerData = null;
            if (!data.ContainsKey(host))
            {
                balancerData = data[host] = new BalancerData();
                for(int i =0; i<options.Nodes.Length;i++)
                {
                    balancerData.Scores[i] = 0;
                }
            }
            else
            {
                balancerData = data[host];
            }

          

            if (options.Policy == "RoundRobin")
            {
            
              
                context.Items["bal-destination"] = RoundRobin(balancerData, options);
                
            }
            else if (options.Policy == "RequestCount")
            {
                context.Items["bal-destination"] = RequestCount(balancerData,options);
            }



            //Requests

            context.Request.Headers["X-Forwarded-For"] = context.Connection.RemoteIpAddress.ToString();
            context.Request.Headers["X-Forwarded-Proto"] = context.Request.Protocol.ToString();
            int port = context.Request.Host.Port ?? (context.Request.IsHttps ? 443 : 80);
            context.Request.Headers["X-Forwarded-Port"] = port.ToString();

         

        }

        private object RoundRobin(BalancerData balancerData, BalancerOptions options)
        {
            balancerData.LastServed = (balancerData.LastServed+1) % options.Nodes.Length;
            return options.Nodes[balancerData.LastServed];
        }

        private Node RequestCount(BalancerData balancerData,BalancerOptions options)
        {
            var key=balancerData.Scores.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            return options.Nodes[key];

            if (balancerData.Scores[key] + 1 < long.MaxValue)
            {
                balancerData.Scores[key]++;
            }
            else
            {
                //If i'm going outside long range I need a reset.
                //i'm the lower value, so if I'm not able to increments all other have my same value (if not, they had ben triggered this previously)
                for (int i = 0; i < balancerData.Scores.Count; i++)
                {
                    balancerData.Scores[i] = 0;
                }

            }

        }
    }
}
