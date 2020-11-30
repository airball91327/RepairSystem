using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EDIS.Data;
using EDIS.Models.Identity;
using Microsoft.AspNetCore.Http;
using X.PagedList;
using Microsoft.AspNetCore.Authorization;

namespace EDIS.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private int pageSize = 50;

        public DepartmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Department
        public IActionResult Index()
        {
            return View();
        }

        // POST: Admin/Department/Index
        [HttpPost]
        public IActionResult Index(IFormCollection fm, int page = 1)
        {
            string dptname = fm["qtyDPTNAME"];
            string dptid = fm["qtyDPTID"];

            var dptlist = _context.Departments.AsQueryable();
            if (!string.IsNullOrEmpty(dptname))
            {
                dptlist = dptlist.Where(d => d.Name_C != null)
                                 .Where(d => d.Name_C.Contains(dptname));
            }
            if (!string.IsNullOrEmpty(dptid))
            {
                dptlist = dptlist.Where(d => d.DptId == dptid);
            }

            if (dptlist.ToPagedList(page, pageSize).Count <= 0)
                return PartialView("List", dptlist.ToPagedList(1, pageSize));

            return PartialView("List", dptlist.ToPagedList(page, pageSize));
        }

        // GET: Admin/Department/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var departmentModel = await _context.Departments
                .FirstOrDefaultAsync(m => m.DptId == id);
            if (departmentModel == null)
            {
                return NotFound();
            }

            return View(departmentModel);
        }

        // GET: Admin/Department/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Department/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DptId,Name_C,Name_E,Email,Loc")] DepartmentModel departmentModel)
        {
            var dptExist = _context.Departments.Find(departmentModel.DptId);
            if (dptExist != null)
            {
                ModelState.AddModelError("DptId", "已有相同的部門代號!");
                return View(departmentModel);
            }
            if (ModelState.IsValid)
            {
                departmentModel.DateCreated = DateTime.Now;
                departmentModel.LastActivityDate = DateTime.Now;
                _context.Add(departmentModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(departmentModel);
        }

        // GET: Admin/Department/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var departmentModel = await _context.Departments.FindAsync(id);
            if (departmentModel == null)
            {
                return NotFound();
            }
            return View(departmentModel);
        }

        // POST: Admin/Department/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("DptId,Name_C,Name_E,Email,Loc,DateCreated")] DepartmentModel departmentModel)
        {
            if (id != departmentModel.DptId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    departmentModel.LastActivityDate = DateTime.Now;
                    _context.Update(departmentModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentModelExists(departmentModel.DptId))
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
            return View(departmentModel);
        }

        
        private bool DepartmentModelExists(string id)
        {
            return _context.Departments.Any(e => e.DptId == id);
        }
    }
}
