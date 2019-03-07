using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EDIS.Data;
using EDIS.Models.RepairModels;
using Microsoft.AspNetCore.Authorization;
using EDIS.Repositories;
using EDIS.Models.Identity;

namespace EDIS.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class EngsInDeptsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<AppUserModel, int> _userRepo;
        private readonly CustomUserManager userManager;

        public EngsInDeptsController(ApplicationDbContext context,
                                     IRepository<AppUserModel, int> userRepo,
                                     CustomUserManager customUserManager)
        {
            _context = context;
            _userRepo = userRepo;
            userManager = customUserManager;
        }

        // GET: Admin/EngsInDepts
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.EngsInDepts.Include(e => e.AppUsers).Include(e => e.Departments);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Admin/EngsInDept
        public async Task<IActionResult> EngsInDept()
        {
            /* Get groups and set department dropdown list. */
            var departments = _context.EngsInDepts.Include(e => e.Departments)
                                      .GroupBy(e => e.DptId).Select(group => group.First()).ToList() ;
            List<SelectListItem> deptList = new List<SelectListItem>();
            foreach (var item in departments)
            {
                deptList.Add(new SelectListItem()
                {
                    Text = item.DptId + item.Departments.Name_C,
                    Value = item.DptId
                });
            }

            ViewData["DptId"] = deptList;
            return View();
        }

        // GET: Admin/EngsInDepts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var engsInDeptsModel = await _context.EngsInDepts
                .Include(e => e.AppUsers)
                .Include(e => e.Departments)
                .SingleOrDefaultAsync(m => m.EngId == id);
            if (engsInDeptsModel == null)
            {
                return NotFound();
            }

            return View(engsInDeptsModel);
        }

        // GET: Admin/EngsInDepts/Create
        public IActionResult Create()
        {
            ViewData["Id"] = new SelectList(_context.AppUsers, "Id", "Email");
            ViewData["DptId"] = new SelectList(_context.Departments, "DptId", "DptId");
            return View();
        }

        // POST: Admin/EngsInDepts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DptId,UserName")] EngsInDeptsModel engsInDeptsModel)
        {
            if (ModelState.IsValid)
            {
                _context.Add(engsInDeptsModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Id"] = new SelectList(_context.AppUsers, "Id", "Email", engsInDeptsModel.EngId);
            ViewData["DptId"] = new SelectList(_context.Departments, "DptId", "DptId", engsInDeptsModel.DptId);
            return View(engsInDeptsModel);
        }

        // GET: Admin/EngsInDepts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var engsInDeptsModel = await _context.EngsInDepts.SingleOrDefaultAsync(m => m.EngId == id);
            if (engsInDeptsModel == null)
            {
                return NotFound();
            }
            ViewData["Id"] = new SelectList(_context.AppUsers, "Id", "Email", engsInDeptsModel.EngId);
            ViewData["DptId"] = new SelectList(_context.Departments, "DptId", "DptId", engsInDeptsModel.DptId);
            return View(engsInDeptsModel);
        }

        // POST: Admin/EngsInDepts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DptId,UserName")] EngsInDeptsModel engsInDeptsModel)
        {
            if (id != engsInDeptsModel.EngId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(engsInDeptsModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EngsInDeptsModelExists(engsInDeptsModel.EngId))
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
            ViewData["Id"] = new SelectList(_context.AppUsers, "Id", "Email", engsInDeptsModel.EngId);
            ViewData["DptId"] = new SelectList(_context.Departments, "DptId", "DptId", engsInDeptsModel.DptId);
            return View(engsInDeptsModel);
        }

        // GET: Admin/EngsInDepts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var engsInDeptsModel = await _context.EngsInDepts
                .Include(e => e.AppUsers)
                .Include(e => e.Departments)
                .SingleOrDefaultAsync(m => m.EngId == id);
            if (engsInDeptsModel == null)
            {
                return NotFound();
            }

            return View(engsInDeptsModel);
        }

        // POST: Admin/EngsInDepts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var engsInDeptsModel = await _context.EngsInDepts.SingleOrDefaultAsync(m => m.EngId == id);
            _context.EngsInDepts.Remove(engsInDeptsModel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EngsInDeptsModelExists(int id)
        {
            return _context.EngsInDepts.Any(e => e.EngId == id);
        }
    }
}
