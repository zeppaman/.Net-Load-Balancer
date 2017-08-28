using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Extension
{
    public static class CookieExtension
    {
        
            public static IDictionary<string, string> FromCookieString(this string legacyCookie)
            {
                return legacyCookie.Split('&').Select(s => s.Split('=')).ToDictionary(kvp => kvp[0], kvp => kvp[1]);
            }

            public static string ToCookieString(this IDictionary<string, string> dict)
            {
                return string.Join("&", dict.Select(kvp => string.Join("=", kvp.Key, kvp.Value)));
            }
        
    }
}
