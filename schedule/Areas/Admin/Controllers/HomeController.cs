
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using it.Areas.Admin.Models;
using it.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

			var user_id = UserManager.GetUserId(this.User);
			var user = _context.UserModel.Where(d => d.Id == user_id).Include(d => d.equipments).FirstOrDefault();
			var user_equipments = user.equipments.Select(d => d.equipment_id).ToList();
			var plans = _context.PlanModel.Where(u => u.deleted_at == null && user.department_id == u.department_id).Select(a => new SelectListItem()
			{
				Value = a.id.ToString(),
				Text = a.name
			}).ToList();
			plans.Insert(0, new SelectListItem()
			{
				Value = "",
				Text = "Không có kế hoạch"
			});
			ViewData["plans"] = plans;
			ViewData["equipments"] = _context.EquipmentModel.Where(u => u.deleted_at == null && user_equipments.Contains(u.id)).OrderBy(d => d.code).Select(a => new SelectListItem()
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

		public async Task<IActionResult> Timeline()
		{

			var user_id = UserManager.GetUserId(this.User);
			var user = _context.UserModel.Where(d => d.Id == user_id).Include(d => d.equipments).FirstOrDefault();
			var user_equipments = user.equipments.Select(d => d.equipment_id).ToList();
			var plans = _context.PlanModel.Where(u => u.deleted_at == null && user.department_id == u.department_id).Select(a => new SelectListItem()
			{
				Value = a.id.ToString(),
				Text = a.name
			}).ToList();
			plans.Insert(0, new SelectListItem()
			{
				Value = "",
				Text = "Không có kế hoạch"
			});
			ViewData["plans"] = plans;
			ViewData["equipments"] = _context.EquipmentModel.Where(u => u.deleted_at == null && user_equipments.Contains(u.id)).OrderBy(d => d.code).Select(a => new SelectListItem()
			{
				Value = a.id.ToString(),
				Text = a.code
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
