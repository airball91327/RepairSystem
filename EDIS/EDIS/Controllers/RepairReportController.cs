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

            var qtyRepairs = _context.Repairs.Where(r => r.ApplyDate.Month == reportMonth.Month)
                                             .Join(_context.RepairDtls, r => r.DocId, d => d.DocId,
                                             (r, d) => new
                                             {
                                                 repair = r,
                                                 repdtl = d
                                             });

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
                var ws = workbook.Worksheets.Add("每月維修件數", 1);

                //Title
                ws.Cell(1, 1).Value = "增設";
                ws.Cell(1, 2).Value = "維修(內修)";
                ws.Cell(1, 3).Value = "維修(外修)";
                ws.Cell(1, 4).Value = "維修(內外修)";

                //Data
                ws.Cell(2, 1).Value = qtyRepairs.Where(r => r.repair.RepType == "增設").Count();
                ws.Cell(2, 2).Value = qtyRepairs.Where(r => r.repdtl.InOut == "內修").Count();
                ws.Cell(2, 3).Value = qtyRepairs.Where(r => r.repdtl.InOut == "外修").Count();
                ws.Cell(2, 4).Value = qtyRepairs.Where(r => r.repdtl.InOut == "內外修").Count();

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
