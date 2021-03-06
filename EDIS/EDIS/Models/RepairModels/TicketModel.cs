﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EDIS.Models.RepairModels
{
    public class TicketModel
    {
        public TicketModel()
        {
            TradeCode = "44";   //Default value.
        }

        [Key]
        [Display(Name = "發票/簽單號碼")]
        public string TicketNo { get; set; }
        [Display(Name = "發票日期")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime? TicDate { get; set; }
        [Display(Name = "廠商代號")]
        public int? VendorId { get; set; }
        [NotMapped]
        [Display(Name = "廠商統編")]
        public string UniteNo { get; set; }
        [Display(Name = "廠商名稱")]
        public string VendorName { get; set; }
        [Display(Name = "總價")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public int TotalAmt { get; set; }
        [Display(Name = "稅額")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public int TaxAmt { get; set; }
        [NotMapped]
        [Display(Name = "累計")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal Amt { get; set; }
        [Display(Name = "備註")]
        public string Note { get; set; }
        [Display(Name = "殘值")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public int ScrapValue { get; set; }
        [Display(Name = "作帳日期")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime? ApplyDate { get; set; }
        [Display(Name = "取消日期")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime? CancelDate { get; set; }
        [Display(Name = "交易代號")]
        public string TradeCode { get; set; }
        [Display(Name = "是否關帳")]
        public string IsShuted { get; set; }
        [Display(Name = "一般支出申請編號")]
        public string Appl_No { get; set; }
        [Display(Name = "一般支出申請日期")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime? Appl_Date { get; set; }
        [Display(Name = "院區")]
        public string Zone { get; set; }
        //[Display(Name = "關帳日期")]
        //public DateTime? ShutDate { get; set; }
        [NotMapped]
        [Display(Name = "費用別")]
        public string StockType { get; set; }
        [NotMapped]
        [Display(Name = "交易說明")]
        public string TradeMemo { get; set; }
        [NotMapped]
        [Display(Name = "請修單/保養單")]
        public string DocType { get; set; }
        [NotMapped]
        [Display(Name = "成本中心")]
        public string AccDpt { get; set; }

        public ICollection<TicketDtlModel> TicketDtls { get; set; }
    }
}
