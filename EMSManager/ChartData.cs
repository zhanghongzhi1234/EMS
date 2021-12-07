using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateProject
{
    public class AxisData
    {
        public string Orientation;      //X or Y
        public string AxisType;             // Primary or Secondary
        public string Title;
        public string AxisMinimum;
        public string AxisMaximum;
        public string Interval;
        public string IntervalType;     //Auto, Number, Years, Months, Weeks, Days, Hours, Minutes, Seconds, Milliseconds
        public string Prefix;
        public string Suffix;
        public string ValueFormatString;        //"dd/MM/yyyy" for datetime, #,0.0# for number, it looks like 3,500.0
    }
    public class TrendLineData
    {
        public string Orientation;      //Vertical or Horizontal
        public string LineColor;
        public string Minimum;
        public string Maximum;
        public string Interval;
        public string LineThickness;
    }
    public class ChartData
    {
        public string name;
        public List<string> dataSourceList = new List<string>();            //draw 2 or more dataset in one chart
        public string Title;
        public string TitleFontSize = "11";
        public string ColumnWidth = "5";
        public string View3D = "false";       //true or false
        public string FreeText = "false";
        public List<AxisData> AxesData = null;
        public List<TrendLineData> TrendLinesData = null;

        /*public ChartData(string name, string[] dataSourceArray, string Title, string TitleFontSize, string ColumnWidth, string View3D, string FreeText,
            List<AxisData> AxesData, List<TrendLineData> TrendLinesData)
        {
            this.name = name;
            this.dataSourceArray = dataSourceArray;
            this.Title = Title;
            if(TitleFontSize != null) this.TitleFontSize = TitleFontSize;
            if(ColumnWidth != null) this.ColumnWidth = ColumnWidth;
            if(View3D != null) this.View3D = View3D;
            if(FreeText != null) this.FreeText = FreeText;
            this.AxesData = AxesData;
            this.TrendLinesData = TrendLinesData;
        }*/
    }
}
