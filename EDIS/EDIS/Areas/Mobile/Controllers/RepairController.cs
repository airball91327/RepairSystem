using System;
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

namespace EDIS.Areas.Mobile.Controllers
{
    [Area("Mobile")]
    [Authorize]
    public class RepairController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<RepairModel, string> _repRepo;
        private readonly IRepository<RepairDtlModel, string> _repdtlRepo;
        private readonly IRepository<RepairFlowModel, string[]> _repflowRepo;
        private readonly IRepository<AppUserModel, int> _userRepo;
        private readonly IRepository<DepartmentModel, string> _dptRepo;
        private readonly IRepository<DocIdStore, string[]> _dsRepo;
        private readonly IRepository<BuildingModel, int> _buildRepo;
        private readonly IEmailSender _emailSender;
        private readonly CustomUserManager userManager;
        private readonly CustomRoleManager roleManager;

        public RepairController(ApplicationDbContext context,
                                IRepository<RepairModel, string> repairRepo,
                                IRepository<RepairDtlModel, string> repairdtlRepo,
                                IRepository<RepairFlowModel, string[]> repairflowRepo,
                                IRepository<AppUserModel, int> userRepo,
                                IRepository<DepartmentModel, string> dptRepo,
                                IRepository<DocIdStore, string[]> dsRepo,
                                IRepository<BuildingModel, int> buildRepo,
                                IEmailSender emailSender,
                                CustomUserManager customUserManager,
                                CustomRoleManager customRoleManager)
        {
            _context = context;
            _repRepo = repairRepo;
            _repdtlRepo = repairdtlRepo;
            _repflowRepo = repairflowRepo;
            _userRepo = userRepo;
            _dptRepo = dptRepo;
            _dsRepo = dsRepo;
            _buildRepo = buildRepo;
            _emailSender = emailSender;
            userManager = customUserManager;
            roleManager = customRoleManager;
        }

        // GET: Mobile/Repair/
        public ActionResult Index()
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

