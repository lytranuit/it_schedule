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
using NPOI.SS.Util;
using DocumentFormat.OpenXml.Spreadsheet;
using Spire.Xls;
using System.Globalization;
using Ical.Net.Serialization.DataTypes;
using Ical.Net.DataTypes;
using Ical.Net.CalendarComponents;
using System.Collections.ObjectModel;

namespace it.Areas.Admin.Controllers
{
    public class MyCalendar : CalendarEvent
    {
        public DateTime? date { get; set; }
        public DateTime? plan_date { get; set; }
        public int? month { get; set; }

    }

    public class PlanFileController : BaseController
    {
        private UserManager<UserModel> UserManager;
        private string _type = "PlanFile";
        public PlanFileController(ItContext context, UserManager<UserModel> UserMgr) : base(context)
        {
            ViewData["controller"] = _type;
            UserManager = UserMgr;
        }

        // GET: Admin/PlanFile
        public IActionResult Index(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            ViewData["plan_id"] = id;
            return View();
        }


        // GET: Admin/PlanFile/Create
        public IActionResult Create(int id)
        {
            if (id == null)
            {
                return NotFound();
            }
            ViewData["plan_id"] = id;

            ViewData["equipments"] = _context.EquipmentModel.Where(d => d.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.id.ToString(),
                Text = a.code + " - " + a.name
            }).ToList();
            return View();
        }

