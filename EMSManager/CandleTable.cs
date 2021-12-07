using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TemplateProject
{
    public class CandleTable
    {
        public DateTime time;
        public double open;
        public double high;
        public double low;
        public double close;
        public double change;
        public double change_percent;
        public double amplitude_percent;
        public decimal trading_lots;
        public decimal trading_money;

        public double macd_diff;
        public double macd_dea;
        public int macd_state;
        public double macd;
    }
}