        // POST: Mobile/Repair/
        [HttpPost]
        public ActionResult Index(QryRepListData qdata)
        {
            string docid = qdata.qtyDOCID;
            string ano = qdata.qtyASSETNO;
            string acc = qdata.qtyACCDPT;
            string aname = qdata.qtyASSETNAME;
            string ftype = qdata.qtyFLOWTYPE;
            string dptid = qdata.qtyDPTID;
            string qtyDate1 = qdata.qtyApplyDateFrom;
            string qtyDate2 = qdata.qtyApplyDateTo;
            string qtyDealStatus = qdata.qtyDealStatus;

            DateTime applyDateFrom = DateTime.Now;
            DateTime applyDateTo = DateTime.Now;
            /* Dealing search by date. */
            if (qtyDate1 != null && qtyDate2 != null)// If 2 date inputs have been insert, compare 2 dates.
            {
                DateTime date1 = DateTime.Parse(qtyDate1);
                DateTime date2 = DateTime.Parse(qtyDate2);
                int result = DateTime.Compare(date1, date2);
                if (result < 0)
                {
                    applyDateFrom = date1.Date;
                    applyDateTo = date2.Date;
                }
                else if (result == 0)
                {
                    applyDateFrom = date1.Date;
                    applyDateTo = date1.Date;
                }
                else
                {
                    applyDateFrom = date2.Date;
                    applyDateTo = date1.Date;
                }
            }
            else if(qtyDate1 == null && qtyDate2 != null)
            {
                applyDateFrom = DateTime.Parse(qtyDate2);
                applyDateTo = DateTime.Parse(qtyDate2);
            }
            else if(qtyDate1 != null && qtyDate2 == null)
            {
                applyDateFrom = DateTime.Parse(qtyDate1);
                applyDateTo = DateTime.Parse(qtyDate1);
            }
            

            List<RepairListVModel> rv = new List<RepairListVModel>();
            /* Get login user. */
            var ur = _userRepo.Find(u => u.UserName == this.User.Identity.Name).FirstOrDefault();

            /* Check search type for engineer, if no search value search users's doc, else search all. */
            var searchAllDoc = false;
            if (!(string.IsNullOrEmpty(docid) && string.IsNullOrEmpty(ano) && string.IsNullOrEmpty(acc) 
                                              && string.IsNullOrEmpty(aname) && string.IsNullOrEmpty(dptid)))
            {
                if (userManager.IsInRole(User, "RepEngineer") == true)
                    searchAllDoc = true;
            }

            var rps = _context.Repairs.ToList();
            if (!string.IsNullOrEmpty(docid))
            {
                rps = rps.Where(v => v.DocId == docid).ToList();
            }
            if (!string.IsNullOrEmpty(ano))
            {
                rps = rps.Where(v => v.AssetNo == ano).ToList();
            }
            if (!string.IsNullOrEmpty(dptid))
            {
                rps = rps.Where(v => v.DptId == dptid).ToList();
            }
            if (!string.IsNullOrEmpty(acc))
            {
                rps = rps.Where(v => v.AccDpt == acc).ToList();
            }
            if (!string.IsNullOrEmpty(aname))
            {
                rps = rps.Where(v => v.AssetName != null)
                        .Where(v => v.AssetName.Contains(aname))
                        .ToList();
            }
            if (string.IsNullOrEmpty(qtyDate1) == false || string.IsNullOrEmpty(qtyDate2) == false)
            {
                rps = rps.Where(v => v.ApplyDate >= applyDateFrom && v.ApplyDate <= applyDateTo).ToList();
            }

            /* If no search result. */
            if (rps.Count() == 0)
            {
                return View("List", rv);
            }

            switch (ftype)
            {
                /* 與登入者相關且流程不在該登入者身上的文件 */
                case "流程中":
                    rps.Join(_context.RepairFlows.Where(f2 => f2.UserId == ur.Id && f2.Status == "1")
                       .Select(f => f.DocId).Distinct(),
                               r => r.DocId, f2 => f2, (r, f2) => r)
                       .Join(_context.RepairFlows.Where(f => f.Status == "?" && f.UserId != ur.Id),
                       r => r.DocId, f => f.DocId,
                       (r, f) => new
                       {
                           repair = r,
                           flow = f
                       })
                       //.Join(_context.Assets, r => r.repair.AssetNo, a => a.AssetNo,
                       //(r, a) => new
                       //{
                       //    repair = r.repair,
                       //    asset = a,
                       //    flow = r.flow
                       //})
                       .Join(_context.RepairDtls, m => m.repair.DocId, d => d.DocId,
                       (m, d) => new
                       {
                           repair = m.repair,
                           //asset = m.asset,
                           flow = m.flow,
                           repdtl = d
                       })
                       .Join(_context.Departments, j => j.repair.AccDpt, d => d.DptId,
                       (j, d) => new
                       {
                            repair = j.repair,
                            //asset = j.asset,
                            flow = j.flow,
                            repdtl = j.repdtl,
                            dpt = d
                       })
                       .ToList()
                       .ForEach(j => rv.Add(new RepairListVModel
                       {
                           DocType = "請修",
                           RepType = j.repair.RepType,
                           DocId = j.repair.DocId,
                           ApplyDate = j.repair.ApplyDate,
                           //AssetNo = j.repair.AssetNo,
                           //AssetName = j.repair.AssetName,
                           //Brand = j.asset.Brand,
                           PlaceLoc = j.repair.LocType,
                           //Type = j.asset.Type,
                           ApplyDpt = j.repair.DptId,
                           AccDpt = j.repair.AccDpt,
                           AccDptName = j.dpt.Name_C,
                           TroubleDes = j.repair.TroubleDes,
                           DealState = _context.DealStatuses.Find(j.repdtl.DealState).Title,
                           DealDes = j.repdtl.DealDes,
                           Cost = j.repdtl.Cost,
                           Days = DateTime.Now.Subtract(j.repair.ApplyDate).Days,
                           Flg = j.flow.Status,
                           FlowUid = j.flow.UserId,
                           FlowCls = j.flow.Cls,
                           FlowDptId = _context.AppUsers.Find(j.flow.UserId).DptId,
                           repdata = j.repair
                       }));
                       break;
                /* 與登入者相關且結案的文件 */
                case "已結案":
                    /* Get all closed repair docs. */
                    List<RepairFlowModel> rf = _context.RepairFlows.Where(f => f.Status == "2").ToList();

                    if (userManager.IsInRole(User, "Admin") || userManager.IsInRole(User, "Manager")
                                                            || userManager.IsInRole(User, "RepEngineer"))
                    {
                        if (userManager.IsInRole(User, "Manager"))
                        {
                            rf = rf.Join(_context.Repairs.Where(r => r.AccDpt == ur.DptId),
                            f => f.DocId, r => r.DocId, (f, r) => f).ToList();
                        }
                        /* If no other search values, search the docs belong the login engineer. */
                        if (userManager.IsInRole(User, "RepEngineer") && searchAllDoc == false)
                        {
                            rf = rf.Join(_context.RepairFlows.Where(f2 => f2.UserId == ur.Id),
                                 f => f.DocId, f2 => f2.DocId, (f, f2) => f).ToList();
                        }
                    }
                    else /* If normal user, search the docs belong himself. */
                    {
                        rf = rf.Join(_context.RepairFlows.Where(f2 => f2.UserId == ur.Id),
                             f => f.DocId, f2 => f2.DocId, (f, f2) => f).ToList();
                    }

                    rf.Select(f => new
                    {
                        f.DocId,
                        f.UserId,
                        f.Status,
                        f.Cls
                    }).Distinct().Join(rps.DefaultIfEmpty(), f => f.DocId, r => r.DocId,
                    (f, r) => new
                    {
                        repair = r,
                        flow = f
                    })
                    //.Join(_context.Assets, r => r.repair.AssetNo, a => a.AssetNo,
                    //(r, a) => new
                    //{
                    //    repair = r.repair,
                    //    asset = a,
                    //    flow = r.flow
                    //})
                    .Join(_context.RepairDtls, m => m.repair.DocId, d => d.DocId,
                    (m, d) => new
                    {
                        repair = m.repair,
                        //asset = m.asset,
                        flow = m.flow,
                        repdtl = d
                    })
                    .Join(_context.Departments, j => j.repair.AccDpt, d => d.DptId,
                    (j, d) => new
                    {
                        repair = j.repair,
                        //asset = j.asset,
                        flow = j.flow,
                        repdtl = j.repdtl,
                        dpt = d
                    }).ToList()
                    .ForEach(j => rv.Add(new RepairListVModel
                    {
                        DocType = "請修",
                        RepType = j.repair.RepType,
                        DocId = j.repair.DocId,
                        ApplyDate = j.repair.ApplyDate,
                        //AssetNo = j.repair.AssetNo,
                        //AssetName = j.repair.AssetName,
                        //Brand = j.asset.Brand,
                        PlaceLoc = j.repair.LocType,
                        //Type = j.asset.Type,
                        ApplyDpt = j.repair.DptId,
                        AccDpt = j.repair.AccDpt,
                        AccDptName = j.dpt.Name_C,
                        TroubleDes = j.repair.TroubleDes,
                        DealState = _context.DealStatuses.Find(j.repdtl.DealState).Title,
                        DealDes = j.repdtl.DealDes,
                        Cost = j.repdtl.Cost,
                        Days = DateTime.Now.Subtract(j.repair.ApplyDate).Days,
                        Flg = j.flow.Status,
                        FlowUid = j.flow.UserId,
                        FlowCls = j.flow.Cls,
                        FlowDptId = _context.AppUsers.Find(j.flow.UserId).DptId,
                        repdata = j.repair
                    }));
                    break;
                /* 與登入者相關且流程在該登入者身上的文件 */
                case "待簽核":
                    /* Get all dealing repair docs. */
                    var repairFlows = _context.RepairFlows.Join(rps.DefaultIfEmpty(), f => f.DocId, r => r.DocId,
                    (f, r) => new
                    {
                        repair = r,
                        flow = f
                    }).ToList();

                    if (userManager.IsInRole(User, "Admin") || userManager.IsInRole(User, "RepEngineer"))
                    {
                        /* If has other search values, search all RepairDocs which flowCls is in engineer. */
                        /* Else return the docs belong the login engineer.  */
                        if (userManager.IsInRole(User, "RepEngineer") && searchAllDoc == true)
                        {
                            repairFlows = repairFlows.Where(f => f.flow.Status == "?" && f.flow.Cls.Contains("工程師")).ToList();
                        }
                        else
                        {
                            repairFlows = repairFlows.Where(f => (f.flow.Status == "?" && f.flow.UserId == ur.Id) ||
                                                                 (f.flow.Status == "?" && f.flow.Cls == "驗收人" && 
                                                                  _context.AppUsers.Find(f.flow.UserId).DptId == ur.DptId)).ToList();
                        }
                    }
                    else
                    {
                        repairFlows = repairFlows.Where(f => ( f.flow.Status == "?" && f.flow.UserId == ur.Id ) ||
                                                             ( f.flow.Status == "?" && f.flow.Cls == "驗收人" && f.repair.DptId == ur.DptId )).ToList();
                    }

                    //repairFlows.Select(f => new
                    //{
                    //    f.DocId,
                    //    f.UserId,
                    //    f.Status,
                    //    f.Cls
                    //}).Distinct().Join(rps.DefaultIfEmpty(), f => f.DocId, r => r.DocId,
                    //(f, r) => new
                    //{
                    //    repair = r,
                    //    flow = f
                    //})
                    //.Join(_context.Assets, r => r.repair.AssetNo, a => a.AssetNo,
                    //(r, a) => new
                    //{
                    //    repair = r.repair,
                    //    asset = a,
                    //    flow = r.flow
                    //})
                    repairFlows.Join(_context.RepairDtls, m => m.repair.DocId, d => d.DocId,
                    (m, d) => new
                    {
                        repair = m.repair,
                        //asset = m.asset,
                        flow = m.flow,
                        repdtl = d
                    })
                    .Join(_context.Departments, j => j.repair.AccDpt, d => d.DptId,
                    (j, d) => new
                    {
                        repair = j.repair,
                        //asset = j.asset,
                        flow = j.flow,
                        repdtl = j.repdtl,
                        dpt = d
                    }).ToList()
                    .ForEach(j => rv.Add(new RepairListVModel
                    {
                        DocType = "請修",
                        RepType = j.repair.RepType,
                        DocId = j.repair.DocId,
                        ApplyDate = j.repair.ApplyDate,
                        //AssetNo = j.repair.AssetNo,
                        //AssetName = j.repair.AssetName,
                        //Brand = j.asset.Brand,
                        PlaceLoc = j.repair.LocType,
                        //Location1 = _context.Buildings.Where(b => b.BuildingId == Convert.ToInt32(j.repair.Building)).FirstOrDefault().BuildingName
                        //            + " " + _context.Floors.Where(f => f.BuildingId == Convert.ToInt32(j.repair.Building) && f.FloorId == j.repair.Floor).FirstOrDefault().FloorName,
                        //Location2 = " " + _context.Places.Where(p => p.BuildingId == Convert.ToInt32(j.repair.Building) && p.FloorId == j.repair.Floor && p.PlaceId == j.repair.Area).FirstOrDefault().PlaceName,
                        //Type = j.asset.Type,
                        ApplyDpt = j.repair.DptId,
                        ApplyDptName = _context.Departments.Find(j.repair.DptId).Name_C,
                        AccDpt = j.repair.AccDpt,
                        AccDptName = j.dpt.Name_C,
                        TroubleDes = j.repair.TroubleDes,
                        DealState = _context.DealStatuses.Find(j.repdtl.DealState).Title,
                        DealDes = j.repdtl.DealDes,
                        Cost = j.repdtl.Cost,
                        Days = DateTime.Now.Subtract(j.repair.ApplyDate).Days,
                        Flg = j.flow.Status,
                        FlowUid = j.flow.UserId,
                        FlowCls = j.flow.Cls,
                        FlowDptId = _context.AppUsers.Find(j.flow.UserId).DptId,
                        repdata = j.repair
                    }));
                    break;
            };

            /* 設備編號"有"、"無"的對應，"有"讀取table相關data，"無"只顯示申請人輸入的設備名稱 */
            foreach(var item in rv)
            {
                //var repairDoc = _context.Repairs.Find(item.DocId);
                //if (repairDoc.AssetNo != null)
                if(!string.IsNullOrEmpty(item.repdata.AssetNo))
                {
                    var asset = _context.Assets.Where(a => a.AssetNo == item.repdata.AssetNo).FirstOrDefault();
                    if(asset != null)
                    {
                        item.AssetNo = asset.AssetNo;
                        item.AssetName = asset.Cname;
                        item.Brand = asset.Brand;
                        item.Type = asset.Type;
                    }
                }
                else
                {
                    item.AssetName = item.repdata.AssetName;
                }
                if (!string.IsNullOrEmpty(item.repdata.Building) && !string.IsNullOrEmpty(item.repdata.Floor)
                    && !string.IsNullOrEmpty(item.repdata.Area))
                {
                    item.Location1 = _context.Buildings.Where(b => b.BuildingId == Convert.ToInt32(item.repdata.Building)).FirstOrDefault().BuildingName
                                    + " "
                                    + _context.Floors.Where(f => f.BuildingId == Convert.ToInt32(item.repdata.Building) && f.FloorId == item.repdata.Floor).FirstOrDefault().FloorName;
                    PlaceModel pm = _context.Places.Where(p => p.BuildingId == Convert.ToInt32(item.repdata.Building) && p.FloorId == item.repdata.Floor && p.PlaceId == item.repdata.Area).FirstOrDefault();
                    if (pm != null)
                        item.Location2 = " " + pm.PlaceName;
                    else
                    {
                        item.Location1 = "(無資料)";
                        item.Location2 = item.repdata.Area + item.PlaceLoc;
                    }
                }
                else
                {
                    item.Location1 = "(無資料)";
                    item.Location2 = item.repdata.Area + item.PlaceLoc;
                }
            }

            /* Sorting search result. */
            if( rv.Count() != 0)
            {
                rv = rv.OrderByDescending(r => r.ApplyDate).ThenByDescending(r => r.DocId).ToList();
            }

            /* Search dealStatus. */
            if (!string.IsNullOrEmpty(qtyDealStatus))
            {
                rv = rv.Where(r => r.DealState == qtyDealStatus).ToList();
            }

            return View("List", rv);
        }

