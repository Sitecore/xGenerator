using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Links;

namespace Colossus.Integration
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

        public Dictionary<string,ItemUrl> SiteUrls { get; set; }

        public string Path { get; set; }
        public List<ItemInfo> Children { get; set; }

        public Dictionary<string, string> Fields { get; set; }

        public ItemInfo()
        {
            Fields = new Dictionary<string, string>();            
            Children = new List<ItemInfo>();
            SiteUrls = new Dictionary<string, ItemUrl>();
        }

        public static ItemInfo FromItem(Item item, IEnumerable<string> websites = null, int? maxDepth = null)
        {
            var info = new ItemInfo();
            info.Id = item.ID.Guid;
            info.Name = item.Name;            
            info.DisplayName = item.DisplayName;
            info.Version = item.Version.Number;
            info.Language = item.Language.Name;
            info.Path = item.Paths.FullPath;
            info.ParentId = item.ParentID.Guid;

            info.TemplateId = item.TemplateID.Guid;
            info.TemplateName = info.TemplateName;

            info.HasLayout = item.Fields[FieldIDs.LayoutField] != null
                             && !string.IsNullOrEmpty(item.Fields[FieldIDs.LayoutField].Value);

            foreach (Field field in item.Fields)
            {
                info.Fields[field.Name] = field.Value;
            }

            if (!maxDepth.HasValue || maxDepth > 0)
            {                
                info.Children = item.Children.Select(child => FromItem(child, websites, maxDepth.HasValue ? maxDepth - 1 : null)).ToList();
            }

            if (websites != null)
            {
                var current = Context.GetSiteName();
                foreach (var website in websites)
                {
                    Context.SetActiveSite(website);
                    var options = LinkManager.GetDefaultUrlOptions();                    
                    options.AlwaysIncludeServerUrl = true;

                    info.SiteUrls[website] = new ItemUrl
                    {
                        Url = LinkManager.GetItemUrl(item, options),
                        InSiteContext = item.Paths.FullPath.StartsWith(Context.Site.RootPath + Context.Site.StartItem)
                    };
                }
                if (!string.IsNullOrEmpty(current))
                {
                    Context.SetActiveSite(current);
                }
            }


            return info;
        }
    }

    public class ItemUrl
    {
        public string Url { get; set; }
        public bool InSiteContext { get; set; }
    }
}
