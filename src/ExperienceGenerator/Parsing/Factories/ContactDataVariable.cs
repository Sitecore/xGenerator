namespace ExperienceGenerator.Parsing.Factories
{
  using System;
  using System.Collections.Generic;
  using Colossus;
  using Faker;

  public class ContactDataVariable : VisitorVariablesBase

  {
    public double IdentifiedPercentage { get; set; }

    public ContactDataVariable(double identifiedPercentage)
    {
      this.IdentifiedPercentage = identifiedPercentage;
    }

    public override void SetValues(SimulationObject target)
    {
      if (Randomness.Random.NextDouble() < this.IdentifiedPercentage)
      {
        target.Variables["ContactId"] = "XGen" + Guid.NewGuid().ToString("N");


        var firstname = Name.First();
        var lastname = Name.Last();
        var email = Internet.Email(firstname + " " + lastname);

        target.Variables["ContactFirstName"] = firstname;
        target.Variables["ContactLastName"] = lastname;
        target.Variables["ContactEmail"] = email;

        //target.Variables["Cont"] = "Colossus" + Guid.NewGuid();
      }
    }

    public override IEnumerable<string> ProvidedVariables => new[]
    {
      "ContactId", "ContactFirstName", "ContactLastName", "ContactEmail"
    };
  }
}