using System.Collections.Generic;
using System.Linq;
using Colossus;
using Faker;
using Sitecore.Analytics;
using Sitecore.Analytics.Model;
using Sitecore.Diagnostics;

namespace ExperienceGenerator.Parsing.Factories
{
    public class ContactDataVariable : VisitorVariablesBase
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

            target.Variables["ContactId"] = "XGen" + VisitorIndex;
            VisitorIndex++;

            var firstName = Name.First();
            var lastName = Name.Last();

            target.Variables["ContactFirstName"] = firstName;
            target.Variables["ContactLastName"] = lastName;
            target.Variables["ContactEmail"] = Internet.Email(firstName + " " + lastName);
        }

        public override IEnumerable<string> ProvidedVariables => new[] { "ContactId", "ContactFirstName", "ContactLastName", "ContactEmail" };
    }
}
