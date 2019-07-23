﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EDIS.Data;
using EDIS.Models;
using EDIS.Models.Identity;
using EDIS.Models.LocationModels;
using EDIS.Models.RepairModels;
using EDIS.Repositories;
using EDIS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EDIS.Controllers
{
    [Authorize]
    public class RepairCostController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<RepairModel, string> _repRepo;
        private readonly IRepository<RepairFlowModel, string[]> _repflowRepo;
        private readonly IRepository<AppUserModel, int> _userRepo;
        private readonly IRepository<DepartmentModel, string> _dptRepo;
        private readonly IRepository<DocIdStore, string[]> _dsRepo;
        private readonly IRepository<RepairEmpModel, string[]> _repempRepo;
        private readonly CustomUserManager userManager;

        public RepairCostController(ApplicationDbContext context,
                                    IRepository<RepairModel, string> repairRepo,
                                    IRepository<RepairFlowModel, string[]> repairflowRepo,
                                    IRepository<AppUserModel, int> userRepo,
                                    IRepository<DepartmentModel, string> dptRepo,
                                    IRepository<DocIdStore, string[]> dsRepo,
                                    IRepository<RepairEmpModel, string[]> repairempRepo,
                                    CustomUserManager customUserManager)
        {
            _context = context;
            _repRepo = repairRepo;
            _repflowRepo = repairflowRepo;
            _userRepo = userRepo;
            _dptRepo = dptRepo;
            _dsRepo = dsRepo;
            _repempRepo = repairempRepo;
            userManager = customUserManager;
        }

        [HttpPost]
        public IActionResult Edit(RepairCostModel repairCost)
        {
            var ur = _userRepo.Find(u => u.UserName == this.User.Identity.Name).FirstOrDefault();
            if(repairCost.TicketDtl.TicketDtlNo != null)
            {
                repairCost.TicketDtl.TicketDtlNo = repairCost.TicketDtl.TicketDtlNo.ToUpper();
            }
            if (repairCost.SignNo != null)
            {
                repairCost.SignNo = repairCost.SignNo.ToUpper();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if(repairCost.StockType == "2" || repairCost.StockType == "4")
                    {
                        var dupData = _context.RepairCosts.Include(c => c.TicketDtl)
                                                          .Where(c => c.DocId == repairCost.DocId &&
                                                                 c.PartName == repairCost.PartName &&
                                                                 c.Standard == repairCost.Standard &&
                                                                 c.TicketDtl.TicketDtlNo == repairCost.TicketDtl.TicketDtlNo).FirstOrDefault();
                        if (dupData != null)
                        {
                            string msg = "資料重複儲存!!";
                            return BadRequest(msg);
                        }
                    }
                    else
                    {
                        var dupData = _context.RepairCosts.Include(c => c.TicketDtl)
                                                          .Where(c => c.DocId == repairCost.DocId &&
                                                                 c.PartName == repairCost.PartName &&
                                                                 c.Standard == repairCost.Standard &&
                                                                 c.SignNo == repairCost.SignNo).FirstOrDefault();
                        if (dupData != null)
                        {
                            string msg = "資料重複儲存!!";
                            return BadRequest(msg);
                        }
                    }  
                    
                    int seqno = _context.RepairCosts.Where(c => c.DocId == repairCost.DocId)
                                                    .Select(c => c.SeqNo).DefaultIfEmpty().Max();
                    repairCost.SeqNo = seqno + 1;
                    if (repairCost.StockType == "2" || repairCost.StockType == "4")
                    {
                        if (string.IsNullOrEmpty(repairCost.TicketDtl.TicketDtlNo))
                        {
                            //throw new Exception("發票號碼不可空白!!");
                            string msg = "發票號碼不可空白!!";
                            return BadRequest(msg);
                        }
                        if (repairCost.AccountDate == null)
                        {
                            //throw new Exception("發票日期不可空白!!");
                            string msg = "發票日期不可空白!!";
                            return BadRequest(msg);
                        }
                        int i = _context.TicketDtls.Where(d => d.TicketDtlNo == repairCost.TicketDtl.TicketDtlNo)
                                                   .Select(d => d.SeqNo).DefaultIfEmpty().Max();
                        repairCost.TicketDtl.SeqNo = i + 1;
                        repairCost.TicketDtl.ObjName = repairCost.VendorName;
                        repairCost.TicketDtl.Qty = repairCost.Qty;
                        repairCost.TicketDtl.Unite = repairCost.Unite;
                        repairCost.TicketDtl.Price = repairCost.Price;
                        repairCost.TicketDtl.Cost = repairCost.TotalCost;
                        TicketModel t = _context.Tickets.Find(repairCost.TicketDtl.TicketDtlNo);
                        if (t == null)
                        {
                            t = new TicketModel();
                            t.TicketNo = repairCost.TicketDtl.TicketDtlNo;
                            t.TicDate = repairCost.AccountDate;
                            t.ApplyDate = null;
                            t.CancelDate = null;
                            t.VendorId = repairCost.VendorId;
                            t.VendorName = repairCost.VendorName;
                            repairCost.TicketDtl.Ticket = t;
                            _context.Tickets.Add(t);
                        }
                    _context.TicketDtls.Add(repairCost.TicketDtl);
                }
                    else
                    {
                        repairCost.AccountDate = DateTime.Now.Date;
                        repairCost.TicketDtl = null;
                    }
                    repairCost.Rtp = ur.Id;
                    repairCost.Rtt = DateTime.Now;
                    if (repairCost.StockType != "0")
                        repairCost.PartNo = "";

                    _context.RepairCosts.Add(repairCost);
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

                    return ViewComponent("RepCostList", new { id = repairCost.DocId });
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
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

        public IActionResult PrintStockDetails(string docId, int seqNo)
        {
            var stockDetails = _context.RepairCosts.Find(docId, seqNo);
            return View(stockDetails);
        }

        public ActionResult Delete(string docid, string seqno)
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
    }
}
