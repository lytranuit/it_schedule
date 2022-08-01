
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using it.Areas.Admin.Models;
using it.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace it.Areas.Admin.Controllers
{
    class MySelect : SelectListItem
    {
        public string Color { get; set; }

    }
    public class HomeController : BaseController
    {
        private UserManager<UserModel> UserManager;


        public HomeController(ItContext context, UserManager<UserModel> UserMgr) : base(context)
        {
            UserManager = UserMgr;
        }
        public async Task<IActionResult> Index()
        {
            ViewData["plans"] = _context.PlanModel.Where(u => u.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.id.ToString(),
                Text = a.name
            }).ToList();
            ViewData["equipments"] = _context.EquipmentModel.Where(u => u.deleted_at == null).OrderBy(d => d.code).Select(a => new SelectListItem()
            {
                Value = a.id.ToString(),
                Text = a.code + " - " + a.name
            }).ToList();
            ViewData["departments"] = _context.DepartmentModel.Where(u => u.deleted_at == null).Select(a => new MySelect()
            {
                Value = a.id.ToString(),
                Text = "<i style='background:" + a.color + "' class='color_i'></i>" + a.name,
                Selected = true,
                Color = a.color
            }).ToList();
            return View();
        }


    }
}
