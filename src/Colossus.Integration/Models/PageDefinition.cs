using System.Collections.Generic;

namespace Colossus.Integration.Models
{
    public class PageDefinition
    {
        public PageDefinition()
        {
            RequestVariables = new Dictionary<VariableKey, object>();
        }

        public string Path { get; set; }
        public Dictionary<VariableKey, object> RequestVariables { get; set; }
    }
}
