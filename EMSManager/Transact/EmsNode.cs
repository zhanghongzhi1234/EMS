using System;
using System.Collections.Generic;
//using System.Data.Linq.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TemplateProject
{
    public class EmsNode
    {
        //basic field
        public string pkey;
        public string name;
        public string locationkey;
        public string parentkey;
        public string equipment_code;
        public string power_subsystem;

        //expand field, only for leaf node
        /*public List<string> dpNameList1;       //SumActivePower entity name list
        public List<string> dpNameList2;       //NeSumActivePower entity name list
        public List<string> dpNameList3;       //SumReactivePower entity name list
        public List<string> dpNameList4;       //ReSumReactivePower entity name list*/
        public int SumActiveKey;                //entitykey of SumActivePower
        public int NeSumActiveKey;
        public int SumReactiveKey;
        public int NeSumReactiveKey;

        public void calculateDpKeyForLeaf()
        {
            if (equipment_code != "")
            {
                SumActiveKey = GetDpKeyForLeaf(equipment_code, locationkey, "SumActivePower");
                NeSumActiveKey = GetDpKeyForLeaf(equipment_code, locationkey, "NeSumActivePower");
                SumReactiveKey = GetDpKeyForLeaf(equipment_code, locationkey, "SumReactivePower");
                NeSumReactiveKey = GetDpKeyForLeaf(equipment_code, locationkey, "NeSumReactivePower");
            }
        }

        private int GetDpKeyForLeaf(string equipment_code, string locationkey, string dpNameRule)
        {
            string locationname = CachedMap.Instance.dicLocation[locationkey].name;
            string dpNameRuleValue = CachedMap.Instance.GetRunParam(dpNameRule);
            string dpNameLike = locationname + "%." + equipment_code + "." + dpNameRuleValue;      //for example: SHG%.IPA01.aiiEMS-Acurrent

            Regex regLike = CachedMap.Instance.LikeToRegex(dpNameLike);
            int dpKey = CachedMap.Instance.dicAllEntity.Where(p => regLike.IsMatch(p.Value)).FirstOrDefault().Key;
            return dpKey;
        }
    }
}
