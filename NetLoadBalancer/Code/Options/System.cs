using Microsoft.Extensions.Configuration;
using NetLoadBalancer.Code.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace NetLoadBalancer.Code.Options
{
    public class System
    {


       

        public class PluginInfo
        {
            public string Name { get; set; }
            public string Impl { get; set; }
        }


        public class BalancerSettings
        {

          
            private static BalancerSettings _current;
            public static BalancerSettings Current
            {
                get { return _current ?? new BalancerSettings(); }
            }

            public List<Mapping> Mappings { get; set; }
            public PluginInfo[] Plugins { get; set; }


            Dictionary<string, string> hostToSettingsMap = new Dictionary<string, string>();
            Dictionary<string, string> settingsToHostMap = new Dictionary<string, string>();

            public Dictionary<string, FilterMiddleware> Middlewares { get; set; }

            bool optimized = false;
            public void Optimize()
            {
                foreach (var item in Mappings)
                {
                    hostToSettingsMap[item.Host] = item.SettingsName;
                    hostToSettingsMap[item.SettingsName] = item.Host;
                }

                optimized = true;
            }

            public BalancerSettings()
            {
                Plugins = new PluginInfo[] { };
                Middlewares = new Dictionary<string, FilterMiddleware>();
            }

            public void Init()
            {
                Optimize();
                foreach (var item in Plugins)
                {
                    var impl = (FilterMiddleware)Activator.CreateInstance(Type.GetType(item.Impl));
                    Middlewares.Add(item.Name, impl);
                }
            }



            public string GetSettingsName(string host)
            {
                if (!optimized) Optimize();
                return hostToSettingsMap[host];
            }

            internal static void Init(IOptions<BalancerSettings> init)
            {
                _current = init.Value;                
                _current.Init();
            }

            public IConfigurationSection GetSettingsSection(string host)
            {
                string settingsName = GetSettingsName(host);
                return Startup.Configuration.GetSection(settingsName);

            }

            public T GetSettings<T>(string host) where T : new()
            {
                var t = new T();
                GetSettingsSection(host).Bind(t);
                return t;
            }

            public class Mapping
            {
                public string Host { get; set; }
                public string SettingsName { get; set; }
            }

        }
    }
}
