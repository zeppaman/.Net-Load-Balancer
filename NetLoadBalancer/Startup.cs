using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Http;
using NetLoadBalancer.Code.Middleware;
using Microsoft.Extensions.Options;
using NetLoadBalancer.Code.Options;
using static NetLoadBalancer.Code.Options.System;

namespace NetLoadBalancer
{
    public class Startup
    {
        public Startup(IConfiguration configuration,IHostingEnvironment env)
        {
            Configuration = configuration;

            var builder = new ConfigurationBuilder()
               .SetBasePath(env.ContentRootPath)
               .AddJsonFile("conf/appsettings.json", optional: true, reloadOnChange: true);

            string[] files = Directory.GetFiles(Path.Combine(env.ContentRootPath, "conf", "vhosts"));

            foreach (var s in files)
            {
                builder = builder.AddJsonFile(s);
            }
            
            builder=builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public static IConfiguration Configuration { get; private set; }

       
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<BalancerSettings>(Configuration.GetSection("Balancersettings"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory  loggerFactory, IOptions<BalancerSettings> init)
        {

            BalancerSettings.Init(init);

           


            foreach (var item in BalancerSettings.Current.Middlewares)
            {
                item.Value.Register(app, Configuration, env, loggerFactory);
            }


        }

       
      

        
    }
}
