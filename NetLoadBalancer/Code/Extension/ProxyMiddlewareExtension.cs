using Microsoft.AspNetCore.Builder;
using NetLoadBalancer.Code.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Extension
{
    
        public static class ProxyMiddlewareExtension
        {
            public static IApplicationBuilder UseProxyServer(this IApplicationBuilder builder)
            {
                return builder.Use(next => new ProxyMiddleware(next).Invoke);
            }
        }
    
}
