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
using NPOI.XSSF.UserModel;
using System.Text;
using NPOI.SS.UserModel;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace it.Areas.Admin.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class EquipmentController : BaseController
    {
        private UserManager<UserModel> UserManager;
        private string _type = "Equipment";
        public EquipmentController(ItContext context, UserManager<UserModel> UserMgr) : base(context)
        {
            ViewData["controller"] = _type;
            UserManager = UserMgr;
        }

        // GET: Admin/Equipment
        public IActionResult Index()
        {
            return View();
        }


        // GET: Admin/Equipment/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Equipment/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Create(EquipmentModel EquipmentModel)
        {
            if (ModelState.IsValid)
            {
                EquipmentModel.created_at = DateTime.Now;
                _context.Add(EquipmentModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return Ok(ModelState);
        }

        // GET: Admin/Equipment/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null || _context.EquipmentModel == null)
            {
                return NotFound();
            }

            var EquipmentModel = _context.EquipmentModel
                .Where(d => d.id == id).FirstOrDefault();
            if (EquipmentModel == null)
            {
                return NotFound();
            }

            return View(EquipmentModel);
        }

        // POST: Admin/Equipment/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Edit(int id, EquipmentModel EquipmentModel)
        {

            if (id != EquipmentModel.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var EquipmentModel_old = await _context.EquipmentModel.FindAsync(id);
                    EquipmentModel_old.updated_at = DateTime.Now;

                    foreach (string key in HttpContext.Request.Form.Keys)
                    {
                        var prop = EquipmentModel_old.GetType().GetProperty(key);

                        dynamic val = Request.Form[key].FirstOrDefault();

                        if (prop != null)
                        {
                            prop.SetValue(EquipmentModel_old, val);
                        }
                    }
                    _context.Update(EquipmentModel_old);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {

                }
                return RedirectToAction(nameof(Index));
            }
            return View(EquipmentModel);
        }


        // GET: Admin/Equipment/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.EquipmentModel == null)
            {
                return Problem("Entity set 'ItContext.EquipmentModel'  is null.");
            }
            var EquipmentModel = await _context.EquipmentModel.FindAsync(id);
            if (EquipmentModel != null)
            {
                EquipmentModel.deleted_at = DateTime.Now;
                _context.EquipmentModel.Update(EquipmentModel);
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
            var customerData = (from tempcustomer in _context.EquipmentModel.Where(u => u.deleted_at == null) select tempcustomer);
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
        public async Task<IActionResult> import()
        {
            //return Ok();
            FileStream fs = new FileStream("./private/excel/equipment/kho.xlsx", FileMode.Open);

            // Khởi tạo workbook để đọc
            XSSFWorkbook wb = new XSSFWorkbook(fs);
            int numberofsheet = wb.NumberOfSheets;
            Console.OutputEncoding = Encoding.UTF8; // để xuất ra console tv có dấu
            Console.WriteLine("Thông tin từ file Excel");
            for (var i = 0; i < numberofsheet; i++)
            {
                ISheet sheet = wb.GetSheetAt(i);

                // đọc sheet này bắt đầu từ row 2 (0, 1 bỏ vì tiêu đề)

                var lastrow = sheet.LastRowNum;
                // nếu vẫn chưa gặp end thì vẫn lấy data
                Console.WriteLine(lastrow);
                for (int rowIndex = 6; rowIndex < lastrow; rowIndex++)
                {
                    // lấy row hiện tại
                    var nowRow = sheet.GetRow(rowIndex);
                    if (nowRow == null)
                        continue;
                    if (nowRow.Cells.All(d => d.CellType == CellType.Blank)) break;
                    // vì ta dùng 3 cột A, B, C => data của ta sẽ như sau
                    //int numcount = nowRow.Cells.Count;
                    //for(int y = 0;y<numcount - 1 ;y++)
                    var cellname = nowRow.GetCell(1);
                    var cellcode = nowRow.GetCell(2);
                    if (cellname == null || cellcode == null)
                    {
                        continue;
                    }
                    var code = "";
                    var name = "";
                    if (cellcode.CellType == CellType.String)
                    {
                        code = cellcode.StringCellValue;
                    }
                    else if (cellcode.CellType == CellType.Numeric)
                    {
                        code = cellcode.NumericCellValue.ToString();
                    }
                    if (cellname.CellType == CellType.String)
                    {
                        name = cellname.StringCellValue;
                    }
                    else if (cellname.CellType == CellType.Numeric)
                    {
                        name = cellname.NumericCellValue.ToString();
                    }
                    if (name == "" || code == "")
                    {
                        continue;
                    }
                    string pattern = "/\r\n|\n|\r/";
                    Regex rgx = new Regex(pattern);
                    var export_name = rgx.Split(name);
                    var name_vn = name;
                    var name_en = "";
                    if (export_name.Length > 1)
                    {
                        name_vn = export_name[0];
                        name_en = export_name[1];
                    }
                    // Xuất ra thông tin lên màn hình
                    Console.WriteLine("MS: {0} ", code);
                    Console.WriteLine("name: {0} ", name_vn);

                    EquipmentModel EquipmentModel = new EquipmentModel { code = code, name = name_vn, name_en = name_en, created_at = DateTime.Now };
                    _context.Add(EquipmentModel);
                    _context.SaveChanges();
                }
            }
            return Ok(1);
        }
    }
}
