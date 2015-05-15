using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossus.Statistics;
using Newtonsoft.Json.Linq;

namespace ExperienceGenerator.Parsing
{
    public static class ParseConfig
    {        
        public static IDictionary<string, Func<XGenParser, JToken, Func<object>>> Factories { get; private set; }

        public static IDictionary<string, Action<XGenParser, TrendBuilder, JToken>> TrendFactories { get; private set; }

        static ParseConfig()
        {
            Factories = new Dictionary<string, Func<XGenParser, JToken, Func<object>>>();
            TrendFactories = new Dictionary<string, Action<XGenParser, TrendBuilder, JToken>>();
        }
    }
}
