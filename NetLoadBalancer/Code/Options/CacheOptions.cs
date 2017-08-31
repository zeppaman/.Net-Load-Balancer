using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Options
{
    
        public class CacheOptions
        {
            public Rule[] rules { get; set; }
        }

        public class Rule
        {
            public string path { get; set; }
            public string duration { get; set; }
        }


    
}
