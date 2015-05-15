using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Analytics.Model.Definitions;
using Sitecore.Analytics.Testing;

namespace Colossus.Integration
{
    public class TestInfo
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public byte[] Combination { get; set; }


        public List<TestVariableInfo> Variables { get; set; }

        public static TestInfo FromTestCombination(TestCombination combination)
        {
            if (combination == null || combination.Testset == null) return null;

            return new TestInfo
            {
                Id = combination.Testset.Id,
                Name = combination.Testset.Name,
                Combination = combination.Combination,
                Variables = combination.Testset.Variables.Select(v =>

                    new TestVariableInfo
                    {
                        Id = v.Id,
                        Label = v.Label,
                        Values =
                            v.Values.Select(vv => new TestValueInfo
                            {
                                Id = vv.Id,
                                Label = vv.Label,
                                IsOrginal = vv.IsOriginal
                            }).ToList()
                    }
                    ).ToList()
            };            
        }
    }

    public class TestVariableInfo
    {
        public Guid Id { get; set; }
        public string Label { get; set; }

        public List<TestValueInfo> Values { get; set; }
    }

    public class TestValueInfo
    {
        public Guid Id { get; set; }

        public string Label { get; set; }

        public bool IsOrginal { get; set; }
    }
}
