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
using System.IO;
using ClosedXML.Excel;
using EDIS.Models;

namespace EDIS.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<AppUserModel, int> _userRepo;

        public InvoiceController(ApplicationDbContext context,
                                 IRepository<AppUserModel, int> userRepo)
        {
            _context = context;
            _userRepo = userRepo;
        }

        // GET: Admin/Invoice
        public async Task<IActionResult> Index()
        {
            return View();
        }

        // POST: Admin/Invoice
        [HttpPost]
        public async Task<IActionResult> Index(QryInvoiceVModel qdata)
        {
            string qryDocType = qdata.qtyDOCTYPE;

            List<RepairCostModel> rv = new List<RepairCostModel>();
            List<KeepCostModel> kv = new List<KeepCostModel>();
            if (qryDocType == "請修")
            {
                rv = GetRepInvoiceList(qdata);
                return PartialView("RepInvoiceList", rv);
            }
            else
            {
                kv = GetKeepInvoiceList(qdata);
                return PartialView("KeepInvoiceList", kv);
            }
        }

        // GET: Admin/Invoice/RepCostEdit/5
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

        // POST: Admin/Invoice/RepCostEdit/5
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

        // GET: Admin/Invoice/KeepCostEdit/5
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

        // POST: Admin/Invoice/KeepCostEdit/5
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

        // GET: Admin/Invoice/RepDelete/5
        public async Task<IActionResult> RepDelete(string docId, int? seqNo)
        {
            if (docId == null || seqNo == null)
            {
                return NotFound();
            }

            var repairCostModel = await _context.RepairCosts.FindAsync(docId, seqNo);
            if (repairCostModel == null)
            {
                return NotFound();
            }

            return View(repairCostModel);
        }

        // POST: Admin/Invoice/RepDelete/5
        [HttpPost]
        [ActionName("RepDelete")]
        public ActionResult RepDeleteConfirm(string docid, int? seqno)
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

        // GET: Admin/Invoice/KeepDelete/5
        public async Task<IActionResult> KeepDelete(string docId, int? seqNo)
        {
            if (docId == null || seqNo == null)
            {
                return NotFound();
            }
            var keepCostModel = await _context.KeepCosts.FindAsync(docId, seqNo);
            if (keepCostModel == null)
            {
                return NotFound();
            }

            return View(keepCostModel);
        }

        // POST: Admin/Invoice/KeepDelete/5
        [HttpPost]
        [ActionName("KeepDelete")]
        public ActionResult KeepDeleteConfirm(string docid, int? seqno)
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

        // GET: Admin/Invoice/TransferEdit/5
        public IActionResult TransferEdit(string DocIds, string SeqNos)
        {
            string[] docIds = DocIds.Split(new char[] { ';' });
            string[] seqNos = SeqNos.Split(new char[] { ';' });
            InvoiceTransferVModel transferVModel = new InvoiceTransferVModel();
            transferVModel.RepairInvoices = new List<RepairInvoice>();
            foreach (var docid in docIds)
            {
                if (!string.IsNullOrEmpty(docid))
                {
                    int index = Array.IndexOf(docIds, docid);
                    int seqNo = Convert.ToInt32(seqNos[index]);
                    var rc = _context.RepairCosts.Find(docid, seqNo);
                    if (rc != null)
                    {
                        RepairInvoice invoice = new RepairInvoice();
                        invoice.DocId = rc.DocId;
                        invoice.SeqNo = rc.SeqNo;
                        invoice.SignNo = rc.SignNo;
                        transferVModel.RepairInvoices.Add(invoice);
                    }
                }
            }
            // Get default engineer.
            var transEng = _context.TransDefaultEng.FirstOrDefault();
            if (transEng != null)
            {
                transferVModel.Engineer = transEng.FullName;
                transferVModel.EngId = transEng.UserId;
                transferVModel.EngDpt = transEng.DptId;
            }
            //
            List<SelectListItem> listItem = new List<SelectListItem>();
            listItem.Add(new SelectListItem { Text = "1.建築物類", Value = "1" });
            listItem.Add(new SelectListItem { Text = "2.醫療設備類", Value = "2" });
            listItem.Add(new SelectListItem { Text = "3.機電設備類", Value = "3" });
            listItem.Add(new SelectListItem { Text = "4.冷凍空調設備類", Value = "4" });
            listItem.Add(new SelectListItem { Text = "5.通訊設備類", Value = "5" });
            listItem.Add(new SelectListItem { Text = "7.照明設備類", Value = "7" });
            listItem.Add(new SelectListItem { Text = "8.資訊設備類", Value = "8" });
            listItem.Add(new SelectListItem { Text = "9.什項修繕類", Value = "9" });
            ViewData["WorkClass"] = new SelectList(listItem, "Value", "Text", transferVModel.WorkClass);
            //
            List<SelectListItem> listItem2 = new List<SelectListItem>();
            listItem2.Add(new SelectListItem { Text = "1.值班", Value = "1" });
            listItem2.Add(new SelectListItem { Text = "2.維修", Value = "2" });
            listItem2.Add(new SelectListItem { Text = "3.增設", Value = "3" });
            listItem2.Add(new SelectListItem { Text = "4.保養", Value = "4" });
            listItem2.Add(new SelectListItem { Text = "5.其他", Value = "5" });
            listItem2.Add(new SelectListItem { Text = "7.報廢", Value = "7" });
            ViewData["Type"] = new SelectList(listItem2, "Value", "Text", "");

            return View(transferVModel);
        }

        // POST: Admin/Invoice/TransferEdit/5
        [HttpPost]
        public IActionResult TransferEdit(InvoiceTransferVModel invoiceTransfer)
        {
            var test = invoiceTransfer;

            return new JsonResult(invoiceTransfer)
            {
                Value = new { success = true, error = "" }
            };
        }

        // POST: Admin/Invoice/EditDefaultEng/5
        [HttpPost]
        public IActionResult EditDefaultEng(int? userId)
        {
            if (userId == null)
            {
                throw new Exception("修改失敗!");
            }
            var defaultEng = _context.TransDefaultEng.FirstOrDefault();
            if (defaultEng != null)
            {
                var user = _context.AppUsers.Find(userId.Value);
                if (user != null)
                {
                    var dpt = _context.Departments.Find(user.DptId);
                    defaultEng.UserId = user.Id;
                    defaultEng.UserName = user.UserName;
                    defaultEng.FullName = user.FullName;
                    defaultEng.DptId = dpt.DptId;
                    defaultEng.DptName = dpt.Name_C;
                    _context.Entry(defaultEng).State = EntityState.Modified;
                }
            }
            else
            {
                var user = _context.AppUsers.Find(userId.Value);
                if (user != null)
                {
                    var dpt = _context.Departments.Find(user.DptId);
                    TransDefaultEng transDefaultEng = new TransDefaultEng();
                    transDefaultEng.Id = 1;
                    transDefaultEng.UserId = user.Id;
                    transDefaultEng.UserName = user.UserName;
                    transDefaultEng.FullName = user.FullName;
                    transDefaultEng.DptId = dpt.DptId;
                    transDefaultEng.DptName = dpt.Name_C;
                    _context.TransDefaultEng.Add(transDefaultEng);
                }
            }
            try
            {
                _context.SaveChanges();
                var eng = _context.TransDefaultEng.FirstOrDefault();
                return new JsonResult(eng)
                {
                    Value = new { success = true, error = "", Data = eng }
                };
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        public List<RepairCostModel> GetRepInvoiceList(QryInvoiceVModel qdata)
        {
            string signno = qdata.qtySIGNNO;
            string vendorname = qdata.qtyVENDORNAME;
            string vendorno = qdata.qtyVENDORNO;
            string docid = qdata.qtyDOCID;
            string ticketStatus = qdata.qtyTICKETSTATUS;
            string qryDocType = qdata.qtyDOCTYPE;
            string qtyShutDate = qdata.qtyShutDate;
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
            if (!string.IsNullOrEmpty(qtyShutDate)) //關帳年月
            {
                int year = Convert.ToInt32(qtyShutDate.Substring(0, 3)) + 1911;
                int month = Convert.ToInt32(qtyShutDate.Substring(3, 2));
                DateTime dateFrom = new DateTime(year, month, 1);
                DateTime dateTo = dateFrom.AddMonths(1).AddSeconds(-1);
                query = query.Join(_context.RepairDtls, q => q.DocId, rdtl => rdtl.DocId,
                             (q, rdtl) => new 
                             { 
                                repairCost = q,
                                dtl = rdtl
                             })
                             .Where(q => q.dtl.ShutDate >= dateFrom && q.dtl.ShutDate <= dateTo)
                             .Select(q => q.repairCost);
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

        public List<KeepCostModel> GetKeepInvoiceList(QryInvoiceVModel qdata)
        {
            string signno = qdata.qtySIGNNO;
            string vendorname = qdata.qtyVENDORNAME;
            string vendorno = qdata.qtyVENDORNO;
            string docid = qdata.qtyDOCID;
            string ticketStatus = qdata.qtyTICKETSTATUS;
            string qryDocType = qdata.qtyDOCTYPE;
            string qtyShutDate = qdata.qtyShutDate;
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
            if (!string.IsNullOrEmpty(qtyShutDate)) //關帳年月
            {
                int year = Convert.ToInt32(qtyShutDate.Substring(0, 3)) + 1911;
                int month = Convert.ToInt32(qtyShutDate.Substring(3, 2));
                DateTime dateFrom = new DateTime(year, month, 1);
                DateTime dateTo = dateFrom.AddMonths(1).AddSeconds(-1);
                query = query.Join(_context.KeepDtls, q => q.DocId, rdtl => rdtl.DocId,
                             (q, rdtl) => new
                             {
                                 keepCost = q,
                                 dtl = rdtl
                             })
                             .Where(q => q.dtl.ShutDate >= dateFrom && q.dtl.ShutDate <= dateTo)
                             .Select(q => q.keepCost);
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

        // GET: Admin/Invoice/ExportExcelRep/5
        public IActionResult ExportExcelRep(string DocIds, string SeqNos)
        {
            string[] docIds = DocIds.Split(new char[] { ';' });
            string[] seqNos = SeqNos.Split(new char[] { ';' });
            List<RepairCostModel> repCostList = new List<RepairCostModel>();
            foreach (var docid in docIds)
            {
                if (!string.IsNullOrEmpty(docid))
                {
                    int index = Array.IndexOf(docIds, docid);
                    int seqNo = Convert.ToInt32(seqNos[index]);
                    var rc = _context.RepairCosts.Find(docid, seqNo);
                    if (rc != null)
                    {
                        repCostList.Add(rc);
                    }
                }
            }

            var output = repCostList.GroupJoin(_context.Vendors, t => t.VendorId, v => v.VendorId,
                                    (t, v) => new
                                    {
                                        repaircost = t,
                                        vendor = v,
                                    })
                                    .Select(r => new
                                    {
                                        repaircost = r.repaircost,
                                        vendor = r.vendor.FirstOrDefault()
                                    })
                                    .Join(_context.Repairs, t => t.repaircost.DocId, r => r.DocId,
                                    (t, r) => new
                                    {
                                        repaircost = t.repaircost,
                                        vendor = t.vendor,
                                        repair = r,
                                        accDpt = r.AccDpt,
                                    })
                                    .ToList();

            //ClosedXML的用法 先new一個Excel Workbook
            using (XLWorkbook workbook = new XLWorkbook())
            {
                //取得要塞入Excel內的資料
                var data = output.Select(c => new
                {
                    SignNo = c.repaircost.SignNo,
                    VendorName = c.vendor.VendorName,
                    VendorUniteNo = c.vendor.UniteNo,
                    AccountDate = c.repaircost.AccountDate,
                    PartName = c.repaircost.PartNo + "/" + c.repaircost.PartName + "/" + c.repaircost.Standard,
                    Qty = c.repaircost.Qty,
                    Price = c.repaircost.Price,
                    TotalCost = c.repaircost.TotalCost,
                    DocId = c.repaircost.DocId,                
                }).ToList();

                //一個workbook內至少會有一個worksheet,並將資料Insert至這個位於A1這個位置上
                var ws = workbook.Worksheets.Add("簽單列表", 1);

                //Title
                ws.Cell(1, 1).Value = "簽單號碼";
                ws.Cell(1, 2).Value = "廠商名稱";
                ws.Cell(1, 3).Value = "廠商統編";
                ws.Cell(1, 4).Value = "日期";
                ws.Cell(1, 5).Value = "料號/零件名稱/規格";
                ws.Cell(1, 6).Value = "數量";
                ws.Cell(1, 7).Value = "單價";
                ws.Cell(1, 8).Value = "總金額";
                ws.Cell(1, 9).Value = "請修單號";

                //如果是要塞入Query後的資料該資料一定要變成是data.AsEnumerable()
                ws.Cell(2, 1).InsertData(data);

                //因為是用Query的方式,這個地方要用串流的方式來存檔
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    workbook.SaveAs(memoryStream);
                    //請注意 一定要加入這行,不然Excel會是空檔
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    //注意Excel的ContentType,是要用這個"application/vnd.ms-excel"
                    string fileName = "簽單作業_" + DateTime.Now.ToString("yyyy-MM-dd") + ".xlsx";
                    return this.File(memoryStream.ToArray(), "application/vnd.ms-excel", fileName);
                }
            }
        }

        // GET: Admin/Invoice/ExportExcelKeep/5
        public IActionResult ExportExcelKeep(string DocIds, string SeqNos)
        {
            string[] docIds = DocIds.Split(new char[] { ';' });
            string[] seqNos = SeqNos.Split(new char[] { ';' });
            List<KeepCostModel> keepCostList = new List<KeepCostModel>();
            foreach (var docid in docIds)
            {
                if (!string.IsNullOrEmpty(docid))
                {
                    int index = Array.IndexOf(docIds, docid);
                    int seqNo = Convert.ToInt32(seqNos[index]);
                    var kc = _context.KeepCosts.Find(docid, seqNo);
                    if (kc != null)
                    {
                        keepCostList.Add(kc);
                    }
                }
            }

            var output = keepCostList.GroupJoin(_context.Vendors, t => t.VendorId, v => v.VendorId,
                                    (t, v) => new
                                    {
                                        keepcost = t,
                                        vendor = v,
                                    })
                                    .Select(r => new
                                    {
                                        keepcost = r.keepcost,
                                        vendor = r.vendor.FirstOrDefault()
                                    })
                                    .Join(_context.Keeps, t => t.keepcost.DocId, r => r.DocId,
                                    (t, r) => new
                                    {
                                        keepcost = t.keepcost,
                                        vendor = t.vendor,
                                        keep = r,
                                        accDpt = r.AccDpt,
                                    })
                                    .ToList();

            //ClosedXML的用法 先new一個Excel Workbook
            using (XLWorkbook workbook = new XLWorkbook())
            {
                //取得要塞入Excel內的資料
                var data = output.Select(c => new
                {
                    SignNo = c.keepcost.SignNo,
                    VendorName = c.vendor.VendorName,
                    VendorUniteNo = c.vendor.UniteNo,
                    AccountDate = c.keepcost.AccountDate,
                    PartName = c.keepcost.PartNo + "/" + c.keepcost.PartName + "/" + c.keepcost.Standard,
                    Qty = c.keepcost.Qty,
                    Price = c.keepcost.Price,
                    TotalCost = c.keepcost.TotalCost,
                    DocId = c.keepcost.DocId,
                }).ToList();

                //一個workbook內至少會有一個worksheet,並將資料Insert至這個位於A1這個位置上
                var ws = workbook.Worksheets.Add("簽單列表", 1);

                //Title
                ws.Cell(1, 1).Value = "簽單號碼";
                ws.Cell(1, 2).Value = "廠商名稱";
                ws.Cell(1, 3).Value = "廠商統編";
                ws.Cell(1, 4).Value = "日期";
                ws.Cell(1, 5).Value = "料號/零件名稱/規格";
                ws.Cell(1, 6).Value = "數量";
                ws.Cell(1, 7).Value = "單價";
                ws.Cell(1, 8).Value = "總金額";
                ws.Cell(1, 9).Value = "保養單號";

                //如果是要塞入Query後的資料該資料一定要變成是data.AsEnumerable()
                ws.Cell(2, 1).InsertData(data);

                //因為是用Query的方式,這個地方要用串流的方式來存檔
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    workbook.SaveAs(memoryStream);
                    //請注意 一定要加入這行,不然Excel會是空檔
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    //注意Excel的ContentType,是要用這個"application/vnd.ms-excel"
                    string fileName = "簽單作業_" + DateTime.Now.ToString("yyyy-MM-dd") + ".xlsx";
                    return this.File(memoryStream.ToArray(), "application/vnd.ms-excel", fileName);
                }
            }
        }

        private bool RepairCostModelExists(string id)
        {
            return _context.RepairCosts.Any(e => e.DocId == id);
        }
    }
}
