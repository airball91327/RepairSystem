using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EDIS.Models;
using EDIS.Models.RepairModels;
using Microsoft.AspNetCore.Authorization;
using EDIS.Data;
using EDIS.Repositories;
using EDIS.Models.Identity;

namespace EDIS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<RepairModel, string> _repRepo;
        private readonly IRepository<AppUserModel, int> _userRepo;
        private readonly CustomUserManager userManager;

        public HomeController(ApplicationDbContext context,
                                IRepository<RepairModel, string> repairRepo,
                                IRepository<AppUserModel, int> userRepo,
                                CustomUserManager customUserManager)
        {
            _context = context;
            _repRepo = repairRepo;
            _userRepo = userRepo;
            userManager = customUserManager;
        }

        [Authorize]
        public IActionResult Index()
        {
            /* Get user details. */
            var ur = _userRepo.Find(u => u.UserName == this.User.Identity.Name).FirstOrDefault();

            var repairCount = _context.RepairFlows.Where(f => f.Status == "?")
                                                  .Where(f => f.UserId == ur.Id).Count();

            UnsignCounts v = new UnsignCounts();
            v.RepairCount = repairCount;
            v.KeepCount = 0;

            return View(v);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