        public ActionResult Edit(string id, int page)
        {
            ViewData["Page"] = page;
            RepairModel repair = _context.Repairs.Find(id);
            if (repair == null)
            {
                return StatusCode(404);
            }

            return View(repair);
        }

        public ActionResult Views(string id)
        {
            RepairModel repair = _context.Repairs.Find(id);
            if (repair == null)
            {
                return StatusCode(404);
            }
            return View(repair);
        }

        [HttpPost]
        public JsonResult GetDptName(string dptId)
        {
            var dpt = _context.Departments.Find(dptId);
            var dptName = "";
            if (dpt == null)
            {
                return Json(dptName);
            }
            dptName = dpt.Name_C;
            return Json(dptName);
        }

        [HttpPost]
        public JsonResult GetDptLoc(string dptId)
        {
            var dptLocations = _context.Places.Where(p => p.PlaceId.Contains(dptId)).ToList();
            if ( dptLocations.Count() == 0 )
            {
                return Json("查無地點");
            }
            else if(dptLocations.Count() == 1 )
            {
                var tempDptLoc = dptLocations.FirstOrDefault();
                var bname = _context.Buildings.Find(tempDptLoc.BuildingId).BuildingName;
                var fname = _context.Floors.Find(tempDptLoc.BuildingId, tempDptLoc.FloorId).FloorName;
                var dptLoc = new
                {
                    BuildingId = tempDptLoc.BuildingId,
                    BuildingName = bname,
                    FloorId = Convert.ToInt32(tempDptLoc.FloorId ),
                    FloorName = fname,
                    PlaceId = Convert.ToInt32(tempDptLoc.PlaceId.Split(new char[] {'-'})[0]),
                    PlaceName = tempDptLoc.PlaceName
                };
                return Json(dptLoc);
            }
            else
            {
                return Json("多個地點");
            }
        }

