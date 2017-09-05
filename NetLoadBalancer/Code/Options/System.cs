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
        /// <summary>
        /// plugin definition
        /// </summary>
        public class PluginInfo
        {
            public string Name { get; set; }
            public string Impl { get; set; }
        }

        /// <summary>
        /// Main settings
        /// </summary>
        public class BalancerSettings
        {
                      
            private static BalancerSettings _current;
            public static BalancerSettings Current
            {
                get { return _current ?? new BalancerSettings(); }
            }

            public List<Mapping> Mappings { get; set; }
            public PluginInfo[] Plugins { get; set; }

            //Internal disctionary for resolve map
            Dictionary<string, string> hostToSettingsMap = new Dictionary<string, string>();
            //Internal disctionary for resolve map
            Dictionary<string, string> settingsToHostMap = new Dictionary<string, string>();

            /// <summary>
            /// List of middleares 
            /// </summary>
            public Dictionary<string, FilterMiddleware> Middlewares { get; set; }

            bool optimized = false;
            /// <summary>
            /// buil internal indexed and optimize class
            /// </summary>
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

            /// <summary>
            /// Init class (also call optimize)
            /// </summary>
            public void Init()
            {
                Optimize();
                foreach (var item in Plugins)
                {
                    var impl = (FilterMiddleware)Activator.CreateInstance(Type.GetType(item.Impl));
                    Middlewares.Add(item.Name, impl);
                }
            }


            internal static void Init(IOptions<BalancerSettings> init)
            {
                _current = init.Value;
                _current.Init();
            }

            /// <summary>
            /// Get the name of section by host, resolving config map
            /// </summary>
            /// <param name="host"></param>
            /// <returns></returns>
            public string GetSettingsName(string host)
            {
                if (!optimized) Optimize();
                return hostToSettingsMap[host];
            }

            /// <summary>
            /// Get a configuration section basing host name.
            /// </summary>
            /// <param name="host"></param>
            /// <returns></returns>
            public IConfigurationSection GetSettingsSection(string host)
            {
                string settingsName = GetSettingsName(host);
                return Startup.Configuration.GetSection(settingsName);

            }

            /// <summary>
            /// get and bing a section to a new instance of a class of type T
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="host"></param>
            /// <returns></returns>
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
