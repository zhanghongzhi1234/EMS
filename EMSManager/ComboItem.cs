using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateProject
{
    public class ComboItem
    {
        protected string name;
		protected int value;
		
		public ComboItem(string name, int value)
		{
			this.name	= name;
			this.value	= value;
		}

		public override string ToString()
		{
			return this.name;
		}
    }
}
