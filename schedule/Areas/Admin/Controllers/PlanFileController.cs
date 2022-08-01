
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using it.Areas.Admin.Models;
using it.Data;
using System.Collections;
using Microsoft.AspNetCore.Identity;
using System.Text;
using Spire.Xls;
using Ical.Net.DataTypes;
using Ical.Net.CalendarComponents;
using System.Text.RegularExpressions;
using System.Drawing;

namespace it.Areas.Admin.Controllers
{
    public class MyCalendar : CalendarEvent
    {
        public DateTime? date { get; set; }
        public DateTime? plan_date { get; set; }
        public string? plan_type { get; set; }
        public int? month { get; set; }

    }
    public class MyMonth
    {
        public string? plan_type { get; set; }
        public string? plan_date { get; set; }
        public int month { get; set; }

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

                var equipments = _context.EquipmentModel.Where(d => equiments_list.Contains(d.id)).OrderBy(d => d.code).ToList();

                var row_c = 1;

                DateTime firstDay = new DateTime((int)PlanFileModel.year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime lastDay = new DateTime((int)PlanFileModel.year, 12, 31, 0, 0, 0, DateTimeKind.Utc);

                var cur_r = 7;
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
                    if (occurrences != null && occurrences.Count == 0)
                    {
                        continue;
                    }
                    sheet.InsertRow(cur_r + row_c);
                    sheet.Copy(sheet.Range["A" + (cur_r + row_c + 1) + ":AB" + (cur_r + row_c + 1)], sheet.Range["A" + (cur_r + row_c) + ":AB" + (cur_r + row_c)], true);
                    var row = sheet.Rows[cur_r + row_c - 1];
                    row.Cells[0].Value = row_c.ToString();
                    richText = row.Cells[1].RichText;
                    richText.Text = equip.name.Trim() + "\n" + equip.name_en.Trim();
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
                            row.Cells[(month * 2) + 3].DateTimeValue = d_real;
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
                    row_c++;
                }
                sheet.DeleteRow(cur_r + row_c);
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

                var equipments = _context.EquipmentModel.Where(d => equiments_list.Contains(d.id)).OrderBy(d => d.code).ToList();

                var row_c = 1;

