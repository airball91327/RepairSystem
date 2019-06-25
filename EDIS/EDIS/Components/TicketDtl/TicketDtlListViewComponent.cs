using EDIS.Data;
using EDIS.Models.Identity;
using EDIS.Models.RepairModels;
using EDIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EDIS.Components.TicketDtl
{
    public class TicketDtlListViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public TicketDtlListViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string id)
        {
            List<TicketDtlModel> dtls = _context.TicketDtls.Where(t => t.TicketDtlNo == id).ToList();
            foreach(var item in dtls)
            {
                var rc = _context.RepairCosts.Where(r => r.TicketDtl.TicketDtlNo == item.TicketDtlNo &&
                                                         r.TicketDtl.SeqNo == item.SeqNo).FirstOrDefault();
                if (rc != null)
                {
                    item.DocId = rc.DocId;
                }
            }
            ViewData["Total"] = dtls.Sum(t => t.Cost);

            return View(dtls);
        }
    }
}
