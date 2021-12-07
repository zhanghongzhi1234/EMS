using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace TemplateProject
{
    public class RawTable
    {
        public object XValue;
        public double YValue;
        public Brush Color = null;
        public object Tag = null;
        public string LabelText;

        public RawTable(object XValue, double YValue)
        {
            this.XValue = XValue;
            this.YValue = YValue;
        }

        public RawTable(object XValue, double YValue, Brush Color)
        {
            this.XValue = XValue;
            this.YValue = YValue;
            this.Color = Color;
        }

        public RawTable(object XValue, double YValue, string LabelText)
        {
            this.XValue = XValue;
            this.YValue = YValue;
            this.LabelText = LabelText;
        }

        public RawTable(object XValue, double YValue, object Tag)
        {
            this.XValue = XValue;
            this.YValue = YValue;
            this.Tag = Tag;
        }

        public RawTable(object XValue, double YValue, Brush Color, object Tag)
        {
            this.XValue = XValue;
            this.YValue = YValue;
            this.Color = Color;
            this.Tag = Tag;
        }
    }
}
