/*
    This file is part of NetLoadBalancer, Daniele Fontani (https://github.com/zeppaman/.Net-Load-Balancer).

    NetLoadBalancer is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    NetLoadBalancer is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Nome-Programma.  If not, see <http://www.gnu.org/licenses/>.
*/
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
    /// Implement balancing logic
    /// </summary>
    public class BalancerMiddleware:FilterMiddleware
    {

        Dictionary<string, BalancerData> data = new Dictionary<string, BalancerData>();
       
        public override string Name =>  "Balancer";
        

        private ILogger<BalancerMiddleware> logger;
    

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
          
            //Different logic basing on algoritm
            //TODO: make it generic using interface\implementation for algoritm
            if (options.Policy == "RoundRobin")
            {
            
              
                context.Items["bal-destination"] = RoundRobin(balancerData, options);
                
            }
            else if (options.Policy == "RequestCount")
            {
                context.Items["bal-destination"] = RequestCount(balancerData,options);
            }



            //Alter request
            context.Request.Headers["X-Forwarded-For"] = context.Connection.RemoteIpAddress.ToString();
            context.Request.Headers["X-Forwarded-Proto"] = context.Request.Protocol.ToString();
            int port = context.Request.Host.Port ?? (context.Request.IsHttps ? 443 : 80);
            context.Request.Headers["X-Forwarded-Port"] = port.ToString();

         

        }

        /// <summary>
        /// Roud robin implementation
        /// </summary>
        /// <param name="balancerData"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private object RoundRobin(BalancerData balancerData, BalancerOptions options)
        {
            balancerData.LastServed = (balancerData.LastServed+1) % options.Nodes.Length;
            return options.Nodes[balancerData.LastServed];
        }

        /// <summary>
        /// Request count implementation
        /// </summary>
        /// <param name="balancerData"></param>
        /// <param name="options"></param>
        /// <returns></returns>
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
