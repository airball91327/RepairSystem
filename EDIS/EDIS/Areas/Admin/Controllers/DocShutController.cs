using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EDIS.Areas.Admin.Models;
using EDIS.Data;
using EDIS.Models.RepairModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EDIS.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class DocShutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DocShutController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // POST: Admin/RepairShut
        [HttpPost]
        public IActionResult Index(QryDocShutVModel qdata)
        {
            string ticketno = qdata.qtyTICKETNO;
            string docid = qdata.qtyDOCID;
            string qryDocType = qdata.qtyDOCTYPE;
            string qtyApplyDate = qdata.qtyApplyDate;
            DateTime? qtySendDateFrom = qdata.qtySendDateFrom;
            DateTime? qtySendDateTo = qdata.qtySendDateTo;
            DateTime? qtyShutDateFrom = qdata.qtyShutDateFrom;
            DateTime? qtyShutDateTo = qdata.qtyShutDateTo;
            DateTime? qtyCloseDateFrom = qdata.qtyCloseDateFrom;
            DateTime? qtyCloseDateTo = qdata.qtyCloseDateTo;
            if (!string.IsNullOrEmpty(docid))
                docid = docid.Trim();
            if (!string.IsNullOrEmpty(ticketno))
                ticketno = ticketno.ToUpper();

            var ts = _context.Tickets.AsQueryable();
            var repairCost = _context.RepairCosts.Include(r => r.TicketDtl).AsQueryable();
            var repair = _context.Repairs.AsQueryable();
            List<RepairListVModel> rv = new List<RepairListVModel>();

            if (!string.IsNullOrEmpty(ticketno))
            {
                //repairCost = repairCost.Where(rc => rc.StockType != "3").ToList();  //篩選掉簽單的資料
                repairCost = repairCost.Where(rc => rc.TicketDtl.TicketDtlNo.ToUpper() == ticketno);
            }
            if (!string.IsNullOrEmpty(docid))
            {
                repair = repair.Where(r => r.DocId == docid);
            }

            var docIdList = repairCost.Select(rc => rc.DocId).Distinct();
            repair.Join(docIdList, r => r.DocId, li => li, (r, li) => r)
                  .Join(_context.RepairDtls, r => r.DocId, d => d.DocId,
                       (r, d) => new
                       {
                           repair = r,
                           repdtl = d
                       })
                       .Join(_context.Departments, j => j.repair.AccDpt, d => d.DptId,
                       (j, d) => new
                       {
                           repair = j.repair,
                           repdtl = j.repdtl,
                           dpt = d
                       })
                  .Where(r => r.repdtl.CloseDate != null)   //篩選已結案的請修單
                  .Where(r => r.repdtl.ShutDate == null)    //篩選尚未關帳的請修單
                  .ToList()
                  .ForEach(j => rv.Add(new RepairListVModel
                  {
                      DocType = "請修",
                      RepType = j.repair.RepType,
                      DocId = j.repair.DocId,
                      ApplyDate = j.repair.ApplyDate,
                      AssetNo = j.repair.AssetNo,
                      AssetName = j.repair.AssetName,
                      PlaceLoc = j.repair.LocType,
                      ApplyDpt = j.repair.DptId,
                      AccDpt = j.repair.AccDpt,
                      AccDptName = j.dpt.Name_C,
                      TroubleDes = j.repair.TroubleDes,
                      DealState = _context.DealStatuses.Find(j.repdtl.DealState).Title,
                      DealDes = j.repdtl.DealDes,
                      Cost = j.repdtl.Cost,
                      Days = DateTime.Now.Subtract(j.repair.ApplyDate).Days,
                      EndDate = j.repdtl.EndDate,
                      CloseDate = j.repdtl.CloseDate,
                      IsCharged = j.repdtl.IsCharged,
                      repdata = j.repair
                  }));


            /* Search date by Date. */
            //if (string.IsNullOrEmpty(qtyDate1) == false || string.IsNullOrEmpty(qtyDate2) == false)
            //{
            //    ts = ts.Where(t => t.ShutDate >= applyDateFrom && t.ShutDate <= applyDateTo).ToList();
            //}

            return PartialView("List", rv);
        }

    }
}
