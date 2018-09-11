using System.Threading;
using ExperienceGenerator.Exm.Models;
using Sitecore.Configuration;
using Sitecore.ExM.Framework.Diagnostics;
using Sitecore.ListManagement;

namespace ExperienceGenerator.Exm.Services
{
    //TODO: This might be able to go away. Locked Lists no longer a thing in Sitecore 9

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
            return _listManager.FindById(xaList.Id) != null && !_listManager.IsLocked(xaList) && !_listManager.IsInUse(xaList);
        }

        private void UnlockList(ContactList list)
        {
            if (!_listManager.IsLocked(list) && !_listManager.IsInUse(list))
            {
                return;
            }

            Logger.Instance.LogWarn($"Force unlocking list {list.Id} - {list.DisplayName}");
            var lockTest = _listManager.GetLock(list);
            if (lockTest != null)
                _listManager.Unlock(lockTest);
        }

    }
}
