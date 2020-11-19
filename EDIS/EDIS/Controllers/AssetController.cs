using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using EDIS.Data;
using EDIS.Models.KeepModels;
using EDIS.Models.RepairModels;
using EDIS.Models.Identity;
using EDIS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using System.Data;
using OfficeOpenXml;

namespace EDIS.Controllers
{
    [Authorize]
    public class AssetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CustomUserManager userManager;
        private readonly CustomRoleManager roleManager;
        private int pageSize = 100;

        public AssetController(ApplicationDbContext context,
                               CustomRoleManager customRoleManager,
                               CustomUserManager customUserManager)
        {
            _context = context;
            roleManager = customRoleManager;
            userManager = customUserManager;
        }

        // GET: Asset/
        public IActionResult Index()
        {
            List<SelectListItem> listItem2 = new List<SelectListItem>();
            SelectListItem li;
            _context.Departments.ToList()
                .ForEach(d =>
                {
                    li = new SelectListItem();
                    li.Text = d.Name_C;
                    li.Value = d.DptId;
                    listItem2.Add(li);

                });
            ViewData["ACCDPT"] = new SelectList(listItem2, "Value", "Text");
            ViewData["DELIVDPT"] = new SelectList(listItem2, "Value", "Text");

            return View();
        }

        // POST: Asset/
        [HttpPost]
        public IActionResult Index(IFormCollection fm, int page = 1)
        {
            QryAsset qryAsset = new QryAsset();
            qryAsset.AssetName = fm["AssetName"];
            qryAsset.AssetNo = fm["AssetNo"];
            qryAsset.AccDpt = fm["AccDpt"];
            qryAsset.DelivDpt = fm["DelivDpt"];
            qryAsset.Type = fm["Type"];

            var assets = _context.Assets.AsQueryable();
            if (!string.IsNullOrEmpty(qryAsset.AssetNo))
            {
                assets = assets.Where(a => a.AssetNo == qryAsset.AssetNo);
            }
            if (!string.IsNullOrEmpty(qryAsset.AssetName))
            {
                assets = assets.Where(a => a.Cname.Contains(qryAsset.AssetName));
            }
            if (!string.IsNullOrEmpty(qryAsset.AccDpt))
            {
                assets = assets.Where(a => a.AccDpt == qryAsset.AccDpt);
            }
            if (!string.IsNullOrEmpty(qryAsset.DelivDpt))
            {
                assets = assets.Where(a => a.DelivDpt == qryAsset.DelivDpt);
            }
            //if (!string.IsNullOrEmpty(qryAsset.BmedNo))
            //{
            //    assets = assets.Where(a => a.BmedNo != null)
            //        .Where(a => a.BmedNo.Contains(qryAsset.BmedNo));
            //}
            if (!string.IsNullOrEmpty(qryAsset.Type))
            {
                assets = assets.Where(a => a.Type == qryAsset.Type);
            }

            List<AssetModel> at = new List<AssetModel>();
            //List<AssetModel> at2 = new List<AssetModel>();
            assets.GroupJoin(_context.Departments, a => a.DelivDpt, d => d.DptId,
                (a, d) => new { Asset = a, Department = d })
                .SelectMany(p => p.Department.DefaultIfEmpty(),
                (x, y) => new { Asset = x.Asset, Department = y })
                .ToList()
                .GroupJoin(_context.AppUsers, e => Convert.ToInt32(e.Asset.DelivUid), u => u.Id,
                (e, u) => new { Asset = e, AppUser = u })
                .SelectMany(p => p.AppUser.DefaultIfEmpty(),
                (e, y) => new { Asset = e.Asset.Asset, Department = e.Asset.Department, AppUser = y })
                .ToList()
                .ForEach(p =>
                {
                    p.Asset.DelivDptName = p.Department == null ? "" : p.Department.Name_C;
                    p.Asset.DelivEmp = p.AppUser == null ? "" : p.AppUser.FullName;
                    at.Add(p.Asset);
                });
            at.GroupJoin(_context.Departments, a => a.AccDpt, d => d.DptId,
                (a, d) => new { Asset = a, Department = d })
                .SelectMany(p => p.Department.DefaultIfEmpty(),
                (x, y) => new { Asset = x.Asset, Department = y })
                .ToList()
                .ForEach(p =>
                {
                    p.Asset.AccDptName = p.Department == null ? "" : p.Department.Name_C;
                    //at2.Add(p.Asset);
                });
            
            // Get MedEngineers to set dropdownlist.
            List<SelectListItem> listItem = new List<SelectListItem>();
            var s = roleManager.GetUsersInRole("RepEngineer").ToList();
            foreach (string l in s)
            {
                AppUserModel u = _context.AppUsers.Where(ur => ur.UserName == l).FirstOrDefault();
                if (u != null)
                {
                    listItem.Add(new SelectListItem { Text = u.FullName, Value = u.Id.ToString() });
                }
            }
            ViewData["KeepEngId"] = new SelectList(listItem, "Value", "Text", "");
            //ViewData["AssetEngId"] = new SelectList(listItem, "Value", "Text", "");
            //
            TempData["qry"] = qryAsset;
            //
            if (at.ToPagedList(page, pageSize).Count <= 0)
                return PartialView("List", at.ToPagedList(1, pageSize));
            return PartialView("List", at.ToPagedList(page, pageSize));
        }

