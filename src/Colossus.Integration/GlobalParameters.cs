using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colossus.Integration
{
    public sealed class GlobalParameters
    {
        static readonly GlobalParameters _instance = new GlobalParameters();

        public string OutputRecordLog { get; set; }

        public static GlobalParameters Instance
        {
            get
            {
                return _instance;
            }
        }
        GlobalParameters()
        {
        }
    }
}
