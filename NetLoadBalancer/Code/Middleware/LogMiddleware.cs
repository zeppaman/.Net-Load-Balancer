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

        public override IApplicationBuilder Register(IApplicationBuilder app, IConfiguration con, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {

            loggerFactory.AddNLog();
            app.AddNLogWeb();
            env.ConfigureNLog(".\\conf\\nlog.config");
            return app;
            
        }
    }
}