        [HttpPost]
        public JsonResult GetAssetName(string assetNo)
        {
            var asset = _context.Assets.Where(a => a.AssetNo == assetNo).FirstOrDefault();        
            if (asset == null)
            {
                return Json("查無資料");
            }
            var returnAsset = new
            {
                AssetNo = asset.AssetNo,
                Cname = asset.Cname,
                AccDate = asset.AccDate == null ? "" : asset.AccDate.Value.ToString("yyyy-MM-dd")
            };
            return Json(returnAsset);
        }

        public JsonResult GetAllEngs()
        {
            /* Get all engineers by role. */
            var allEngs = roleManager.GetUsersInRole("RepEngineer").ToList();
            List<AppUserModel> list = new List<AppUserModel>();
            foreach (string l in allEngs)
            {
                var u = _context.AppUsers.Where(a => a.UserName == l).FirstOrDefault();
                if (u != null)
                {
                    list.Add(u);
                }
            }
            list = list.OrderBy(l => l.DptId).ToList();
            return Json(list);
        }

        [HttpPost]
        public JsonResult GetFloorEngId(int buildingId, string floorId)
        {
            var engineer = _context.FloorEngs.Where(f => f.BuildingId == buildingId && f.FloorId == floorId);
            var engId = 0;
            if (engineer.Count() == 0)
            {
                return Json(engId);
            }
            engId = engineer.First().EngId;
            return Json(engId);
        }

