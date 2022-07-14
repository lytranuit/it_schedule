using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using it.Areas.Admin.Models;
using System.Collections;
using it.Data;
using Microsoft.AspNetCore.Authorization;

namespace it.Areas.Admin.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class RoleController : BaseController
    {
        private RoleManager<IdentityRole> roleManager;
        public RoleController(ItContext context, RoleManager<IdentityRole> roleMgr) : base(context)
        {
            roleManager = roleMgr;
        }
        // GET: RoleController
        public ActionResult Index()
        {
            return View(roleManager.Roles);
        }

        // GET: RoleController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: RoleController/Create
        [HttpPost]
        public async Task<IActionResult> Create(IdentityRole role)
        {
            try
            {
                IdentityResult result = await roleManager.CreateAsync(new IdentityRole(role.Name));
                if (result.Succeeded)
                    return RedirectToAction("Index");
                else
                    return Ok(role);
            }
            catch
            {
                return View();
            }
        }

        // GET: RoleController/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            IdentityRole role = await roleManager.FindByIdAsync(id);
            return View(role);
        }

        // POST: RoleController/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(string id, IdentityRole role)
        {
            //return Ok(id);
            if (id != role.Id)
            {
                return Ok(role);
            }
            if (ModelState.IsValid)
            {
                IdentityRole role_old = await roleManager.FindByIdAsync(role.Id);
                role_old.Name = role.Name;

                IdentityResult result = await roleManager.UpdateAsync(role_old);
                if (result.Succeeded)
                    return RedirectToAction("Index");
                else
                    return Ok(result);
            }
            return View("Index", roleManager.Roles);
        }

        // GET: RoleController/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            IdentityRole role = await roleManager.FindByIdAsync(id);
            if (role != null)
            {
                IdentityResult result = await roleManager.DeleteAsync(role);
                if (result.Succeeded)
                    return RedirectToAction("Index");
                else
                    return NotFound(result);
            }
            else
                ModelState.AddModelError("", "No role found");
            return View("Index", roleManager.Roles);
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
            var customerData = (from tempcustomer in roleManager.Roles select tempcustomer);

            int recordsTotal = customerData.Count();
            if (!string.IsNullOrEmpty(searchValue))
            {
                customerData = customerData.Where(m => m.Name.Contains(searchValue));
            }
            int recordsFiltered = customerData.Count();
            var datapost = customerData.Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            foreach (var record in datapost)
            {
                var data1 = new
                {
                    action = "<div class='btn-group'><a href='/admin/role/delete/" + record.Id + "' class='btn btn-danger btn-sm' title='Xóa?' data-type='confirm'>'"
                        + "<i class='fas fa-trash-alt'>"
                        + "</i>"
                        + "</a></div>",
                    Id = "<a href='/admin/role/edit/" + record.Id + "'><i class='fas fa-pencil-alt mr-2'></i> " + record.Id + "</a>",
                    Name = record.Name
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }

    }
}
