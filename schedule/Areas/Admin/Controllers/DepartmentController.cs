using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using it.Areas.Admin.Models;
using it.Data;
using System.Collections;
using Microsoft.AspNetCore.Identity;

namespace it.Areas.Admin.Controllers
{
    public class DepartmentController : BaseController
    {
        private UserManager<UserModel> UserManager;
        private string _type = "Department";
        public DepartmentController(ItContext context, UserManager<UserModel> UserMgr) : base(context)
        {
            ViewData["controller"] = _type;
            UserManager = UserMgr;
        }

        // GET: Admin/Department
        public IActionResult Index()
        {
            return View();
        }


        // GET: Admin/Department/Create
        public IActionResult Create()
        {
            ViewData["users"] = UserManager.Users.Where(u => u.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.Id,
                Text = a.FullName + "<" + a.Email + ">"
            }).ToList();

            return View();
        }

        // POST: Admin/Department/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Create(DepartmentModel DepartmentModel)
        {
            if (ModelState.IsValid)
            {
                DepartmentModel.created_at = DateTime.Now;
                _context.Add(DepartmentModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return Ok(ModelState);
        }

        // GET: Admin/Department/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null || _context.DepartmentModel == null)
            {
                return NotFound();
            }

            var DepartmentModel = _context.DepartmentModel
                .Where(d => d.id == id).FirstOrDefault();
            if (DepartmentModel == null)
            {
                return NotFound();
            }

            ViewData["users"] = UserManager.Users.Where(u => u.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.Id,
                Text = a.FullName + "<" + a.Email + ">"
            }).ToList();

            return View(DepartmentModel);
        }

        // POST: Admin/Department/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Edit(int id, DepartmentModel DepartmentModel)
        {

            if (id != DepartmentModel.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var DepartmentModel_old = await _context.DepartmentModel.FindAsync(id);
                    DepartmentModel_old.updated_at = DateTime.Now;

                    foreach (string key in HttpContext.Request.Form.Keys)
                    {
                        var prop = DepartmentModel_old.GetType().GetProperty(key);

                        dynamic val = Request.Form[key].FirstOrDefault();

                        if (prop != null)
                        {
                            prop.SetValue(DepartmentModel_old, val);
                        }
                    }
                    _context.Update(DepartmentModel_old);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {

                }
                return RedirectToAction(nameof(Index));
            }
            return View(DepartmentModel);
        }


        // GET: Admin/Department/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.DepartmentModel == null)
            {
                return Problem("Entity set 'ItContext.DepartmentModel'  is null.");
            }
            var DepartmentModel = await _context.DepartmentModel.FindAsync(id);
            if (DepartmentModel != null)
            {
                DepartmentModel.deleted_at = DateTime.Now;
                _context.DepartmentModel.Update(DepartmentModel);
            }

            await _context.SaveChangesAsync();
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
            var customerData = (from tempcustomer in _context.DepartmentModel.Where(u => u.deleted_at == null) select tempcustomer);
            int recordsTotal = customerData.Count();
            customerData = customerData.Where(m => m.deleted_at == null);
            if (!string.IsNullOrEmpty(searchValue))
            {
                customerData = customerData.Where(m => m.name.Contains(searchValue));
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
                    name = record.name
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }
    }
}
