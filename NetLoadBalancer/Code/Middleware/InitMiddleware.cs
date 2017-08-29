using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Middleware
{
    public class InitMiddleware
    {
       
            private readonly RequestDelegate _next;

            private ILogger<InitMiddleware> logger;

            public InitMiddleware(RequestDelegate next)
            {
              
                _next = next;
            }

            public async Task Invoke(HttpContext context)
            {
            context.Items["proxy-options"] = null;//default option will be used

            if (_next != null)
                {
                    await _next(context);
                }
           }

           
        
    }
}