                DateTime firstDay = new DateTime((int)PlanFileModel.year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime lastDay = new DateTime((int)PlanFileModel.year, 12, 31, 0, 0, 0, DateTimeKind.Utc);


                var cur_r = 6;


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
                    if (occurrences != null && occurrences.Count == 0)
                    {
                        continue;
                    }
                    sheet.InsertRow(cur_r + row_c);
                    sheet.Copy(sheet.Range["A" + (cur_r + row_c + 1) + ":P" + (cur_r + row_c + 1)], sheet.Range["A" + (cur_r + row_c) + ":P" + (cur_r + row_c)], true);
                    var row = sheet.Rows[cur_r + row_c - 1];
                    row.Cells[0].Value = row_c.ToString();
                    richText = row.Cells[1].RichText;
                    richText.Text = equip.name.Trim() + "\n" + equip.name_en.Trim();
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
                    row_c++;
                }
                sheet.DeleteRow(cur_r + row_c);
                var file = "/private/excel/plan/010061.05_01 - Master data backup plan_Effective 15.06.21(" + PlanFileModel.created_at.ToString("yyyy-MM-dd") + ")_" + DateTimeOffset.Now.ToUnixTimeSeconds() + ".xlsx";
                book.SaveToFile("." + file, ExcelVersion.Version2013);
                PlanFileModel.file_url = file;
                _context.Update(PlanFileModel);
                _context.SaveChanges();
            }
            else if (PlanFileModel.plan_id == 3)
            ///KẾ hoạch thời gian
            {
                // Khởi tạo workbook để đọc
                Spire.Xls.Workbook book = new Spire.Xls.Workbook();
                book.LoadFromFile("./private/excel/template/070001.11_08 - Lịch bảo trì thiết bị.xlsx", ExcelVersion.Version2013);


                Spire.Xls.Worksheet sheet = book.Worksheets[0];

                ExcelFont fontItalic1 = book.CreateFont();
                fontItalic1.IsItalic = true;
                fontItalic1.Size = 14;
                fontItalic1.FontName = "Arial";
                fontItalic1.IsBold = true;

                RichText richText = sheet.Range["A2"].RichText;
                var name_1 = "LỊCH BẢO TRÌ ĐỊNH KỲ CHO NĂM";
                var name_2 = "PREVENTIVE MAINTENANCE SCHEDULE";
                var name_3 = "NĂM";
                var name_4 = "YEAR";
                richText.Text = name_1 + "\n" + name_2 + "\n" + name_3 + "/" + name_4 + ": " + PlanFileModel.year.ToString();
                richText.SetFont(name_1.Length, name_1.Length + name_2.Length + 1, fontItalic1);
                richText.SetFont(name_1.Length + name_2.Length + name_3.Length + 3, name_1.Length + name_2.Length + name_3.Length + 3 + name_4.Length, fontItalic1);

                ExcelFont fontItalic = book.CreateFont();
                fontItalic.IsItalic = true;
                fontItalic.Size = 10;
                fontItalic.FontName = "Arial";

                richText = sheet.Range["C3"].RichText;
                richText.Text = PlanFileModel.department_name + "\n" + PlanFileModel.department_name_en;
                richText.SetFont(PlanFileModel.department_name.Length, richText.Text.Length - 1, fontItalic);

                fontItalic.IsBold = false;
                //sheet.Range["E5"].Value = PlanFileModel.year.ToString();

                var equiments_list = PlanFileModel.equipments.Select(e => e.equipment_id).ToList();

                var equipments = _context.EquipmentModel.Where(d => equiments_list.Contains(d.id)).OrderBy(d => d.code).ToList();

                var row_c = 1;

                DateTime firstDay = new DateTime((int)PlanFileModel.year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime lastDay = new DateTime((int)PlanFileModel.year, 12, 31, 0, 0, 0, DateTimeKind.Utc);

                var cur_r = 6;

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
                            plan_type = plan.type_plan,
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
                    if (occurrences != null && occurrences.Count == 0)
                    {
                        continue;
                    }
                    sheet.InsertRow(cur_r + row_c + 1);
                    sheet.Copy(sheet.Range["A" + (cur_r + row_c) + ":AB" + (cur_r + row_c)], sheet.Range["A" + (cur_r + row_c + 1) + ":AB" + (cur_r + row_c + 1)], true);
                    var row = sheet.Rows[cur_r + row_c - 1];
                    row.Cells[0].Value = row_c.ToString();
                    richText = row.Cells[1].RichText;
                    richText.Text = equip.name + "\n" + equip.name_en;
                    richText.SetFont(equip.name.Length, richText.Text.Length - 1, fontItalic);


                    row.Cells[2].Value = equip.code;
                    row.Cells[3].Value = equip.sop_maintenance;
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
                        var plan_type = eventc.plan_type;
                        //d_real = occurrence.Period.StartTime.Date;
                        int month = d_plan.Month;
                        row.Cells[(month * 2) + 2].DateTimeValue = d_plan;
                        row.Cells[(month * 2) + 2].Style.NumberFormat = "dd";
                        row.Cells[(month * 2) + 2].Style.Color = Color.White;
                        row.Cells[(month * 2) + 3].Value = plan_type;
                        row.Cells[(month * 2) + 3].Style.Color = Color.White;
                    }
                    row_c++;
                }
                sheet.DeleteRow(cur_r + row_c);
                var file = "/private/excel/plan/070001.11_08 - Lịch bảo trì thiết bị(" + PlanFileModel.created_at.ToString("yyyy-MM-dd") + ")_" + DateTimeOffset.Now.ToUnixTimeSeconds() + ".xlsx";
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

