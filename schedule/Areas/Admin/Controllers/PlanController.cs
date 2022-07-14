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
using Microsoft.AspNetCore.Authorization;

namespace it.Areas.Admin.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class PlanController : BaseController
    {
        private UserManager<UserModel> UserManager;
        private string _type = "Plan";
        public PlanController(ItContext context, UserManager<UserModel> UserMgr) : base(context)
        {
            ViewData["controller"] = _type;
            UserManager = UserMgr;
        }

        // GET: Admin/Plan
        public IActionResult Index()
        {
            return View();
        }


        // GET: Admin/Plan/Create
        public IActionResult Create()
        {

            ViewData["departments"] = _context.DepartmentModel.Where(d => d.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.id.ToString(),
                Text = a.name
            }).ToList();

            return View();
        }

        // POST: Admin/Plan/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Create(PlanModel PlanModel)
        {
            if (ModelState.IsValid)
            {
                PlanModel.created_at = DateTime.Now;
                _context.Add(PlanModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return Ok(ModelState);
        }

        // GET: Admin/Plan/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null || _context.PlanModel == null)
            {
                return NotFound();
            }

            var PlanModel = _context.PlanModel
                .Where(d => d.id == id).FirstOrDefault();
            if (PlanModel == null)
            {
                return NotFound();
            }

            ViewData["departments"] = _context.DepartmentModel.Where(d => d.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.id.ToString(),
                Text = a.name
            }).ToList();

            return View(PlanModel);
        }

        // POST: Admin/Plan/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Edit(int id, PlanModel PlanModel)
        {

            if (id != PlanModel.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var PlanModel_old = await _context.PlanModel.FindAsync(id);
                    PlanModel_old.updated_at = DateTime.Now;

                    foreach (string key in HttpContext.Request.Form.Keys)
                    {
                        var prop = PlanModel_old.GetType().GetProperty(key);
                        var prop_new = PlanModel.GetType().GetProperty(key);
                        //if (key == "keyword")
                        //{
                        //    var type1 = "";
                        //}
                        if (prop != null)
                        {
                            string temp = Request.Form[key].FirstOrDefault();
                            var value = prop.GetValue(PlanModel_old, null);
                            var value_new = prop.GetValue(PlanModel, null);
                            if (value == null && value_new == null)
                                continue;

                            var type = value != null ? value.GetType() : value_new.GetType();


                            if (type == typeof(int))
                            {
                                int val = Int32.Parse(temp);
                                prop.SetValue(PlanModel_old, val);
                            }
                            else if (type == typeof(string))
                            {
                                prop.SetValue(PlanModel_old, temp);
                            }
                            else if (type == typeof(DateTime))
                            {
                                if (string.IsNullOrEmpty(temp))
                                {
                                    prop.SetValue(PlanModel_old, null);
                                }
                                else
                                {
                                    DateTime.TryParse(temp, out DateTime val);
                                    prop.SetValue(PlanModel_old, val);
                                }
                            }
                        }
                    }
                    _context.Update(PlanModel_old);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {

                }
                return RedirectToAction(nameof(Index));
            }
            return View(PlanModel);
        }


        // GET: Admin/Plan/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.PlanModel == null)
            {
                return Problem("Entity set 'ItContext.PlanModel'  is null.");
            }
            var PlanModel = await _context.PlanModel.FindAsync(id);
            if (PlanModel != null)
            {
                PlanModel.deleted_at = DateTime.Now;
                _context.PlanModel.Update(PlanModel);
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
            var customerData = (from tempcustomer in _context.PlanModel.Where(u => u.deleted_at == null) select tempcustomer);
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
                    name = record.name,
                    code = record.code
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }
    }
}
