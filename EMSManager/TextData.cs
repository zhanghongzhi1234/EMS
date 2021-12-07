using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateProject
{
    public class TextData
    {
        public string name;                 //name of textblock
        public string dataSourceName;       //name of datasource

        public TextData(string name, string dataSourceName)
        {
            this.name = name;
            this.dataSourceName = dataSourceName;
        }
    }
}
