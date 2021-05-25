using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EDIS.Models.RepairModels
{
    public class QryRepListData
    {
        public string qtyDOCID { get; set; }
        public string qtyASSETNO { get; set; }
        public string qtyACCDPT { get; set; }
        public string qtyASSETNAME { get; set; }
        public string qtyFLOWTYPE { get; set; }
        public string qtyDPTID { get; set; }
        public string qtyDealStatus { get; set; }
        [DataType(DataType.Date)]
        [Column(TypeName = "Date")]
        public string qtyApplyDateFrom { get; set; }
        [DataType(DataType.Date)]
        [Column(TypeName = "Date")]
        public string qtyApplyDateTo { get; set; }
        public string qtyDateType { get; set; }
        public string qtyIsCharged { get; set; }
        public bool qtySearchAllDoc { get; set; }
        public string qtyRepType { get; set; }
        public string qtyOrderType { get; set; }
        public string qtyTroubleDes { get; set; }
        public string qtyEngId { get; set; }
    }
    public class AssetQryResult
    {
        public string ASSET_NO { get; set; }
        public string NAME_C { get; set; }
        public string ACC_DPT { get; set; }
        public string DELIV_DPT { get; set; }
        public string ACCDATE { get; set; }
    }

}
