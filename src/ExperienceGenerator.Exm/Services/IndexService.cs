using ExperienceGenerator.Exm.Models;
using Sitecore.ContentSearch;
using Sitecore.ListManagement.Configuration;

namespace ExperienceGenerator.Exm.Services
{
    public static class IndexService
    {
        public static void RebuildListIndexes(Job job)
        {
            job.Status = "Rebuilding Contact List Index";
            ContentSearchManager.GetIndex(ListManagementSettings.ContactListIndexName).Rebuild(IndexingOptions.ForcedIndexing);
        }
    }
}
