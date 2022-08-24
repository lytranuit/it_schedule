
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using it.Areas.Admin.Models;
using it.Data;
using System.Collections;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace it.Areas.Admin.Controllers
{
	public class ScheduleController : BaseController
	{
		private UserManager<UserModel> UserManager;
		private string _type = "Schedule";
		public ScheduleController(ItContext context, UserManager<UserModel> UserMgr) : base(context)
		{
			ViewData["controller"] = _type;
			UserManager = UserMgr;
		}

		// GET: Admin/Schedule
		public IActionResult Index()
		{
			return View();
		}

		public ContentResult Read(string callback)
		{
			var user_id = UserManager.GetUserId(this.User);
			var user = _context.UserModel.Where(d => d.Id == user_id).Include(d => d.equipments).FirstOrDefault();
			var user_equipments = user.equipments.Select(d => d.equipment_id).ToList();

			var data = _context.ScheduleModel.Where(d => d.deleted_at == null && user_equipments.Contains(d.equipment_id)).ToList();
			return Content(String.Format("{0}({1});",
		  callback,
			JsonConvert.SerializeObject(data)),
		  "application/javascript");
		}
		[HttpPost]
		public ContentResult Create(string callback, string models)
		{

			var user_id = UserManager.GetUserId(this.User);
			var list = JsonConvert.DeserializeObject<List<ScheduleModel>>(models);
			foreach (var model in list)
			{
				model.created_at = DateTime.Now;
				model.user_id_created = user_id;
				model.date_plan = model.date_plan + new TimeSpan(7, 0, 0);
				_context.Add(model);
			}
			_context.SaveChanges();

			return Content(String.Format("{0}({1});",
		  callback,
			JsonConvert.SerializeObject(list)),
		  "application/javascript");
		}

		[HttpPost]
		public ContentResult Destroy(string callback, string models)
		{
			var list = JsonConvert.DeserializeObject<List<ScheduleModel>>(models);
			foreach (var model in list)
			{
				model.deleted_at = DateTime.Now;
				_context.Update(model);
			}
			_context.SaveChanges();

			return Content(String.Format("{0}({1});",
		  callback,
			JsonConvert.SerializeObject(list)),
		  "application/javascript");
		}

		[HttpPost]
		public async Task<ContentResult> Update(string callback, string models)
		{
			var list = JsonConvert.DeserializeObject<List<ScheduleModel>>(models);
			foreach (var model in list)
			{
				model.updated_at = DateTime.Now;
				model.date_plan = model.date_plan + new TimeSpan(7, 0, 0);
				_context.Update(model);
			}

			_context.SaveChanges();

			return Content(String.Format("{0}({1});",
		  callback,
			JsonConvert.SerializeObject(list)),
		  "application/javascript");
		}

	}
}
