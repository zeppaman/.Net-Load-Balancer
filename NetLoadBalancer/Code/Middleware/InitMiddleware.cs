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
    /// <summary>
    /// Init middleware, used to setup enviornment. Is the first step, not optional
    /// </summary>
    public class InitMiddleware: FilterMiddleware
    {
       
          

            private ILogger<InitMiddleware> logger;
          

        public override string Name => "Init";


        public override async Task Invoke(HttpContext context)
        {
            InvokeImpl(context, null, null, null);
            await NextStep(context);
            
        }

        public async override Task InvokeImpl(HttpContext context, string host, VHostOptions vhost, IConfigurationSection settings)
        {
            host = context.Request.Host.Value;
            if (string.IsNullOrEmpty(host)) throw new Exception("HOST is empty. Please check configuration.");
            vhost = BalancerSettings.Current.GetSettings<VHostOptions>(host);
            if (vhost == null || string.IsNullOrEmpty(vhost.Host)) throw new Exception($"VHOST is missing for {host}. Please check configuration.");


         
           

            context.Items["bal-host"] = host;
            context.Items["bal-vhost"] = vhost;
        }
    }
    
}
