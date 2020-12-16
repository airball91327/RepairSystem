using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EDIS.Areas.Admin.Models
{
    public class QryPettyVModel
    {
        [Display(Name = "簽單號碼")]
        public string qtySIGNNO{ get; set; }
        [Display(Name = "廠商統編")]
        public string qtyVENDORNO { get; set; }
        [Display(Name = "廠商關鍵字")]
        public string qtyVENDORNAME { get; set; }
        [Display(Name = "請修單號")]
        public string qtyDOCID { get; set; }
        [Display(Name = "簽單狀態")]
        public string qtyTICKETSTATUS { get; set; }
        [Display(Name = "表單類別")]
        public string qtyDOCTYPE { get; set; }
    }
}