        // GET: Asset/List
        public IActionResult List()
        {
            List<AssetModel> at = _context.Assets.ToList();
            DepartmentModel d;
            at.ForEach(a =>
            {
                a.DelivDptName = (d = _context.Departments.Find(a.DelivDpt)) == null ? "" : d.Name_C;
            });

            return PartialView(at);
        }

        // GET: Asset/Details/5
        public IActionResult Details(string id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            AssetModel asset = _context.Assets.Find(id);
            if (asset == null)
            {
                return StatusCode(404);
            }
            if (asset.DelivUid != null)
            {
                var uid = Convert.ToInt32(asset.DelivUid);
                var userName = _context.AppUsers.Find(uid).UserName;
                asset.DelivEmp = "(" + userName + ") " + asset.DelivEmp;
            }
            asset.DelivDptName = _context.Departments.Find(asset.DelivDpt).Name_C;
            asset.AccDptName = _context.Departments.Find(asset.AccDpt).Name_C;
            asset.VendorName = asset.VendorId == null ? "" : _context.Vendors.Find(Convert.ToInt32(asset.VendorId)).VendorName;

            return View(asset);
        }

        // GET: Asset/AssetView/5
        public IActionResult AssetView(string id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            AssetModel asset = _context.Assets.Find(id);
            if (asset == null)
            {
                return StatusCode(404);
            }
            asset.DelivDptName = _context.Departments.Find(asset.DelivDpt).Name_C;
            asset.AccDptName = _context.Departments.Find(asset.AccDpt).Name_C;

            return PartialView(asset);
        }

        // GET: Asset/Create
        public ActionResult Create()
        {
            List<SelectListItem> listItem = new List<SelectListItem>();
            _context.Departments.ToList().ForEach(d =>
            {
                listItem.Add(new SelectListItem { Text = d.Name_C, Value = d.DptId });
            });
            ViewData["DelivDpt"] = new SelectList(listItem, "Value", "Text", "");

            List<SelectListItem> listItem2 = new List<SelectListItem>();
            listItem2.Add(new SelectListItem { Text = "", Value = "" });
            ViewData["DelivUid"] = new SelectList(listItem2, "Value", "Text", "");

            ViewData["AccDpt"] = new SelectList(listItem, "Value", "Text", "");

            List<SelectListItem> listItem3 = new List<SelectListItem>();
            listItem3.Add(new SelectListItem { Text = "正常", Value = "正常" });
            listItem3.Add(new SelectListItem { Text = "報廢", Value = "報廢" });
            ViewData["DisposeKind"] = new SelectList(listItem3, "Value", "Text", "");
            //
            //List<SelectListItem> listItem4 = new List<SelectListItem>();
            //_context.DeviceClassCodes.ToList()
            //    .ForEach(d =>
            //    {
            //        listItem4.Add(new SelectListItem { Text = d.M_name, Value = d.M_code });
            //    });
            //ViewData["BmedNo"] = new SelectList(listItem4, "Value", "Text", "");
            //
            // Get RepEngineers to set dropdownlist.
            var s = roleManager.GetUsersInRole("RepEngineer").ToList();
            List<SelectListItem> listItem5 = new List<SelectListItem>();
            foreach (string l in s)
            {
                AppUserModel u = _context.AppUsers.Where(ur => ur.UserName == l).FirstOrDefault();
                if (u != null)
                {
                    listItem5.Add(new SelectListItem { Text = u.FullName, Value = u.Id.ToString() });
                }
            }
            //ViewData["AssetEngId"] = new SelectList(listItem5, "Value", "Text", "");

            return View();
        }