        [HttpPost]
        public JsonResult GetAreaEngId(int BuildingId, string FloorId, string PlaceId)
        {
            var engineers = _context.EngsInDepts.Include(e => e.AppUsers).Include(e => e.Departments)
                                                .Where(e => e.BuildingId == BuildingId &&
                                                            e.FloorId == FloorId &&
                                                            e.PlaceId == PlaceId).ToList();

            /* 擷取預設負責工程師 */
            if (engineers.Count() == 0)  //該部門無預設工程師
            {
                var tempEng = _context.AppUsers.Where(a => a.UserName == "181316")
                                               .Select(a => new
                                               {
                                                   EngId = a.Id,
                                                   UserName = a.UserName,
                                                   FullName = a.FullName
                                               }).FirstOrDefault();
                return Json(tempEng);
            }
            else
            {
                var eng = engineers.Join(_context.EngDealingDocs, ed => ed.EngId, e => e.EngId,
                                (ed, e) => new
                                {
                                    ed.EngId,
                                    ed.UserName,
                                    ed.AppUsers.FullName,
                                    e.DealingDocs
                                }).OrderBy(o => o.DealingDocs).FirstOrDefault();
                return Json(eng);
            }
        }
        
        public JsonResult QueryUsers(string QueryStr)
        {
            /* Search user by fullname or username. */
            var users = _context.AppUsers.Where(u => u.FullName.Contains(QueryStr) || 
                                                     u.UserName.Contains(QueryStr)).ToList();
            List<SelectListItem> list = new List<SelectListItem>();
            if(users.Count() != 0)
            {
                users.ForEach(ur => {
                    list.Add(new SelectListItem { Text = ur.FullName + "(" + ur.UserName + ")",
                                                  Value = ur.Id.ToString() });
                });
            }
            return Json(list);
        }

