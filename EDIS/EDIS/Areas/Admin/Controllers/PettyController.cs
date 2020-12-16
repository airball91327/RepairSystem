using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EDIS.Data;
using EDIS.Models.RepairModels;
using Microsoft.AspNetCore.Authorization;
using EDIS.Areas.Admin.Models;
using EDIS.Models.KeepModels;
using EDIS.Repositories;
using EDIS.Models.Identity;

namespace EDIS.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class PettyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<AppUserModel, int> _userRepo;

        public PettyController(ApplicationDbContext context,
                               IRepository<AppUserModel, int> userRepo)
        {
            _context = context;
            _userRepo = userRepo;
        }

        // GET: Admin/Petty
        public async Task<IActionResult> Index()
        {
            return View();
        }

        // POST: Admin/Petty
        [HttpPost]
        public async Task<IActionResult> Index(QryPettyVModel qdata)
        {
            string qryDocType = qdata.qtyDOCTYPE;

            List<RepairCostModel> rv = new List<RepairCostModel>();
            List<KeepCostModel> kv = new List<KeepCostModel>();
            if (qryDocType == "請修")
            {
                rv = GetRepPettyList(qdata);
                return PartialView("RepPettyList", rv);
            }
            else
            {
                kv = GetKeepPettyList(qdata);
                return PartialView("KeepPettyList", kv);
            }
        }

        // GET: Admin/Petty/RepCostEdit/5
        public IActionResult RepCostEdit(string docid, string seqno)
        {
            RepairCostModel repairCost = _context.RepairCosts.Include(rc => rc.TicketDtl)
                                                             .SingleOrDefault(rc => rc.DocId == docid && rc.SeqNo == Convert.ToInt32(seqno));

            if (repairCost.StockType == "0")
                ViewData["StockType"] = "庫存";
            else if (repairCost.StockType == "2")
                ViewData["StockType"] = "發票(含收據)";
            else if (repairCost.StockType == "4")
                ViewData["StockType"] = "零用金";
            else
                ViewData["StockType"] = "簽單";

            return View(repairCost);
        }

        // POST: Admin/Petty/RepCostEdit/5
        [HttpPost]
        public IActionResult RepCostEdit(RepairCostModel repairCost)
        {
            var ur = _userRepo.Find(u => u.UserName == this.User.Identity.Name).FirstOrDefault();
            if (repairCost.SignNo != null)
            {
                repairCost.SignNo = repairCost.SignNo.ToUpper();
            }
            if (repairCost.StockType == "3")
            {
                ModelState.Remove("TicketDtl.SeqNo");
            }
            if (ModelState.IsValid)
            {
                repairCost.Rtp = ur.Id;
                repairCost.Rtt = DateTime.Now;
                _context.Entry(repairCost).State = EntityState.Modified;
                _context.SaveChanges();

                RepairDtlModel dtl = _context.RepairDtls.Where(d => d.DocId == repairCost.DocId)
                                                        .FirstOrDefault();
                if (dtl != null)
                {
                    dtl.Cost = _context.RepairCosts.Where(k => k.DocId == repairCost.DocId)
                                                   .Select(k => k.TotalCost)
                                                   .DefaultIfEmpty(0).Sum();
                    _context.Entry(dtl).State = EntityState.Modified;
                    _context.SaveChanges();
                }

                return new JsonResult(repairCost)
                {
                    Value = new { success = true, error = "" }
                };
            }
            else
            {
                string msg = "";
                foreach (var error in ViewData.ModelState.Values.SelectMany(modelState => modelState.Errors))
                {
                    msg += error.ErrorMessage + Environment.NewLine;
                }
                throw new Exception(msg);
            }
        }

        // GET: Admin/Petty/KeepCostEdit/5
        public IActionResult KeepCostEdit(string docid, string seqno)
        {
            KeepCostModel keepCost = _context.KeepCosts.Include(rc => rc.TicketDtl)
                                                       .SingleOrDefault(rc => rc.DocId == docid && rc.SeqNo == Convert.ToInt32(seqno));

            if (keepCost.StockType == "0")
                ViewData["StockType"] = "庫存";
            else if (keepCost.StockType == "2")
                ViewData["StockType"] = "發票(含收據)";
            else if (keepCost.StockType == "4")
                ViewData["StockType"] = "零用金";
            else
                ViewData["StockType"] = "簽單";

            return View(keepCost);
        }

        // POST: Admin/Petty/KeepCostEdit/5
        [HttpPost]
        public IActionResult KeepCostEdit(KeepCostModel keepCost)
        {
            var ur = _userRepo.Find(u => u.UserName == this.User.Identity.Name).FirstOrDefault();
            if (keepCost.SignNo != null)
            {
                keepCost.SignNo = keepCost.SignNo.ToUpper();
            }
            if (keepCost.StockType == "3")
            {
                ModelState.Remove("TicketDtl.SeqNo");
            }
            if (ModelState.IsValid)
            {
                keepCost.Rtp = ur.Id;
                keepCost.Rtt = DateTime.Now;
                _context.Entry(keepCost).State = EntityState.Modified;
                _context.SaveChanges();

                KeepDtlModel dtl = _context.KeepDtls.Where(d => d.DocId == keepCost.DocId)
                                                    .FirstOrDefault();
                if (dtl != null)
                {
                    dtl.Cost = _context.KeepCosts.Where(k => k.DocId == keepCost.DocId)
                                                 .Select(k => k.TotalCost)
                                                 .DefaultIfEmpty(0).Sum();
                    _context.Entry(dtl).State = EntityState.Modified;
                    _context.SaveChanges();
                }

                return new JsonResult(keepCost)
                {
                    Value = new { success = true, error = "" }
                };
            }
            else
            {
                string msg = "";
                foreach (var error in ViewData.ModelState.Values.SelectMany(modelState => modelState.Errors))
                {
                    msg += error.ErrorMessage + Environment.NewLine;
                }
                throw new Exception(msg);
            }
        }

        // GET: Admin/Petty/RepDelete/5
        public async Task<IActionResult> RepDelete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var repairCostModel = await _context.RepairCosts
                .FirstOrDefaultAsync(m => m.DocId == id);
            if (repairCostModel == null)
            {
                return NotFound();
            }

            return View(repairCostModel);
        }

        // POST: Admin/Petty/RepDelete/5
        [HttpPost]
        public ActionResult RepDelete(string docid, string seqno)
        {
            try
            {
                RepairCostModel repairCost = _context.RepairCosts.Find(docid, Convert.ToInt32(seqno));
                _context.RepairCosts.Remove(repairCost);
                _context.SaveChanges();
                //
                RepairDtlModel dtl = _context.RepairDtls.Where(d => d.DocId == repairCost.DocId)
                                                        .FirstOrDefault();
                if (dtl != null)
                {
                    dtl.Cost = _context.RepairCosts.Where(k => k.DocId == repairCost.DocId)
                                                   .Select(k => k.TotalCost)
                                                   .DefaultIfEmpty(0).Sum();
                    _context.Entry(dtl).State = EntityState.Modified;
                    _context.SaveChanges();
                }
                return new JsonResult(repairCost)
                {
                    Value = new { success = true, error = "" }
                };
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        // GET: Admin/Petty/KeepDelete/5
        public async Task<IActionResult> KeepDelete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var keepCostModel = await _context.KeepCosts
                .FirstOrDefaultAsync(m => m.DocId == id);
            if (keepCostModel == null)
            {
                return NotFound();
            }

            return View(keepCostModel);
        }

        // POST: Admin/Petty/KeepDelete/5
        [HttpPost]
        public ActionResult KeepDelete(string docid, string seqno)
        {
            try
            {
                KeepCostModel keepCost = _context.KeepCosts.Find(docid, Convert.ToInt32(seqno));
                _context.KeepCosts.Remove(keepCost);
                _context.SaveChanges();
                //
                KeepDtlModel dtl = _context.KeepDtls.Where(d => d.DocId == keepCost.DocId)
                                                    .FirstOrDefault();
                if (dtl != null)
                {
                    dtl.Cost = _context.KeepCosts.Where(k => k.DocId == keepCost.DocId)
                                                 .Select(k => k.TotalCost)
                                                 .DefaultIfEmpty(0).Sum();
                    _context.Entry(dtl).State = EntityState.Modified;
                    _context.SaveChanges();
                }
                return new JsonResult(keepCost)
                {
                    Value = new { success = true, error = "" }
                };
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public List<RepairCostModel> GetRepPettyList(QryPettyVModel qdata)
        {
            string signno = qdata.qtySIGNNO;
            string vendorname = qdata.qtyVENDORNAME;
            string vendorno = qdata.qtyVENDORNO;
            string docid = qdata.qtyDOCID;
            string ticketStatus = qdata.qtyTICKETSTATUS;
            string qryDocType = qdata.qtyDOCTYPE;
            if (!string.IsNullOrEmpty(docid))
                docid = docid.Trim();
            if (!string.IsNullOrEmpty(signno))
                signno = signno.ToUpper();

            var query = _context.RepairCosts.Where(rc => rc.StockType == "3").AsQueryable();
            if (!string.IsNullOrEmpty(signno))    //簽單號碼
            {
                query = query.Where(r => r.SignNo.ToUpper() == signno);
            }
            if (!string.IsNullOrEmpty(vendorname))  //廠商關鍵字
            {
                query = query.Where(t => t.VendorId != null)
                       .Join(_context.Vendors, t => t.VendorId, v => v.VendorId,
                       (t, v) => new
                       {
                           repCost = t,
                           vendor = v
                       }).Where(t => t.vendor.VendorName.Contains(vendorname)).Select(r => r.repCost);
                //ts = ts.Where(t => !string.IsNullOrEmpty(t.VendorName))
                //       .Where(t => t.VendorName.Contains(vendorname));
            }
            if (!string.IsNullOrEmpty(vendorno))    //廠商統編
            {
                query = query.Where(t => t.VendorId != null)
                       .Join(_context.Vendors, t => t.VendorId, v => v.VendorId,
                       (t, v) => new
                       {
                           repCost = t,
                           vendor = v
                       }).Where(r => r.vendor.UniteNo.Contains(vendorno)).Select(r => r.repCost);
            }
            if (!string.IsNullOrEmpty(docid))    //請修單號
            {
                query = query.Where(r => r.DocId == docid);
            }

            foreach (var item in query)
            {
                var vendor = _context.Vendors.Find(item.VendorId);
                if (vendor != null)
                {
                    item.VendorUniteNo = vendor.UniteNo;
                }
            }

            return query.ToList();
        }

        public List<KeepCostModel> GetKeepPettyList(QryPettyVModel qdata)
        {
            string signno = qdata.qtySIGNNO;
            string vendorname = qdata.qtyVENDORNAME;
            string vendorno = qdata.qtyVENDORNO;
            string docid = qdata.qtyDOCID;
            string ticketStatus = qdata.qtyTICKETSTATUS;
            string qryDocType = qdata.qtyDOCTYPE;
            if (!string.IsNullOrEmpty(docid))
                docid = docid.Trim();
            if (!string.IsNullOrEmpty(signno))
                signno = signno.ToUpper();

            var query = _context.KeepCosts.Where(rc => rc.StockType == "3").AsQueryable();
            if (!string.IsNullOrEmpty(signno))    //簽單號碼
            {
                query = query.Where(r => r.SignNo.ToUpper() == signno);
            }
            if (!string.IsNullOrEmpty(vendorname))  //廠商關鍵字
            {
                query = query.Where(t => t.VendorId != 0)
                       .Join(_context.Vendors, t => t.VendorId, v => v.VendorId,
                       (t, v) => new
                       {
                           keepCost = t,
                           vendor = v
                       }).Where(t => t.vendor.VendorName.Contains(vendorname)).Select(r => r.keepCost);
                //ts = ts.Where(t => !string.IsNullOrEmpty(t.VendorName))
                //       .Where(t => t.VendorName.Contains(vendorname));
            }
            if (!string.IsNullOrEmpty(vendorno))    //廠商統編
            {
                query = query.Where(t => t.VendorId != 0)
                       .Join(_context.Vendors, t => t.VendorId, v => v.VendorId,
                       (t, v) => new
                       {
                           keepCost = t,
                           vendor = v
                       }).Where(r => r.vendor.UniteNo.Contains(vendorno)).Select(r => r.keepCost);
            }
            if (!string.IsNullOrEmpty(docid))    //保養單號
            {
                query = query.Where(r => r.DocId == docid);
            }

            foreach(var item in query)
            {
                var vendor = _context.Vendors.Find(item.VendorId);
                if (vendor != null)
                {
                    item.VendorUniteNo = vendor.UniteNo;
                }
            }

            return query.ToList();
        }

        private bool RepairCostModelExists(string id)
        {
            return _context.RepairCosts.Any(e => e.DocId == id);
        }
    }
}
