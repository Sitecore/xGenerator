namespace ExperienceGenerator.Parsing.Factories
{
  using System.Collections.Generic;
  using Colossus;

  public class IdentifiedContactDataVariable : VisitorVariablesBase
  {
    public override void SetValues(SimulationObject target)
    {
      target.Variables["ContactId"] = this.Email;
      target.Variables["ContactFirstName"] = this.FirstName;
      target.Variables["ContactMiddleName"] = this.MiddleName;
      target.Variables["ContactLastName"] = this.LastName;
      target.Variables["ContactEmail"] = this.Email;
      target.Variables["ContactGender"] = this.Gender;
      target.Variables["ContactBirthDate"] = this.BirthDate;
      target.Variables["ContactJobTitle"] = this.JobTitle;
      target.Variables["ContactPhone"] = this.Phone;
      target.Variables["ContactPicture"] = this.Picture;
      target.Variables["ContactAddress"] = this.Address;
    }

    public string Email { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string BirthDate { get; set; }
    public string Gender { get; set; }
    public string JobTitle { get; set; }
    public string Phone { get; set; }
    public string Picture { get; set; }
    public string Address { get; set; }


    public override IEnumerable<string> ProvidedVariables => new[]
    {
      "ContactId",
      "ContactFirstName",
      "ContactMiddleName",
      "ContactLastName",
      "ContactEmail",
      "ContactGender",
      "ContactBirthDate",
      "ContactJobTitle",
      "ContactPhone",
      "ContactPicture",
      "ContactAddress"
    };
  }
}