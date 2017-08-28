using Microsoft.AspNetCore.Builder;
using NetLoadBalancer.Code.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Extension
{
    
        public static class InitMiddlewareExtension
    {
            public static IApplicationBuilder UseInit(this IApplicationBuilder builder)
            {
                return builder.Use(next => new InitMiddleware(next).Invoke);
            }
        }
    
}
