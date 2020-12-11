﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EDIS.Areas.Admin.Models
{
    public class QryDocShutVModel
    {
        [Display(Name = "發票號碼")]
        public string qtyTICKETNO { get; set; }
        [Display(Name = "表單編號")]
        public string qtyDOCID { get; set; }
        [Display(Name = "表單類別")]
        public string qtyDOCTYPE { get; set; }
        [Display(Name = "關帳年月")]
        public string qtyApplyDate { get; set; }
        [Display(Name = "送單日")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime? qtySendDateFrom { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime? qtySendDateTo { get; set; }
        [Display(Name = "完帳日")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime? qtyShutDateFrom { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime? qtyShutDateTo { get; set; }
        [Display(Name = "完工日")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime? qtyCloseDateFrom { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime? qtyCloseDateTo { get; set; }

    }
}
