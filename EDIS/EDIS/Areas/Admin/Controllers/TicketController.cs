using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using EDIS.Data;
using EDIS.Models.RepairModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EDIS.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TicketController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Ticket
        public IActionResult Index()
        {
            return View();
        }

        // POST: Admin/Ticket
        [HttpPost]
        public IActionResult Index(QryTicketListData qdata)
        {
            string ticketno = qdata.qtyTICKETNO;
            string vendorname = qdata.qtyVENDORNAME;
            string vendorno = qdata.qtyVENDORNO;
            string docid = qdata.qtyDOCID;
            string ticketStatus = qdata.qtyTICKETSTATUS;
            string qryStockType = qdata.qtySTOCKTYPE;
            string qryDocType = qdata.qtyDOCTYPE;
            if (!string.IsNullOrEmpty(docid))
                docid = docid.Trim();
            if (!string.IsNullOrEmpty(ticketno))
                ticketno = ticketno.ToUpper();

            DateTime? qtyDate1 = qdata.qtyApplyDateFrom;
            DateTime? qtyDate2 = qdata.qtyApplyDateFrom;

            DateTime applyDateFrom = DateTime.Now;
            DateTime applyDateTo = DateTime.Now;
            /* Dealing search by date. */
            if (qtyDate1 != null && qtyDate2 != null)// If 2 date inputs have been insert, compare 2 dates.
            {
                DateTime date1 = qtyDate1.Value;
                DateTime date2 = qtyDate2.Value;
                int result = DateTime.Compare(date1, date2);
                if (result < 0)
                {
                    applyDateFrom = date1.Date;
                    applyDateTo = date2.Date;
                }
                else if (result == 0)
                {
                    applyDateFrom = date1.Date;
                    applyDateTo = date1.Date;
                }
                else
                {
                    applyDateFrom = date2.Date;
                    applyDateTo = date1.Date;
                }
            }
            else if (qtyDate1 == null && qtyDate2 != null)
            {
                applyDateFrom = qtyDate2.Value;
                applyDateTo = qtyDate2.Value;
            }
            else if (qtyDate1 != null && qtyDate2 == null)
            {
                applyDateFrom = qtyDate1.Value;
                applyDateTo = qtyDate1.Value;
            }

            var ts = _context.Tickets.AsQueryable();
            var repairCost = _context.RepairCosts.Include(r => r.TicketDtl).AsQueryable();

            if (!string.IsNullOrEmpty(ticketStatus))    //關帳
            {
                if (ticketStatus == "已關帳")
                {
                    ts = ts.Where(t => t.IsShuted == "Y");
                }
                else
                {
                    ts = ts.Where(t => t.IsShuted != "Y");
                }
            }
            if (!string.IsNullOrEmpty(ticketno))    //發票號碼
            {
                ts = ts.Where(t => t.TicketNo.ToUpper() == ticketno);
            }
            if (!string.IsNullOrEmpty(vendorname))  //廠商關鍵字
            {
                ts = ts.Where(t => t.VendorId != null)
                       .Join(_context.Vendors, t => t.VendorId, v => v.VendorId,
                       (t, v) => new
                       {
                           ticket = t,
                           vendor = v
                       }).Where(t => t.vendor.VendorName.Contains(vendorname)).Select(r => r.ticket);
                //ts = ts.Where(t => !string.IsNullOrEmpty(t.VendorName))
                //       .Where(t => t.VendorName.Contains(vendorname));
            }
            if (!string.IsNullOrEmpty(vendorno))    //廠商統編
            {
                ts = ts.Where(t => t.VendorId != null)
                       .Join(_context.Vendors, t => t.VendorId, v => v.VendorId,
                       (t, v) => new
                       {
                           ticket = t,
                           vendor = v
                       }).Where(r => r.vendor.UniteNo.Contains(vendorno)).Select(r => r.ticket);
            }
            if (!string.IsNullOrEmpty(docid))   //若依單號搜尋，搜尋該單的所有費用(包括簽單)
            {
                var rc = repairCost.Where(r => r.DocId == docid).OrderBy(r => r.SeqNo).ToList();
                rc.ForEach(r => {
                    r.VendorUniteNo = _context.Vendors.Find(r.VendorId) == null ? "" : _context.Vendors.Find(r.VendorId).UniteNo;
                    if (r.StockType == "0")
                        r.StockType = "庫存";
                    else if (r.StockType == "2")
                        r.StockType = "發票(含收據)";
                    else if (r.StockType == "4")
                        r.StockType = "零用金";
                    else
                        r.StockType = "簽單";
                });
                return PartialView("List2", rc);
            }

            /* Search date by Date. */
            if (qtyDate1 != null || qtyDate2 != null)   //關帳日期
            {
                ts = ts.Where(t => t.ApplyDate != null)
                       .Where(t => t.ApplyDate >= applyDateFrom && t.ApplyDate <= applyDateTo);
            }

            /* Get StockType for all Tickets */
            foreach (var item in ts)
            {
                var repCost = _context.RepairCosts.Where(r => r.TicketDtl.TicketDtlNo.ToUpper() == item.TicketNo.ToUpper())
                                                  .ToList().OrderBy(r => r.SeqNo).FirstOrDefault();
                var keepCost = _context.KeepCosts.Where(r => r.TicketDtl.TicketDtlNo.ToUpper() == item.TicketNo.ToUpper())
                                                 .ToList().OrderBy(r => r.SeqNo).FirstOrDefault();
                if (repCost != null)
                {
                    item.DocType = "請修";
                    if (repCost.StockType == "0")
                    {
                        item.StockType = "庫存";
                    }
                    else if (repCost.StockType == "2")
                    {
                        item.StockType = "發票";
                    }
                    else if (repCost.StockType == "4")
                    {
                        item.StockType = "零用金";
                    }
                    else 
                    {
                        item.StockType = "簽單";
                    }
                }
                else if(keepCost != null)
                {
                    item.DocType = "保養";
                    if (keepCost.StockType == "0")
                    {
                        item.StockType = "庫存";
                    }
                    else if (keepCost.StockType == "2")
                    {
                        item.StockType = "發票";
                    }
                    else if (keepCost.StockType == "4")
                    {
                        item.StockType = "零用金";
                    }
                    else
                    {
                        item.StockType = "簽單";
                    }
                }
                else
                {
                    item.DocType = "";
                    item.StockType = "";
                }
                //
                if (item.VendorId != null)
                {
                    var vendor = _context.Vendors.Find(item.VendorId);
                    if (vendor != null)
                    {
                        item.UniteNo = vendor.UniteNo;
                    }
                }
            }
            if (!string.IsNullOrEmpty(qryStockType))    //費用別(發票/零用金)
            {
                if (qryStockType == "無類別")
                {
                    ts = ts.Where(t => t.StockType == "");
                }
                else
                {
                    ts = ts.Where(t => t.StockType == qryStockType);
                }
                ViewData["QryStockType"] = qryStockType;
            }
            if (!string.IsNullOrEmpty(qryDocType))    //(請修單/保養單)
            {
                ts = ts.Where(t => t.DocType == qryDocType || t.DocType == "");
            }

            return PartialView("List", ts.ToList());
        }

        // GET: Admin/Ticket/List
        public IActionResult List()
        {
            return PartialView(_context.Tickets.ToList());
        }

        // GET: Admin/Tickets/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("TicketNo,TicDate,VendorId,VendorName,TotalAmt,TaxAmt,Note,ScrapValue,ApplyDate,CancelDate")] TicketModel ticketModel)
        {
            if (ModelState.IsValid)
            {
                _context.Tickets.Add(ticketModel);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(ticketModel);
        }

        // GET: Admin/Tickets/Edit/5
        public IActionResult Edit(string id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            TicketModel ticket = _context.Tickets.Find(id);
            decimal total = _context.TicketDtls.Where(t => t.TicketDtlNo == id).DefaultIfEmpty()
                                               .Sum(t => t.Cost);

            ticket.ScrapValue = ticket.TotalAmt - Convert.ToInt32(total);

            /* 交易代號列表 */
            List<SelectListItem> TradeCodeList = new List<SelectListItem>();
            TradeCodeList.Add(new SelectListItem { Text = "476", Value = "476" });
            TradeCodeList.Add(new SelectListItem { Text = "41", Value = "41" });
            TradeCodeList.Add(new SelectListItem { Text = "44", Value = "44" });
            TradeCodeList.Add(new SelectListItem { Text = "608", Value = "608" });
            TradeCodeList.Add(new SelectListItem { Text = "609", Value = "609" });
            TradeCodeList.Add(new SelectListItem { Text = "610", Value = "610" });
            TradeCodeList.Add(new SelectListItem { Text = "151", Value = "151" });
            TradeCodeList.Add(new SelectListItem { Text = "468", Value = "468" });
            TradeCodeList.Add(new SelectListItem { Text = "386", Value = "386" });
            TradeCodeList.Add(new SelectListItem { Text = "106", Value = "106" });
            TradeCodeList.Add(new SelectListItem { Text = "105", Value = "105" });
            TradeCodeList.Add(new SelectListItem { Text = "24", Value = "24" });
            TradeCodeList.Add(new SelectListItem { Text = "188", Value = "188" });
            TradeCodeList.Add(new SelectListItem { Text = "527", Value = "527" });
            TradeCodeList.Add(new SelectListItem { Text = "458", Value = "458" });
            ViewData["TradeCode"] = new SelectList(TradeCodeList, "Value", "Text", ticket.TradeCode);

            return View(ticket);
        }

        // POST: Admin/Tickets/Edit/5
        [HttpPost]
        public IActionResult Edit([Bind("TicketNo,TicDate,VendorId,VendorName,TotalAmt,TaxAmt,Note,ScrapValue,ApplyDate,CancelDate,TradeCode,Appl_No,Appl_Date")] TicketModel ticketModel)
        {
            if (ModelState.IsValid)
            {
                decimal total = _context.TicketDtls.Where(t => t.TicketDtlNo == ticketModel.TicketNo).DefaultIfEmpty()
                                                   .Sum(t => t.Cost);

                ticketModel.ScrapValue = ticketModel.TotalAmt - Convert.ToInt32(total);

                if(ticketModel.ScrapValue < 0)
                {
                    throw new Exception("總價低於發票明細總額!");
                }

                _context.Entry(ticketModel).State = EntityState.Modified;
                _context.SaveChanges();

                // 更新所有與此發票相關的費用明細日期
                if (ticketModel.TicDate != null)
                {
                    var relatedRepairCosts = _context.RepairCosts.Include(rc => rc.TicketDtl)
                                             .Where(rc => rc.TicketDtl.TicketDtlNo == ticketModel.TicketNo);
                    foreach (var item in relatedRepairCosts)
                    {
                        item.AccountDate = ticketModel.TicDate;
                        _context.Entry(item).State = EntityState.Modified;
                    }
                    _context.SaveChanges();
                }

                return new JsonResult(ticketModel)
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

        // GET: Admin/Tickets/RefreshCost/5
        public IActionResult RefreshCost(string ticketNo)
        {
            TicketModel ticketModel = _context.Tickets.Find(ticketNo);
            if (ticketModel != null)
            {
                decimal total = _context.TicketDtls.Where(t => t.TicketDtlNo == ticketModel.TicketNo).DefaultIfEmpty()
                                                   .Sum(t => t.Cost);

                ticketModel.ScrapValue = ticketModel.TotalAmt - Convert.ToInt32(total);
                _context.Entry(ticketModel).State = EntityState.Modified;
                _context.SaveChanges();
                return new JsonResult(ticketModel)
                {                   
                    Value = new { Data = ticketModel.ScrapValue, success = true, error = "" }
                };
            }
            throw new Exception("沖銷失敗!");
        }

        // POST: Admin/Ticket/SetApplyNo/5
        [HttpPost]
        public IActionResult SetApplyNo(string ticketNos)
        {
            string[] s = ticketNos.Split(new char[] { ';' });
            return new JsonResult(ticketNos)
            {
                Value = new { success = true, error = "" }
            };
        }

        // GET: Admin/Ticket/ExportExcel/5
        public IActionResult ExportExcel(string ticketNos)
        {
            string[] s = ticketNos.Split(new char[] { ';' });
            List<TicketModel> ticketList = new List<TicketModel>();
            foreach (var ticketNo in s)
            {
                var ticket = _context.Tickets.Find(ticketNo);
                if (ticket != null)
                {
                    ticketList.Add(ticket);
                }
            }

            var repairCost = _context.RepairCosts.Include(c => c.TicketDtl)
                                                 .Where(c => c.TicketDtl != null)
                                                 .Where(c => c.StockType == "2" || c.StockType == "4");
            var output = ticketList.GroupJoin(_context.Vendors, t => t.VendorId, v => v.VendorId,
                                    (t, v) => new
                                    {
                                        ticket = t,
                                        vendor = v,
                                        ticDate = t.TicDate.HasValue == false ? "" : (t.TicDate.Value.Year - 1911).ToString() + t.TicDate.Value.ToString("MM")
                                    })
                                    .Select(r => new 
                                    {
                                        ticket = r.ticket,
                                        vendor = r.vendor.FirstOrDefault(),
                                        ticDate = r.ticDate
                                    }).Join(repairCost, t => t.ticket.TicketNo, rc => rc.TicketDtl.TicketDtlNo,
                                    (t, rc) => new 
                                    {
                                        ticket = t.ticket,
                                        vendor = t.vendor,
                                        ticDate = t.ticDate,
                                        repairCost = rc
                                    })
                                    .Join(_context.Repairs, t => t.repairCost.DocId, r => r.DocId,
                                    (t, r) => new 
                                    {
                                        ticket = t.ticket,
                                        vendor = t.vendor,
                                        ticDate = t.ticDate,
                                        repairCost = t.repairCost,
                                        repair = r,
                                        accDpt = r.AccDpt,
                                        tradeMemo = t.ticket.TicketNo + "_" + t.repairCost.DocId + "，" + t.repairCost.PartName + t.repairCost.Standard + t.repairCost.PartNo
                                    })
                                    .ToList();


            //ClosedXML的用法 先new一個Excel Workbook
            using (XLWorkbook workbook = new XLWorkbook())
            {
                //取得要塞入Excel內的資料
                List<TicketExcelViewModel> data = new List<TicketExcelViewModel>();
                data = output.Select(c => new TicketExcelViewModel
                {
                    Appl_No = c.ticket.Appl_No,
                    UniteNo = c.vendor != null ? c.vendor.UniteNo : "",
                    TicketNo = c.ticket.TicketNo,
                    TotalAmt = c.ticket.TotalAmt,
                    TicDate = c.ticDate,
                    AccDpt = c.accDpt,
                    TradeCode = c.ticket.TradeCode,
                    TradeMemo = c.tradeMemo,
                }).ToList();

                //一個workbook內至少會有一個worksheet,並將資料Insert至這個位於A1這個位置上
                var ws = workbook.Worksheets.Add("sheet1", 1);

                //Title
                ws.Cell(1, 1).Value = "申請單序號";
                ws.Cell(1, 2).Value = "受款人統一編號";
                ws.Cell(1, 3).Value = "發票號碼";
                ws.Cell(1, 4).Value = "金額";
                ws.Cell(1, 5).Value = "憑證年月\n(發票年月)";
                ws.Cell(1, 6).Value = "成本中心";
                ws.Cell(1, 7).Value = "交易代號";
                ws.Cell(1, 8).Value = "交易說明(含發票號碼,請修單號,零件名稱/規格/料號)";

                //Data
                if (data.Count() > 0)
                {
                    var previousObj = data.FirstOrDefault();
                    int count = 0;
                    foreach (var item in data)
                    {
                        if (item.TicketNo == previousObj.TicketNo && count != 0)
                        {
                            item.Appl_No = "";
                            item.UniteNo = "";
                            item.TicketNo = "";
                            item.TotalAmt = null;
                            item.TicDate = null;
                            item.TradeCode = "";
                        }
                        count++;
                    }
                }

                //如果是要塞入Query後的資料該資料一定要變成是data.AsEnumerable()
                ws.Cell(2, 1).InsertData(data);

                //因為是用Query的方式,這個地方要用串流的方式來存檔
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    workbook.SaveAs(memoryStream);
                    //請注意 一定要加入這行,不然Excel會是空檔
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    //注意Excel的ContentType,是要用這個"application/vnd.ms-excel"
                    string fileName = "發票作業_" + DateTime.Now.ToString("yyyy-MM-dd") + ".xlsx";
                    return this.File(memoryStream.ToArray(), "application/vnd.ms-excel", fileName);
                }
            }
        }

        // GET: Admin/Ticket/Index2
        public IActionResult Index2()
        {
            return View();
        }


    }
}