        // POST: Asset/Create
        [HttpPost]
        public ActionResult Create(AssetModel asset)
        {
            if (ModelState.IsValid)
            {
                asset.AssetNo = asset.AssetNo.Trim();
                if (_context.Assets.Find(asset.DeviceNo) != null)
                {
                    throw new Exception("設備編號已經存在!!");
                }
                //if (_context.Assets.Find(asset.AssetNo) != null)
                //{
                //    throw new Exception("財產編號已經存在!!");
                //}
                try
                {
                    asset.DelivEmp = asset.DelivUid == null ? "" : _context.AppUsers.Find(Convert.ToInt32(asset.DelivUid)).FullName;
                    //asset.AssetEngName = asset.AssetEngId == 0 ? "" : _context.AppUsers.Find(asset.AssetEngId).FullName;
                    _context.Assets.Add(asset);
                    AssetKeepModel ak = new AssetKeepModel();
                    ak.DeviceNo = asset.DeviceNo;
                    _context.AssetKeeps.Add(ak);
                    _context.SaveChanges();
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }

                return new JsonResult(asset)
                {
                    Value = new { success = true, error = "", id = asset.DeviceNo }
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
            //List<SelectListItem> listItem = new List<SelectListItem>();
            //db.Departments.ToList().ForEach(d => {
            //    listItem.Add(new SelectListItem { Text = d.Name_C, Value = d.DptId });
            //});
            //ViewData["DelivDpt"] = new SelectList(listItem, "Value", "Text", "");

            //List<SelectListItem> listItem2 = new List<SelectListItem>();
            //listItem2.Add(new SelectListItem { Text = "", Value = "" });
            //ViewData["DelivUid"] = new SelectList(listItem2, "Value", "Text", "");

            //ViewData["AccDpt"] = new SelectList(listItem, "Value", "Text", "");

            //List<SelectListItem> listItem3 = new List<SelectListItem>();
            //listItem3.Add(new SelectListItem { Text = "正常", Value = "正常" });
            //listItem3.Add(new SelectListItem { Text = "報廢", Value = "報廢" });
            //ViewData["DisposeKind"] = new SelectList(listItem3, "Value", "Text", "");

            //return View(asset);
        }

        // GET: Asset/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            AssetModel asset = _context.Assets.Find(id);
            if (asset == null)
            {
                return StatusCode(404);
            }
            //
            List<SelectListItem> listItem = new List<SelectListItem>();
            _context.Departments.ToList().ForEach(d =>
            {
                listItem.Add(new SelectListItem { Text = d.Name_C, Value = d.DptId });
            });
            ViewData["DelivDpt"] = new SelectList(listItem, "Value", "Text", "");

            List<SelectListItem> listItem2 = new List<SelectListItem>();
            _context.AppUsers.Where(u => u.DptId == asset.DelivDpt).ToList().ForEach(u =>
            {
                listItem2.Add(new SelectListItem { Text = u.FullName, Value = u.Id.ToString() });
            });
            if (listItem2.Where(item => item.Value == asset.DelivUid.ToString()).Count() == 0)
                listItem2.Add(new SelectListItem { Text = asset.DelivEmp, Value = asset.DelivUid.ToString() });
            ViewData["DelivUid"] = new SelectList(listItem2, "Value", "Text", "");

            ViewData["AccDpt"] = new SelectList(listItem, "Value", "Text", "");

            List<SelectListItem> listItem3 = new List<SelectListItem>();
            listItem3.Add(new SelectListItem { Text = "正常", Value = "正常" });
            listItem3.Add(new SelectListItem { Text = "報廢", Value = "報廢" });
            ViewData["DisposeKind"] = new SelectList(listItem3, "Value", "Text", "");
            //
            //List<SelectListItem> listItem4 = new List<SelectListItem>();
            //_context.DeviceClassCodes.ToList()
            //    .ForEach(d =>
            //    {
            //        listItem4.Add(new SelectListItem { Text = d.M_name, Value = d.M_code });
            //    });
            //ViewData["BmedNo"] = new SelectList(listItem4, "Value", "Text", "");
            //
            // Get MedEngineers to set dropdownlist.
            var s = roleManager.GetUsersInRole("RepEngineer").ToList();
            List<SelectListItem> listItem5 = new List<SelectListItem>();
            foreach (string l in s)
            {
                AppUserModel u = _context.AppUsers.Where(ur => ur.UserName == l).FirstOrDefault();
                if (u != null)
                {
                    listItem5.Add(new SelectListItem { Text = u.FullName, Value = u.Id.ToString() });
                }
            }
            //ViewData["AssetEngId"] = new SelectList(listItem5, "Value", "Text", "");
            //
            if (asset.VendorId != null)
            {
                asset.VendorName = _context.Vendors.Where(v => v.VendorId == Convert.ToInt32(asset.VendorId)).FirstOrDefault().VendorName;
            }

            return View(asset);
        }

        // POST: Asset/Edit/5
        [HttpPost]
        public ActionResult Edit(AssetModel asset)
        {
            if (ModelState.IsValid)
            {
                asset.DelivEmp = asset.DelivUid == null ? "" : _context.AppUsers.Find(Convert.ToInt32(asset.DelivUid)).FullName;
                //asset.AssetEngName = asset.AssetEngId == 0 ? "" : _context.AppUsers.Find(asset.AssetEngId).FullName;
                _context.Entry(asset).State = EntityState.Modified;
                try
                {
                    _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }

                return new JsonResult(asset)
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

        public IActionResult AssetExcel(QryAsset v)
        {
            DataTable dt = new DataTable();
            DataRow dw;
            dt.Columns.Add("類別");
            dt.Columns.Add("設備編號");
            dt.Columns.Add("財產編號");
            dt.Columns.Add("設備名稱");
            dt.Columns.Add("成本中心");
            dt.Columns.Add("保管部門");
            dt.Columns.Add("保管人員");
            dt.Columns.Add("工程師名稱");
            dt.Columns.Add("廠牌");
            dt.Columns.Add("規格");
            dt.Columns.Add("型號");
            dt.Columns.Add("廠商名稱");
            dt.Columns.Add("廠商統編");
            dt.Columns.Add("製造號碼");
            dt.Columns.Add("財產狀況");
            dt.Columns.Add("成本(取得金額)");
            dt.Columns.Add("保養週期");
            dt.Columns.Add("保養起始月");
            dt.Columns.Add("保養方式");
            dt.Columns.Add("維修工程師(保養用)");
            dt.Columns.Add("購入日(取得日期)");

            List<AssetModel> mv = QryAsset(v);
            mv.Join(_context.AppUsers, a => Convert.ToInt32(a.DelivUid), u => u.Id,
                (a, u) => new {
                    asset = a,
                    user = u
                })
                .GroupJoin(_context.AssetKeeps, a => a.asset.DeviceNo, k => k.DeviceNo,
                (a, k) => new {
                    asset = a.asset,
                    user = a.user,
                    assetkeep = k
                }).SelectMany(a => a.assetkeep.DefaultIfEmpty(),
                (a, s) => new {
                    asset = a.asset,
                    user = a.user,
                    assetkeep = s
                })
                .GroupJoin(_context.Vendors, a => Convert.ToInt32(a.asset.VendorId), vd => vd.VendorId,
                (a, vd) => new {
                    asset = a.asset,
                    user = a.user,
                    assetkeep = a.assetkeep,
                    vendor = vd
                }).SelectMany(a => a.vendor.DefaultIfEmpty(),
                (a, s) => new {
                    asset = a.asset,
                    user = a.user,
                    assetkeep = a.assetkeep,
                    vendor = s
                })
                .ToList()
            .ForEach(m =>
            {
                dw = dt.NewRow();
                dw[0] = m.asset.AssetClass;
                dw[1] = m.asset.DeviceNo;
                dw[2] = m.asset.AssetNo;
                dw[3] = m.asset.Cname;
                dw[4] = m.asset.AccDptName;
                dw[5] = m.asset.DelivDptName;
                dw[6] = m.asset.DelivEmp;
                dw[7] = m.user.FullName;
                dw[8] = m.asset.Brand;
                dw[9] = m.asset.Standard;
                dw[10] = m.asset.Type;
                dw[11] = m.vendor == null ? "" : m.vendor.VendorName;
                dw[12] = m.vendor == null ? "" : m.vendor.UniteNo;
                dw[13] = m.asset.MakeNo;
                dw[14] = m.asset.DisposeKind;
                dw[15] = m.asset.Cost;
                dw[16] = m.assetkeep == null ? null : m.assetkeep.Cycle;
                dw[17] = m.assetkeep == null ? null : m.assetkeep.KeepYm;
                dw[18] = m.assetkeep == null ? "" :
                         m.assetkeep.InOut == "0" ? "自行" :
                         m.assetkeep.InOut == "1" ? "委外" :
                         m.assetkeep.InOut == "2" ? "租賃" :
                         m.assetkeep.InOut == "3" ? "保固" : "";
                dw[19] = m.assetkeep == null ? "" : m.assetkeep.KeepEngName;
                dw[20] = m.asset.BuyDate == null ? "" : m.asset.BuyDate.Value.ToString("yyyy/MM/dd");
                dt.Rows.Add(dw);
            });
            //
            ExcelPackage excel = new ExcelPackage();
            var workSheet = excel.Workbook.Worksheets.Add("設備列表清單");
            workSheet.Cells[1, 1].LoadFromDataTable(dt, true);
            // Generate the Excel, convert it into byte array and send it back to the controller.
            byte[] fileContents;
            fileContents = excel.GetAsByteArray();

            if (fileContents == null || fileContents.Length == 0)
            {
                return NotFound();
            }

            return File(
                fileContents: fileContents,
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: "設備列表清單.xlsx"
            );
        }

        public List<AssetModel> QryAsset(QryAsset qryAsset)
        {
            List<AssetModel> at = new List<AssetModel>();

            var assets = _context.Assets.AsQueryable();
            if (!string.IsNullOrEmpty(qryAsset.AssetNo))
            {
                assets = assets.Where(a => a.AssetNo == qryAsset.AssetNo);
            }
            if (!string.IsNullOrEmpty(qryAsset.AssetName))
            {
                assets = assets.Where(a => a.Cname.Contains(qryAsset.AssetName));
            }
            if (!string.IsNullOrEmpty(qryAsset.AccDpt))
            {
                assets = assets.Where(a => a.AccDpt == qryAsset.AccDpt);
            }
            if (!string.IsNullOrEmpty(qryAsset.DelivDpt))
            {
                assets = assets.Where(a => a.DelivDpt == qryAsset.DelivDpt);
            }
            if (!string.IsNullOrEmpty(qryAsset.Type))
            {
                assets = assets.Where(a => a.Type == qryAsset.Type);
            }

            assets.GroupJoin(_context.Departments, a => a.DelivDpt, d => d.DptId,
                (a, d) => new { Asset = a, Department = d })
                .SelectMany(p => p.Department.DefaultIfEmpty(),
                (x, y) => new { Asset = x.Asset, Department = y })
                .ToList()
                .GroupJoin(_context.AppUsers, e => Convert.ToInt32(e.Asset.DelivUid), u => u.Id,
                (e, u) => new { Asset = e, AppUser = u })
                .SelectMany(p => p.AppUser.DefaultIfEmpty(),
                (e, y) => new { Asset = e.Asset.Asset, Department = e.Asset.Department, AppUser = y })
                .ToList()
                .ForEach(p =>
                {
                    p.Asset.DelivDptName = p.Department == null ? "" : p.Department.Name_C;
                    p.Asset.DelivEmp = p.AppUser == null ? "" : p.AppUser.FullName;
                    at.Add(p.Asset);
                });
            at.GroupJoin(_context.Departments, a => a.AccDpt, d => d.DptId,
                (a, d) => new { Asset = a, Department = d })
                .SelectMany(p => p.Department.DefaultIfEmpty(),
                (x, y) => new { Asset = x.Asset, Department = y })
                .ToList()
                .ForEach(p =>
                {
                    p.Asset.AccDptName = p.Department == null ? "" : p.Department.Name_C;
                    //at2.Add(p.Asset);
                });

            //at = at.GroupBy(a => a.AssetNo).Select(g => g.First()).ToList();

            return at;
        }

        // GET: Asset/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            AssetModel asset = _context.Assets.Find(id);
            AssetKeepModel ak = _context.AssetKeeps.Find(id);
            _context.Assets.Remove(asset);
            _context.AssetKeeps.Remove(ak);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // POST: Asset/UpdEngineer/5
        //[HttpPost]
        //public ActionResult UpdEngineer(string id, string assets)
        //{
        //    string[] s = assets.Split(new char[] { ';' });
        //    AssetModel asset;
        //    foreach (string ss in s)
        //    {
        //        asset = _context.Assets.Find(ss);
        //        if (asset != null)
        //        {
        //            AppUserModel u = _context.AppUsers.Find(Convert.ToInt32(id));
        //            if (u != null)
        //            {
        //                asset.AssetEngId = u.Id;
        //                asset.AssetEngName = u.FullName;
        //                _context.Entry(asset).State = EntityState.Modified;
        //                _context.SaveChanges();
        //            }
        //        }
        //    }
        //    return new JsonResult(id)
        //    {
        //        Value = new { success = true, error = "" }
        //    };
        //}
    }
}