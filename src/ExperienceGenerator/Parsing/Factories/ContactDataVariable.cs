using System.Collections.Generic;
using Colossus;
using Faker;

namespace ExperienceGenerator.Parsing.Factories
{
    public class ContactDataVariable : VisitorVariableBase
    {
        public static int VisitorIndex;

        public double IdentifiedPercentage { get; set; }

        public ContactDataVariable(double identifiedPercentage)
        {
            IdentifiedPercentage = identifiedPercentage;
        }

        public override void SetValues(SimulationObject target)
        {
            if (!(Randomness.Random.NextDouble() < IdentifiedPercentage))
                return;

            target.Variables[VariableKey.ContactId] = "XGen" + VisitorIndex;
            VisitorIndex++;

            var firstname = Name.First();
            var lastname = Name.Last();
            var email = Internet.Email(firstname + " " + lastname);

            target.Variables[VariableKey.ContactFirstName] = firstname;
            target.Variables[VariableKey.ContactLastName] = lastname;
            target.Variables[VariableKey.ContactEmail] = email;
        }

        public override IEnumerable<VariableKey> ProvidedVariables => new[] {VariableKey.ContactId, VariableKey.ContactFirstName, VariableKey.ContactLastName, VariableKey.ContactEmail};
    }
}
