using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Interfaces
{
    public abstract class IFilter
    {
         public abstract Task Invoke(HttpContext context);

        private  RequestDelegate _next;

        public RequestDelegate NextStep
        {
            get { return _next; }
        }

        public IFilter Init(RequestDelegate next)
        {
            _next = next;
            return this;
        }
    }
}
