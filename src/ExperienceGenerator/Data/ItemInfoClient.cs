using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Colossus;
using ExperienceGenerator.Models;
using ExperienceGenerator.Repositories;
using Sitecore.Data;
using Sitecore.Globalization;

namespace ExperienceGenerator.Data
{
  using Colossus.Integration.Models;

  public class ItemInfoClient
    {
        public string ItemServiceRoot { get; set; }
        private readonly GeoDataRepository _getDataRepository;
        private readonly SiteRepository _siteRepository;

        public ItemInfoClient(string itemServiceRoot)
        {
            if (!itemServiceRoot.EndsWith("/")) itemServiceRoot += "/";

            ItemServiceRoot = itemServiceRoot;
            _siteRepository = new SiteRepository();
            _getDataRepository = new GeoDataRepository();
        }

        private readonly ConcurrentDictionary<string, ItemInfo> _cache = new ConcurrentDictionary<string, ItemInfo>();

        private Dictionary<string, SiteInfo> _sites;
        public Dictionary<string, SiteInfo> Sites
        {
            get
            {
                if (_sites == null)
                {
                    _sites = _siteRepository.ValidSites.ToDictionary(s => s.Id);
                }

                return _sites;
            }
        }

        public IEnumerable<ItemInfo> Query(string query, string language = null, int? maxDepth = 0)
        {
            var db = Database.GetDatabase("web");

            foreach (var item in db.SelectItems(query))
            {
                Language itemLanguage = null;
                if (!string.IsNullOrEmpty(language))
                {
                    Language.TryParse(language, out itemLanguage);
                }

                yield return ItemInfo.FromItem(item, _siteRepository.ValidSites.Select(w => w.Id), maxDepth, itemLanguage);
            }
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
