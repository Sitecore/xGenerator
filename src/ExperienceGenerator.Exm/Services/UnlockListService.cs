using System.Linq;
using System.Threading;
using ExperienceGenerator.Exm.Models;
using Sitecore.Configuration;
using Sitecore.ExM.Framework.Diagnostics;
using Sitecore.ListManagement;
using Sitecore.ListManagement.ContentSearch.Model;

namespace ExperienceGenerator.Exm.Services
{
    public class UnlockListService
    {
        private readonly ListManager<ContactList, ContactData> _listManager;
        private const int UNLOCK_ATTEMPTS = 300;

        public UnlockListService()
        {
            _listManager = (ListManager<ContactList, ContactData>)Factory.CreateObject("contactListManager", false);
        }

        public void UnlockList(Job job, ContactList xaList)
        {
            var tries = 0;
            while (tries <= UNLOCK_ATTEMPTS && !IsListReady(xaList))
            {
                if (job.JobStatus == JobStatus.Cancelling)
                    return;

                tries++;
                job.Status = $"Waiting for list '{xaList.Name}' to unlock ({tries}/{UNLOCK_ATTEMPTS})";
                Thread.Sleep(1000);
            }
            if (tries > UNLOCK_ATTEMPTS)
            {
                UnlockList(xaList);
            }
        }

        private bool IsListReady(ContactList xaList)
        {
            return _listManager.GetAll().ToList().Any(x => x.Id == xaList.Id) && !(_listManager.IsLocked(xaList) || _listManager.IsInUse(xaList));
        }

        private void UnlockList(ContactList list)
        {
            if (!_listManager.IsLocked(list) && !_listManager.IsInUse(list))
            {
                return;
            }

            Logger.Instance.LogWarn($"Force unlocking list {list.Id} - {list.DisplayName}");
            var lockTest = _listManager.GetLock(list);
            _listManager.Unlock(lockTest);
        }

    }
}
