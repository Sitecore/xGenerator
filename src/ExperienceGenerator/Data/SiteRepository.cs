using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ExperienceGenerator.Models;
using Sitecore.Configuration;
using Sitecore.Sites;

namespace ExperienceGenerator.Data
{
    public class SiteRepository
    {
        private SiteInfo[] _sites;
        private SiteInfo[] _validSites;

        public SiteInfo[] Sites => _sites ?? (_sites = GetSites());

        public SiteInfo[] ValidSites => _validSites ?? (_validSites = GetValidSites());

        private SiteInfo[] GetValidSites()
        {
            var excludedSites = GetExcludedSites();
            return Sites.Where(site => !excludedSites.Contains(site.Id) && HasValidStartPath(site)).ToArray();
        }

        private bool HasValidStartPath(SiteInfo site)
        {
            if (string.IsNullOrEmpty(site.Database) || string.IsNullOrEmpty(site.StartPath))
                return false;
            var database = Sitecore.Data.Database.GetDatabase(site.Database);
            return database?.GetItem(site.StartPath) != null;
        }

        private static HashSet<string> GetExcludedSites()
        {
            var excludedSites = new HashSet<string>();

            var exportNode = Factory.GetConfigNode("experienceGenerator/excludeSites") as XmlElement;
            var sites = exportNode?.SelectNodes("site");
            if (sites == null)
                return excludedSites;

            foreach (var site in sites.OfType<XmlElement>())
            {
                excludedSites.Add(site.GetAttribute("name"));
            }
            return excludedSites;
        }

        private SiteInfo[] GetSites()
        {
            return SiteManager.GetSites().Select(s => new SiteContext(new Sitecore.Web.SiteInfo(s.Properties))).Select(site => new SiteInfo
                                                                                                                               {
                                                                                                                                   Id = site.Name,
                                                                                                                                   Host = GetHostName(site),
                                                                                                                                   StartPath = site.RootPath + site.StartItem,
                                                                                                                                   Label = site.Name,
                                                                                                                                   Database = site.Database != null ? site.Database.Name : ""
                                                                                                                               }).ToArray();
        }

        private static string GetHostName(SiteContext site)
        {
            if (!string.IsNullOrEmpty(site.TargetHostName))
                return site.TargetHostName;
            var hostnames = site.HostName.Split('|');
            foreach (var host in hostnames)
            {
                if (Uri.CheckHostName(host) != UriHostNameType.Unknown)
                    return host;
            }
            return "";
        }
    }
}
