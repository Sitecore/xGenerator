using System;
using System.Collections.Generic;
using System.Linq;
using Colossus;
using Colossus.Integration.Processing;

namespace ExperienceGenerator.Parsing.Factories
{
    public class MvTestVariable : VisitorVariablesBase
    {
        public string Variants { get; set; }
        public string TestId { get; set; }

        public MvTestVariable(string testId, string variants)
        {
            Variants = variants;
            TestId = testId;
        }

        public override void SetValues(SimulationObject target)
        {
            target.Variables["MvVariants"] = Variants;

            //var variants = Variants();

            //if (variants.Any())
            //{
            //    target.Variables["MvVariants"] = variants;
            //target.Variables["MvVariants"] =
            //    new MvVariantData
            //    {
            //        PreferredExperience = int.Parse(variants),
            //        VariantWeight = 0
            //    };
            //}
        }

        public override IEnumerable<string> ProvidedVariables
        {
            get { yield return "MvVariants"; }
        }
    }
}
