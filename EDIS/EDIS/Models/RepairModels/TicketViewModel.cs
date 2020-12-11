using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EDIS.Models.RepairModels
{
    public class TicketExcelViewModel
    {
        public TicketExcelViewModel()
        {
            TradeCode = "44";   //Default value.
        }

        [Display(Name = "一般支出申請編號")]
        public string Appl_No { get; set; }
        [Display(Name = "廠商統編")]
        public string UniteNo { get; set; }
        [Display(Name = "發票/簽單號碼")]
        public string TicketNo { get; set; }
        [Display(Name = "總價")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public int? TotalAmt { get; set; }
        [Display(Name = "憑證年月\n(發票年月)")]
        public string TicDate { get; set; }
        [Display(Name = "成本中心")]
        public string AccDpt { get; set; }
        [Display(Name = "交易代號")]
        public string TradeCode { get; set; }
        [Display(Name = "交易說明")]
        public string TradeMemo { get; set; }

    }
}
