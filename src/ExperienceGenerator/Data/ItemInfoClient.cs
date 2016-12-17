using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Colossus;
using Colossus.Integration.Models;
using ExperienceGenerator.Models;
using Newtonsoft.Json;

namespace ExperienceGenerator.Data
{
    public class ItemInfoClient
    {
        public string ItemServiceRoot { get; set; }

        public ItemInfoClient(string itemServiceRoot)
        {
            if (!itemServiceRoot.EndsWith("/"))
                itemServiceRoot += "/";

            ItemServiceRoot = itemServiceRoot;
        }

        private readonly ConcurrentDictionary<string, ItemInfo> _cache = new ConcurrentDictionary<string, ItemInfo>();

        private Dictionary<string, SiteInfo> _sites;

        public Dictionary<string, SiteInfo> Sites
        {
            get { return _sites ?? (_sites = Request<SiteInfo[]>("websites").ToDictionary(s => s.Id)); }
        }


        private TResponse Request<TResponse>(string url)
        {
            return JsonConvert.DeserializeObject<TResponse>(new WebClient().DownloadString(ItemServiceRoot + url));
        }

        public IEnumerable<ItemInfo> Query(string query, string language = null, int? maxDepth = 0)
        {
            var url = new StringBuilder().Append("items?query=").Append(HttpUtility.UrlEncode(query));
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
            if (string.IsNullOrEmpty(itemId))
                return null;

            var key = language + "/" + itemId;

            return _cache.GetOrAdd(key, _ => Query(itemId, language, 0).FirstOrDefault());
        }
    }
}
