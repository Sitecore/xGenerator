using System.Collections.Generic;

namespace ExperienceGenerator.Data
{
    public class SearchEngine
    {
        public string Id { get; set; }
        public string Label { get; set; }

        public string Url { get; set; }

        public bool Ppc { get; set; }

        public string ChannelId { get; set; }

        public HashSet<string> AllowedTlds { get; set; }


        public static List<SearchEngine> SearchEngines { get; set; }

        static SearchEngine()
        {
            SearchEngines = new List<SearchEngine>();

            SearchEngines.Add(new SearchEngine { Id = "google", Label = "Google", Url = "https://www.google{tld}?q={keywords}" });
            SearchEngines.Add(new SearchEngine { Id = "yahoo", Label = "Yahoo", Url = "https://{country.}search.yahoo.com?p={keywords}" });
            SearchEngines.Add(new SearchEngine { Id = "bing", Label = "Bing", Url = "https://www.bing.com?q={keywords}" });
            SearchEngines.Add(new SearchEngine { Id = "lycos", Label = "Lycos", Url = "https://search.lycos{tld}?query={keywords}" });
            SearchEngines.Add(new SearchEngine
            {
                Id = "baidu",
                Label = "Baidu",
                Url = "https://www.baidu.com?query={keywords}"                
            });

            SearchEngines.Add(new SearchEngine { Id = "googleppc", Ppc = true, Label = "Google", Url = "https://www.google{tld}?q={keywords}", ChannelId = "67150678-B200-44BB-BBAE-1D7B901D0860" });
            SearchEngines.Add(new SearchEngine { Id = "yahooppc", Ppc = true, Label = "Yahoo", Url = "https://{country.}search.yahoo{tld}?p={keywords}", ChannelId = "B5234879-DFFC-47AF-8267-59D4D3DF6226" });
            SearchEngines.Add(new SearchEngine { Id = "bingppc", Ppc = true, Label = "Bing", Url = "https://www.bing.com?q={keywords}", ChannelId = "B55EC2C2-CD7A-4E03-B155-EEFDAE872B7D" });            

        }
    }
}
