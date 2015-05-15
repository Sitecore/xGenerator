using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Data.Items;

namespace Colossus.Integration
{

    public class RenderingInfo
    {

        public ItemInfo Item { get; set; }

        public string Conditions { get; set; }
        public string DataSource { get; set; }
        public string MultiVariateTest { get; set; }
        public string Parameters { get; set; }
        public string PersonalizationTest { get; set; }
        public string Placeholder { get; set; }

    }
}
