﻿using EDIS.Data;
using EDIS.Models.Identity;
using EDIS.Models.RepairModels;
using EDIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EDIS.Components.RepairDtl
{
    public class RepDtlEditViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<AppUserModel, int> _userRepo;
        private readonly CustomUserManager userManager;

        public RepDtlEditViewComponent(ApplicationDbContext context,
                                       IRepository<AppUserModel, int> userRepo,
                                       CustomUserManager customUserManager)
        {
            _context = context;
            _userRepo = userRepo;
            userManager = customUserManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(string id)
        {
            RepairDtlModel repairDtl = _context.RepairDtls.Find(id);
            var ur = _userRepo.Find(us => us.UserName == this.User.Identity.Name).FirstOrDefault();

            // Get dealstatuses for dropdownlist.
            List<SelectListItem> listItem = new List<SelectListItem>();
            _context.DealStatuses.Where(d => d.Flg == "Y")
                .ToList()
                .ForEach(d => {
                    listItem.Add(new SelectListItem { Text = d.Title, Value = d.Id.ToString() });
                });
            repairDtl.DealStates = listItem;

            // Get failfactors for dropdownlist.
            List<SelectListItem> listItem2 = new List<SelectListItem>();
            _context.FailFactors.Where(d => d.Flg == "Y")
               .ToList()
               .ForEach(d => {
                   listItem2.Add(new SelectListItem { Text = d.Title, Value = d.Id.ToString() });
               });
            repairDtl.FailFactors = listItem2;

            if (id == null)
            {
                RepairDtlModel dtl = new RepairDtlModel();
                dtl.IsCharged = "N";
                return View(dtl);
            }

            if(repairDtl.InOut == null)  // Set default value.
            {
                repairDtl.InOut = "內修";
            }
            if(repairDtl.IsCharged == null)  // Set default value.
            {
                repairDtl.IsCharged = "N";
            }
            /* Get assetNo. */
            repairDtl.AssetNo = _context.Repairs.Find(repairDtl.DocId).AssetNo;

            RepairFlowModel rf = _context.RepairFlows.Where(f => f.DocId == id)
                                                     .Where(f => f.Status == "?").FirstOrDefault();
            var isEngineer = _context.UsersInRoles.Where(u => u.AppRoles.RoleName == "RepEngineer" &&
                                                              u.UserId == ur.Id).FirstOrDefault();
            /* 流程 => 工程師，Login User => 負責之工程師 */
            if (rf.Cls.Contains("工程師") && rf.UserId == ur.Id)
            {
                return View(repairDtl);
            }
            /* 流程 => 工程師，Login User => 非負責之工程師 */
            else if (rf.Cls.Contains("工程師") && isEngineer != null)
            {
                return View(repairDtl);
            }
            /* 流程 => 其他 */
            else
            {
                repairDtl.DealStateTitle = _context.DealStatuses.Find(repairDtl.DealState).Title;
                var failFactor = _context.FailFactors.Where(f => f.Id == repairDtl.FailFactor).FirstOrDefault();
                if (failFactor != null)  //防止工程師忘記輸入資訊就送出
                {
                    repairDtl.FailFactorTitle = failFactor.Title;
                }
                return View("Details", repairDtl);
            }
        }
    }
}
