using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EDIS.Data;
using EDIS.Models.RepairModels;
using EDIS.Models.Identity;
using X.PagedList;
using ClosedXML.Excel;
using System.IO;
using EDIS.Models.KeepModels;
using Microsoft.AspNetCore.Authorization;

namespace EDIS.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CustomUserManager userManager;
        private readonly CustomRoleManager roleManager;
        private int pageSize = 50;

        public SearchController(ApplicationDbContext context,
                                CustomUserManager customUserManager,
                                CustomRoleManager customRoleManager)
        {
            _context = context;
            userManager = customUserManager;
            roleManager = customRoleManager;
        }

        // GET: Admin/Search
        public async Task<IActionResult> Index()
        {
            return View(await _context.Repairs.ToListAsync());
        }

        // GET: Admin/Search/RepIndex
        public IActionResult RepIndex()
        {
            List<SelectListItem> FlowlistItem = new List<SelectListItem>();
            FlowlistItem.Add(new SelectListItem { Text = "未結案", Value = "未結案" });
            FlowlistItem.Add(new SelectListItem { Text = "已結案", Value = "已結案" });
            ViewData["FLOWTYPE"] = new SelectList(FlowlistItem, "Value", "Text");

            /* Get all engineers. */
            var s = roleManager.GetUsersInRole("RepEngineer").ToList();
            List<SelectListItem> listItem1 = new List<SelectListItem>();
            SelectListItem li;
            AppUserModel u;
            foreach (string l in s)
            {
                u = _context.AppUsers.Where(ur => ur.UserName == l).FirstOrDefault();
                li = new SelectListItem();
                li.Text = u.FullName + "(" + u.UserName + ")";
                li.Value = u.Id.ToString();
                listItem1.Add(li);
            }
            ViewData["Engineers"] = new SelectList(listItem1, "Value", "Text");

            /* 處理狀態的下拉選單 */
            var dealStatuses = _context.DealStatuses.ToList();
            List<SelectListItem> listItem2 = new List<SelectListItem>();
            foreach (var item in dealStatuses)
            {
                listItem2.Add(new SelectListItem
                {
                    Text = item.Title,
                    Value = item.Id.ToString()
                });
            }
            ViewData["DealStatus"] = new SelectList(listItem2, "Value", "Text");

            return View();
        }

        // POST: Admin/Search/RepIndex
        [HttpPost]
        public ActionResult RepIndex(QryRepListData qdata, int page = 1)
        {
            string ftype = qdata.qtyFLOWTYPE;
            string qtyDealStatus = qdata.qtyDealStatus;
            string qryEngId = qdata.qtyEngId;

            List<RepairSearchListVModel> rv = new List<RepairSearchListVModel>();
            /* Querying data. */
            var rps = _context.Repairs.AsQueryable();
            var repairFlows = _context.RepairFlows.AsQueryable();
            var repairDtls = _context.RepairDtls.AsQueryable();
            if (!string.IsNullOrEmpty(ftype))   //流程狀態
            {
                switch (ftype)
                {
                    case "未結案":
                        repairFlows = repairFlows.Where(f => f.Status == "?");
                        break;
                    case "已結案":
                        repairFlows = repairFlows.Where(f => f.Status == "2");
                        break;
                }
            }
            else
            {
                repairFlows = repairFlows.Where(f => f.Status == "2" || f.Status == "?");
            }
            if (!string.IsNullOrEmpty(qtyDealStatus))   //處理狀態
            {
                repairDtls = repairDtls.Where(r => r.DealState == Convert.ToInt32(qtyDealStatus));
            }
            if (!string.IsNullOrEmpty(qryEngId))   //負責工程師
            {
                rps = rps.Where(r => r.EngId == Convert.ToInt32(qryEngId));
            }

            /* If no search result. */
            if (rps.Count() == 0)
            {
                return PartialView("RepList", rv.ToPagedList(1, pageSize));
            }

            rps.Join(repairFlows, r => r.DocId, f => f.DocId,
                (r, f) => new
                {
                    repair = r,
                    flow = f
                })
                .Join(repairDtls, m => m.repair.DocId, d => d.DocId,
                (m, d) => new
                {
                    repair = m.repair,
                    flow = m.flow,
                    repdtl = d
                })
                .Join(_context.Departments, j => j.repair.AccDpt, d => d.DptId,
                (j, d) => new
                {
                    repair = j.repair,
                    flow = j.flow,
                    repdtl = j.repdtl,
                    dpt = d
                })
                .ToList()
                .ForEach(j => rv.Add(new RepairSearchListVModel
                {
                    DocType = "請修",
                    RepType = j.repair.RepType,
                    DocId = j.repair.DocId,
                    ApplyDate = j.repair.ApplyDate,
                    PlaceLoc = j.repair.LocType,
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
                    FlowUidName = _context.AppUsers.Find(j.flow.UserId).FullName,
                    EndDate = j.repdtl.EndDate,
                    CloseDate = j.repdtl.CloseDate,
                    repdata = j.repair
                }));

            /* 設備編號"有"、"無"的對應，"有"讀取table相關data，"無"只顯示申請人輸入的設備名稱 */
            foreach (var item in rv)
            {
                var repairDoc = _context.Repairs.Find(item.DocId);
                if (repairDoc.AssetNo != null)
                {
                    var asset = _context.Assets.Where(a => a.AssetNo == repairDoc.AssetNo).FirstOrDefault();
                    if (asset != null)
                    {
                        item.AssetNo = asset.AssetNo;
                        item.AssetName = asset.Cname;
                        item.Brand = asset.Brand;
                        item.Type = asset.Type;
                    }
                }
                else
                {
                    item.AssetName = repairDoc.AssetName;
                }
            }

            if (rv.ToPagedList(page, pageSize).Count <= 0)
                return PartialView("RepList", rv.ToPagedList(1, pageSize));

            return PartialView("RepList", rv.ToPagedList(page, pageSize));
        }

        // GET: Admin/Search/KeepIndex
        public IActionResult KeepIndex()
        {
            List<SelectListItem> FlowlistItem = new List<SelectListItem>();
            FlowlistItem.Add(new SelectListItem { Text = "未結案", Value = "未結案" });
            FlowlistItem.Add(new SelectListItem { Text = "已結案", Value = "已結案" });
            ViewData["FLOWTYPE"] = new SelectList(FlowlistItem, "Value", "Text");

            /* Get all engineers. */
            var s = roleManager.GetUsersInRole("RepEngineer").ToList();
            List<SelectListItem> listItem1 = new List<SelectListItem>();
            SelectListItem li;
            AppUserModel u;
            foreach (string l in s)
            {
                u = _context.AppUsers.Where(ur => ur.UserName == l).FirstOrDefault();
                li = new SelectListItem();
                li.Text = u.FullName + "(" + u.UserName + ")";
                li.Value = u.Id.ToString();
                listItem1.Add(li);
            }
            ViewData["Engineers"] = new SelectList(listItem1, "Value", "Text");

            /* 處理保養狀態的下拉選單 */
            var keepResults = _context.KeepResults.ToList();
            List<SelectListItem> listItem2 = new List<SelectListItem>();
            foreach (var item in keepResults)
            {
                listItem2.Add(new SelectListItem
                {
                    Text = item.Title,
                    Value = item.Id.ToString()
                });
            }
            ViewData["KeepResult"] = new SelectList(listItem2, "Value", "Text");

            return View();
        }

        // POST: Admin/Search/KeepIndex
        [HttpPost]
        public IActionResult KeepIndex(QryKeepListData qdata, int page = 1)
        {
            string ftype = qdata.KqtyFLOWTYPE;
            string qtyKeepResult = qdata.KqtyKeepResult;
            string qryEngId = qdata.KqtyEngCode;

            List<KeepSearchListViewModel> kv = new List<KeepSearchListViewModel>();
            /* Querying data. */
            var kps = _context.Keeps.AsQueryable();
            var keepFlows = _context.KeepFlows.AsQueryable();
            var keepDtls = _context.KeepDtls.AsQueryable();

            if (!string.IsNullOrEmpty(qryEngId))
            {
                kps = kps.Where(r => r.EngId == Convert.ToInt32(qryEngId));
            }
            if (!string.IsNullOrEmpty(ftype))   //流程狀態
            {
                switch (ftype)
                {
                    case "未結案":
                        keepFlows = keepFlows.Where(kf => kf.Status == "?");
                        break;
                    case "已結案":
                        keepFlows = keepFlows.Where(kf => kf.Status == "2");
                        break;
                }
            }
            else
            {
                keepFlows = keepFlows.Where(kf => kf.Status== "?" || kf.Status == "2");
            }

            /* If no search result. */
            if (kps.Count() == 0)
            {
                return PartialView("KeepList", kv.ToPagedList(1, pageSize));
            }
            kps.Join(keepFlows, r => r.DocId, f => f.DocId,
                (k, f) => new
                {
                    keep = k,
                    flow = f
                })
                .Join(keepDtls, m => m.keep.DocId, d => d.DocId,
                (m, d) => new
                {
                    keep = m.keep,
                    flow = m.flow,
                    keepdtl = d
                })
                .Join(_context.Departments, j => j.keep.AccDpt, d => d.DptId,
                (j, d) => new
                {
                    keep = j.keep,
                    flow = j.flow,
                    keepdtl = j.keepdtl,
                    dpt = d
                })
                .ToList()
                .ForEach(j => kv.Add(new KeepSearchListViewModel
                {
                    DocType = "保養",
                    DocId = j.keep.DocId,
                    PlaceLoc = j.keep.PlaceLoc,
                    ApplyDpt = j.keep.DptId,
                    AccDpt = j.keep.AccDpt,
                    AccDptName = j.dpt.Name_C,
                    Result = (j.keepdtl.Result == null || j.keepdtl.Result == 0) ? "" : _context.KeepResults.Find(j.keepdtl.Result).Title,
                    InOut = j.keepdtl.InOut == "0" ? "自行" :
                            j.keepdtl.InOut == "1" ? "委外" :
                            j.keepdtl.InOut == "2" ? "租賃" :
                            j.keepdtl.InOut == "3" ? "保固" : "",
                    Memo = j.keepdtl.Memo,
                    Cost = j.keepdtl.Cost,
                    Days = DateTime.Now.Subtract(j.keep.SentDate.GetValueOrDefault()).Days,
                    Flg = j.flow.Status,
                    FlowUid = j.flow.UserId,
                    FlowCls = j.flow.Cls,
                    FlowUidName = _context.AppUsers.Find(j.flow.UserId).FullName,
                    Src = j.keep.Src,
                    SentDate = j.keep.SentDate,
                    EndDate = j.keepdtl.EndDate,
                    CloseDate = j.keepdtl.CloseDate.HasValue == true ? j.keepdtl.CloseDate.Value.Date : j.keepdtl.CloseDate,
                    IsCharged = j.keepdtl.IsCharged,
                    keepdata = j.keep
                }));

            /* 設備編號"有"、"無"的對應，"有"讀取table相關data，"無"只顯示申請人輸入的設備名稱 */
            foreach (var item in kv)
            {
                if (!string.IsNullOrEmpty(item.keepdata.AssetNo))
                {
                    var asset = _context.Assets.Where(a => a.AssetNo == item.keepdata.AssetNo).FirstOrDefault();
                    if (asset != null)
                    {
                        item.AssetNo = asset.AssetNo;
                        item.AssetName = asset.Cname;
                        item.Brand = asset.Brand;
                        item.Type = asset.Type;
                    }
                }
                else
                {
                    item.AssetName = item.keepdata.AssetName;
                }
            }

            /* Search KeepResults. */
            if (!string.IsNullOrEmpty(qtyKeepResult))
            {
                kv = kv.Where(r => r.Result == _context.KeepResults.Find(Convert.ToInt32(qtyKeepResult)).Title).ToList();
            }

            if (kv.ToPagedList(page, pageSize).Count <= 0)
                return PartialView("KeepList", kv.ToPagedList(1, pageSize));

            return PartialView("KeepList", kv.ToPagedList(page, pageSize));
        }

        // GET: Admin/Search/RepToExcel
        public ActionResult RepToExcel(QryRepListData qdata)
        {
            string ftype = qdata.qtyFLOWTYPE;
            string qtyDealStatus = qdata.qtyDealStatus;
            string qryEngId = qdata.qtyEngId;

            List<RepairSearchListVModel> rv = new List<RepairSearchListVModel>();
            /* Querying data. */
            var rps = _context.Repairs.AsQueryable();
            var repairFlows = _context.RepairFlows.AsQueryable();
            var repairDtls = _context.RepairDtls.AsQueryable();
            if (!string.IsNullOrEmpty(ftype))   //流程狀態
            {
                switch (ftype)
                {
                    case "未結案":
                        repairFlows = repairFlows.Where(f => f.Status == "?");
                        break;
                    case "已結案":
                        repairFlows = repairFlows.Where(f => f.Status == "2");
                        break;
                }
            }
            else
            {
                repairFlows = repairFlows.Where(f => f.Status == "2" || f.Status == "?");
            }
            if (!string.IsNullOrEmpty(qtyDealStatus))   //處理狀態
            {
                repairDtls = repairDtls.Where(r => r.DealState == Convert.ToInt32(qtyDealStatus));
            }
            if (!string.IsNullOrEmpty(qryEngId))   //負責工程師
            {
                rps = rps.Where(r => r.EngId == Convert.ToInt32(qryEngId));
            }

            rps.Join(repairFlows, r => r.DocId, f => f.DocId,
                (r, f) => new
                {
                    repair = r,
                    flow = f
                })
                .Join(repairDtls, m => m.repair.DocId, d => d.DocId,
                (m, d) => new
                {
                    repair = m.repair,
                    flow = m.flow,
                    repdtl = d
                })
                .Join(_context.Departments, j => j.repair.AccDpt, d => d.DptId,
                (j, d) => new
                {
                    repair = j.repair,
                    flow = j.flow,
                    repdtl = j.repdtl,
                    dpt = d
                })
                .ToList()
                .ForEach(j => rv.Add(new RepairSearchListVModel
                {
                    DocType = "請修",
                    RepType = j.repair.RepType,
                    DocId = j.repair.DocId,
                    ApplyDate = j.repair.ApplyDate,
                    PlaceLoc = j.repair.LocType,
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
                    FlowUidName = _context.AppUsers.Find(j.flow.UserId).FullName,
                    EndDate = j.repdtl.EndDate,
                    CloseDate = j.repdtl.CloseDate,
                    repdata = j.repair
                }));

            /* 設備編號"有"、"無"的對應，"有"讀取table相關data，"無"只顯示申請人輸入的設備名稱 */
            foreach (var item in rv)
            {
                var repairDoc = _context.Repairs.Find(item.DocId);
                if (repairDoc.AssetNo != null)
                {
                    var asset = _context.Assets.Where(a => a.AssetNo == repairDoc.AssetNo).FirstOrDefault();
                    if (asset != null)
                    {
                        item.AssetNo = asset.AssetNo;
                        item.AssetName = asset.Cname;
                        item.Brand = asset.Brand;
                        item.Type = asset.Type;
                    }
                }
                else
                {
                    item.AssetName = repairDoc.AssetName;
                }
            }

            //ClosedXML的用法 先new一個Excel Workbook
            using (XLWorkbook workbook = new XLWorkbook())
            {
                //取得要塞入Excel內的資料
                var data = rv.Select(c => new {
                    c.RepType,
                    c.DocId,
                    c.ApplyDate,
                    AccDpt = c.AccDptName + "(" + c.AccDpt + ")",
                    Asset = c.AssetName + "(" + c.AssetNo + ")",
                    c.PlaceLoc,
                    c.TroubleDes,
                    c.DealDes,
                    c.DealState,
                    c.EndDate,
                    c.CloseDate,
                    c.Cost,
                    c.Days,
                    c.FlowCls,
                    c.FlowUidName
                });

                //一個workbook內至少會有一個worksheet,並將資料Insert至這個位於A1這個位置上
                var ws = workbook.Worksheets.Add("sheet1", 1);

                //Title
                ws.Cell(1, 1).Value = "請修類別";
                ws.Cell(1, 2).Value = "表單編號";
                ws.Cell(1, 3).Value = "申請日期";
                ws.Cell(1, 4).Value = "成本中心";
                ws.Cell(1, 5).Value = "物品名稱(財產編號)";
                ws.Cell(1, 6).Value = "請修地點";
                ws.Cell(1, 7).Value = "故障描述";
                ws.Cell(1, 8).Value = "處理描述";
                ws.Cell(1, 9).Value = "處理狀態";
                ws.Cell(1, 10).Value = "完工日期";
                ws.Cell(1, 11).Value = "結案日期";
                ws.Cell(1, 12).Value = "費用";
                ws.Cell(1, 13).Value = "天數";
                ws.Cell(1, 14).Value = "關卡";
                ws.Cell(1, 15).Value = "關卡人員";

                //如果是要塞入Query後的資料該資料一定要變成是data.AsEnumerable()
                ws.Cell(2, 1).InsertData(data);

                //因為是用Query的方式,這個地方要用串流的方式來存檔
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    workbook.SaveAs(memoryStream);
                    //請注意 一定要加入這行,不然Excel會是空檔
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    //注意Excel的ContentType,是要用這個"application/vnd.ms-excel"
                    string fileName = "工程師案件搜尋(請修)_" + DateTime.Now.ToString("yyyy-MM-dd") + ".xlsx";
                    return this.File(memoryStream.ToArray(), "application/vnd.ms-excel", fileName);
                }
            }

        }

        // GET: Admin/Search/KeepToExcel
        public ActionResult KeepToExcel(QryKeepListData qdata)
        {
            string ftype = qdata.KqtyFLOWTYPE;
            string qtyKeepResult = qdata.KqtyKeepResult;
            string qryEngId = qdata.KqtyEngCode;

            List<KeepSearchListViewModel> kv = new List<KeepSearchListViewModel>();
            /* Querying data. */
            var kps = _context.Keeps.AsQueryable();
            var keepFlows = _context.KeepFlows.AsQueryable();
            var keepDtls = _context.KeepDtls.AsQueryable();

            if (!string.IsNullOrEmpty(qryEngId))
            {
                kps = kps.Where(r => r.EngId == Convert.ToInt32(qryEngId));
            }
            if (!string.IsNullOrEmpty(ftype))   //流程狀態
            {
                switch (ftype)
                {
                    case "未結案":
                        keepFlows = keepFlows.Where(kf => kf.Status == "?");
                        break;
                    case "已結案":
                        keepFlows = keepFlows.Where(kf => kf.Status == "2");
                        break;
                }
            }
            else
            {
                keepFlows = keepFlows.Where(kf => kf.Status == "?" || kf.Status == "2");
            }

            /* If no search result. */
            if (kps.Count() == 0)
            {
                return PartialView("KeepList", kv.ToPagedList(1, pageSize));
            }
            kps.Join(keepFlows, r => r.DocId, f => f.DocId,
                (k, f) => new
                {
                    keep = k,
                    flow = f
                })
                .Join(keepDtls, m => m.keep.DocId, d => d.DocId,
                (m, d) => new
                {
                    keep = m.keep,
                    flow = m.flow,
                    keepdtl = d
                })
                .Join(_context.Departments, j => j.keep.AccDpt, d => d.DptId,
                (j, d) => new
                {
                    keep = j.keep,
                    flow = j.flow,
                    keepdtl = j.keepdtl,
                    dpt = d
                })
                .ToList()
                .ForEach(j => kv.Add(new KeepSearchListViewModel
                {
                    DocType = "保養",
                    DocId = j.keep.DocId,
                    PlaceLoc = j.keep.PlaceLoc,
                    ApplyDpt = j.keep.DptId,
                    AccDpt = j.keep.AccDpt,
                    AccDptName = j.dpt.Name_C,
                    Result = (j.keepdtl.Result == null || j.keepdtl.Result == 0) ? "" : _context.KeepResults.Find(j.keepdtl.Result).Title,
                    InOut = j.keepdtl.InOut == "0" ? "自行" :
                            j.keepdtl.InOut == "1" ? "委外" :
                            j.keepdtl.InOut == "2" ? "租賃" :
                            j.keepdtl.InOut == "3" ? "保固" : "",
                    Memo = j.keepdtl.Memo,
                    Cost = j.keepdtl.Cost,
                    Days = DateTime.Now.Subtract(j.keep.SentDate.GetValueOrDefault()).Days,
                    Flg = j.flow.Status,
                    FlowUid = j.flow.UserId,
                    FlowCls = j.flow.Cls,
                    FlowUidName = _context.AppUsers.Find(j.flow.UserId).FullName,
                    Src = j.keep.Src,
                    SentDate = j.keep.SentDate,
                    EndDate = j.keepdtl.EndDate,
                    CloseDate = j.keepdtl.CloseDate.HasValue == true ? j.keepdtl.CloseDate.Value.Date : j.keepdtl.CloseDate,
                    IsCharged = j.keepdtl.IsCharged,
                    keepdata = j.keep
                }));

            /* 設備編號"有"、"無"的對應，"有"讀取table相關data，"無"只顯示申請人輸入的設備名稱 */
            foreach (var item in kv)
            {
                if (!string.IsNullOrEmpty(item.keepdata.AssetNo))
                {
                    var asset = _context.Assets.Where(a => a.AssetNo == item.keepdata.AssetNo).FirstOrDefault();
                    if (asset != null)
                    {
                        item.AssetNo = asset.AssetNo;
                        item.AssetName = asset.Cname;
                        item.Brand = asset.Brand;
                        item.Type = asset.Type;
                    }
                }
                else
                {
                    item.AssetName = item.keepdata.AssetName;
                }
            }

            /* Search KeepResults. */
            if (!string.IsNullOrEmpty(qtyKeepResult))
            {
                kv = kv.Where(r => r.Result == _context.KeepResults.Find(Convert.ToInt32(qtyKeepResult)).Title).ToList();
            }

            //ClosedXML的用法 先new一個Excel Workbook
            using (XLWorkbook workbook = new XLWorkbook())
            {
                //取得要塞入Excel內的資料
                var data = kv.Select(c => new {
                    c.DocId,
                    c.SentDate,
                    AccDpt = c.AccDptName + "(" + c.AccDpt + ")",
                    Asset = c.AssetName + "(" + c.AssetNo + ")",
                    c.PlaceLoc,
                    c.Result,
                    c.InOut,
                    c.Memo,
                    c.EndDate,
                    c.CloseDate,
                    c.Cost,
                    c.Days,
                    c.FlowCls,
                    c.FlowUidName
                });

                //一個workbook內至少會有一個worksheet,並將資料Insert至這個位於A1這個位置上
                var ws = workbook.Worksheets.Add("sheet1", 1);

                //Title
                ws.Cell(1, 1).Value = "表單編號";
                ws.Cell(1, 2).Value = "申請日期";
                ws.Cell(1, 3).Value = "成本中心";
                ws.Cell(1, 4).Value = "物品名稱(財產編號)";
                ws.Cell(1, 5).Value = "放置地點";
                ws.Cell(1, 6).Value = "保養狀態";
                ws.Cell(1, 7).Value = "保養方式";
                ws.Cell(1, 8).Value = "保養描述";
                ws.Cell(1, 9).Value = "完工日期";
                ws.Cell(1, 10).Value = "結案日期";
                ws.Cell(1, 11).Value = "費用";
                ws.Cell(1, 12).Value = "天數";
                ws.Cell(1, 13).Value = "關卡";
                ws.Cell(1, 14).Value = "關卡人員";

                //如果是要塞入Query後的資料該資料一定要變成是data.AsEnumerable()
                ws.Cell(2, 1).InsertData(data);

                //因為是用Query的方式,這個地方要用串流的方式來存檔
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    workbook.SaveAs(memoryStream);
                    //請注意 一定要加入這行,不然Excel會是空檔
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    //注意Excel的ContentType,是要用這個"application/vnd.ms-excel"
                    string fileName = "工程師案件搜尋(保養)_" + DateTime.Now.ToString("yyyy-MM-dd") + ".xlsx";
                    return this.File(memoryStream.ToArray(), "application/vnd.ms-excel", fileName);
                }
            }

        }

        private bool RepairModelExists(string id)
        {
            return _context.Repairs.Any(e => e.DocId == id);
        }
    }
}
