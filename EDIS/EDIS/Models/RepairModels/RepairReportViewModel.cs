using System;
using System.ComponentModel.DataAnnotations;

namespace EDIS.Models.RepairModels
{
    public class RepairReportListVModel
    {
        [Display(Name = "姓名")]
        public string FullName { get; set; }
        [Display(Name = "增設件數")]
        public int RepAdds { get; set; }
        [Display(Name = "內修件數")]
        public int RepIns { get; set; }
        [Display(Name = "外修件數")]
        public int RepOuts { get; set; }
        [Display(Name = "內外修件數")]
        public int RepInOuts { get; set; }
        [Display(Name = "報廢件數")]
        public int RepScraps { get; set; }
        [Display(Name = "未處理件數")]
        public int RepNoDeals { get; set; }
        [Display(Name = "總件數")]
        public int RepTotals { get; set; }
        [Display(Name = "增設完成率")]
        public string RepAddEndRate { get; set; }
        [Display(Name = "內修完成率")]
        public string RepInEndRate { get; set; }
        [Display(Name = "外修完成率")]
        public string RepOutEndRate { get; set; }
        [Display(Name = "內外修完成率")]
        public string RepInOutEndRate { get; set; }
        [Display(Name = "增設結案率")]
        public string RepAddCloseRate { get; set; }
        [Display(Name = "內修結案率")]
        public string RepInCloseRate { get; set; }
        [Display(Name = "外修結案率")]
        public string RepOutCloseRate { get; set; }
        [Display(Name = "內外修結案率")]
        public string RepInOutCloseRate { get; set; }
        [Display(Name = "增設未結案率")]
        public string RepAddNotCloseRate { get; set; }
        [Display(Name = "內修未結案率")]
        public string RepInNotCloseRate { get; set; }
        [Display(Name = "外修未結案率")]
        public string RepOutNotCloseRate { get; set; }
        [Display(Name = "內外修未結案率")]
        public string RepInOutNotCloseRate { get; set; }
        [Display(Name = "維修且內修3日完成率")]
        public string RepInEndRate1 { get; set; }
        [Display(Name = "4~7日完成率")]
        public string RepInEndRate2 { get; set; }
        [Display(Name = "8日以上完成率")]
        public string RepInEndRate3 { get; set; }
        [Display(Name = "增設15日內完成率")]
        public string RepAddEndRate1 { get; set; }
        [Display(Name = "16~30日完成率")]
        public string RepAddEndRate2 { get; set; }
        [Display(Name = "31日以上完成率")]
        public string RepAddEndRate3 { get; set; }
        [Display(Name = "有費用件數")]
        public int IsChargedReps { get; set; }
        [Display(Name = "無費用件數")]
        public int NoChargedReps { get; set; }
        [Display(Name = "總費用")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal TotalCosts { get; set; }
        [Display(Name = "平均每件維修費用")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal AvgCosts { get; set; }
    }
}
