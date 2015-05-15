using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossus;

namespace ExperienceGenerator.Parsing.Factories
{
    public class ContactDataVariable : VisitorVariablesBase

    {
        public double IdentifiedPercentage { get; set; }

        public ContactDataVariable(double identifiedPercentage)
        {
            IdentifiedPercentage = identifiedPercentage;
        }

        public override void SetValues(SimulationObject target)
        {
            if (Randomness.Random.NextDouble() < IdentifiedPercentage)
            {
                target.Variables["ContactId"] = "XGen" + Guid.NewGuid().ToString("N");


                var firstname = Faker.Name.First();
                var lastname = Faker.Name.Last();
                var email = Faker.Internet.Email(firstname + " " + lastname);

                target.Variables["ContactFirstName"] = firstname;
                target.Variables["ContactLastName"] = lastname;
                target.Variables["ContactEmail"] = email;

                //target.Variables["Cont"] = "Colossus" + Guid.NewGuid();
            }
        }

        public override IEnumerable<string> ProvidedVariables
        {
            get { return new[] { "ContactId", "ContactFirstName", "ContactLastName", "ContactEmail" }; }
        }
    }
}
