﻿using EDIS.Data;
using EDIS.Models.Identity;
using EDIS.Models.KeepModels;
using EDIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EDIS.Components.KeepCost
{
    public class KeepCostEditViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<AppUserModel, int> _userRepo;
        private readonly CustomUserManager userManager;

        public KeepCostEditViewComponent(ApplicationDbContext context,
                                         IRepository<AppUserModel, int> userRepo,
                                         CustomUserManager customUserManager)
        {
            _context = context;
            _userRepo = userRepo;
            userManager = customUserManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(string id)
        {
            KeepCostModel cost = new KeepCostModel();
            var ur = _userRepo.Find(u => u.UserName == this.User.Identity.Name).FirstOrDefault();

            /* Check the device's contract. */
            //var keepDtl = _context.KeepDtls.Find(id);
            //if (keepDtl.NotInExceptDevice == "Y") //該案件為統包
            //{
            //    ViewData["HideCost"] = "Y";
            //}
            //else
            //{
            //    ViewData["HideCost"] = "N";
            //}
            ViewData["HideCost"] = "N";
            int seqno = _context.KeepCosts.Where(c => c.DocId == id)
                                          .Select(c => c.SeqNo).DefaultIfEmpty().Max();
            cost.DocId = id;
            cost.SeqNo = seqno + 1;
            KeepFlowModel rf = _context.KeepFlows.Where(f => f.DocId == id)
                                                 .Where(f => f.Status == "?").FirstOrDefault();
            var isEngineer = _context.UsersInRoles.Where(u => u.AppRoles.RoleName == "RepEngineer" &&
                                                              u.UserId == ur.Id).FirstOrDefault();
            if (!(rf.Cls.Contains("工程師") && rf.UserId == ur.Id))    /* 流程 => 其他 */
            {

                if (rf.Cls.Contains("工程師") && isEngineer != null)   /* 流程 => 工程師，Login User => 非負責之工程師 */
                {
                    return View(cost);
                }
                List<KeepCostModel> t = _context.KeepCosts.Include(r => r.TicketDtl).Where(c => c.DocId == id).ToList();
                return View("List", t);
            }
            /* 流程 => 工程師，Login User => 負責之工程師 */
            return View(cost);
        }

    }
}
