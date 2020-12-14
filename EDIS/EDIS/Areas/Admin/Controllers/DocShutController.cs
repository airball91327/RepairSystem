using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EDIS.Areas.Admin.Models;
using EDIS.Data;
using EDIS.Models.KeepModels;
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
            string qryDocType = qdata.qtyDOCTYPE;
            string qryShutStatus = qdata.qtyShutStatus;

            List<RepairListVModel> rv = new List<RepairListVModel>();
            List<KeepListVModel> kv = new List<KeepListVModel>();
            ViewData["SHUTSTATUS"] = qryShutStatus;
            if (qryDocType == "請修")
            {
                rv = GetRepairShutList(qdata);
                return PartialView("RepList", rv);
            }
            else
            {
                kv = GetKeepShutList(qdata);
                return PartialView("KeepList", kv);
            }
        }

        public List<RepairListVModel> GetRepairShutList(QryDocShutVModel qdata)
        {
            string ticketno = qdata.qtyTICKETNO;
            string docid = qdata.qtyDOCID;
            string qryDocType = qdata.qtyDOCTYPE;
            string qryShutStatus = qdata.qtyShutStatus;
            string qtyShutDate = qdata.qtyShutDate;
            DateTime? qtySendDateFrom = qdata.qtySendDateFrom;
            DateTime? qtySendDateTo = qdata.qtySendDateTo;
            DateTime? qtyApplyDateFrom = qdata.qtyApplyDateFrom;
            DateTime? qtyApplyDateTo = qdata.qtyApplyDateTo;
            DateTime? qtyEndDateFrom = qdata.qtyEndDateFrom;
            DateTime? qtyEndDateTo = qdata.qtyEndDateTo;
            if (!string.IsNullOrEmpty(docid))
                docid = docid.Trim();
            if (!string.IsNullOrEmpty(ticketno))
                ticketno = ticketno.ToUpper();

            var ts = _context.Tickets.AsQueryable();
            var repairCost = _context.RepairCosts.Include(r => r.TicketDtl).AsQueryable();
            var repair = _context.Repairs.AsQueryable();
            List<RepairListVModel> rv = new List<RepairListVModel>();

            if (!string.IsNullOrEmpty(ticketno))    //發票號碼
            {
                repairCost = repairCost.Where(rc => rc.TicketDtl.TicketDtlNo.ToUpper() == ticketno);
            }
            if (!string.IsNullOrEmpty(docid))   //表單編號
            {
                repair = repair.Where(r => r.DocId == docid);
            }
            if (qtyApplyDateFrom != null || qtyApplyDateTo != null)   //完帳日(發票作帳日)
            {
                if (qtyApplyDateFrom != null && qtyApplyDateTo != null)
                {
                    qtyApplyDateTo = qtyApplyDateTo.Value.AddDays(1).AddSeconds(-1);
                    ts = ts.Where(t => t.ApplyDate.Value >= qtyApplyDateFrom && t.ApplyDate.Value <= qtyApplyDateTo);
                }
                else
                {
                    var qDate = qtyApplyDateFrom.HasValue == true ? qtyApplyDateFrom.Value : qtyApplyDateTo.Value;
                    ts = ts.Where(t => t.ApplyDate.Value.Date == qDate.Date);
                }
                repairCost = repairCost.Where(rc => rc.StockType == "2" || rc.StockType == "4")
                                       .Join(ts, rc => rc.TicketDtl.TicketDtlNo, t => t.TicketNo,
                                       (rc, t) => rc);
            }

            var query = repair.Join(repairCost.Select(rc => rc.DocId).Distinct(),
                                    r => r.DocId, rc => rc, (r, rc) => r)
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
                              .Where(r => r.repdtl.CloseDate != null);   //篩選已結案的表單

            if (!string.IsNullOrEmpty(qryShutStatus))   //是否已經關帳
            {
                if (qryShutStatus == "已關帳")
                    query = query.Where(q => q.repdtl.ShutDate != null);
                else
                    query = query.Where(q => q.repdtl.ShutDate == null);
            }
            if (!string.IsNullOrEmpty(qtyShutDate)) //關帳年月
            {
                int year = Convert.ToInt32(qtyShutDate.Substring(0, 3)) + 1911;
                int month = Convert.ToInt32(qtyShutDate.Substring(3, 2));
                DateTime dateFrom = new DateTime(year, month, 1);
                DateTime dateTo = dateFrom.AddMonths(1).AddSeconds(-1);
                query = query.Where(q => q.repdtl.ShutDate >= dateFrom && q.repdtl.ShutDate <= dateTo);
            }
            if (qtySendDateFrom != null || qtySendDateTo != null)   //申請日
            {
                if (qtySendDateFrom != null && qtySendDateTo != null)
                {
                    qtySendDateTo = qtySendDateTo.Value.AddDays(1).AddSeconds(-1);
                    query = query.Where(q => q.repair.ApplyDate >= qtySendDateFrom && q.repair.ApplyDate <= qtySendDateTo);
                }
                else
                {
                    var qDate = qtySendDateFrom.HasValue == true ? qtySendDateFrom.Value : qtySendDateTo.Value;
                    query = query.Where(q => q.repair.ApplyDate.Date == qDate.Date);
                }
            }
            if (qtyEndDateFrom != null || qtyEndDateTo != null) //完工日
            {
                if (qtyEndDateFrom != null && qtyEndDateTo != null)
                {
                    qtyEndDateTo = qtyEndDateTo.Value.AddDays(1).AddSeconds(-1);
                    query = query.Where(q => q.repdtl.EndDate.Value >= qtyEndDateFrom && q.repdtl.EndDate.Value <= qtyEndDateTo);
                }
                else
                {
                    var qDate = qtyEndDateFrom.HasValue == true ? qtyEndDateFrom.Value : qtyEndDateTo.Value;
                    query = query.Where(q => q.repdtl.EndDate.Value.Date == qDate.Date);
                }
            }

            query.ToList()
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

            return rv;
        }

        public List<KeepListVModel> GetKeepShutList(QryDocShutVModel qdata)
        {
            string ticketno = qdata.qtyTICKETNO;
            string docid = qdata.qtyDOCID;
            string qryDocType = qdata.qtyDOCTYPE;
            string qtyShutDate = qdata.qtyShutDate;
            string qryShutStatus = qdata.qtyShutStatus;
            DateTime? qtySendDateFrom = qdata.qtySendDateFrom;
            DateTime? qtySendDateTo = qdata.qtySendDateTo;
            DateTime? qtyApplyDateFrom = qdata.qtyApplyDateFrom;
            DateTime? qtyApplyDateTo = qdata.qtyApplyDateTo;
            DateTime? qtyEndDateFrom = qdata.qtyEndDateFrom;
            DateTime? qtyEndDateTo = qdata.qtyEndDateTo;
            if (!string.IsNullOrEmpty(docid))
                docid = docid.Trim();
            if (!string.IsNullOrEmpty(ticketno))
                ticketno = ticketno.ToUpper();

            var ts = _context.Tickets.AsQueryable();
            var keepCost = _context.KeepCosts.Include(r => r.TicketDtl).AsQueryable();
            var keep = _context.Keeps.AsQueryable();
            List<KeepListVModel> kv = new List<KeepListVModel>();

            if (!string.IsNullOrEmpty(ticketno))    //發票號碼
            {
                keepCost = keepCost.Where(rc => rc.TicketDtl.TicketDtlNo.ToUpper() == ticketno);
            }
            if (!string.IsNullOrEmpty(docid))   //表單編號
            {
                keep = keep.Where(r => r.DocId == docid);
            }
            if (qtyApplyDateFrom != null || qtyApplyDateTo != null)   //完帳日(發票作帳日)
            {
                if (qtyApplyDateFrom != null && qtyApplyDateTo != null)
                {
                    qtyApplyDateTo = qtyApplyDateTo.Value.AddDays(1).AddSeconds(-1);
                    ts = ts.Where(t => t.ApplyDate.Value >= qtyApplyDateFrom && t.ApplyDate.Value <= qtyApplyDateTo);
                }
                else
                {
                    var qDate = qtyApplyDateFrom.HasValue == true ? qtyApplyDateFrom.Value : qtyApplyDateTo.Value;
                    ts = ts.Where(t => t.ApplyDate.Value.Date == qDate.Date);
                }
                keepCost = keepCost.Where(rc => rc.StockType == "2" || rc.StockType == "4")
                                       .Join(ts, rc => rc.TicketDtl.TicketDtlNo, t => t.TicketNo,
                                       (rc, t) => rc);
            }

            var query = keep.Join(keepCost.Select(kc => kc.DocId).Distinct(),
                                    k => k.DocId, kc => kc, (k, kc) => k)
                              .Join(_context.KeepDtls, k => k.DocId, d => d.DocId,
                                   (k, d) => new
                                   {
                                       keep = k,
                                       keepdtl = d
                                   })
                              .Join(_context.Departments, j => j.keep.AccDpt, d => d.DptId,
                                   (j, d) => new
                                   {
                                       keep = j.keep,
                                       keepdtl = j.keepdtl,
                                       dpt = d
                                   })
                              .Where(k => k.keepdtl.CloseDate != null);   //篩選已結案的表單

            if (!string.IsNullOrEmpty(qryShutStatus))   //是否已經關帳
            {
                if (qryShutStatus == "已關帳")
                    query = query.Where(q => q.keepdtl.ShutDate != null);
                else
                    query = query.Where(q => q.keepdtl.ShutDate == null);
            }
            if (!string.IsNullOrEmpty(qtyShutDate)) //關帳年月
            {
                int year = Convert.ToInt32(qtyShutDate.Substring(0, 3)) + 1911;
                int month = Convert.ToInt32(qtyShutDate.Substring(3, 2));
                DateTime dateFrom = new DateTime(year, month, 1);
                DateTime dateTo = dateFrom.AddMonths(1).AddSeconds(-1);
                query = query.Where(q => q.keepdtl.ShutDate >= dateFrom && q.keepdtl.ShutDate <= dateTo);
            }
            if (qtySendDateFrom != null || qtySendDateTo != null)   //申請日
            {
                if (qtySendDateFrom != null && qtySendDateTo != null)
                {
                    qtySendDateTo = qtySendDateTo.Value.AddDays(1).AddSeconds(-1);
                    query = query.Where(q => q.keep.SentDate.Value >= qtySendDateFrom && q.keep.SentDate.Value <= qtySendDateTo);
                }
                else
                {
                    var qDate = qtySendDateFrom.HasValue == true ? qtySendDateFrom.Value : qtySendDateTo.Value;
                    query = query.Where(q => q.keep.SentDate.Value.Date == qDate.Date);
                }
            }
            if (qtyEndDateFrom != null || qtyEndDateTo != null) //完工日
            {
                if (qtyEndDateFrom != null && qtyEndDateTo != null)
                {
                    qtyEndDateTo = qtyEndDateTo.Value.AddDays(1).AddSeconds(-1);
                    query = query.Where(q => q.keepdtl.EndDate.Value >= qtyEndDateFrom && q.keepdtl.EndDate.Value <= qtyEndDateTo);
                }
                else
                {
                    var qDate = qtyEndDateFrom.HasValue == true ? qtyEndDateFrom.Value : qtyEndDateTo.Value;
                    query = query.Where(q => q.keepdtl.EndDate.Value.Date == qDate.Date);
                }
            }

            query.ToList()
            .ForEach(j => kv.Add(new KeepListVModel
            {
                DocType = "保養",
                DocId = j.keep.DocId,
                PlaceLoc = j.keep.PlaceLoc,
                ApplyDpt = j.keep.DptId,
                AccDpt = j.keep.AccDpt,
                AccDptName = j.dpt.Name_C,
                Result = (j.keepdtl.Result == null || j.keepdtl.Result == 0) ? "" : _context.KeepResults.Find(j.keepdtl.Result).Title,
                InOut = j.keepdtl.InOut == "0" ? "自行" :
                        j.keepdtl.InOut == "1" ? "委外" :
                        j.keepdtl.InOut == "2" ? "租賃" :
                        j.keepdtl.InOut == "3" ? "保固" : "",
                Memo = j.keepdtl.Memo,
                Cost = j.keepdtl.Cost,
                Days = DateTime.Now.Subtract(j.keep.SentDate.GetValueOrDefault()).Days,
                Src = j.keep.Src,
                SentDate = j.keep.SentDate,
                EndDate = j.keepdtl.EndDate,
                IsCharged = j.keepdtl.IsCharged,
                keepdata = j.keep
            }));

            return kv;
        }

        [HttpPost]
        public IActionResult ShutRep(string repairs, string shutType, string shutDate = null)
        {
            if (shutType == "關帳")
            {
                // Get Login User's details.
                var ur = _context.AppUsers.Where(u => u.UserName == this.User.Identity.Name).FirstOrDefault();

                string[] s = repairs.Split(new char[] { ';' });
                RepairModel repair;
                RepairDtlModel repairDtl;
                RepairShutRecordsModel shutRecord = new RepairShutRecordsModel();
                foreach (string ss in s)
                {
                    repair = _context.Repairs.Find(ss);
                    if (repair != null)
                    {
                        // Update shut time on RepairDtl.
                        repairDtl = _context.RepairDtls.Find(ss);
                        repairDtl.ShutDate = DateTime.Now;
                        if (!string.IsNullOrEmpty(shutDate))
                        {
                            int year = Convert.ToInt32(shutDate.Substring(0, 3)) + 1911;
                            int month = Convert.ToInt32(shutDate.Substring(3, 2));
                            DateTime date = new DateTime(year, month, 1);
                            repairDtl.ShutDate = date;
                        }
                        _context.Entry(repairDtl).State = EntityState.Modified;
                        // Record the shut user on RepairShutRecords.
                        shutRecord.DocId = repairDtl.DocId;
                        shutRecord.Rtp = ur.Id;
                        shutRecord.Rtt = DateTime.Now;
                        _context.Add(shutRecord);
                        // Shut all tickets.
                        var rcs = _context.RepairCosts.Include(rc =>rc.TicketDtl).Where(rc => rc.DocId == repair.DocId).ToList();
                        if (rcs.Count() > 0)
                        {
                            foreach (var item in rcs)
                            {
                                var ticket = _context.Tickets.Find(item.TicketDtl.TicketDtlNo);
                                if (ticket != null)
                                {
                                    ticket.IsShuted = "Y";
                                    _context.Entry(ticket).State = EntityState.Modified;
                                }
                            }
                        }
                        _context.SaveChanges();
                    }
                }
            }
            else if(shutType == "反關帳")
            {
                string[] s = repairs.Split(new char[] { ';' });
                RepairModel repair;
                RepairDtlModel repairDtl;
                foreach (string ss in s)
                {
                    repair = _context.Repairs.Find(ss);
                    if (repair != null)
                    {
                        // Update shut time on RepairDtl.
                        repairDtl = _context.RepairDtls.Find(ss);
                        repairDtl.ShutDate = null;
                        _context.Entry(repairDtl).State = EntityState.Modified;
                        // delete the data of RepairShutRecords.
                        var shutRecord = _context.RepairShutRecords.Find(ss);
                        if (shutRecord != null)
                        {
                            _context.Remove(shutRecord);
                        }                     
                        // Reshut all tickets.
                        var rcs = _context.RepairCosts.Include(rc => rc.TicketDtl).Where(rc => rc.DocId == repair.DocId).ToList();
                        if (rcs.Count() > 0)
                        {
                            foreach (var item in rcs)
                            {
                                var ticket = _context.Tickets.Find(item.TicketDtl.TicketDtlNo);
                                if (ticket != null)
                                {
                                    ticket.IsShuted = "N";
                                    _context.Entry(ticket).State = EntityState.Modified;
                                }
                            }
                        }
                        _context.SaveChanges();
                    }
                }
            }

            return new JsonResult(repairs)
            {
                Value = new { success = true, error = "" }
            };
        }

        [HttpPost]
        public IActionResult ShutKeep(string keeps, string shutType, string shutDate = null)
        {
            if (shutType == "關帳")
            {
                // Get Login User's details.
                var ur = _context.AppUsers.Where(u => u.UserName == this.User.Identity.Name).FirstOrDefault();

                string[] s = keeps.Split(new char[] { ';' });
                KeepModel keep;
                KeepDtlModel keepDtl;
                foreach (string ss in s)
                {
                    keep = _context.Keeps.Find(ss);
                    if (keep != null)
                    {
                        // Update shut time on RepairDtl.
                        keepDtl = _context.KeepDtls.Find(ss);
                        keepDtl.ShutDate = DateTime.Now;
                        if (!string.IsNullOrEmpty(shutDate))
                        {
                            int year = Convert.ToInt32(shutDate.Substring(0, 3)) + 1911;
                            int month = Convert.ToInt32(shutDate.Substring(3, 2));
                            DateTime date = new DateTime(year, month, 1);
                            keepDtl.ShutDate = date;
                        }
                        _context.Entry(keepDtl).State = EntityState.Modified;
                        // Shut all tickets.
                        var kcs = _context.KeepCosts.Include(rc => rc.TicketDtl).Where(rc => rc.DocId == keep.DocId).ToList();
                        if (kcs.Count() > 0)
                        {
                            foreach (var item in kcs)
                            {
                                var ticket = _context.Tickets.Find(item.TicketDtl.TicketDtlNo);
                                if (ticket != null)
                                {
                                    ticket.IsShuted = "Y";
                                    _context.Entry(ticket).State = EntityState.Modified;
                                }
                            }
                        }
                        _context.SaveChanges();
                    }
                }
            }
            else if (shutType == "反關帳")
            {
                string[] s = keeps.Split(new char[] { ';' });
                KeepModel keep;
                KeepDtlModel keepDtl;
                foreach (string ss in s)
                {
                    keep = _context.Keeps.Find(ss);
                    if (keep != null)
                    {
                        // Update shut time on RepairDtl.
                        keepDtl = _context.KeepDtls.Find(ss);
                        keepDtl.ShutDate = null;
                        _context.Entry(keepDtl).State = EntityState.Modified;
                        // Reshut all tickets.
                        var kcs = _context.KeepCosts.Include(rc => rc.TicketDtl).Where(rc => rc.DocId == keep.DocId).ToList();
                        if (kcs.Count() > 0)
                        {
                            foreach (var item in kcs)
                            {
                                var ticket = _context.Tickets.Find(item.TicketDtl.TicketDtlNo);
                                if (ticket != null)
                                {
                                    ticket.IsShuted = "N";
                                    _context.Entry(ticket).State = EntityState.Modified;
                                }
                            }
                        }
                        _context.SaveChanges();
                    }
                }
            }

            return new JsonResult(keeps)
            {
                Value = new { success = true, error = "" }
            };
        }


    }
}
