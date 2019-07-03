using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            if (!string.IsNullOrEmpty(ticketno))
            {
                ts = ts.Where(t => t.TicketNo == ticketno).ToList();
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

            /* Search date by Date. */
            //if (string.IsNullOrEmpty(qtyDate1) == false || string.IsNullOrEmpty(qtyDate2) == false)
            //{
            //    ts = ts.Where(t => t.ShutDate >= applyDateFrom && t.ShutDate <= applyDateTo).ToList();
            //}

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

            return View(ticket);
        }

        // POST: Admin/Tickets/Edit/5
        [HttpPost]
        public IActionResult Edit([Bind("TicketNo,TicDate,VendorId,VendorName,TotalAmt,TaxAmt,Note,ScrapValue,ApplyDate,CancelDate")] TicketModel ticketModel)
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