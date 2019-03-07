using EDIS.Data;
using EDIS.Models.RepairModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EDIS.Components.Repair
{
    public class RepIndexViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public RepIndexViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            List<SelectListItem> FlowlistItem = new List<SelectListItem>();
            FlowlistItem.Add(new SelectListItem { Text = "待處理", Value = "待處理" });
            FlowlistItem.Add(new SelectListItem { Text = "已處理", Value = "已處理" });
            FlowlistItem.Add(new SelectListItem { Text = "已結案", Value = "已結案" });
            ViewData["FLOWTYPE"] = new SelectList(FlowlistItem, "Value", "Text", "待處理");

            /* 成本中心 & 申請部門的下拉選單資料 */
            var departments = _context.Departments.ToList();
            List<SelectListItem> listItem = new List<SelectListItem>();
            foreach(var item in departments)
            {
                listItem.Add(new SelectListItem
                {
                    Text = item.Name_C,
                    Value = item.DptId
                });
            }

            ViewData["ACCDPT"] = new SelectList(listItem, "Value", "Text");
            ViewData["APPLYDPT"] = new SelectList(listItem, "Value", "Text");
            QryRepListData data = new QryRepListData();

            return View(data);
        }
    }
}
