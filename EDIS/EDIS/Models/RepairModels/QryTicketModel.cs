using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EDIS.Models.RepairModels
{
    public class QryTicketListData
    {
        [Display(Name = "發票號碼")]
        public string qtyTICKETNO { get; set; }
        [Display(Name = "廠商統編")]
        public string qtyVENDORNO { get; set; }
        [Display(Name = "廠商關鍵字")]
        public string qtyVENDORNAME { get; set; }
        [Display(Name = "請修單號")]
        public string qtyDOCID { get; set; }
        [Display(Name = "發票狀態")]
        public string qtyTICKETSTATUS { get; set; }
        [Display(Name = "作帳日")]
        [DataType(DataType.Date)]
        public DateTime? qtyApplyDateFrom { get; set; }
        [DataType(DataType.Date)]
        public DateTime? qtyApplyDateTo { get; set; }
        [Display(Name = "發票類別")]
        public string qtySTOCKTYPE { get; set; }
        [Display(Name = "表單類別")]
        public string qtyDOCTYPE { get; set; }
    }
}
