using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
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

namespace EDIS.Controllers
{
    [Authorize]
    public class RepairReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CustomUserManager userManager;
        private readonly CustomRoleManager roleManager;

        public RepairReportController(ApplicationDbContext context,
                                      CustomUserManager customUserManager,
                                      CustomRoleManager customRoleManager)
        {
            _context = context;
            userManager = customUserManager;
            roleManager = customRoleManager;
        }

        // GET: /RepairReport/Index
        public ActionResult Index()
        {
            return View();
        }

        public IActionResult ExportToExcel(DateTime qtyMonth)
        {
            DateTime reportMonth = qtyMonth;

            var repairFlows = _context.RepairFlows.OrderBy(rf => rf.StepId).GroupBy(rf => rf.DocId)
                                                  .Select(g => g.LastOrDefault()).ToList(); //剔除廢除的單
            repairFlows = repairFlows.Where(rf => rf.Status != "3").ToList();
            var qtyRepairs = _context.Repairs.Where(r => r.ApplyDate.Month == reportMonth.Month)
                                             .Join(_context.RepairDtls, r => r.DocId, d => d.DocId,
                                             (r, d) => new
                                             {
                                                 repair = r,
                                                 repdtl = d
                                             })
                                             .Join(repairFlows, r => r.repair.DocId, rf => rf.DocId,
                                             (r, rf) => new
                                             {
                                                 repair = r.repair,
                                                 repdtl = r.repdtl,
                                                 repflow = rf
                                             });

            //找出各案件的負責工程師(流程最後一位工程師)
            foreach(var item in qtyRepairs)
            {
                var tempEng = _context.RepairFlows.Where(rf => rf.DocId == item.repair.DocId)
                                                  .Where(rf => rf.Cls.Contains("工程師"))
                                                  .OrderByDescending(rf => rf.StepId).FirstOrDefault();
                if (tempEng != null)
                {
                    item.repair.EngId = tempEng.UserId;
                }
            }

            // 增設、內修、外修、內外修、報廢件數(月為單位)
            var repAdds = qtyRepairs.Where(r => r.repair.RepType == "增設");
            var repIns = qtyRepairs.Where(r => r.repair.RepType != "增設" && r.repdtl.InOut == "內修" && r.repdtl.DealState != 4);
            var repOuts = qtyRepairs.Where(r => r.repair.RepType != "增設" && r.repdtl.InOut == "外修" && r.repdtl.DealState != 4);
            var repInOuts = qtyRepairs.Where(r => r.repair.RepType != "增設" && r.repdtl.InOut == "內外修" && r.repdtl.DealState != 4);
            var repScraps = qtyRepairs.Where(r => r.repair.RepType != "增設" && r.repdtl.DealState == 4);

            //ClosedXML的用法 先new一個Excel Workbook
            using (XLWorkbook workbook = new XLWorkbook())
            {
                //取得要塞入Excel內的資料
                //var data = qtyRepairs.Select(c => new {
                //    count1 = qtyRepairs.Where(r => r.repair.RepType == "增設").Count(),
                //    count2 = qtyRepairs.Where(r => r.repdtl.InOut == "內修").Count(),
                //    conut3 = qtyRepairs.Where(r => r.repdtl.InOut == "外修").Count(),
                //    conut4 = qtyRepairs.Where(r => r.repdtl.InOut == "內外修").Count(),
                //});

                //一個workbook內至少會有一個worksheet,並將資料Insert至這個位於A1這個位置上
                //WorkSheet1
                var ws = workbook.Worksheets.Add("綜合月指標", 1);

                //Title1  【每月維修件數(申請日為該月的案件)】
                ws.Cell(1, 1).Value = "【" + qtyMonth.Year + "年" + qtyMonth.Month + "月維修件數】";
                ws.Cell(2, 1).Value = "增設";
                ws.Cell(2, 2).Value = "維修(內修)";
                ws.Cell(2, 3).Value = "維修(外修)";
                ws.Cell(2, 4).Value = "維修(內外修)";
                ws.Cell(2, 5).Value = "報廢";
                ws.Cell(2, 6).Value = "維修尚未處理";
                ws.Cell(2, 7).Value = "總件數";

                //Data1
                ws.Cell(3, 1).Value = repAdds.Count();
                ws.Cell(3, 2).Value = repIns.Count();
                ws.Cell(3, 3).Value = repOuts.Count();
                ws.Cell(3, 4).Value = repInOuts.Count();
                ws.Cell(3, 5).Value = repScraps.Count();
                ws.Cell(3, 6).Value = qtyRepairs.Where(r => r.repair.RepType != "增設" && r.repdtl.InOut == null).Count();
                ws.Cell(3, 7).Value = qtyRepairs.Count();

                //Title2    【維修完成、結案率 (該月申請且已完成或已結案案件 / 該月申請總件數)】
                ws.Cell(5, 1).Value = "【維修完成率】";
                ws.Cell(5, 5).Value = "【維修結案率】";
                ws.Cell(6, 1).Value = "增設";
                ws.Cell(6, 2).Value = "維修(內修)";
                ws.Cell(6, 3).Value = "維修(外修)";
                ws.Cell(6, 4).Value = "維修(內外修)";
                ws.Cell(6, 5).Value = "增設";
                ws.Cell(6, 6).Value = "維修(內修)";
                ws.Cell(6, 7).Value = "維修(外修)";
                ws.Cell(6, 8).Value = "維修(內外修)";

                //Data2         //.ToString("P")轉為百分比顯示的字串
                ws.Cell(7, 1).Value = repAdds.Count() != 0 ? (Convert.ToDecimal(repAdds.Where(r => r.repdtl.EndDate != null).Count()) / Convert.ToDecimal(repAdds.Count())).ToString("P") : "0.00%";
                ws.Cell(7, 2).Value = repIns.Count() != 0 ? (Convert.ToDecimal(repIns.Where(r => r.repdtl.EndDate != null).Count()) / Convert.ToDecimal(repIns.Count())).ToString("P") : "0.00%";
                ws.Cell(7, 3).Value = repOuts.Count() != 0 ? (Convert.ToDecimal(repOuts.Where(r => r.repdtl.EndDate != null).Count()) / Convert.ToDecimal(repOuts.Count())).ToString("P") : "0.00%";
                ws.Cell(7, 4).Value = repInOuts.Count() != 0 ? (Convert.ToDecimal(repInOuts.Where(r => r.repdtl.EndDate != null).Count()) / Convert.ToDecimal(repInOuts.Count())).ToString("P") : "0.00%";
                ws.Cell(7, 5).Value = repAdds.Count() != 0 ? (Convert.ToDecimal(repAdds.Where(r => r.repflow.Status == "2").Count()) / Convert.ToDecimal(repAdds.Count())).ToString("P") : "0.00%";
                ws.Cell(7, 6).Value = repIns.Count() != 0 ? (Convert.ToDecimal(repIns.Where(r => r.repflow.Status == "2").Count()) / Convert.ToDecimal(repIns.Count())).ToString("P") : "0.00%";
                ws.Cell(7, 7).Value = repOuts.Count() != 0 ? (Convert.ToDecimal(repOuts.Where(r => r.repflow.Status == "2").Count()) / Convert.ToDecimal(repOuts.Count())).ToString("P") : "0.00%";
                ws.Cell(7, 8).Value = repInOuts.Count() != 0 ? (Convert.ToDecimal(repInOuts.Where(r => r.repflow.Status == "2").Count()) / Convert.ToDecimal(repInOuts.Count())).ToString("P") : "0.00%";

                //Title3    【維修且內修完成率(該月申請的內修且已完成的案件)】
                ws.Cell(9, 1).Value = "【維修且內修案件】";
                ws.Cell(10, 1).Value = "3日完成率";
                ws.Cell(10, 2).Value = "4~7日完成率";
                ws.Cell(10, 3).Value = "8日以上";

                var hasEndDateRepIns = repIns.Where(r => r.repdtl.EndDate != null);
                decimal count1 = 0, count2 = 0, count3 = 0;
                if(hasEndDateRepIns.Count() != 0)
                {
                    foreach (var item in hasEndDateRepIns)
                    {
                        //計算時間差(天為單位)
                        var result = new TimeSpan(item.repdtl.EndDate.Value.Ticks - item.repair.ApplyDate.Ticks).Days;
                        if (result <= 3)
                        {
                            count1++;
                        }
                        else if (result > 3 && result < 8)
                        {
                            count2++;
                        }
                        else
                        {
                            count3++;
                        }
                    }
                }

                //Data3
                ws.Cell(11, 1).Value = repIns.Count() != 0 ? (count1 / Convert.ToDecimal(repIns.Count())).ToString("P") : "0.00%";
                ws.Cell(11, 2).Value = repIns.Count() != 0 ? (count2 / Convert.ToDecimal(repIns.Count())).ToString("P") : "0.00%";
                ws.Cell(11, 3).Value = repIns.Count() != 0 ? (count3 / Convert.ToDecimal(repIns.Count())).ToString("P") : "0.00%";

                //Title4    【增設案件】
                ws.Cell(13, 1).Value = "【增設案件完成率】";
                ws.Cell(14, 1).Value = "15日內";
                ws.Cell(14, 2).Value = "16~30日";
                ws.Cell(14, 3).Value = "31日以上";

                var hasEndDateRepAdds = repAdds.Where(r => r.repdtl.EndDate != null);
                count1 = 0; count2 = 0; count3 = 0;
                if (hasEndDateRepAdds.Count() != 0)
                {
                    foreach (var item in hasEndDateRepAdds)
                    {
                        //計算時間差(天為單位)
                        var result = new TimeSpan(item.repdtl.EndDate.Value.Ticks - item.repair.ApplyDate.Ticks).Days;
                        if (result <= 15)
                        {
                            count1++;
                        }
                        else if (result > 15 && result < 31)
                        {
                            count2++;
                        }
                        else
                        {
                            count3++;
                        }
                    }
                }              

                //Data4
                ws.Cell(15, 1).Value = repAdds.Count() != 0 ? (count1 / Convert.ToDecimal(repAdds.Count())).ToString("P") : "0.00%";
                ws.Cell(15, 2).Value = repAdds.Count() != 0 ? (count2 / Convert.ToDecimal(repAdds.Count())).ToString("P") : "0.00%";
                ws.Cell(15, 3).Value = repAdds.Count() != 0 ? (count3 / Convert.ToDecimal(repAdds.Count())).ToString("P") : "0.00%";

                //Title5    【有費用及無費用案件數】
                ws.Cell(17, 1).Value = "【有費用及無費用案件數】";
                ws.Cell(18, 1).Value = "有費用";
                ws.Cell(18, 2).Value = "無費用";
                ws.Cell(18, 3).Value = "尚未輸入費用";

                //Data5
                ws.Cell(19, 1).Value = qtyRepairs.Where(r => r.repdtl.IsCharged == "Y").Count();
                ws.Cell(19, 2).Value = qtyRepairs.Where(r => r.repdtl.IsCharged == "N").Count();
                ws.Cell(19, 3).Value = qtyRepairs.Where(r => r.repdtl.IsCharged == null).Count();

                //Title6    【有費用件數(總費用/有費用件數)】
                ws.Cell(21, 1).Value = "【有費用件數】";
                ws.Cell(22, 1).Value = "總費用";
                ws.Cell(22, 2).Value = "平均每件維修費用";

                //Data6
                int costRepairs = qtyRepairs.Where(r => r.repdtl.IsCharged == "Y").Count();
                decimal totalCosts = qtyRepairs.Where(r => r.repdtl.IsCharged == "Y").Select(rd => rd.repdtl.Cost).DefaultIfEmpty(0).Sum();
                decimal avgCosts = costRepairs != 0 ? totalCosts / costRepairs : 0;
                ws.Cell(23, 1).Value = String.Format("{0:N0}", totalCosts); 
                ws.Cell(23, 2).Value = String.Format("{0:N0}", avgCosts);

                //WorkSheet2
                var ws2 = workbook.Worksheets.Add("個人月指標", 2);

                //Title
                ws2.Cell(1, 1).Value = "姓名";
                ws2.Cell(1, 2).Value = "增設件數";
                ws2.Cell(1, 3).Value = "內修件數";
                ws2.Cell(1, 4).Value = "外修件數";
                ws2.Cell(1, 5).Value = "內外修件數";
                ws2.Cell(1, 6).Value = "報廢件數";
                ws2.Cell(1, 7).Value = "總件數";
                ws2.Cell(1, 8).Value = "增設完成率";
                ws2.Cell(1, 9).Value = "內修完成率";
                ws2.Cell(1, 10).Value = "外修完成率";
                ws2.Cell(1, 11).Value = "內外修完成率";
                ws2.Cell(1, 12).Value = "增設結案率";
                ws2.Cell(1, 13).Value = "內修結案率";
                ws2.Cell(1, 14).Value = "外修結案率";
                ws2.Cell(1, 15).Value = "內外修結案率";
                ws2.Cell(1, 16).Value = "維修且內修3日完成率";
                ws2.Cell(1, 17).Value = "4~7日完成率";
                ws2.Cell(1, 18).Value = "8日以上完成率";
                ws2.Cell(1, 19).Value = "增設15日內完成率";
                ws2.Cell(1, 20).Value = "16~30日完成率";
                ws2.Cell(1, 21).Value = "31日以上完成率";
                ws2.Cell(1, 22).Value = "有費用件數";
                ws2.Cell(1, 23).Value = "無費用件數";
                ws2.Cell(1, 24).Value = "總費用";
                ws2.Cell(1, 25).Value = "平均每件維修費用";

                //Data 整理及統計

                // Get all engineers.
                var engs = roleManager.GetUsersInRole("RepEngineer").Where(m => m != "344027").ToList();
                List<RepairReportListVModel> rvData = new List<RepairReportListVModel>();
                AppUserModel u;

                foreach (string l in engs)
                {
                    u = _context.AppUsers.Where(ur => ur.UserName == l).FirstOrDefault();
                    if (u != null)
                    {
                        // 個人總案件數
                        var qtyEngRepairs = qtyRepairs.Where(r => r.repair.EngId == u.Id);
                        // 個案件總數
                        var engRepAdds = qtyEngRepairs.Where(r => r.repair.RepType == "增設");
                        var engRepIns = qtyEngRepairs.Where(r => r.repair.RepType != "增設" && r.repdtl.InOut == "內修" && r.repdtl.DealState != 4);
                        var engRepOuts = qtyEngRepairs.Where(r => r.repair.RepType != "增設" && r.repdtl.InOut == "外修" && r.repdtl.DealState != 4);
                        var engRepInOuts = qtyEngRepairs.Where(r => r.repair.RepType != "增設" && r.repdtl.InOut == "內外修" && r.repdtl.DealState != 4);
                        var engRepScraps = qtyEngRepairs.Where(r => r.repair.RepType != "增設" && r.repdtl.DealState == 4);

                        // 總花費 & 平均
                        int engCostRepairs = qtyEngRepairs.Where(r => r.repdtl.IsCharged == "Y").Count();
                        decimal engTotalCosts = qtyEngRepairs.Where(r => r.repdtl.IsCharged == "Y").Select(rd => rd.repdtl.Cost).DefaultIfEmpty(0).Sum();
                        decimal engAvgCosts = engCostRepairs != 0 ? engTotalCosts / engCostRepairs : 0;

                        var endEngRepAdds = engRepAdds.Where(r => r.repdtl.EndDate != null);
                        decimal addCount1 = 0, addCount2 = 0, addCount3 = 0;
                        var endEngRepIns = engRepIns.Where(r => r.repdtl.EndDate != null);
                        decimal inCount1 = 0, inCount2 = 0, inCount3 = 0;
                        // 內修日期區間完成率
                        if (endEngRepIns.Count() != 0)
                        {
                            foreach (var item in endEngRepIns)
                            {
                                //計算時間差(天為單位)
                                var result = new TimeSpan(item.repdtl.EndDate.Value.Ticks - item.repair.ApplyDate.Ticks).Days;
                                if (result <= 3)
                                {
                                    inCount1++;
                                }
                                else if (result > 3 && result < 8)
                                {
                                    inCount2++;
                                }
                                else
                                {
                                    inCount3++;
                                }
                            }
                        }
                        // 增設日期區間完成率
                        if (endEngRepAdds.Count() != 0)
                        {
                            foreach (var item in endEngRepAdds)
                            {
                                //計算時間差(天為單位)
                                var result = new TimeSpan(item.repdtl.EndDate.Value.Ticks - item.repair.ApplyDate.Ticks).Days;
                                if (result <= 15)
                                {
                                    addCount1++;
                                }
                                else if (result > 15 && result < 31)
                                {
                                    addCount2++;
                                }
                                else
                                {
                                    addCount3++;
                                }
                            }
                        }

                        rvData.Add(new RepairReportListVModel
                        {
                            FullName = u.FullName + "(" + u.UserName + ")",
                            RepAdds = engRepAdds.Count(),
                            RepIns = engRepIns.Count(),
                            RepOuts = engRepOuts.Count(),
                            RepInOuts = engRepInOuts.Count(),
                            RepScraps = engRepScraps.Count(),
                            RepTotals = qtyEngRepairs.Count(),
                            RepAddEndRate = engRepAdds.Count() != 0 ? (Convert.ToDecimal(engRepAdds.Where(r => r.repdtl.EndDate != null).Count()) / Convert.ToDecimal(engRepAdds.Count())).ToString("P") : "0.00%",
                            RepInEndRate = engRepIns.Count() != 0 ? (Convert.ToDecimal(engRepIns.Where(r => r.repdtl.EndDate != null).Count()) / Convert.ToDecimal(engRepIns.Count())).ToString("P") : "0.00%",
                            RepOutEndRate = engRepOuts.Count() != 0 ? (Convert.ToDecimal(engRepOuts.Where(r => r.repdtl.EndDate != null).Count()) / Convert.ToDecimal(engRepOuts.Count())).ToString("P") : "0.00%",
                            RepInOutEndRate = engRepInOuts.Count() != 0 ? (Convert.ToDecimal(engRepInOuts.Where(r => r.repdtl.EndDate != null).Count()) / Convert.ToDecimal(engRepInOuts.Count())).ToString("P") : "0.00%",
                            RepAddCloseRate = engRepAdds.Count() != 0 ? (Convert.ToDecimal(engRepAdds.Where(r => r.repflow.Status == "2").Count()) / Convert.ToDecimal(engRepAdds.Count())).ToString("P") : "0.00%",
                            RepInCloseRate = engRepIns.Count() != 0 ? (Convert.ToDecimal(engRepIns.Where(r => r.repflow.Status == "2").Count()) / Convert.ToDecimal(engRepIns.Count())).ToString("P") : "0.00%",
                            RepOutCloseRate = engRepOuts.Count() != 0 ? (Convert.ToDecimal(engRepOuts.Where(r => r.repflow.Status == "2").Count()) / Convert.ToDecimal(engRepOuts.Count())).ToString("P") : "0.00%",
                            RepInOutCloseRate = engRepInOuts.Count() != 0 ? (Convert.ToDecimal(engRepInOuts.Where(r => r.repflow.Status == "2").Count()) / Convert.ToDecimal(engRepInOuts.Count())).ToString("P") : "0.00%",
                            RepInEndRate1 = engRepIns.Count() != 0 ? (inCount1 / Convert.ToDecimal(engRepIns.Count())).ToString("P") : "0.00%",
                            RepInEndRate2 = engRepIns.Count() != 0 ? (inCount2 / Convert.ToDecimal(engRepIns.Count())).ToString("P") : "0.00%",
                            RepInEndRate3 = engRepIns.Count() != 0 ? (inCount3 / Convert.ToDecimal(engRepIns.Count())).ToString("P") : "0.00%",
                            RepAddEndRate1 = engRepAdds.Count() != 0 ? (addCount1 / Convert.ToDecimal(engRepAdds.Count())).ToString("P") : "0.00%",
                            RepAddEndRate2 = engRepAdds.Count() != 0 ? (addCount2 / Convert.ToDecimal(engRepAdds.Count())).ToString("P") : "0.00%",
                            RepAddEndRate3 = engRepAdds.Count() != 0 ? (addCount3 / Convert.ToDecimal(engRepAdds.Count())).ToString("P") : "0.00%",
                            IsChargedReps = qtyEngRepairs.Where(r => r.repdtl.IsCharged == "Y").Count(),
                            NoChargedReps = qtyEngRepairs.Where(r => r.repdtl.IsCharged == "N").Count(),
                            TotalCosts = engTotalCosts,
                            AvgCosts = engAvgCosts,
                        });
                    }

                }

                //如果是要塞入Query後的資料該資料一定要變成是data.AsEnumerable()
                ws2.Cell(2, 1).InsertData(rvData);

                //因為是用Query的方式,這個地方要用串流的方式來存檔
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    workbook.SaveAs(memoryStream);
                    //請注意 一定要加入這行,不然Excel會是空檔
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    //注意Excel的ContentType,是要用這個"application/vnd.ms-excel"
                    string fileName = "月報表_" + qtyMonth.ToString("yyyy-MM") + ".xlsx";
                    return this.File(memoryStream.ToArray(), "application/vnd.ms-excel", fileName);
                }
            }
        }
    }
}
