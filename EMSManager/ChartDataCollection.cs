using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateProject
{
    public class ChartDataCollection
    {
        public string name;                //chartName
        public List<ChartData> chartDataList = new List<ChartData>();                  //use one chart for many dataset, one time can only show one dataset
        int currentIndex = 0;       //current index of chartData

        public ChartDataCollection(string name, List<ChartData> chartDataList)
        {
            this.name = name;
            this.chartDataList = chartDataList;
        }

        public ChartData GetCurrentChartData()
        {
            return chartDataList[currentIndex];
        }

        public ChartData GetPreviousChartData()
        {
            ChartData ret;
            currentIndex--;
            if (currentIndex < 0)
            {
                currentIndex++;
                ret = null;
            }
            else
                ret = chartDataList[currentIndex];
            return ret;
        }

        public ChartData GetNextChartData()
        {
            ChartData ret;
            currentIndex++;
            if (currentIndex >= chartDataList.Count())
            {
                ret = null;
                currentIndex--;
            }
            else
                ret = chartDataList[currentIndex];
            return ret;
        }

        public ChartData GetNextChartDataCycle()
        {
            currentIndex++;
            if (currentIndex >= chartDataList.Count())
                currentIndex = 0;
            return chartDataList[currentIndex];
        }
    }
}
