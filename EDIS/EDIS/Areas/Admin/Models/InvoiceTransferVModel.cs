using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EDIS.Areas.Admin.Models
{
    public class InvoiceTransferVModel
    {
        public InvoiceTransferVModel()
        {
            WorkClass = "9";    //施工類別預設9
        }

        [Display(Name = "請修單號")]
        public List<RepairInvoice> RepairInvoices { get; set; }
        [Display(Name = "入帳年月")]
        public string AccountDate { get; set; }
        [Display(Name = "施工人員(外部人員預設)")]
        public string Engineer { get; set; }
        public int EngId { get; set; }
        [Display(Name = "負責部門(外部人員預設)")]
        public string EngDpt { get; set; }
        [Display(Name = "單據號碼")]
        public string InvoiceNo { get; set; }
        [Display(Name = "合計總價")]
        public decimal TotalCost { get; set; }

        [Display(Name = "施工類別")]    
        public string WorkClass { get; set; }
        [Display(Name = "性質")]
        public string Type { get; set; }
        [Display(Name = "使用部門(成本中心)")]
        public string AccDpt { get; set; }

    }

    public class InvoiceTransfer
    {
        [Display(Name = "請修單號")]
        public string DocId { get; set; }
        [Display(Name = "入帳年月")]
        public string AccountDate { get; set; }
        [Display(Name = "施工人員")]
        public string Engineer { get; set; }
        public int EngId { get; set; }
        [Display(Name = "負責部門")]
        public string EngDpt { get; set; }
        [Display(Name = "單據號碼")]
        public string InvoiceNo { get; set; }
        [Display(Name = "合計總價")]
        public decimal TotalCost { get; set; }
        [Display(Name = "施工類別")]
        public string WorkClass { get; set; }
        [Display(Name = "性質")]
        public string Type { get; set; }
        [Display(Name = "使用部門(成本中心)")]
        public string AccDpt { get; set; }
    }

    public class RepairInvoice
    {
        public string DocId { get; set; }
        public int SeqNo { get; set; }
        public string SignNo { get; set; }
    }
}
