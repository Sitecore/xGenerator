using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Colossus;
using Newtonsoft.Json;
using ExperienceGenerator.Models;

namespace ExperienceGenerator.Data
{
  using Colossus.Integration.Models;

  public class ItemInfoClient
    {
        public string ItemServiceRoot { get; set; }

        public ItemInfoClient(string itemServiceRoot)
        {
            if (!itemServiceRoot.EndsWith("/")) itemServiceRoot += "/";

            ItemServiceRoot = itemServiceRoot;
        }

        private readonly ConcurrentDictionary<string, ItemInfo> _cache = new ConcurrentDictionary<string, ItemInfo>();

        private Dictionary<string, SiteInfo> _sites;
        public Dictionary<string, SiteInfo> Sites
        {
            get
            {
                if (_sites == null)
                {
                    _sites = Request<SiteInfo[]>("websites")                        
                        .ToDictionary(s=>s.Id);
                }

                return _sites;
            }
        }


        private TResponse Request<TResponse>(string url)
        {
            return JsonConvert.DeserializeObject<TResponse>(new WebClient().DownloadString(ItemServiceRoot + url));
        }

        public IEnumerable<ItemInfo> Query(string query, string language = null, int? maxDepth = 0)
        {
            var url = new StringBuilder()                
                .Append("items?query=")
                .Append(HttpUtility.UrlEncode(query));
            if (maxDepth.HasValue)
            {            
              url.Append("&maxDepth=").Append(maxDepth);
            }
            if (!string.IsNullOrEmpty(language))
            {
                url.Append("&language=").Append(language);
            }

            return Request<ItemInfo[]>(url.ToString());
        }

        public ItemInfo GetItemInfo(string itemId, string language = null)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            var key = language + "/" + itemId;
                                                   
            return _cache.GetOrAdd(key, _=> 
                Query(itemId, language, 0).FirstOrDefault());
        }

        public string GetUrl(string itemId, string language = null, string site = null)
        {
            return GetItemInfo(itemId, language)
                .TryGetValue(item => item.SiteUrls.GetOrDefault(site ?? "website")
                    .TryGetValue(url => url.Url));

        }
    }
}
