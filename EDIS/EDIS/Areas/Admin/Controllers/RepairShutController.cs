using System;
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
    public class RepairShutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RepairShutController(ApplicationDbContext context)
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

            List<TicketModel> ts = _context.Tickets.ToList();
            List<RepairCostModel> repairCost = _context.RepairCosts.Include(r => r.TicketDtl).Where(r => r.StockType != "3").ToList();
            List<RepairModel> repair = _context.Repairs.ToList();
            List<RepairListVModel> rv = new List<RepairListVModel>();

            if (!string.IsNullOrEmpty(ticketno))
            {
                ts = ts.Where(t => t.TicketNo.ToUpper() == ticketno).ToList();
            }
            if (!string.IsNullOrEmpty(vendorname))
            {
                ts = ts.Where(t => t.VendorName != null && t.VendorName != "")
                       .Where(t => t.VendorName.Contains(vendorname)).ToList();
            }
            if (!string.IsNullOrEmpty(vendorno))
            {
                ts = ts.Where(t => t.VendorId != null)
                       .Where(t => t.VendorId == Convert.ToInt32(vendorno)).ToList();
            }
            if (!string.IsNullOrEmpty(docid)) 
            {
                repair = repair.Where(r => r.DocId == docid).ToList();
            }

            var docIdList = ts.Join(repairCost, t => t.TicketNo, r => r.TicketDtl.TicketDtlNo,
                               (t, r) => new
                               {
                                   ticket = t,
                                   repairCost = r
                               }).Select(r => r.repairCost.DocId).Distinct();
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

        // GET: Admin/Ticket/List
        public IActionResult List()
        {
            return PartialView(_context.Tickets.ToList());
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