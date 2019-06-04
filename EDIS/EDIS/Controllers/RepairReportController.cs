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
                var ws = workbook.Worksheets.Add("月報表_1", 1);

                //Title1  【每月維修件數(申請日為該月的案件)】
                ws.Cell(1, 1).Value = "增設";
                ws.Cell(1, 2).Value = "維修(內修)";
                ws.Cell(1, 3).Value = "維修(外修)";
                ws.Cell(1, 4).Value = "維修(內外修)";
                ws.Cell(1, 5).Value = "報廢";
                ws.Cell(1, 6).Value = "尚未處理";
                ws.Cell(1, 7).Value = "總件數";

                //Data1
                ws.Cell(2, 1).Value = repAdds.Count();
                ws.Cell(2, 2).Value = repIns.Count();
                ws.Cell(2, 3).Value = repOuts.Count();
                ws.Cell(2, 4).Value = repInOuts.Count();
                ws.Cell(2, 5).Value = repScraps.Count();
                ws.Cell(2, 6).Value = qtyRepairs.Where(r => r.repdtl.InOut == null).Count();
                ws.Cell(2, 7).Value = qtyRepairs.Count();

                //Title2    【維修完成、結案率 (該月申請且已完成或已結案案件 / 該月申請總件數)】
                ws.Cell(4, 1).Value = "維修完成率";
                ws.Cell(4, 5).Value = "維修結案率";
                ws.Cell(5, 1).Value = "增設";
                ws.Cell(5, 2).Value = "維修(內修)";
                ws.Cell(5, 3).Value = "維修(外修)";
                ws.Cell(5, 4).Value = "維修(內外修)";
                ws.Cell(5, 5).Value = "增設";
                ws.Cell(5, 6).Value = "維修(內修)";
                ws.Cell(5, 7).Value = "維修(外修)";
                ws.Cell(5, 8).Value = "維修(內外修)";

                //Data2         //.ToString("P")轉為百分比顯示的字串
                ws.Cell(6, 1).Value = (Convert.ToDecimal((repAdds.Where(r => r.repdtl.EndDate != null).Count())) / Convert.ToDecimal(repAdds.Count())).ToString("P");
                ws.Cell(6, 2).Value = (Convert.ToDecimal((repIns.Where(r => r.repdtl.EndDate != null).Count())) / Convert.ToDecimal(repIns.Count())).ToString("P");
                ws.Cell(6, 3).Value = (Convert.ToDecimal((repOuts.Where(r => r.repdtl.EndDate != null).Count())) / Convert.ToDecimal(repOuts.Count())).ToString("P");
                ws.Cell(6, 4).Value = (Convert.ToDecimal((repInOuts.Where(r => r.repdtl.EndDate != null).Count())) / Convert.ToDecimal(repInOuts.Count())).ToString("P");
                ws.Cell(6, 5).Value = (Convert.ToDecimal((repAdds.Where(r => r.repflow.Status == "2").Count())) / Convert.ToDecimal(repAdds.Count())).ToString("P");
                ws.Cell(6, 6).Value = (Convert.ToDecimal((repIns.Where(r => r.repflow.Status == "2").Count())) / Convert.ToDecimal(repIns.Count())).ToString("P");
                ws.Cell(6, 7).Value = (Convert.ToDecimal((repOuts.Where(r => r.repflow.Status == "2").Count())) / Convert.ToDecimal(repOuts.Count())).ToString("P");
                ws.Cell(6, 8).Value = (Convert.ToDecimal((repInOuts.Where(r => r.repflow.Status == "2").Count())) / Convert.ToDecimal(repInOuts.Count())).ToString("P");

                //Title3    【維修且內修完成率(該月申請的內修且已完成的案件)】
                ws.Cell(8, 1).Value = "維修且內修案件";
                ws.Cell(9, 1).Value = "3日完成率";
                ws.Cell(9, 2).Value = "4~7日完成率";
                ws.Cell(9, 3).Value = "8日以上";

                var hasEndDateReps = repIns.Where(r => r.repdtl.EndDate != null);
                decimal count1 = 0, count2 = 0, count3 = 0;
                foreach(var item in hasEndDateReps)
                {
                    //計算時間差(天為單位)
                    var result = new TimeSpan(item.repdtl.EndDate.Value.Ticks - item.repair.ApplyDate.Ticks).Days;
                    if(result <= 3)
                    {
                        count1++;
                    }
                    else if(result > 3 && result < 8)
                    {
                        count2++;
                    }
                    else
                    {
                        count3++;
                    }
                }

                //Data3
                ws.Cell(10, 1).Value = (count1 / Convert.ToDecimal(repIns.Count())).ToString("P");
                ws.Cell(10, 2).Value = (count2 / Convert.ToDecimal(repIns.Count())).ToString("P");
                ws.Cell(10, 3).Value = (count3 / Convert.ToDecimal(repIns.Count())).ToString("P");



                //如果是要塞入Query後的資料該資料一定要變成是data.AsEnumerable()
                //ws.Cell(2, 1).InsertData(data);








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
