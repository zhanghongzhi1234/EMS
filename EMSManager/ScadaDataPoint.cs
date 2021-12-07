using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateProject
{
    public class ScadaDataPoint
    {
        public int pkey;        //not required for OPC, only required for created datapoint proxy
        public string name;
        public object value;
        public int quality = 8;         //8: QUALITY_BAD_NOT_CONNECTED, 192: QUALITY_GOOD_NO_SPECIFIC_REASON
        public bool isObserved = false;
        public string valueName;

        public ScadaDataPoint(string name)
        {
            this.name = name;
            this.valueName = name + ".Value";
        }

        public ScadaDataPoint(int pkey, string name)
        {
            this.pkey = pkey;
            this.name = name;
        }

        public ScadaDataPoint(int pkey, string name, object value, int quality)
        {
            this.pkey = pkey;
            this.name = name;
            this.value = value;
            this.quality = quality;
        }

    }
}
