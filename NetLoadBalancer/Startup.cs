using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;
using NLog.Web;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.AspNetCore.Rewrite;
using NetLoadBalancer.Code.Extension;

namespace NetLoadBalancer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

       
        public void ConfigureServices(IServiceCollection services)
        {
           
            services.AddResponseCaching();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory  loggerFactory)
        {
            //Configure Log
            ConfigureLog(app, env, loggerFactory);

            ConfigureCaching(app, env);
            //Configure Redirect
            ConfigureRedirect(app, env);

            ConfigureProxy(app, env);


        }

        private void ConfigureProxy(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseProxyServer();
        }

        private void ConfigureCaching(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseResponseCaching();
        }

        private void ConfigureRedirect(IApplicationBuilder app, IHostingEnvironment env)
        {
            using (StreamReader apacheModRewriteStreamReader = File.OpenText(".\\conf\\ApacheModRewrite.txt"))
            using (StreamReader iisUrlRewriteStreamReader = File.OpenText(".\\conf\\IISUrlRewrite.xml"))
            {
                var options = new RewriteOptions()
                    .AddApacheModRewrite(apacheModRewriteStreamReader)
                    .AddIISUrlRewrite(iisUrlRewriteStreamReader);

                app.UseRewriter(options);
            }
        }

        private static void ConfigureLog(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            env.ConfigureNLog(".\\conf\\nlog.config");
            //add NLog to ASP.NET Core
            loggerFactory.AddNLog();

            //add NLog.Web
            app.AddNLogWeb();
        }
    }
}
