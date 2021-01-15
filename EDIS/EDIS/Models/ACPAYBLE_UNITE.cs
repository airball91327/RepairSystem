using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EDIS.Models
{
    public class ACPAYBLE_UNITE
    {
        public int APPL_DATE { get; set; }
        public int APPL_NO { get; set; }
        public string ZONE { get; set; }
        public string UNITE_NO { get; set; }
        public decimal AMT { get; set; }
        public string UNITE_NAME { get; set; }
        public int? SEQ { get; set; }
        public string PREPAY { get; set; }
    }
}