        public async Task<IActionResult> import(string department)
        {
            return Ok();

            // Khởi tạo workbook để đọc
            Spire.Xls.Workbook workbook = new Workbook();
            workbook.LoadFromFile("private/excel/template/bao tri/" + department + ".xlsx", ExcelVersion.Version2013);
            Console.OutputEncoding = Encoding.UTF8; // để xuất ra console tv có dấu
            Console.WriteLine("Thông tin từ file Excel");

            Spire.Xls.Worksheet sheet = workbook.Worksheets[0];

            // đọc sheet này bắt đầu từ row 2 (0, 1 bỏ vì tiêu đề)

            var lastrow = sheet.LastRow;
            // nếu vẫn chưa gặp end thì vẫn lấy data
            Console.WriteLine(lastrow);
            for (int rowIndex = 6; rowIndex < lastrow; rowIndex++)
            {
                // lấy row hiện tại
                var nowRow = sheet.Rows[rowIndex];
                if (nowRow == null)
                    continue;
                var cellName = nowRow.Cells[1];
                var cellCode = nowRow.Cells[2];
                var cellSOP = nowRow.Cells[3];
                if (cellName == null || cellCode == null)
                {
                    continue;
                }
                Dictionary<int, MyMonth> dict = new Dictionary<int, MyMonth>();
                for (var thang = 1; thang <= 12; thang++)
                {
                    dict[thang] = new MyMonth
                    {
                        plan_date = nowRow.Cells[(thang * 2) + 2].Value,
                        plan_type = nowRow.Cells[(thang * 2) + 3].Value,
                        month = thang,
                    };
                }
                var name = cellName.Value;
                var code = cellCode.Value;
                var SOP = cellSOP.Value;
                if (code == "" || name == "")
                    continue;
                // vì ta dùng 3 cột A, B, C => data của ta sẽ như sau
                //int numcount = nowRow.Cells.Count;
                //for(int y = 0;y<numcount - 1 ;y++)

                string pattern = "/\r\n|\n|\r/";
                Regex rgx = new Regex(pattern);
                var export_name = rgx.Split(name);
                var name_vn = name;
                var name_en = "";
                if (export_name.Length > 1)
                {
                    name_vn = export_name[0].Trim();
                    name_en = export_name[1].Trim();
                }
                // Xuất ra thông tin lên màn hình
                Console.WriteLine("MS: {0} ", code);
                Console.WriteLine("name: {0} ", name_vn);
                Console.WriteLine("SOP: {0} ", SOP);


                EquipmentModel EquipmentModel = _context.EquipmentModel.Where(d => d.code == code).FirstOrDefault();
                if (EquipmentModel != null)
                {
                    EquipmentModel.name = name_vn;
                    EquipmentModel.name_en = name_en;
                    EquipmentModel.sop_maintenance = SOP;
                    _context.Update(EquipmentModel);
                }
                else
                {
                    EquipmentModel = new EquipmentModel { code = code, name = name_vn, name_en = name_en, sop_maintenance = SOP, created_at = DateTime.Now };
                    _context.Add(EquipmentModel);
                }
                _context.SaveChanges();

                foreach (var item in dict)
                {
                    var key = item.Key;
                    MyMonth MyMonth = item.Value;
                    if (MyMonth.plan_date == null || MyMonth.plan_date == "")
                        continue;
                    DateTime date_plan = new DateTime(2022, MyMonth.month, Int32.Parse(MyMonth.plan_date));
                    DateTimeOffset start_time = new DateTimeOffset(date_plan, new TimeSpan(7, 0, 0));
                    DateTimeOffset end_time = new DateTimeOffset(date_plan.AddHours(1), new TimeSpan(7, 0, 0));
                    ScheduleModel ScheduleModel = new ScheduleModel
                    {
                        created_at = DateTime.Now,
                        title = "LỊCH BẢO TRÌ ĐỊNH KỲ",
                        type_plan = MyMonth.plan_type,
                        date_plan = date_plan,
                        equipment_id = EquipmentModel.id,
                        plan_id = 3,
                        department_id = 1016,
                        start_time = start_time,
                        end_time = end_time,
                    };
                    _context.Add(ScheduleModel);
                }
                _context.SaveChanges();
            }
            return Ok(1);
        }


        public async Task<IActionResult> importtime(string department)
        {
            return Ok();

            // Khởi tạo workbook để đọc
            Spire.Xls.Workbook workbook = new Workbook();
            workbook.LoadFromFile("private/excel/template/time/" + department + ".xlsx", ExcelVersion.Version2013);
            Console.OutputEncoding = Encoding.UTF8; // để xuất ra console tv có dấu
            Console.WriteLine("Thông tin từ file Excel");

            Spire.Xls.Worksheet sheet = workbook.Worksheets[0];

            // đọc sheet này bắt đầu từ row 2 (0, 1 bỏ vì tiêu đề)

            var lastrow = sheet.LastRow;
            // nếu vẫn chưa gặp end thì vẫn lấy data
            Console.WriteLine(lastrow);
            for (int rowIndex = 6; rowIndex < lastrow; rowIndex++)
            {
                // lấy row hiện tại
                var nowRow = sheet.Rows[rowIndex];
                if (nowRow == null)
                    continue;
                var cellName = nowRow.Cells[1];
                var cellCode = nowRow.Cells[2];
                if (cellName == null || cellCode == null)
                {
                    continue;
                }
                Dictionary<int, MyMonth> dict = new Dictionary<int, MyMonth>();
                for (var thang = 1; thang <= 12; thang++)
                {
                    dict[thang] = new MyMonth
                    {
                        plan_date = nowRow.Cells[thang + 3].Value,
                        plan_type = "M",
                        month = thang,
                    };
                }
                var name = cellName.Value;
                var code = cellCode.Value;
                if (code == "" || name == "")
                    continue;
                // vì ta dùng 3 cột A, B, C => data của ta sẽ như sau
                //int numcount = nowRow.Cells.Count;
                //for(int y = 0;y<numcount - 1 ;y++)

                string pattern = "/\r\n|\n|\r/";
                Regex rgx = new Regex(pattern);
                var export_name = rgx.Split(name);
                var name_vn = name;
                var name_en = "";
                if (export_name.Length > 1)
                {
                    name_vn = export_name[0].Trim();
                    name_en = export_name[1].Trim();
                }
                // Xuất ra thông tin lên màn hình
                Console.WriteLine("MS: {0} ", code);
                Console.WriteLine("name: {0} ", name_vn);


                EquipmentModel EquipmentModel = _context.EquipmentModel.Where(d => d.code == code).FirstOrDefault();
                if (EquipmentModel == null)
                {
                    continue;
                }

                foreach (var item in dict)
                {
                    var key = item.Key;
                    MyMonth MyMonth = item.Value;
                    if (MyMonth.plan_date == null || MyMonth.plan_date == "")
                        continue;
                    DateTime date_plan = new DateTime(2022, MyMonth.month, Int32.Parse(MyMonth.plan_date));
                    DateTimeOffset start_time = new DateTimeOffset(date_plan, new TimeSpan(7, 0, 0));
                    DateTimeOffset end_time = new DateTimeOffset(date_plan.AddHours(1), new TimeSpan(7, 0, 0));
                    ScheduleModel ScheduleModel = new ScheduleModel
                    {
                        created_at = DateTime.Now,
                        title = "Kiểm tra thời gian",
                        type_plan = MyMonth.plan_type,
                        date_plan = date_plan,
                        equipment_id = EquipmentModel.id,
                        plan_id = 2,
                        department_id = 2,
                        start_time = start_time,
                        end_time = end_time,
                    };
                    _context.Add(ScheduleModel);
                }
                _context.SaveChanges();
            }
            return Ok(1);
        }

