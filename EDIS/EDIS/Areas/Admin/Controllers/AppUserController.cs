using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EDIS.Data;
using EDIS.Models.Identity;
using EDIS.Repositories;
using X.PagedList;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

namespace EDIS.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class AppUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<AppUserModel, int> _userRepo;
        private readonly IRepository<DepartmentModel, string> _dptRepo;
        private readonly CustomUserManager userManager;
        private readonly CustomRoleManager roleManager;
        private int pageSize = 50;

        public AppUserController(ApplicationDbContext context,
                                 IRepository<AppUserModel, int> userRepo,
                                 IRepository<DepartmentModel, string> dptRepo,
                                 CustomUserManager customUserManager,
                                 CustomRoleManager customRoleManager)
        {
            _context = context;
            _userRepo = userRepo;
            _dptRepo = dptRepo;
            userManager = customUserManager;
            roleManager = customRoleManager;
        }

        // GET: Admin/AppUser
        public IActionResult Index()
        {
            List<SelectListItem> listItem = new List<SelectListItem>();
            SelectListItem li;
            _context.Departments.ToList()
                    .ForEach(d =>
                    {
                        li = new SelectListItem();
                        li.Text = d.Name_C + "(" + d.DptId + ")";
                        li.Value = d.DptId;
                        listItem.Add(li);

                    });
            ViewData["DEPT"] = new SelectList(listItem, "Value", "Text");

            return View();
        }

        // POST: Admin/AppUser/Index
        [HttpPost]
        public IActionResult Index(IFormCollection fm, int page = 1)
        {
            string qname = fm["qtyNAME"];
            string dpt = fm["qtyDEPT"];
            string uname = fm["qtyUSERNAME"];
            List<AppUserModel> ulist;
            AppUserModel usr = _context.AppUsers.Where(a => a.UserName == User.Identity.Name).FirstOrDefault();

            if (userManager.IsInRole(User, "Admin") || userManager.IsInRole(User, "Manager"))
            {
                ulist = _context.AppUsers.ToList();
                if (userManager.IsInRole(User, "Manager"))
                {
                    ulist = ulist.Where(u => u.DptId == usr.DptId).ToList();
                }
            }
            else
            {
                ulist = _context.AppUsers.Where(u => u.UserName == User.Identity.Name).ToList();
            }
            if (!string.IsNullOrEmpty(qname))
            {
                ulist = ulist.Where(u => u.FullName != null)
                             .Where(u => u.FullName.Contains(qname)).ToList();
            }
            if (!string.IsNullOrEmpty(dpt))
            {
                ulist = ulist.Where(u => u.DptId == dpt).ToList();
            }
            if (!string.IsNullOrEmpty(uname))
            {
                ulist = ulist.Where(u => u.UserName == uname).ToList();
            }
            if (ulist.ToPagedList(page, pageSize).Count <= 0)
                return PartialView("List", ulist.ToPagedList(1, pageSize));

            return PartialView("List", ulist.ToPagedList(page, pageSize));
        }

        // GET: Admin/AppUser/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appUserModel = await _context.AppUsers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appUserModel == null)
            {
                return NotFound();
            }
            List<UserInRolesViewModel> rv = roleManager.getRoles();
            UserInRolesViewModel uv;
            foreach (string r in roleManager.GetRolesForUser(appUserModel.UserName))
            {
                uv = rv.Where(v => v.RoleName == r).ToList().FirstOrDefault();
                if (uv != null)
                {
                    uv.IsSelected = true;
                }
            }
            appUserModel.InRoles = rv;
            return View(appUserModel);
        }

        // GET: Admin/AppUser/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/AppUser/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserName,FullName,Password,Email,Ext,Mobile,DptId,VendorId,DateCreated,LastActivityDate,Status")] AppUserModel appUserModel)
        {
            if (ModelState.IsValid)
            {
                _context.Add(appUserModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(appUserModel);
        }

        // GET: Admin/AppUser/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appUserModel = await _context.AppUsers.FindAsync(id);
            if (appUserModel == null)
            {
                return NotFound();
            }
            else
            {
                if (appUserModel.VendorId != null)
                    appUserModel.VendorName = _context.Vendors.Find(appUserModel.VendorId).VendorName;
                List<UserInRolesViewModel> rv = roleManager.getRoles();
                UserInRolesViewModel uv;
                foreach (string r in roleManager.GetRolesForUser(appUserModel.UserName))
                {
                    uv = rv.Where(v => v.RoleName == r).ToList().FirstOrDefault();
                    if (uv != null)
                    {
                        uv.IsSelected = true;
                    }
                }
                appUserModel.InRoles = rv;
            }
            return View(appUserModel);
        }

        // POST: Admin/AppUser/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AppUserModel appUserModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = _context.AppUsers.Find(appUserModel.Id);
                    if (user != null)
                    {
                        //user.Email = appUser.Email;
                        if (userManager.IsInRole(User, "Admin"))
                        {
                            var ur = await userManager.FindByNameAsync(user.UserName);
                            if (roleManager.GetRolesForUser(appUserModel.UserName).Count() > 0)
                            {                            
                                userManager.RemoveFromRoles(ur, roleManager.GetRolesForUser(appUserModel.UserName));
                            }                 
                            List<UserInRolesViewModel> uv = appUserModel.InRoles.Where(v => v.IsSelected == true).ToList();
                            foreach (UserInRolesViewModel u in uv)
                            {
                                userManager.AddToRole(ur, u.RoleName);
                            }
                        }
                        //
                        _context.Entry(user).State = EntityState.Detached;
                        appUserModel.LastActivityDate = DateTime.Now;
                        _context.Entry(appUserModel).State = EntityState.Modified;
                        _context.SaveChanges();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppUserModelExists(appUserModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(appUserModel);
        }

        // GET: Admin/AppUser/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appUserModel = await _context.AppUsers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appUserModel == null)
            {
                return NotFound();
            }

            return View(appUserModel);
        }

        // POST: Admin/AppUser/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appUserModel = await _context.AppUsers.FindAsync(id);
            _context.AppUsers.Remove(appUserModel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AppUserModelExists(int id)
        {
            return _context.AppUsers.Any(e => e.Id == id);
        }
    }
}
