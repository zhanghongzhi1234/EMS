using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateProject
{
    public class TriggerData
    {
        public string Button;                 //name of Button triggering
        public string DataSourceName;       //name of datasource rule
        //public string Target;              //name of target, usually table or chart

        public TriggerData(string Button, string DataSourceName)
        {
            this.Button = Button;
            this.DataSourceName = DataSourceName;
            //this.Target = Target;
        }
    }
}
