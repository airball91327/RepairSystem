﻿using System;
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

namespace EDIS.Controllers
{
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

        // GET: /<controller>/
        //Not Used
        public ActionResult Index()
        {
            List<SelectListItem> FlowlistItem = new List<SelectListItem>();
            FlowlistItem.Add(new SelectListItem { Text = "待處理", Value = "待處理" });
            FlowlistItem.Add(new SelectListItem { Text = "已處理", Value = "已處理" });
            FlowlistItem.Add(new SelectListItem { Text = "已結案", Value = "已結案" });
            ViewData["FLOWTYPE"] = new SelectList(FlowlistItem, "Value", "Text");
            //
            List<SelectListItem> listItem = new List<SelectListItem>();
            listItem.Add(new SelectListItem {
                Text = "醫工部",
                Value = "8420"
            });
            ViewData["ACCDPT"] = new SelectList(listItem, "Value", "Text");
            ViewData["APPLYDPT"] = new SelectList(listItem, "Value", "Text");
            return View();
        }

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
                           FlowCls = j.flow.Cls
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
                        FlowCls = j.flow.Cls
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
                            repairFlows = repairFlows.Where(f => f.flow.Status == "?" && f.flow.Cls == "工務工程師").ToList();
                        }
                        else
                        {

                            repairFlows = repairFlows.Where(f => (f.flow.Status == "?" && f.flow.UserId == ur.Id) ||
                                                             (f.flow.Status == "?" && f.flow.Cls == "驗收人" && f.repair.DptId == ur.DptId)).ToList();
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
                        FlowCls = j.flow.Cls
                    }));
                    break;
            };

            /* 設備編號"有"、"無"的對應，"有"讀取table相關data，"無"只顯示申請人輸入的設備名稱 */
            foreach(var item in rv)
            {
                var repairDoc = _context.Repairs.Find(item.DocId);
                if (repairDoc.AssetNo != null)
                {
                    var asset = _context.Assets.Where(a => a.AssetNo == repairDoc.AssetNo).FirstOrDefault();
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
                    item.AssetName = repairDoc.AssetName;
                }
            }

            /* Sorting search result. */
            if( rv.Count() != 0)
            {
                rv = rv.OrderByDescending(r => r.ApplyDate).ThenByDescending(r => r.DocId).ToList();
            }

            return View("List", rv);
        }
        [Authorize]
        public ActionResult Create()
        {
            RepairModel repair = new RepairModel();
            var ur = _userRepo.Find(u => u.UserName == this.User.Identity.Name).FirstOrDefault();
            var dpt = _dptRepo.FindById(ur.DptId);
            repair.DocId = GetID2();
            repair.UserId = ur.Id;
            repair.UserName = ur.FullName;
            repair.UserAccount = ur.UserName;
            repair.DptId = ur.DptId;
            repair.DptName = dpt.Name_C;
            repair.AccDpt = ur.DptId;
            repair.AccDptName = dpt.Name_C;
            repair.ApplyDate = DateTime.Now;
            var bs = new List<SelectListItem> { };
            _buildRepo.Find(null).ToList()
                .ForEach(b =>
                {
                    bs.Add(new SelectListItem { Text = b.BuildingName, Value = b.BuildingId.ToString() });
                });
            repair.Buildings = bs;

            /* 擷取該使用者單位底下所有人員 */
            var dptUsers = _context.AppUsers.Where(a => a.DptId == ur.DptId).ToList();
            List<SelectListItem> dptMemberList = new List<SelectListItem>();
            foreach (var item in dptUsers)
            {
                dptMemberList.Add(new SelectListItem
                {
                    Text = item.FullName,
                    Value = item.Id.ToString()
                });
            }
            ViewData["DptMembers"] = new SelectList(dptMemberList, "Value", "Text");

            /* Get all engineers by role. */
            var allEngs = roleManager.GetUsersInRole("RepEngineer").ToList();
            List<SelectListItem> list = new List<SelectListItem>();
            SelectListItem li = new SelectListItem();
            foreach (string l in allEngs)
            {
                var u = _context.AppUsers.Where(a => a.UserName == l).FirstOrDefault();
                if (u != null)
                {
                    li = new SelectListItem();
                    li.Text = u.FullName;
                    li.Value = u.Id.ToString();
                    list.Add(li);
                }
            }
            ViewData["AllEngs"] = new SelectList(list, "Value", "Text");
            repair.CheckerId = ur.Id;

            return View(repair);
        }

        [HttpPost]
        public IActionResult Create([FromForm]RepairModel repair)
        {
            /* 如有指定工程師，將預設工程師改為指定 */
            if(repair.PrimaryEngId != null && repair.PrimaryEngId != 0)
            {
                repair.EngId = Convert.ToInt32(repair.PrimaryEngId);
            }
            /* 如有代理人，將工程師改為代理人*/
            var subStaff = _context.EngSubStaff.SingleOrDefault(e => e.EngId == repair.EngId);
            if(subStaff != null)
            {
                int startDate = Convert.ToInt32(subStaff.StartDate.ToString("yyyyMMdd"));
                int endDate = Convert.ToInt32(subStaff.EndDate.ToString("yyyyMMdd"));
                int today = Convert.ToInt32(DateTime.UtcNow.AddHours(08).ToString("yyyyMMdd"));
                /* 如在代理期間內，將代理人指定為負責工程師 */
                if(today >= startDate && today <= endDate)
                {
                    repair.EngId = subStaff.SubstituteId;
                }
            }

            /* 請修地點為"本單位" */
            if (repair.LocType == "本單位")
            {
                /* 選擇地點與本單位不同 */
                if(repair.DptId != repair.Area)
                {
                    return BadRequest("需選擇本單位的確切地點!");
                }
            }

            if (repair.RepType != "增設")
            {
                ModelState.Remove("DptMgrId");      //移除DptMgrId的Model驗證
            }

            var ur = _userRepo.Find(u => u.UserName == this.User.Identity.Name).First();
            string msg = "";
            try
            {
                if (ModelState.IsValid)
                {
                    // Create Repair Doc.
                    //repair.DocId = "12345678";
                    repair.ApplyDate = DateTime.Now;
                    _repRepo.Create(repair);
                    //_repRepo.Update(repair);

                    // Create Repair Details.
                    RepairDtlModel dtl = new RepairDtlModel();
                    dtl.DocId = repair.DocId;
                    dtl.DealState = 1;  // 狀態"已處理"
                    _repdtlRepo.Create(dtl);

                    //Create first Repair Flow.
                    RepairFlowModel flow = new RepairFlowModel();
                    flow.DocId = repair.DocId;
                    flow.StepId = 1;
                    flow.UserId = ur.Id;
                    //flow.UserId = userManager.GetCurrentUserId(User);
                    flow.Status = "1";  // 狀態"已處理"
                    flow.Rtp = ur.Id;
                    //flow.Rtp = userManager.GetCurrentUserId(User);
                    flow.Rtt = DateTime.Now;
                    flow.Cls = "申請人";
                    _repflowRepo.Create(flow);

                    // Create next flow.
                    flow = new RepairFlowModel();
                    flow.DocId = repair.DocId;
                    flow.StepId = 2;
                    flow.UserId = repair.EngId;
                    flow.Status = "?";  // 狀態"未處理"
                    flow.Rtt = DateTime.Now;
                    flow.Cls = "工務工程師";
                    // If repair type is "增設", send next flow to department manager.
                    if (repair.RepType == "增設")
                    {
                        flow.UserId = Convert.ToInt32(repair.DptMgrId);
                        flow.Cls = "單位主管";
                    }
                    _repflowRepo.Create(flow);

                    // Add 1 dealing doc to engineer.
                    var eng = _context.EngDealingDocs.Find(repair.EngId);
                    eng.DealingDocs = eng.DealingDocs + 1;
                    _context.Entry(eng).State = EntityState.Modified;
                    _context.SaveChanges();

                    //Send Mail 
                    //To user and the next flow user.
                    Tmail mail = new Tmail();
                    string body = "";
                    var mailToUser = _userRepo.Find(u => u.UserName == this.User.Identity.Name).FirstOrDefault();
                    mail.from = new System.Net.Mail.MailAddress(mailToUser.Email); //u.Email
                    mailToUser = _context.AppUsers.Find(flow.UserId);
                    mail.to = new System.Net.Mail.MailAddress(mailToUser.Email); //u.Email
                    //mail.cc = new System.Net.Mail.MailAddress("344027@cch.org.tw");
                    mail.message.Subject = "工務智能請修系統[請修案]：設備名稱： " + repair.AssetName;
                    body += "<p>表單編號：" + repair.DocId + "</p>";
                    body += "<p>申請日期：" + repair.ApplyDate.ToString("yyyy/MM/dd") + "</p>";
                    body += "<p>申請人：" + repair.UserName + "</p>";
                    body += "<p>財產編號：" + repair.AssetNo + "</p>";
                    body += "<p>設備名稱：" + repair.AssetName + "</p>";
                    body += "<p>故障描述：" + repair.TroubleDes + "</p>";
                    //body += "<p>放置地點：" + repair.PlaceLoc + "</p>";
                    body += "<p><a href='http://dms.cch.org.tw/EDIS/Account/Login'" + "?docId=" + repair.DocId + "&dealType=Edit" + ">處理案件</a></p>";
                    body += "<br/>";
                    body += "<h3>此封信件為系統通知郵件，請勿回覆。</h3>";
                    mail.message.Body = body;
                    mail.message.IsBodyHtml = true;
                    mail.SendMail();

                    return Ok(repair);
                }
                else
                {
                    foreach (var error in ViewData.ModelState.Values.SelectMany(modelState => modelState.Errors))
                    {
                        msg += error.ErrorMessage + Environment.NewLine;
                    }
                }
            }catch(Exception ex)
            {
                msg = ex.Message;
            }

            return BadRequest(msg);
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

        public ActionResult Detail()
        {
            RepairModel repair = new RepairModel();
            repair.Buildings = new List<SelectListItem>
            {
                new SelectListItem{Text = "第一醫療大樓",Value="第一醫療大樓"},
                new SelectListItem{Text = "第二醫療大樓",Value="第二醫療大樓"},
                new SelectListItem{Text = "第三醫療大樓",Value="第三醫療大樓"},
                new SelectListItem{Text = "中華路院區",Value="中華路院區"},
                new SelectListItem{Text = "兒童醫院",Value="兒童醫院"},
                new SelectListItem{Text = "向上大樓",Value="向上大樓"}
            };
            return PartialView(repair);
        }

        public ActionResult List()
        {
            return PartialView();
        }
        public string GetID()
        {
            DocIdStore ds = new DocIdStore();
            ds.DocType = "請修";
            string s = _dsRepo.Find(x => x.DocType == "請修").Select(x => x.DocId).Max();
            string did = "";
            int yymm = (System.DateTime.Now.Year - 1911) * 100 + System.DateTime.Now.Month;
            if (!string.IsNullOrEmpty(s))
            {
                did = s;
            }
            if (did != "")
            {
                if (Convert.ToInt64(did) / 100000 == yymm)
                    did = Convert.ToString(Convert.ToInt64(did) + 1);
                else
                    did = Convert.ToString(yymm * 100000 + 1);
                ds.DocId = did;
                _dsRepo.Update(ds);
            }
            else
            {
                did = Convert.ToString(yymm * 100000 + 1);
                ds.DocId = did;
                _dsRepo.Create(ds);
            }

            return did;
        }

        public string GetID2()
        {
            string did = "";
            try
            {
                DocIdStore ds = new DocIdStore();
                ds.DocType = "請修";
                string s = _dsRepo.Find(x => x.DocType == "請修").Select(x => x.DocId).Max();
                int yymmdd = (System.DateTime.Now.Year - 1911) * 10000 + (System.DateTime.Now.Month) * 100 + System.DateTime.Now.Day;
                if (!string.IsNullOrEmpty(s))
                {
                    did = s;
                }
                if (did != "")
                {
                    if (Convert.ToInt64(did) / 1000 == yymmdd)
                        did = Convert.ToString(Convert.ToInt64(did) + 1);
                    else
                        did = Convert.ToString(yymmdd * 1000 + 1);
                    ds.DocId = did;
                    _dsRepo.Update(ds);
                }
                else
                {
                    did = Convert.ToString(yymmdd * 1000 + 1);
                    ds.DocId = did;
                    _dsRepo.Create(ds);
                }
            }
            catch (Exception e)
            {
                RedirectToAction("Create", "Repair", new { Area = "" });
            }
            return did;
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
            var dptLocations = _context.Places.Where(p => p.PlaceId == dptId).ToList();
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
                    PlaceId = Convert.ToInt32(tempDptLoc.PlaceId ),
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
                var tempEng = _context.AppUsers.Where(a => a.UserName == "149761").FirstOrDefault();
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
                vm.EngName = emp == null ? "" : _context.AppUsers.Find(repair.EngId).FullName;
                var engMgr = _context.RepairFlows.Where(r => r.DocId == DocId)
                                                 .Where(r => r.Cls.Contains("工務主管")).LastOrDefault();
                vm.EngMgr = engMgr == null ? "" : _context.AppUsers.Find(engMgr.UserId).FullName;
                var engDirector = _context.RepairFlows.Where(r => r.DocId == DocId)
                                                      .Where(r => r.Cls.Contains("工務主任")).LastOrDefault();
                vm.EngDirector = engDirector == null ? "" : _context.AppUsers.Find(engDirector.UserId).FullName;
                var delivMgr = _context.RepairFlows.Where(r => r.DocId == DocId)
                                                   .Where(r => r.Cls.Contains("單位主任")).LastOrDefault();
                vm.DelivMgr = delivMgr == null ? "" : _context.AppUsers.Find(delivMgr.UserId).FullName;
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
