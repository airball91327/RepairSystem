using EDIS.Data;
using EDIS.Models.Identity;
using EDIS.Models.RepairModels;
using EDIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EDIS.Components.Repair
{
    public class RepDetail2ViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<AppUserModel, int> _userRepo;
        private readonly CustomUserManager userManager;

        public RepDetail2ViewComponent(ApplicationDbContext context,
                                       IRepository<AppUserModel, int> userRepo,
                                       CustomUserManager customUserManager)
        {
            _context = context;
            _userRepo = userRepo;
            userManager = customUserManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(string id)
        {
            RepairModel repair = _context.Repairs.Find(id);

            repair.DptName = _context.Departments.Find(repair.DptId).Name_C;
            repair.AccDptName = _context.Departments.Find(repair.AccDpt).Name_C;

            return View(repair);
        }

    }
}
