using NetLoadBalancer.Code.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NetLoadBalancer.Code.Options;
using System.Text.RegularExpressions;
using System.Net;

namespace NetLoadBalancer.Code.Middleware
{
    public  class RequestFilterMiddleware : FilterMiddleware
    {
       

        public override string Name => "RequestFilter";

        public override async Task InvokeImpl(HttpContext context, string host, VHostOptions vhost, IConfigurationSection settings)
        {
            context.Items["bal-filter-end"] = false;
            FiltersOption options = settings.GetSection("Settings:RequestFilter").Get<FiltersOption>();
            foreach (var option in options.rules)
            {
                Regex r = new Regex(option.path);
                if (r.Match(context.Request.Path).Success)
                {
                    context.Items["bal-filter-end"] = true;
                    break;
                }
            }
        }

        public override bool Terminate(HttpContext httpContext)
        {
            if ((bool)httpContext.Items["bal-filter-end"] == true)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return true;
            }
            return false;
        }
    }
}