        // GET: Repair/Delete/5
        public ActionResult Delete(string id)
        {
            // Find document.
            RepairModel repair = _context.Repairs.Find(id);
            // Find names to view.
            if (!string.IsNullOrEmpty(repair.Building))
            {
                int buildingId = System.Convert.ToInt32(repair.Building);
                repair.BuildingName = _context.Buildings.Find(buildingId).BuildingName;
                if (!string.IsNullOrEmpty(repair.Floor))
                {
                    repair.FloorName = _context.Floors.Find(buildingId, repair.Floor).FloorName;
                    repair.AreaName = _context.Places.Find(buildingId, repair.Floor, repair.Area).PlaceName;
                }
            }
            //int buildingId = System.Convert.ToInt32(repair.Building);
            repair.DptName = _context.Departments.Find(repair.DptId).Name_C;
            repair.AccDptName = _context.Departments.Find(repair.AccDpt).Name_C;
            //repair.BuildingName = _context.Buildings.Find(buildingId).BuildingName;
            //repair.FloorName = _context.Floors.Find(buildingId, repair.Floor).FloorName;
            //repair.AreaName = _context.Places.Find(buildingId, repair.Floor, repair.Area).PlaceName;
            repair.EngName = _context.AppUsers.Find(repair.EngId).FullName;
            repair.UserAccount = _context.AppUsers.Find(repair.UserId).UserName;

            if (repair == null)
            {
                return StatusCode(404);
            }
            return View(repair);
        }

