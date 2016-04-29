namespace ExperienceGenerator.Tests
{
  using System;
  using System.IO;
  using Colossus.Integration.Processing;
  using Colossus.Web;
  using ExperienceGenerator.Tests.Facets;
  using FluentAssertions;
  using NSubstitute;
  using Ploeh.AutoFixture.AutoNSubstitute;
  using Sitecore.Analytics.Model.Entities;
  using Sitecore.Analytics.Tracking;
  using Sitecore.Data.Items;
  using Sitecore.FakeDb;
  using Sitecore.FakeDb.AutoFixture;
  using Sitecore.FakeDb.Resources.Media;
  using Sitecore.Resources.Media;
  using Xunit;

  public class ContactDataProcessorTests
  {
    [Theory]
    [AutoDbData]
    public void UpdateSession_ContactPicture_SetWithValidItemId_ShouldSetContactPictureStream(Db db, byte[] streamContent, [Content] Item item, ContactDataProcessor processor, [Substitute] MediaProvider mediaProvider, [Substitute] Session session, RequestInfo request)
    {
      request.Variables.Add("ContactPicture", item.ID.ToString());
      var ms = new MediaStream(new MemoryStream(streamContent), "someExt", new MediaItem(item));
      mediaProvider.GetMedia(Arg.Any<MediaUri>()).GetStream().Returns(ms);

      using (new MediaProviderSwitcher(mediaProvider))
      {
        processor.UpdateSession(session, request);
        session.Contact.GetFacet<IContactPicture>("Picture").Picture.Should().BeEquivalentTo(streamContent);
      }
    }

    [Theory]
    [AutoDbData]
    public void UpdateSession_ContactPicture_SetWithInalidItemId_ShouldNotOverwriteContactPicture(Db db, byte[] streamContent, [Content] Item item, ContactDataProcessor processor, [Substitute] MediaProvider mediaProvider, [Substitute] Session session, RequestInfo request)
    {
      request.Variables.Add("ContactPicture", Guid.NewGuid().ToString());
      session.Contact.GetFacet<IContactPicture>("Picture").Picture = streamContent;

      processor.UpdateSession(session, request);

      session.Contact.GetFacet<IContactPicture>("Picture").Picture.Should().BeEquivalentTo(streamContent);
    }


    [Theory]
    [AutoDbData]
    public void UpdateSession_ContactPicture_EmptyID_ShouldNotOverwriteContactPicture(Db db, byte[] streamContent, [Content] Item item, ContactDataProcessor processor, [Substitute] MediaProvider mediaProvider, [Substitute] Session session, RequestInfo request)
    {
      request.Variables.Add("ContactPicture", string.Empty);
      session.Contact.GetFacet<IContactPicture>("Picture").Picture = streamContent;

      processor.UpdateSession(session, request);

      session.Contact.GetFacet<IContactPicture>("Picture").Picture.Should().BeEquivalentTo(streamContent);
    }


    [Theory]
    [AutoDbData]
    public void UpdateSession_PersonalInfo_ValidData_ShouldBeSetInFacet(string firstName, string lastName, string middleName, string gender, string job, ContactDataProcessor processor, [Substitute] Session session, RequestInfo request)
    {
      request.Variables.Add("ContactFirstName", firstName);
      request.Variables.Add("ContactLastName", lastName);
      request.Variables.Add("ContactMiddleName", middleName);
      request.Variables.Add("ContactBirthDate", "20160102");
      request.Variables.Add("ContactGender", gender);
      request.Variables.Add("ContactJobTitle", job);

      processor.UpdateSession(session, request);

      var facet = session.Contact.GetFacet<IContactPersonalInfo>("Personal");
      facet.FirstName.Should().BeEquivalentTo(firstName);
      facet.Surname.Should().BeEquivalentTo(lastName);
      facet.MiddleName.Should().BeEquivalentTo(middleName);
      facet.BirthDate.Value.Year.Should().Be(2016);
      facet.BirthDate.Value.Day.Should().Be(2);
      facet.BirthDate.Value.Month.Should().Be(1);
      facet.Gender.Should().BeEquivalentTo(gender);
      facet.JobTitle.Should().BeEquivalentTo(job);
    }

    [Theory]
    [AutoDbData]
    public void UpdateSession_ContactInformation_ShouldBeSetInFacet(string email, string phone, string address, ContactDataProcessor processor, [Substitute] Session session, RequestInfo request)
    {
      request.Variables.Add("ContactEmail", email);
      request.Variables.Add("ContactPhone", phone);
      request.Variables.Add("ContactAddress", address);

      session.Contact.GetFacet<IContactAddresses>("Addresses").Returns(new ContactAddresses());
      session.Contact.GetFacet<IContactPhoneNumbers>("Phone Numbers").Returns(new ContactPhones());
      session.Contact.GetFacet<IContactEmailAddresses>("Emails").Returns(new ContactEmails());


      processor.UpdateSession(session, request);

      var addresses = session.Contact.GetFacet<IContactAddresses>("Addresses");
      addresses.Entries[addresses.Preferred].StreetLine1.Should().Be(address);

      var phones = session.Contact.GetFacet<IContactPhoneNumbers>("Phone Numbers");
      phones.Entries[phones.Preferred].Number.Should().Be(phone);

      var emails = session.Contact.GetFacet<IContactEmailAddresses>("Emails");
      emails.Entries[emails.Preferred].SmtpAddress.Should().Be(email);
    }
  }
}