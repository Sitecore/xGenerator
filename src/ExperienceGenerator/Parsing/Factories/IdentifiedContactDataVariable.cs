namespace ExperienceGenerator.Parsing.Factories
{
  using System.Collections.Generic;
  using Colossus;

  public class IdentifiedContactDataVariable : VisitorVariableBase
  {
    public override void SetValues(SimulationObject target)
    {
      target.Variables[VariableKey.ContactId] = this.Email;
      target.Variables[VariableKey.ContactFirstName] = this.FirstName;
      target.Variables[VariableKey.ContactMiddleName] = this.MiddleName;
      target.Variables[VariableKey.ContactLastName] = this.LastName;
      target.Variables[VariableKey.ContactEmail] = this.Email;
      target.Variables[VariableKey.ContactGender] = this.Gender;
      target.Variables[VariableKey.ContactBirthDate] = this.BirthDate;
      target.Variables[VariableKey.ContactJobTitle] = this.JobTitle;
      target.Variables[VariableKey.ContactPhone] = this.Phone;
      target.Variables[VariableKey.ContactPicture] = this.Picture;
      target.Variables[VariableKey.ContactAddress] = this.Address;
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


    public override IEnumerable<VariableKey> ProvidedVariables => new[]
    {
      VariableKey.ContactId,
      VariableKey.ContactFirstName,
      VariableKey.ContactMiddleName,
      VariableKey.ContactLastName,
      VariableKey.ContactEmail,
      VariableKey.ContactGender,
      VariableKey.ContactBirthDate,
      VariableKey.ContactJobTitle,
      VariableKey.ContactPhone,
      VariableKey.ContactPicture,
      VariableKey.ContactAddress
    };
  }
}