        public async Task<IActionResult> importbackup(string department)
        {
            return Ok();

            // Khởi tạo workbook để đọc
            Spire.Xls.Workbook workbook = new Workbook();
            workbook.LoadFromFile("private/excel/template/backup/" + department + ".xlsx", ExcelVersion.Version2013);
            Console.OutputEncoding = Encoding.UTF8; // để xuất ra console tv có dấu
            Console.WriteLine("Thông tin từ file Excel");

            Spire.Xls.Worksheet sheet = workbook.Worksheets[0];

            // đọc sheet này bắt đầu từ row 2 (0, 1 bỏ vì tiêu đề)

            var lastrow = sheet.LastRow;
            // nếu vẫn chưa gặp end thì vẫn lấy data
            Console.WriteLine(lastrow);
            for (int rowIndex = 7; rowIndex < lastrow; rowIndex++)
            {
                // lấy row hiện tại
                var nowRow = sheet.Rows[rowIndex];
                if (nowRow == null)
                    continue;
                var cellName = nowRow.Cells[1];
                var cellCode = nowRow.Cells[2];
                if (cellName == null || cellCode == null)
                {
                    continue;
                }
                Dictionary<int, MyMonth> dict = new Dictionary<int, MyMonth>();
                for (var thang = 1; thang <= 12; thang++)
                {
                    dict[thang] = new MyMonth
                    {
                        plan_date = nowRow.Cells[(thang * 2) + 2].Value,
                        plan_type = "M",
                        month = thang,
                    };
                }
                var name = cellName.Value;
                var code = cellCode.Value;
                if (code == "" || name == "")
                    continue;
                // vì ta dùng 3 cột A, B, C => data của ta sẽ như sau
                //int numcount = nowRow.Cells.Count;
                //for(int y = 0;y<numcount - 1 ;y++)

                string pattern = "/\r\n|\n|\r/";
                Regex rgx = new Regex(pattern);
                var export_name = rgx.Split(name);
                var name_vn = name;
                var name_en = "";
                if (export_name.Length > 1)
                {
                    name_vn = export_name[0].Trim();
                    name_en = export_name[1].Trim();
                }
                // Xuất ra thông tin lên màn hình
                Console.WriteLine("MS: {0} ", code);
                Console.WriteLine("name: {0} ", name_vn);


                EquipmentModel EquipmentModel = _context.EquipmentModel.Where(d => d.code == code).FirstOrDefault();
                if (EquipmentModel == null)
                {
                    continue;
                }

                foreach (var item in dict)
                {
                    var key = item.Key;
                    MyMonth MyMonth = item.Value;
                    if (MyMonth.plan_date == null || MyMonth.plan_date == "")
                        continue;
                    DateTime date_plan = new DateTime(2022, MyMonth.month, Int32.Parse(MyMonth.plan_date));
                    DateTimeOffset start_time = new DateTimeOffset(date_plan, new TimeSpan(7, 0, 0));
                    DateTimeOffset end_time = new DateTimeOffset(date_plan.AddHours(1), new TimeSpan(7, 0, 0));
                    ScheduleModel ScheduleModel = new ScheduleModel
                    {
                        created_at = DateTime.Now,
                        title = "SAO LƯU DỮ LIỆU",
                        type_plan = MyMonth.plan_type,
                        date_plan = date_plan,
                        equipment_id = EquipmentModel.id,
                        plan_id = 1,
                        department_id = 2,
                        start_time = start_time,
                        end_time = end_time,
                    };
                    _context.Add(ScheduleModel);
                }
                _context.SaveChanges();
            }
            return Ok(1);
        }
    }
}
