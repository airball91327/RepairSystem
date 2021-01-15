using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EDIS.Models
{
    public partial class ACPAYBLE_DETL
    {
        public int APPL_DATE { get; set; }
        public int APPL_NO { get; set; }
        public string ZONE { get; set; }
        public string INVC_SEQ { get; set; }
        public decimal INVC_AMT { get; set; }
        public decimal INVC_YYYMM { get; set; }
        public string DEPT { get; set; }
        public int TRANSE_CODE { get; set; }
        public string TRANSE_NOTE { get; set; }
        public decimal APPL_EMP_NO { get; set; }
        public int? OFF_SEQ { get; set; }
        public string INVC_NO { get; set; }
        public int? ACC_CODE { get; set; }
        public int? SUMM_CODE { get; set; }
        public int? SUBSID_CODE { get; set; }
        public int? CASH_CODE { get; set; }
        public string TAX_CODE { get; set; }
        public int? LOG_SEQ { get; set; }
        public int? PAID_OBJECT { get; set; }
        public string PAID_NAME { get; set; }
    }
}
