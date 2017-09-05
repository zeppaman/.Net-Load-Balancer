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
        /// <summary>
        /// application startup
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="env"></param>
        public Startup(IConfiguration configuration, IHostingEnvironment env)
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

            builder = builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public static IConfiguration Configuration { get; private set; }


        /// <summary>
        /// service configuration
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<BalancerSettings>(Configuration.GetSection("Balancersettings"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<BalancerSettings> init)
        {

            BalancerSettings.Init(init);


            foreach (var item in BalancerSettings.Current.Middlewares)
            {
                item.Value.Register(app, Configuration, env, loggerFactory);
            }


        }



    }
}
