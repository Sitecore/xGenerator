using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ExperienceGenerator.Exm.Models;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.Data.Managers;
using Sitecore.ListManagement.Configuration;
using Sitecore.Publishing;

namespace ExperienceGenerator.Exm.Services
{
    public static class IndexService
    {
        public static void IndexContactLists(Job job)
        {
            job.Status = "Rebuilding Contact List Index";
            ContentSearchManager.GetIndex(ListManagementSettings.ContactListIndexName).Rebuild();
            job.Status = "Rebuilding Contacts Index";
            ContentSearchManager.GetIndex(ListManagementSettings.ContactsIndexName).Rebuild();
        }
    }
}
