using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ExperienceGenerator.Exm.Infrastructure;
using ExperienceGenerator.Exm.Models;
using Sitecore.Data;
using Sitecore.DependencyInjection;
using Sitecore.ListManagement;
using Sitecore.ListManagement.Providers;
using Sitecore.ListManagement.Services.Model;
using Sitecore.ListManagement.XConnect.Web;
using Sitecore.Marketing.Definitions.ContactLists;
using Sitecore.Modules.EmailCampaign.Factories;
using Sitecore.Modules.EmailCampaign.Messages;
using Sitecore.SecurityModel;
using Sitecore.Services.Core;
using Sitecore.XConnect;
using Sitecore.XConnect.Collection.Model;

namespace ExperienceGenerator.Exm.Repositories
{
    public class ContactListRepository
    {

        private const string DefaultListManagerOwner = "xGenerator";
        private readonly IContactListProvider _listManager;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IRepository<ContactListModel> _contactListRepository;
        private readonly IContactProvider _contactProvider;
        private readonly IRecipientManagerFactory _recipientManagerFactory;

        public ContactListRepository()
        {

            _listManager = (IContactListProvider) ServiceLocator.ServiceProvider.GetService(typeof(IContactListProvider));
            _contactProvider = (IContactProvider) ServiceLocator.ServiceProvider.GetService(typeof(IContactProvider));
            _subscriptionService = (ISubscriptionService) ServiceLocator.ServiceProvider.GetService(typeof(ISubscriptionService));
            _recipientManagerFactory = (IRecipientManagerFactory)ServiceLocator.ServiceProvider.GetService(typeof(IRecipientManagerFactory));
            _contactListRepository = (IRepository<ContactListModel>) ServiceLocator.ServiceProvider.GetService(typeof(IRepository<ContactListModel>));
        }


        private ContactList GetList(ID id)
        {
            return GetList(id.ToGuid());
        }

        private ContactList GetList(Guid id)
        {
            return _listManager.Get(id, CultureInfo.CurrentCulture);
        }

        public ContactList CreateList(Job job, string name, IEnumerable<Contact> addContacts, string listManagerOwner = DefaultListManagerOwner)
        {
            job.Status = $"Creating List {name}";

            var listId = Guid.NewGuid();
            var newDef = new ContactListModel
            {
                Name = name,
                Id = listId.ToString(),
                Type = ListType.ContactList.ToString(),
                //Owner = listManagerOwner,
                Description = "Generated list by Experience Generator"
            };

            using (new SecurityDisabler())
            {
                // list creation runs on a separate thread as 'extranet\anonymous', so SecurityDisabler on this thread does not apply
                Database master = Sitecore.Configuration.Factory.GetDatabase("master");
                var item = master.GetItem("/sitecore/system/Marketing Control Panel/Contact Lists");
                string accessRights = item["__Security"];
                try
                {
                    // temporarily give access to the anonymous user
                    item.Editing.BeginEdit();
                    item.Fields["__Security"].Value = "au|extranet\\Anonymous|pe|+item:create|+item:read|+item:rename|+item:delete|+item:write|+item:admin|pd|+item:create|+item:read|+item:rename|+item:delete|+item:write|+item:admin|";
                    item.Editing.EndEdit();

                    // create list
                    _contactListRepository.Add(newDef);
                }
                finally
                {
                    // restore original access rights
                    item.Editing.BeginEdit();
                    item["__Security"] = accessRights;
                    item.Editing.EndEdit();
                }
            }
            
            if (addContacts != null)
            {
                _subscriptionService.Subscribe(listId, addContacts);

            }

            job.CompletedLists++;
            return GetList(listId);

        }


        public IEnumerable<Contact> GetContacts(Job job, ContactList xaList)
        {
            string[] facets =
            {
                CollectionModel.FacetKeys.PersonalInformation,
                CollectionModel.FacetKeys.EmailAddressList,
                CollectionModel.FacetKeys.ListSubscriptions
            };
            var contactList = _listManager.Get(xaList.ContactListDefinition.Id, xaList.ContactListDefinition.Culture);
            var contactBatchEnumerator = _contactProvider.GetContactBatchEnumerator(contactList, 200, facets);
            var contacts = new List<Contact>();
            while (contactBatchEnumerator.MoveNext())
            {
                var batch = contactBatchEnumerator.Current;
                if (batch != null)
                    contacts.AddRange(batch.ToList());
            }

            return contacts;
        }


        public IEnumerable<Contact> GetContacts(MessageItem message, IEnumerable<Guid> excludeContacts)
        {
            string[] facets =
            {
                CollectionModel.FacetKeys.PersonalInformation,
                CollectionModel.FacetKeys.EmailAddressList,
                CollectionModel.FacetKeys.ListSubscriptions
            };
            var recipientManager = _recipientManagerFactory.GetRecipientManager(message);
            var contactBatchEnumerator = recipientManager.GetMessageRecipients(200,null, facets);

            var contacts = new List<Contact>();
            while (contactBatchEnumerator.MoveNext())
            {
                var batch = contactBatchEnumerator.Current;
                if (batch != null)
                    contacts.AddRange(batch.ToList());
            }

            return contacts.DistinctBy(x => x.Id).Where(x => x.Id.HasValue && !excludeContacts.Contains(x.Id.Value));
        }

        public bool Exists(ID contactListId)
        {
            return GetList(contactListId) != null;
        }
    }
}
