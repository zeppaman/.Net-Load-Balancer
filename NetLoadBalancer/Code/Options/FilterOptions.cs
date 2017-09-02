using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Options
{
    
        public class FiltersOption
        {
            public FilterRule[] rules { get; set; }
        }

        public class FilterRule
    {
            public string path { get; set; }
        }


    
}
