using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Colossus;
using ExperienceGenerator.Data;

namespace ExperienceGenerator.Parsing.Factories
{
    public class ExternalSearchVariable : VisitorVariablesBase
    {
        public Func<SearchEngine> Engine { get; set; }
        public Func<IEnumerable<string>> Keywords { get; set; }
        public double LocalizeTld { get; set; }


        public ExternalSearchVariable(Func<SearchEngine> engine,
            Func<IEnumerable<string>> keywords,
            double localizeTld = 0.5)
        {
            Engine = engine;
            Keywords = keywords;
            LocalizeTld = localizeTld;

            DependentVariables.Add("Channel");
            DependentVariables.Add("DomainPostfix");
            DependentVariables.Add("Tld");
        }

        public override void SetValues(SimulationObject target)
        {
            var engine = Engine();
            if (engine != null)
            {

                var domainPostfix = target.GetVariable<string>("DomainPostfix");

                var localize = Randomness.Random.NextDouble() < LocalizeTld;
                var postfix = !string.IsNullOrEmpty(domainPostfix) && localize ? domainPostfix : ".com";

                if (engine.AllowedTlds != null && !engine.AllowedTlds.Contains(postfix))
                {
                    postfix = ".com";
                }

                var tld = target.GetVariable<string>("Tld");
                var country = !string.IsNullOrEmpty(tld) && localize ? tld.Substring(1) : "";

                var keywords = Keywords();
               
                target.Variables["Referrer"] = engine.Url.Replace("{tld}", postfix)
                    .Replace("{country.}", !string.IsNullOrEmpty(country) ? country + "." : "")
                    .Replace("{country}", country)
                    .Replace("{keywords}", keywords != null ? HttpUtility.UrlEncode(string.Join(" ", keywords)).Replace("%20", "+") : "");

                if (engine.ChannelId != null)
                {
                    target.Variables["Channel"] = engine.ChannelId;
                }
                else
                {
                    target.Variables["Channel"] = "";
                }
            }
        }

        public override IEnumerable<string> ProvidedVariables
        {
            get
            {
                yield return "Channel";
                yield return "Referrer";
            }
        }        
    }
}
