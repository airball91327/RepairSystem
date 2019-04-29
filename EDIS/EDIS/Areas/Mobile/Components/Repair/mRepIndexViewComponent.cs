using EDIS.Data;
using EDIS.Models.RepairModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EDIS.Areas.Mobile.Components.Repair
{
    public class mRepIndexViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public mRepIndexViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            List<SelectListItem> FlowlistItem = new List<SelectListItem>();
            FlowlistItem.Add(new SelectListItem { Text = "待簽核", Value = "待簽核" });
            FlowlistItem.Add(new SelectListItem { Text = "流程中", Value = "流程中" });
            FlowlistItem.Add(new SelectListItem { Text = "已結案", Value = "已結案" });
            ViewData["FLOWTYPE"] = new SelectList(FlowlistItem, "Value", "Text", "待簽核");

            /* 成本中心 & 申請部門的下拉選單資料 */
            var departments = _context.Departments.ToList();
            List<SelectListItem> listItem = new List<SelectListItem>();
            foreach(var item in departments)
            {
                listItem.Add(new SelectListItem
                {
                    Text = item.Name_C + "(" + item.DptId + ")",    //show DptName(DptId)
                    Value = item.DptId
                });
            }
            ViewData["ACCDPT"] = new SelectList(listItem, "Value", "Text");

            /* 處理狀態的下拉選單 */
            List<SelectListItem> listItem2 = new List<SelectListItem>();
            listItem2.Add(new SelectListItem { Text = "未處理", Value = "未處理" });
            listItem2.Add(new SelectListItem { Text = "處理中", Value = "處理中" });
            ViewData["DealStatus"] = new SelectList(listItem2, "Value", "Text", "未處理");

            QryRepListData data = new QryRepListData();

            return View(data);
        }
    }
}
