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
    /// <summary>
    /// Module to filter the request
    /// </summary>
    public  class RequestFilterMiddleware : FilterMiddleware
    {
       

        public override string Name => "RequestFilter";

        /// <summary>
        /// End the request if path match one provided rules.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="host"></param>
        /// <param name="vhost"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
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
