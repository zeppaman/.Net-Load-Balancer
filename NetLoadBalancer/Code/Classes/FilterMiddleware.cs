using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetLoadBalancer.Code.Interfaces;
using NetLoadBalancer.Code.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static NetLoadBalancer.Code.Options.System;

namespace NetLoadBalancer.Code.Classes
{
    public abstract class FilterMiddleware:IFilter
    {
       
      
       
       

        private ILogger<FilterMiddleware> logger;

       

        public abstract string  Name{ get;}

        public FilterMiddleware()
        {
            
           
        }
        
        public bool IsActive(HttpContext context)
        {
            string host = context.Items["bal-host"] as string;
            if (string.IsNullOrEmpty(host)) throw new Exception("HOST is empty. Please check configuration.");
            VHostOptions VHost= context.Items["bal-vhost"] as VHostOptions;
            if (VHost==null) throw new Exception($"VHOST is missing for {host}. Please check configuration.");
            return VHost.Filters != null && VHost.Filters.Contains(this.Name, StringComparer.InvariantCultureIgnoreCase);
        }


        public override async Task Invoke(HttpContext context)
        {
            var endRequest = false;
            if (this.IsActive(context))
            {
                
                object urlToProxy = null;
               
                string host = context.Items["bal-host"] as string;
                VHostOptions vHost = context.Items["bal-vhost"] as VHostOptions;
                IConfigurationSection settings = BalancerSettings.Current.GetSettingsSection(host);
                await InvokeImpl(context, host, vHost,settings);

                endRequest = this.Terminate(context);
                
               
            }

            if (!endRequest && NextStep != null)
            {
                await NextStep(context);
            }
        }

        public virtual bool Terminate(HttpContext httpContext)
        {
            return false;
        }

        public virtual IApplicationBuilder Register(IApplicationBuilder app, IConfiguration con, IHostingEnvironment env)
        {
            
            return app.Use(next => 
            {
                var instance = (IFilter)Activator.CreateInstance(this.GetType());
                return instance.Init(next).Invoke;
            });
        }

        public abstract Task InvokeImpl(HttpContext context,string host, VHostOptions vhost,IConfigurationSection settings);

    }
}
