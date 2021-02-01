using System.Linq;
using Sitecore.Analytics;
using Sitecore.Analytics.Tracking;
using Sitecore.Diagnostics;
using Sitecore.XConnect;
using Sitecore.XConnect.Client;
using Sitecore.XConnect.Collection.Model;
using Contact = Sitecore.XConnect.Contact;

namespace Colossus.Integration.Processing
{
    using System;
    using Colossus.Web;
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore;
    using Sitecore.Analytics.Model;
    using Sitecore.Data.Items;

    public class ContactDataProcessor : ISessionPatcher
    {
        public void UpdateSession(Session session, RequestInfo requestInfo)
        {
            #region Local Declarations

            var firstNameValue = string.Empty;
            var lastNameValue = string.Empty;
            var emailValue = string.Empty;
            var genderValue = string.Empty;
            var phoneNumberValue = string.Empty;
            var birthDateValue = DateTime.MinValue;
            var jobTitleValue = string.Empty;
            var phoneNumber = string.Empty;
            var avatar = string.Empty;
            var addressValue = string.Empty;
            Item itemValue = null;

            #endregion

            #region Get Personal Contact Information

            //First Name
            requestInfo.SetIfVariablePresent("ContactFirstName", firstName =>
            {
                firstNameValue = firstName;
            });
            //Last Name
            requestInfo.SetIfVariablePresent("ContactLastName", lastName =>
            {
                lastNameValue = lastName;
            });
            //Gender
            requestInfo.SetIfVariablePresent("ContactGender", gender =>
            {
                genderValue = gender;
            });
            //Birth Date
            requestInfo.SetIfVariablePresent("ContactBirthDate", date =>
            {
                if (string.IsNullOrEmpty(date))
                {
                    return;
                }

                birthDateValue = DateTime.ParseExact(date.Substring(0, 8), "yyyyMMdd", null);
            });
            //Job Title
            requestInfo.SetIfVariablePresent("ContactJobTitle", jobTitle =>
            {
                jobTitleValue = jobTitle;
            });

            #endregion

            #region Get Address Information

            //Address
            requestInfo.SetIfVariablePresent("ContactAddress", address =>
            {
                addressValue = address;
            });

            #endregion

            #region Get PhoneNumber

            //Phone Number
            requestInfo.SetIfVariablePresent("ContactPhone", phone =>
            {
                phoneNumberValue = phone;
            });

            #endregion

            #region Get Avatar Information

            //Picture
            requestInfo.SetIfVariablePresent("ContactPicture", pictureItemID =>
            {
                if (string.IsNullOrEmpty(pictureItemID))
                {
                    return;
                }

                var item = (Context.ContentDatabase ?? Context.Database).GetItem(pictureItemID);

                if (item == null)
                {
                    return;
                }

                itemValue = item;

            });

            #endregion

            #region Get Email Information

            requestInfo.SetIfVariablePresent("ContactEmail", email =>
            {
                emailValue = email;
            });

            #endregion

            var xGenIdentifier = Tracker.Current.Contact.Identifiers.FirstOrDefault(x => x.Source == "xGenerator");

            //Use the email to uniquely identify the contact between visits.
            if (xGenIdentifier == null)
            {
                var contactIdentificationManager = Sitecore.DependencyInjection.ServiceLocator.ServiceProvider.GetRequiredService<Sitecore.Analytics.Tracking.Identification.IContactIdentificationManager>();
                if (!String.IsNullOrWhiteSpace(emailValue))
                {
                    contactIdentificationManager.IdentifyAs(new Sitecore.Analytics.Tracking.Identification.KnownContactIdentifier("xGenerator", emailValue));
                }
                else
                {
                    contactIdentificationManager.IdentifyAs(new Sitecore.Analytics.Tracking.Identification.KnownContactIdentifier("xGenerator", Tracker.Current.Contact.ContactId.ToString("N")));
                }
            }

            var manager = Sitecore.Configuration.Factory.CreateObject("tracking/contactManager", true) as Sitecore.Analytics.Tracking.ContactManager;

            if (Tracker.Current.Contact.IsNew)
            {
                Log.Info($"ExperienceGenerator ContactDataProcessor: Tracker.Current.Contact.IsNew: {Tracker.Current.Contact.IsNew}, TrackerContactId: {Tracker.Current.Contact.ContactId:N}", this);

                if (manager != null)
                {
                    // Save contact to xConnect; at this point, a contact has an anonymous
                    // TRACKER IDENTIFIER, which follows a specific format. Do not use the contactId overload
                    // and make sure you set the ContactSaveMode as demonstrated
                    Sitecore.Analytics.Tracker.Current.Contact.ContactSaveMode = ContactSaveMode.AlwaysSave;
                    manager.SaveContactToCollectionDb(Sitecore.Analytics.Tracker.Current.Contact);

                    Log.Info($"ExperienceGenerator ContactDataProcessor: Session Identified using xGenerator", this);

                    // Now that the contact is saved, you can retrieve it using the tracker identifier

                    IdentifiedContactReference trackerIdentifier;
                    var anyIdentifier = Tracker.Current.Contact.Identifiers.FirstOrDefault(x => (x.Source == "xGenerator" || x.Source == Sitecore.Analytics.XConnect.DataAccess.Constants.IdentifierSource));
                    if (anyIdentifier != null)
                    {
                        trackerIdentifier = new IdentifiedContactReference(anyIdentifier.Source, anyIdentifier.Identifier); ;
                    }
                    else
                    {
                        trackerIdentifier = new IdentifiedContactReference(Sitecore.Analytics.XConnect.DataAccess.Constants.IdentifierSource, Tracker.Current.Contact.ContactId.ToString("N"));
                    }
                    
                    using (var client = Sitecore.XConnect.Client.Configuration.SitecoreXConnectClientConfiguration.GetClient())
                    {
                        try
                        {
                            var contact = client.Get<Contact>(trackerIdentifier, new ContactExecutionOptions());

                            if (contact != null)
                            {
                                Log.Info($"ExperienceGenerator ContactDataProcessor: FirstName: {firstNameValue}, LastName: {lastNameValue}, Email: {emailValue}", this);

                                client.SetFacet<PersonalInformation>(contact, PersonalInformation.DefaultFacetKey, new PersonalInformation()
                                {
                                    FirstName = firstNameValue ?? string.Empty,
                                    LastName = lastNameValue ?? string.Empty,
                                    Birthdate = birthDateValue,
                                    JobTitle = jobTitleValue ?? string.Empty,
                                    Gender = genderValue ?? string.Empty
                                });

                                Log.Info($"ExperienceGenerator ContactDataProcessor: PersonalInformationFacet set for New Contact", this);

                                if (!string.IsNullOrWhiteSpace(emailValue))
                                {
                                    var emails = new EmailAddressList(new EmailAddress(emailValue, true), "Home");
                                    client.SetFacet(contact, EmailAddressList.DefaultFacetKey, emails);
                                    Log.Info($"ExperienceGenerator ContactDataProcessor: EmailFacet set for New Contact", this);
                                }

                                if (!string.IsNullOrWhiteSpace(addressValue))
                                {
                                    var addresses = new AddressList(new Address { AddressLine1 = addressValue }, "Home");
                                    client.SetFacet(contact, AddressList.DefaultFacetKey, addresses);
                                    Log.Info($"ExperienceGenerator ContactDataProcessor: AddressFacet set for New Contact", this);
                                }

                                if (!string.IsNullOrWhiteSpace(phoneNumberValue))
                                {
                                    var phoneNumbers = new PhoneNumberList(new PhoneNumber(String.Empty, phoneNumberValue), "Home");
                                    client.SetFacet(contact, PhoneNumberList.DefaultFacetKey, phoneNumbers);
                                    Log.Info($"ExperienceGenerator ContactDataProcessor: PhoneNumberFacet set for New Contact", this);
                                }

                                client.Submit();
                            }
                        }
                        catch (XdbExecutionException ex)
                        {
                            Log.Error($"ExperienceGenerator ContactDataProcessor: There was an exception while trying to set facet information for new contact, ContactId: {Tracker.Current.Contact.ContactId:N}", ex, this);
                        }
                    }
                }
            }
            else if (!Tracker.Current.Contact.IsNew && manager != null)
            {
                Log.Info($"ExperienceGenerator ContactDataProcessor: Tracker.Current.Contact.IsNew: {Tracker.Current.Contact.IsNew}, TrackerContactId: {Tracker.Current.Contact.ContactId:N}", this);
                var anyIdentifier = Tracker.Current.Contact.Identifiers.FirstOrDefault(x => x.Source == "xGenerator");

                if (anyIdentifier != null)
                {
                    Log.Info($"ExperienceGenerator ContactDataProcessor: FirstName: {firstNameValue}, LastName: {lastNameValue}, Email: {emailValue}", this);
                    Log.Info($"ExperienceGenerator ContactDataProcessor: TrackerContactId: {Tracker.Current.Contact.ContactId:N}, Tracker.Current.Contact.IsNew: False, Tracker.Current.Contact.Facets: {Tracker.Current.Contact.Facets.Count}", this);
                    using (var client = Sitecore.XConnect.Client.Configuration.SitecoreXConnectClientConfiguration.GetClient())
                    {
                        try
                        {
                            var contact = client.Get<Contact>(
                                new IdentifiedContactReference(anyIdentifier.Source, anyIdentifier.Identifier),
                                new ContactExecutionOptions(
                                    new ContactExpandOptions(PersonalInformation.DefaultFacetKey,
                                        EmailAddressList.DefaultFacetKey,
                                        PhoneNumberList.DefaultFacetKey,
                                        AddressList.DefaultFacetKey)));

                            if (contact != null)
                            {
                                Log.Info($"ExperienceGenerator ContactDataProcessor: TrackerContactId: {Tracker.Current.Contact.ContactId:N}, XConnectContactId: {contact.Id.ToString()}, Contact using anyIdentifier loaded with requested ContactExpandOptions", this);

                                var personalInformationFacet = contact.GetFacet<PersonalInformation>(PersonalInformation.DefaultFacetKey);
                                var emailFacet = contact.GetFacet<EmailAddressList>(EmailAddressList.DefaultFacetKey);
                                var phoneNumbersFacet = contact.GetFacet<PhoneNumberList>(PhoneNumberList.DefaultFacetKey);
                                var addressFacet = contact.GetFacet<AddressList>(AddressList.DefaultFacetKey);

                                if (personalInformationFacet != null)
                                {
                                    Log.Info($"ExperienceGenerator ContactDataProcessor: TrackerContactId: {Tracker.Current.Contact.ContactId:N}, XConnectContactId: {contact.Id.ToString()}, PersonalInformationFacet is not null", this);
                                    personalInformationFacet.FirstName = firstNameValue ?? string.Empty;
                                    personalInformationFacet.LastName = lastNameValue ?? string.Empty;
                                    personalInformationFacet.Birthdate = birthDateValue;
                                    personalInformationFacet.JobTitle = jobTitleValue ?? string.Empty;
                                    personalInformationFacet.Gender = genderValue ?? string.Empty;

                                    client.SetFacet(contact, PersonalInformation.DefaultFacetKey, personalInformationFacet);
                                }
                                else
                                {
                                    Log.Info($"ExperienceGenerator ContactDataProcessor: TrackerContactId: {Tracker.Current.Contact.ContactId:N}, XConnectContactId: {contact.Id.ToString()}, PersonalInformationFacet is null", this);
                                    client.SetFacet<PersonalInformation>(contact, PersonalInformation.DefaultFacetKey, new PersonalInformation()
                                    {
                                        FirstName = firstNameValue ?? string.Empty,
                                        LastName = lastNameValue ?? string.Empty,
                                        Birthdate = birthDateValue,
                                        JobTitle = jobTitleValue ?? string.Empty,
                                        Gender = genderValue ?? string.Empty
                                    });
                                }

                                if (!string.IsNullOrWhiteSpace(emailValue))
                                {
                                    if (emailFacet != null)
                                    {
                                        if (string.IsNullOrEmpty(emailFacet.PreferredEmail.SmtpAddress))
                                        {
                                            Log.Info($"ExperienceGenerator ContactDataProcessor: TrackerContactId: {Tracker.Current.Contact.ContactId:N}, XConnectContactId: {contact.Id.ToString()}, EmailFacet is not null", this);
                                            emailFacet.PreferredEmail = new EmailAddress(emailValue, true);
                                            emailFacet.PreferredKey = "Home";
                                            client.SetFacet(contact, EmailAddressList.DefaultFacetKey, emailFacet);
                                        }
                                    }
                                    else
                                    {
                                        Log.Info($"ExperienceGenerator ContactDataProcessor: TrackerContactId: {Tracker.Current.Contact.ContactId:N}, XConnectContactId: {contact.Id.ToString()}, EmailFacet is null", this);
                                        var emails = new EmailAddressList(new EmailAddress(emailValue, true), "Home");
                                        client.SetFacet(contact, EmailAddressList.DefaultFacetKey, emails);
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(addressValue))
                                {
                                    if (addressFacet != null)
                                    {
                                        if (string.IsNullOrEmpty(addressFacet.PreferredAddress.AddressLine1))
                                        {
                                            Log.Info($"ExperienceGenerator ContactDataProcessor: TrackerContactId: {Tracker.Current.Contact.ContactId:N}, XConnectContactId: {contact.Id.ToString()}, AddressFacet is not null", this);
                                            addressFacet.PreferredAddress = new Address() { AddressLine1 = addressValue };
                                            addressFacet.PreferredKey = "Home";
                                            client.SetFacet(contact, AddressList.DefaultFacetKey, addressFacet);
                                        }
                                    }
                                    else
                                    {
                                        Log.Info($"ExperienceGenerator ContactDataProcessor: TrackerContactId: {Tracker.Current.Contact.ContactId:N}, XConnectContactId: {contact.Id.ToString()}, AddressFacet is null", this);
                                        var addresses = new AddressList(new Address { AddressLine1 = addressValue }, "Home");
                                        client.SetFacet(contact, AddressList.DefaultFacetKey, addresses);
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(phoneNumberValue))
                                {
                                    if (phoneNumbersFacet != null)
                                    {
                                        if (string.IsNullOrEmpty(phoneNumbersFacet.PreferredPhoneNumber.Number))
                                        {
                                            Log.Info($"ExperienceGenerator ContactDataProcessor: TrackerContactId: {Tracker.Current.Contact.ContactId:N}, XConnectContactId: {contact.Id.ToString()}, PhoneNumbersFacet is not null", this);
                                            phoneNumbersFacet.PreferredPhoneNumber = new PhoneNumber(String.Empty, phoneNumberValue);
                                            phoneNumbersFacet.PreferredKey = "Home";
                                            client.SetFacet(contact, PhoneNumberList.DefaultFacetKey, phoneNumbersFacet);
                                        }
                                    }
                                    else
                                    {
                                        Log.Info($"ExperienceGenerator ContactDataProcessor: TrackerContactId: {Tracker.Current.Contact.ContactId:N}, XConnectContactId: {contact.Id.ToString()}, PhoneNumbersFacet is null", this);
                                        var phoneNumbers = new PhoneNumberList(new PhoneNumber(String.Empty, phoneNumberValue), "Home");
                                        client.SetFacet(contact, PhoneNumberList.DefaultFacetKey, phoneNumbers);
                                    }
                                }

                                client.Submit();
                            }
                        }
                        catch (XdbExecutionException ex)
                        {
                            Log.Error($"ExperienceGenerator ContactDataProcessor: There was an exception while trying to set facet information for known contact, ContactId: {Tracker.Current.Contact.ContactId:N}", ex, this);
                        }
                    }
                }
            }
        }
    }
}
