
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

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // GET: Admin/Schedule/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null || _context.ScheduleModel == null)
            {
                return NotFound();
            }

            var ScheduleModel = _context.ScheduleModel
                .Where(d => d.id == id).FirstOrDefault();
            if (ScheduleModel == null)
            {
                return NotFound();
            }

            return View(ScheduleModel);
        }

        // POST: Admin/Schedule/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Edit(int id, ScheduleModel ScheduleModel)
        {

            if (id != ScheduleModel.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var ScheduleModel_old = await _context.ScheduleModel.FindAsync(id);
                    ScheduleModel_old.updated_at = DateTime.Now;

                    foreach (string key in HttpContext.Request.Form.Keys)
                    {
                        var prop = ScheduleModel_old.GetType().GetProperty(key);

                        dynamic val = Request.Form[key].FirstOrDefault();

                        if (prop != null)
                        {
                            prop.SetValue(ScheduleModel_old, val);
                        }
                    }
                    _context.Update(ScheduleModel_old);
                    _context.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {

                }
                return RedirectToAction(nameof(Index));
            }
            return View(ScheduleModel);
        }


        // GET: Admin/Schedule/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.ScheduleModel == null)
            {
                return Problem("Entity set 'ItContext.ScheduleModel'  is null.");
            }
            var ScheduleModel = await _context.ScheduleModel.FindAsync(id);
            if (ScheduleModel != null)
            {
                ScheduleModel.deleted_at = DateTime.Now;
                _context.ScheduleModel.Update(ScheduleModel);
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<JsonResult> Table()
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            var customerData = (from tempcustomer in _context.ScheduleModel.Where(u => u.deleted_at == null) select tempcustomer);
            int recordsTotal = customerData.Count();
            customerData = customerData.Where(m => m.deleted_at == null);
            if (!string.IsNullOrEmpty(searchValue))
            {
                customerData = customerData.Where(m => m.title.Contains(searchValue));
            }
            int recordsFiltered = customerData.Count();
            var datapost = customerData.Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            foreach (var record in datapost)
            {
                var data1 = new
                {
                    action = "<div class='btn-group'><a href='/admin/" + _type + "/delete/" + record.id + "' class='btn btn-danger btn-sm' title='Xóa?' data-type='confirm'>'"
                        + "<i class='fas fa-trash-alt'>"
                        + "</i>"
                        + "</a></div>",
                    id = "<a href='/admin/" + _type + "/edit/" + record.id + "'><i class='fas fa-pencil-alt mr-2'></i> " + record.id + "</a>",
                    name = record.title
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }

        public ContentResult Read(string callback)
        {
            var data = _context.ScheduleModel.Where(d => d.deleted_at == null).ToList();
            return Content(String.Format("{0}({1});",
          callback,
            JsonConvert.SerializeObject(data)),
          "application/javascript");
        }
        [HttpPost]
        public ContentResult Create(string callback, string models)
        {
            var list = JsonConvert.DeserializeObject<List<ScheduleModel>>(models);
            foreach (var model in list)
            {
                model.created_at = DateTime.Now;
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
