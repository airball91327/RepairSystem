﻿using System;
using System.ComponentModel.DataAnnotations;

namespace EDIS.Models.RepairModels
{
    public class RepairListVModel
    {
        [Display(Name = "類別")]
        public string DocType { get; set; }
        [Display(Name = "表單編號")]
        public string DocId { get; set; }
        [Display(Name = "財產編號")]
        public string AssetNo { get; set; }
        [Display(Name = "物品名稱")]
        public string AssetName { get; set; }
        [Display(Name = "廠牌")]
        public string Brand { get; set; }
        [Display(Name = "請修地點")]
        public string PlaceLoc { get; set; }
        [Display(Name = "詳細地點1")]
        public string Location1 { get; set; }
        [Display(Name = "詳細地點2")]
        public string Location2 { get; set; }
        [Display(Name = "型號")]
        public string Type { get; set; }
        [Display(Name = "申請部門代號")]
        public string ApplyDpt { get; set; }
        [Display(Name = "成本中心代號")]
        public string AccDpt { get; set; }
        [Display(Name = "成本中心名稱")]
        public string AccDptName { get; set; }
        [Display(Name = "故障描述")]
        public string TroubleDes { get; set; }
        [Display(Name = "處理狀態")]
        public string DealState { get; set; }
        [Display(Name = "處理描述")]
        public string DealDes { get; set; }
        [Display(Name = "費用")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? Cost { get; set; }
        [Display(Name = "天數")]
        public int? Days { get; set; }
        public string Flg { get; set; }
        public int FlowUid { get; set; }
        public string FlowCls { get; set; }
        [Display(Name = "申請日期")]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime ApplyDate { get; set; }
        public RepairModel repdata { get; set; }
    }
}
