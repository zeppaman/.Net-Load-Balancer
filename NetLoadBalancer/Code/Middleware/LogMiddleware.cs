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
using NetLoadBalancer.Code.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NetLoadBalancer.Code.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using NLog.Web;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Web;

namespace NetLoadBalancer.Code.Middleware
{
    /// <summary>
    /// Log middleware. 
    /// </summary>
    public class LogMiddleware : FilterMiddleware
    {
        public override string Name =>"Log";

        public override bool IsActive(HttpContext context)
        {
            return true;
        }
        public override Task InvokeImpl(HttpContext context, string host, VHostOptions vhost, IConfigurationSection settings)
        {
            //nothing to do here...
            return null;
        }

        /// <summary>
        /// Register the module
        /// </summary>
        /// <param name="app"></param>
        /// <param name="con"></param>
        /// <param name="env"></param>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        public override IApplicationBuilder Register(IApplicationBuilder app, IConfiguration con, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {

            loggerFactory.AddNLog();
            app.AddNLogWeb();
            env.ConfigureNLog(".\\conf\\nlog.config");
            return app;
            
        }
    }
}
