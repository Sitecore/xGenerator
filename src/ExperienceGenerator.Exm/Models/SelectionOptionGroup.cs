using System.Collections.Generic;

namespace ExperienceGenerator.Exm.Models
{
    public class SelectionOptionGroup
    {
        public string Label { get; set; }

        public IEnumerable<SelectionOption> Options { get; set; }
    }
}