        // GET: Repairs/PrintRepairDoc/5
        public ActionResult PrintRepairDoc(string DocId, int printType)
        {
            /* Get all print details according to the DocId. */
            RepairModel repair = _context.Repairs.Find(DocId);
            RepairDtlModel dtl = _context.RepairDtls.Find(DocId);
            RepairEmpModel emp = _context.RepairEmps
                                 .Where(ep => ep.DocId == DocId).FirstOrDefault();
            /* Get the last flow. */
            string[] s = new string[] { "?", "2" };
            RepairFlowModel flow = _context.RepairFlows.Where(f => f.DocId == DocId)
                                                       .Where(f => s.Contains(f.Status)).FirstOrDefault();
            RepairPrintVModel vm = new RepairPrintVModel();
            if (repair == null)
            {
                return StatusCode(404);
            }
            else
            {
                vm.Docid = DocId;
                vm.UserId = repair.UserId;
                vm.UserName = repair.UserName;
                vm.UserAccount = _context.AppUsers.Find(repair.UserId).UserName;
                vm.AccDpt = repair.AccDpt;
                vm.ApplyDate = repair.ApplyDate;
                vm.AssetNo = repair.AssetNo;
                vm.AssetNam = repair.AssetName;
                if(repair.AssetNo != null && repair.AssetNo != "")
                {
                    try
                    {
                        vm.AssetAccDate = _context.Assets.Where(a => a.AssetNo == repair.AssetNo).First().AccDate;
                    }
                    catch
                    {
                        vm.AssetAccDate = null;
                    }
                }
                vm.Company = _context.Departments.Find(repair.DptId).Name_C;
                //vm.Amt = repair.Amt;
                vm.Contact = repair.Ext;
                vm.MVPN = repair.Mvpn;
                var place = _context.Places.Where(p => p.BuildingId == Convert.ToInt32(repair.Building) &&
                                                       p.FloorId == repair.Floor && p.PlaceId == repair.Area).FirstOrDefault();
                if(place == null)
                {
                    vm.PlaceLoc = "本單位";
                }
                else
                {
                    vm.PlaceLoc = place.PlaceName;
                }               
                //vm.PlantDoc = repair.PlantDoc;
                vm.RepType = repair.RepType;
                vm.TroubleDes = repair.TroubleDes;
                if (dtl != null)
                {
                    vm.DealDes = dtl.DealDes;
                    vm.EndDate = dtl.EndDate;
                }
                //
                vm.AccDptNam = _context.Departments.Find(repair.AccDpt).Name_C;
                vm.Hour = dtl.Hour;
                vm.InOut = dtl.InOut;
                //vm.EngName = emp == null ? "" : _context.AppUsers.Find(emp.UserId).FullName;
                var lastFlowEng = _context.RepairFlows.Where(rf => rf.DocId == DocId)
                                                      .Where(rf => rf.Cls.Contains("工程師"))
                                                      .OrderByDescending(rf => rf.StepId).FirstOrDefault();
                AppUserModel EngTemp = null;
                if(lastFlowEng != null)
                {
                    EngTemp = _context.AppUsers.Find(lastFlowEng.UserId);
                }                  
                if (EngTemp != null)
                {
                    vm.EngName = EngTemp.FullName + "(" + EngTemp.UserName + ")";
                }
                else
                {
                    vm.EngName = "";
                }
                var engMgr = _context.RepairFlows.Where(r => r.DocId == DocId)
                                                 .Where(r => r.Cls.Contains("工務主管") || r.Cls.Contains("營建主管")).ToList();
                if(engMgr.Count() != 0)
                {
                    engMgr = engMgr.GroupBy(e => e.UserId).Select(group => group.FirstOrDefault()).ToList();
                    foreach (var item in engMgr)
                    {
                        vm.EngMgr += item == null ? "" : _context.AppUsers.Find(item.UserId).FullName + "  ";
                    }
                }

                var engDirector = _context.RepairFlows.Where(r => r.DocId == DocId)
                                                      .Where(r => r.Cls.Contains("工務主任") || r.Cls.Contains("營建主任")).LastOrDefault();
                vm.EngDirector = engDirector == null ? "" : _context.AppUsers.Find(engDirector.UserId).FullName;

                var delivMgr = _context.RepairFlows.Where(r => r.DocId == DocId)
                                                   .Where(r => r.Cls.Contains("單位主管")).ToList();
                if (delivMgr.Count() != 0)
                {
                    delivMgr = delivMgr.GroupBy(e => e.UserId).Select(group => group.FirstOrDefault()).ToList();
                    foreach (var item in delivMgr)
                    {
                        vm.DelivMgr += item == null ? "" : _context.AppUsers.Find(item.UserId).FullName + "  ";
                    }
                }

                var delivDirector = _context.RepairFlows.Where(r => r.DocId == DocId)
                                                        .Where(r => r.Cls.Contains("單位主任")).LastOrDefault();
                vm.DelivDirector = delivDirector == null ? "" : _context.AppUsers.Find(delivDirector.UserId).FullName;

                var ViceSI = _context.RepairFlows.Where(r => r.DocId == DocId)
                                                 .Where(r => r.Cls.Contains("副院長")).LastOrDefault();
                vm.ViceSuperintendent = ViceSI == null ? "" : _context.AppUsers.Find(ViceSI.UserId).FullName;

                if (flow != null)
                {
                    if (flow.Status == "2")
                    {
                        vm.CloseDate = flow.Rtt;
                        AppUserModel u = _context.AppUsers.Find(flow.UserId);
                        if (u != null)
                        {
                            vm.DelivEmp = u.UserName;
                            vm.DelivEmpName = u.FullName;
                        }
                    }
                }
            }
            if( printType != 0 )
            {
                return View("PrintRepairDoc2", vm);
            }
            return View(vm);
        }

        // POST: Repairs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            var ur = _userRepo.Find(u => u.UserName == this.User.Identity.Name).FirstOrDefault();
            RepairFlowModel repflow = _context.RepairFlows.Where(f => f.DocId == id && f.Status == "?")
                                                          .FirstOrDefault();
            repflow.Status = "3";
            repflow.Rtp = ur.Id;
            repflow.Rtt = DateTime.Now;

            _context.SaveChanges();

            return RedirectToAction("Index", "Home");
        }
    }
}
