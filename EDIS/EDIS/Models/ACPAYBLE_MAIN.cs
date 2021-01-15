using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EDIS.Models
{
    public class ACPAYBLE_MAIN
    {
        public ACPAYBLE_MAIN()
        {
            APPL_DATE = (DateTime.Now.Year - 1911) * 10000 + (DateTime.Now.Month * 100)
                + DateTime.Now.Day;
            ZONE = "1";
            PHONE = 3033;
            IS_TEMP_PAY = "N";
            EXPECT_PAY_DAY =
                (DateTime.Now.Year - 1911) * 10000 + (DateTime.Now.Month * 100 + 1) + 15;
            IS_DEPT_AMT = "N";
            IS_PAPER = "N";
            STATUS = "申請中";
            RTT = DateTime.Now;
            INSTALLMENT = "N";
        }

        public int APPL_DATE { get; set; }
        public int APPL_NO { get; set; }
        public string ZONE { get; set; }
        public int? ACNT_NO { get; set; }
        public decimal APPL_EMP_NO { get; set; }
        public int PHONE { get; set; }
        public string REASONS { get; set; }
        public string IS_TEMP_PAY { get; set; }
        public int? EXPECT_PAY_DAY { get; set; }
        public string IS_DEPT_AMT { get; set; }
        public decimal APPL_TOTAL_AMT { get; set; }
        public decimal? BATCH_NO { get; set; }
        public int? ENTER_MONTH { get; set; }
        public string IS_PAPER { get; set; }
        public string STATUS { get; set; }
        public int? APPOINT_PAY_DAY { get; set; }
        public int? PAY_DAY { get; set; }
        public int? EXP_OFF_DAT { get; set; }
        public int? OFF_DAT { get; set; }
        public int? APVOF_DAT { get; set; }
        public string NOTE { get; set; }
        public decimal? APRVE_EMP { get; set; }
        public int? SLIP_TYPE { get; set; }
        public decimal RTP { get; set; }
        public System.DateTime RTT { get; set; }
        public int? OFF_SEQ { get; set; }
        public string HUMAN_FEE { get; set; }
        public int? PMT_SEQ { get; set; }
        public string OFF_APPL { get; set; }
        public string FEE_DEPT { get; set; }
        public string INSTALLMENT { get; set; }
        public string CONTRACT_NO { get; set; }
        public decimal? APRV_MGR { get; set; }
        public string IS_AGENT { get; set; }
        public string SLARY_TYPE { get; set; }
        public int? RELEASE_DAY { get; set; }
        public string SLARY_PAY { get; set; }
        public string SPECIAL { get; set; }

    }
}
