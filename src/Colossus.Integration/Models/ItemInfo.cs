using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Sites;

namespace Colossus.Integration.Models
{
    public class ItemInfo
    {
        public Guid Id { get; set; }

        public string Name { get; set; }
        public string DisplayName { get; set; }

        public bool HasLayout { get; set; }

        public int Version { get; set; }

        public string Language { get; set; }

        public string TemplateName { get; set; }
        public Guid TemplateId { get; set; }

        public Guid? ParentId { get; set; }

        public Dictionary<string, string> SiteUrls { get; set; }

        public string Path { get; set; }
        public List<ItemInfo> Children { get; set; }

        public Dictionary<string, string> Fields { get; set; }

        public ItemInfo()
        {
            Fields = new Dictionary<string, string>();
            Children = new List<ItemInfo>();
            SiteUrls = new Dictionary<string, string>();
        }

        public static ItemInfo FromItem(Item item, IEnumerable<string> websites = null, int maxDepth = 0, Language language = null)
        {
            var info = new ItemInfo
                       {
                           Id = item.ID.Guid,
                           Name = item.Name,
                           DisplayName = item.DisplayName,
                           Version = item.Version.Number,
                           Language = item.Language.Name,
                           Path = item.Paths.FullPath,
                           ParentId = item.ParentID.Guid,
                           TemplateId = item.TemplateID.Guid,
                           HasLayout = item.Fields[FieldIDs.LayoutField] != null && !string.IsNullOrEmpty(item.Fields[FieldIDs.LayoutField].Value)
                       };

            foreach (Field field in item.Fields)
            {
                info.Fields[field.Name] = field.Value;
            }

            if (maxDepth > 0)
            {
                info.Children = item.Children.Select(child => FromItem(child, websites, maxDepth - 1)).ToList();
            }

            if (websites != null)
            {
                foreach (var siteName in websites)
                {
                    var site = Factory.GetSite(siteName);
                    if (site == null)
                        continue;

                    using (new SiteContextSwitcher(site))
                    {
                        var options = LinkManager.GetDefaultUrlOptions();
                        options.AlwaysIncludeServerUrl = true;
                        if (language != null)
                        {
                            options.Language = language;
                        }

                        info.SiteUrls[siteName] = LinkManager.GetItemUrl(item, options);
                    }
                }
            }

            return info;
        }
    }
}
