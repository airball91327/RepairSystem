using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EDIS.Models.RepairModels
{
    public class RepFlowOpinionModel
    {
        [Key]
        [Display(Name = "案件編號")]
        public string DocId { get; set; }
        [Display(Name = "意見描述")]
        public string Opinions { get; set; }
        [Display(Name = "異動人員")]
        public int? Rtp { get; set; }
        [Display(Name = "異動時間")]
        public DateTime? Rtt { get; set; }
    }
}