        // POST: Admin/PlanFile/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Add(PlanFileModel PlanFileModel, List<int> equipments)
        {
            if (ModelState.IsValid)
            {
                PlanFileModel.created_at = DateTime.Now;
                _context.Add(PlanFileModel);
                await _context.SaveChangesAsync();

                var equips = new List<PlanFileEquipmentModel>();
                //ADD equipments
                if (equipments != null)
                {
                    foreach (var equipment in equipments)
                    {
                        PlanFileEquipmentModel PlanFileEquipmentModel = new PlanFileEquipmentModel
                        {
                            plan_file_id = PlanFileModel.id,
                            equipment_id = equipment
                        };
                        equips.Add(PlanFileEquipmentModel);
                        _context.Add(PlanFileEquipmentModel);
                    }
                    await _context.SaveChangesAsync();
                }
                PlanFileModel.equipments = equips;
                ///WRITE TO EXCEL
                //return Ok();
                createFile(PlanFileModel);

                return RedirectToAction(nameof(Index), new { id = PlanFileModel.plan_id });
            }
            return Ok(ModelState);
        }
        private void createFile(PlanFileModel PlanFileModel)
        {


            if (PlanFileModel.plan_id == 1)
            ///KẾ hoạch sao lưu
            {
                // Khởi tạo workbook để đọc
                Spire.Xls.Workbook book = new Spire.Xls.Workbook();
                book.LoadFromFile("./private/excel/template/010061.05_01 - Master data backup plan_Effective 15.06.21.xlsx", ExcelVersion.Version2013);


                Spire.Xls.Worksheet sheet = book.Worksheets[0];

                ExcelFont fontItalic = book.CreateFont();
                fontItalic.IsItalic = true;
                fontItalic.Size = 10;
                fontItalic.FontName = "Arial";


                RichText richText = sheet.Range["C3"].RichText;
                richText.Text = PlanFileModel.department_name + "\n" + PlanFileModel.department_name_en;
                richText.SetFont(PlanFileModel.department_name.Length, richText.Text.Length - 1, fontItalic);
                sheet.Range["E5"].Value = PlanFileModel.year.ToString();

                var equiments_list = PlanFileModel.equipments.Select(e => e.equipment_id).ToList();

                var equipments = _context.EquipmentModel.Where(d => equiments_list.Contains(d.id)).OrderByDescending(d => d.code).ToList();

                var row_c = equipments.Count;

                DateTime firstDay = new DateTime((int)PlanFileModel.year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime lastDay = new DateTime((int)PlanFileModel.year, 12, 31, 0, 0, 0, DateTimeKind.Utc);


                foreach (var equip in equipments)
                {
                    Ical.Net.Calendar iCalendar = new Ical.Net.Calendar();
                    var plans = _context.ScheduleModel.Where(d => d.deleted_at == null && d.plan_id == PlanFileModel.plan_id && d.equipment_id == equip.id).ToList();
                    foreach (var plan in plans)
                    {
                        var start_time = (DateTimeOffset)plan.start_time + new TimeSpan(7, 0, 0);
                        var end_time = (DateTimeOffset)plan.end_time + new TimeSpan(7, 0, 0);
                        var date_plan = (DateTime)plan.date_plan;
                        var e = new MyCalendar
                        {
                            plan_date = date_plan,
                            Start = new CalDateTime(start_time.DateTime, "+07:00"),
                            End = new CalDateTime(end_time.DateTime, "+07:00"),
                            IsAllDay = false
                        };
                        if (plan.recurrenceRule != null && plan.recurrenceRule != "") /// recurrence
                        {

                            if (plan.recurrenceException != null && plan.recurrenceException != "")
                            {
                                var exceptionDateList = new PeriodList();
                                var explode = plan.recurrenceException.Split(",");
                                foreach (var exp in explode)
                                {
                                    DateTime expdate = DateTime.ParseExact(exp, "yyyyMMddTHHmmssZ",
                                               System.Globalization.CultureInfo.InvariantCulture);
                                    exceptionDateList.Add(new Period(new CalDateTime(expdate)));
                                }
                                e.ExceptionDates.Add(exceptionDateList);
                            }
                            var rrule = new RecurrencePattern(plan.recurrenceRule);
                            e.RecurrenceRules.Add(rrule);

                        }
                        iCalendar.Events.Add(e);
                    }

                    var occurrences = iCalendar.GetOccurrences(firstDay, lastDay);

                    sheet.InsertRow(8);
                    sheet.Copy(sheet.Range["A9:AB9"], sheet.Range["A8:AB8"], true);
                    var row = sheet.Rows[8];
                    row.Cells[0].Value = row_c.ToString();
                    richText = row.Cells[1].RichText;
                    richText.Text = equip.name + "\n" + equip.name_en;
                    richText.SetFont(equip.name.Length, richText.Text.Length - 1, fontItalic);
                    row.Cells[2].Value = equip.code;
                    foreach (var occurrence in occurrences)
                    {
                        DateTime d_plan;
                        DateTime d_real;
                        var eventc = occurrence.Source as MyCalendar;
                        if (eventc.RecurrenceRules.Count > 0)
                        {
                            d_plan = occurrence.Period.StartTime.Date;
                        }
                        else
                        {
                            d_plan = (DateTime)eventc.plan_date;
                        }
                        d_real = occurrence.Period.StartTime.Date;
                        int month = d_plan.Month;
                        row.Cells[(month * 2) + 2].DateTimeValue = d_plan;
                        row.Cells[(month * 2) + 2].Style.NumberFormat = "dd";
                        if (d_real < DateTime.Now)
                        {
                            row.Cells[month + 4].DateTimeValue = d_real;
                            if (d_real.Month != month)
                            {
                                row.Cells[(month * 2) + 3].Style.NumberFormat = "dd/mm";
                            }
                            else
                            {
                                row.Cells[(month * 2) + 3].Style.NumberFormat = "dd";
                            }
                        }

                    }
                    row_c--;
                }
                sheet.DeleteRow(8);
                var file = "/private/excel/plan/010061.05_01 - Master data backup plan_Effective 15.06.21(" + PlanFileModel.created_at.ToString("yyyy-MM-dd") + ")_" + DateTimeOffset.Now.ToUnixTimeSeconds() + ".xlsx";
                book.SaveToFile("." + file, ExcelVersion.Version2013);
                PlanFileModel.file_url = file;
                _context.Update(PlanFileModel);
                _context.SaveChanges();
            }
            else if (PlanFileModel.plan_id == 2)
            ///KẾ hoạch thời gian
            {
                // Khởi tạo workbook để đọc
                Spire.Xls.Workbook book = new Spire.Xls.Workbook();
                book.LoadFromFile("./private/excel/template/160029.05_01 - System time checking plan_Effec 01.06.20.xlsx", ExcelVersion.Version2013);


                Spire.Xls.Worksheet sheet = book.Worksheets[0];

                ExcelFont fontItalic = book.CreateFont();
                fontItalic.IsItalic = true;
                fontItalic.Size = 10;
                fontItalic.FontName = "Arial";


                RichText richText = sheet.Range["C3"].RichText;
                richText.Text = PlanFileModel.department_name + "\n" + PlanFileModel.department_name_en;
                richText.SetFont(PlanFileModel.department_name.Length, richText.Text.Length - 1, fontItalic);
                sheet.Range["E5"].Value = PlanFileModel.year.ToString();

                var equiments_list = PlanFileModel.equipments.Select(e => e.equipment_id).ToList();

                var equipments = _context.EquipmentModel.Where(d => equiments_list.Contains(d.id)).OrderByDescending(d => d.code).ToList();

                var row_c = equipments.Count;

                DateTime firstDay = new DateTime((int)PlanFileModel.year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime lastDay = new DateTime((int)PlanFileModel.year, 12, 31, 0, 0, 0, DateTimeKind.Utc);



                foreach (var equip in equipments)
                {
                    Ical.Net.Calendar iCalendar = new Ical.Net.Calendar();
                    var plans = _context.ScheduleModel.Where(d => d.deleted_at == null && d.plan_id == PlanFileModel.plan_id && d.equipment_id == equip.id).ToList();
                    foreach (var plan in plans)
                    {
                        var start_time = (DateTimeOffset)plan.start_time + new TimeSpan(7, 0, 0);
                        var end_time = (DateTimeOffset)plan.end_time + new TimeSpan(7, 0, 0);
                        var date_plan = (DateTime)plan.date_plan;
                        var e = new MyCalendar
                        {
                            plan_date = date_plan,
                            Start = new CalDateTime(start_time.DateTime, "+07:00"),
                            End = new CalDateTime(end_time.DateTime, "+07:00"),
                            IsAllDay = false
                        };
                        if (plan.recurrenceRule != null && plan.recurrenceRule != "") /// recurrence
                        {

                            if (plan.recurrenceException != null && plan.recurrenceException != "")
                            {
                                var exceptionDateList = new PeriodList();
                                var explode = plan.recurrenceException.Split(",");
                                foreach (var exp in explode)
                                {
                                    DateTime expdate = DateTime.ParseExact(exp, "yyyyMMddTHHmmssZ",
                                               System.Globalization.CultureInfo.InvariantCulture);
                                    exceptionDateList.Add(new Period(new CalDateTime(expdate)));
                                }
                                e.ExceptionDates.Add(exceptionDateList);
                            }
                            var rrule = new RecurrencePattern(plan.recurrenceRule);
                            e.RecurrenceRules.Add(rrule);

                        }
                        iCalendar.Events.Add(e);
                    }

                    var occurrences = iCalendar.GetOccurrences(firstDay, lastDay);

                    sheet.InsertRow(7);
                    sheet.Copy(sheet.Range["A8:P8"], sheet.Range["A7:P7"], true);
                    var row = sheet.Rows[7];
                    row.Cells[0].Value = row_c.ToString();
                    richText = row.Cells[1].RichText;
                    richText.Text = equip.name + "\n" + equip.name_en;
                    richText.SetFont(equip.name.Length, richText.Text.Length - 1, fontItalic);


                    row.Cells[2].Value = equip.code;
                    foreach (var occurrence in occurrences)
                    {
                        DateTime d_plan;
                        DateTime d_real;
                        var eventc = occurrence.Source as MyCalendar;
                        if (eventc.RecurrenceRules.Count > 0)
                        {
                            d_plan = occurrence.Period.StartTime.Date;
                        }
                        else
                        {
                            d_plan = (DateTime)eventc.plan_date;
                        }
                        //d_real = occurrence.Period.StartTime.Date;
                        int month = d_plan.Month;
                        row.Cells[month + 3].DateTimeValue = d_plan;
                        row.Cells[month + 3].Style.NumberFormat = "dd";
                    }
                    row_c--;
                }
                sheet.DeleteRow(7);
                var file = "/private/excel/plan/010061.05_01 - Master data backup plan_Effective 15.06.21(" + PlanFileModel.created_at.ToString("yyyy-MM-dd") + ")_" + DateTimeOffset.Now.ToUnixTimeSeconds() + ".xlsx";
                book.SaveToFile("." + file, ExcelVersion.Version2013);
                PlanFileModel.file_url = file;
                _context.Update(PlanFileModel);
                _context.SaveChanges();
            }


        }

        // GET: Admin/PlanFile/Create
        public IActionResult Duplicate(int id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var PlanFileModel = _context.PlanFileModel.Include(d => d.equipments)
                .Where(d => d.id == id).FirstOrDefault();

            if (PlanFileModel == null)
            {
                return NotFound();
            }

            ViewData["equipments"] = _context.EquipmentModel.Where(d => d.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.id.ToString(),
                Text = a.code + " - " + a.name
            }).ToList();
            return View(PlanFileModel);
        }
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.PlanFileModel == null)
            {
                return Problem("Entity set 'ItContext.PlanFileModel'  is null.");
            }
            var PlanFileModel = await _context.PlanFileModel.FindAsync(id);
            if (PlanFileModel != null)
            {
                PlanFileModel.deleted_at = DateTime.Now;
                _context.PlanFileModel.Update(PlanFileModel);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { id = PlanFileModel.plan_id });
        }

        [HttpPost]
        public async Task<JsonResult> Table()
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var plan_id_string = Request.Form["plan_id"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            int plan_id = plan_id_string != null ? Convert.ToInt32(plan_id_string) : 0;
            var customerData = (from tempcustomer in _context.PlanFileModel.Where(u => u.deleted_at == null && u.plan_id == plan_id) select tempcustomer);
            int recordsTotal = customerData.Count();
            customerData = customerData.Where(m => m.deleted_at == null);
            if (!string.IsNullOrEmpty(searchValue))
            {
                customerData = customerData.Where(m => m.department_name.Contains(searchValue));
            }
            int recordsFiltered = customerData.Count();
            var datapost = customerData.Skip(skip).Take(pageSize).OrderByDescending(d => d.id).ToList();
            var data = new ArrayList();
            foreach (var record in datapost)
            {
                var file = record.file_url;
                if (file != null)
                {
                    file = "<a href='" + file + "' download class='btn btn-secondary btn-xs'><i class='fas fa-file-excel mr-2'></i>Download</a>";
                }
                var data1 = new
                {
                    action = "<div class='btn-group'><a href='/admin/" + _type + "/duplicate/" + record.id + "' class='btn btn-primary btn-sm'><i class='fas fa-copy'></i></a><a href='/admin/" + _type + "/delete/" + record.id + "' class='btn btn-danger btn-sm' title='Xóa?' data-type='confirm'>'"
                        + "<i class='fas fa-trash-alt'>"
                        + "</i>"
                        + "</a></div>",
                    id = "<a href='#'>" + record.id + "</a>",
                    name = record.department_name,
                    created_at = record.created_at.ToString("yyyy-MM-dd"),
                    year = record.year,
                    file = file
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }
    }
}
