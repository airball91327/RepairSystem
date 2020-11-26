﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public IActionResult Index(IFormCollection fm)
        {
            string ticketno = fm["qtyTICKET"];
            string vendorname = fm["qtyVENDORNAME"];
            string vendorno = fm["qtyVENDORNO"];
            string docid = fm["qtyDOCID"];
            docid = docid.Trim();
            ticketno = ticketno.ToUpper();

            //string qtyDate1 = fm["qtyApplyDateFrom"];
            //string qtyDate2 = fm["qtyApplyDateTo"];

            //DateTime applyDateFrom = DateTime.Now;
            //DateTime applyDateTo = DateTime.Now;
            ///* Dealing search by date. */
            //if (qtyDate1 != "" && qtyDate2 != "")// If 2 date inputs have been insert, compare 2 dates.
            //{
            //    DateTime date1 = DateTime.Parse(qtyDate1);
            //    DateTime date2 = DateTime.Parse(qtyDate2);
            //    int result = DateTime.Compare(date1, date2);
            //    if (result < 0)
            //    {
            //        applyDateFrom = date1.Date;
            //        applyDateTo = date2.Date;
            //    }
            //    else if (result == 0)
            //    {
            //        applyDateFrom = date1.Date;
            //        applyDateTo = date1.Date;
            //    }
            //    else
            //    {
            //        applyDateFrom = date2.Date;
            //        applyDateTo = date1.Date;
            //    }
            //}
            //else if (qtyDate1 == "" && qtyDate2 != "")
            //{
            //    applyDateFrom = DateTime.Parse(qtyDate2);
            //    applyDateTo = DateTime.Parse(qtyDate2);
            //}
            //else if (qtyDate1 != "" && qtyDate2 == "")
            //{
            //    applyDateFrom = DateTime.Parse(qtyDate1);
            //    applyDateTo = DateTime.Parse(qtyDate1);
            //}

            var ts = _context.Tickets.AsQueryable();
            var repairCost = _context.RepairCosts.Include(r => r.TicketDtl).AsQueryable();

            if (!string.IsNullOrEmpty(ticketno))
            {
                ts = ts.Where(t => t.TicketNo.ToUpper() == ticketno);
            }
            if (!string.IsNullOrEmpty(vendorname))
            {
                ts = ts.Where(t => !string.IsNullOrEmpty(t.VendorName))
                       .Where(t => t.VendorName.Contains(vendorname));
            }
            if (!string.IsNullOrEmpty(vendorno))
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
            //if (string.IsNullOrEmpty(qtyDate1) == false || string.IsNullOrEmpty(qtyDate2) == false)
            //{
            //    ts = ts.Where(t => t.ShutDate >= applyDateFrom && t.ShutDate <= applyDateTo).ToList();
            //}

            /* Get StockType for all Tickets */
            foreach(var item in ts)
            {
                var repCost = _context.RepairCosts.Where(r => r.TicketDtl.TicketDtlNo.ToUpper() == item.TicketNo.ToUpper()).ToList()
                                                  .OrderBy(r => r.SeqNo).FirstOrDefault();
                                        
                if (repCost != null)
                {
                    if (repCost.StockType == "2")
                    {
                        item.StockType = "發票";
                    }
                    if (repCost.StockType == "4")
                    {
                        item.StockType = "零用金";
                    }
                }
                else
                {
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

            return PartialView("List", ts);
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
            TradeCodeList.Add(new SelectListItem { Text = "維476", Value = "476" });
            TradeCodeList.Add(new SelectListItem { Text = "維41", Value = "41" });
            TradeCodeList.Add(new SelectListItem { Text = "維44", Value = "44" });
            TradeCodeList.Add(new SelectListItem { Text = "維608", Value = "608" });
            TradeCodeList.Add(new SelectListItem { Text = "維609", Value = "609" });
            TradeCodeList.Add(new SelectListItem { Text = "維610", Value = "610" });
            TradeCodeList.Add(new SelectListItem { Text = "其它151", Value = "151" });
            TradeCodeList.Add(new SelectListItem { Text = "購468", Value = "468" });
            TradeCodeList.Add(new SelectListItem { Text = "購386", Value = "386" });
            TradeCodeList.Add(new SelectListItem { Text = "購106", Value = "106" });
            TradeCodeList.Add(new SelectListItem { Text = "購105", Value = "105" });
            TradeCodeList.Add(new SelectListItem { Text = "慈24", Value = "24" });
            TradeCodeList.Add(new SelectListItem { Text = "教188", Value = "188" });
            TradeCodeList.Add(new SelectListItem { Text = "其它527", Value = "527" });
            TradeCodeList.Add(new SelectListItem { Text = "其它458", Value = "458" });
            ViewData["TradeCode"] = new SelectList(TradeCodeList, "Value", "Text", "44");

            return View(ticket);
        }

        // POST: Admin/Tickets/Edit/5
        [HttpPost]
        public IActionResult Edit([Bind("TicketNo,TicDate,VendorId,VendorName,TotalAmt,TaxAmt,Note,ScrapValue,ApplyDate,CancelDate,TradeCode")] TicketModel ticketModel)
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

    }
}