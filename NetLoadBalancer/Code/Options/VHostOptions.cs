using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Options
{
    public class VHostOptions
    {
          public string Host { get; set; }
        public int Port { get; set; }
        public string Scheme { get; set; }
        public List<string> Filters { get; set; }
    
    }


}
