using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EAHSA.Models;
using ClosedXML.Excel;
using System.IO;

namespace EAHSA.Pages.Admin
{
    public class AttendanceModel : PageModel
    {
        private readonly SupabaseService _supabase;
        private readonly IWebHostEnvironment _environment;

        public AttendanceModel(SupabaseService supabase, IWebHostEnvironment environment)
        {
            _supabase = supabase;
            _environment = environment;
        }

        public List<Attendance> AttendanceList { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? grade { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? date { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Section { get; set; }

        public string Status { get; set; } = "";
        public string GradeLevel { get; set; } = "";
        public string Gender { get; set; } = "";
        public string Name { get; set; } = "";
        public string LRN { get; set; } = "";
        public DateTime Date { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; }
        private const int PageSize = 10;

        public List<string> GradeLevels { get; set; } = new();
        public List<string> Sections { get; set; } = new();

        // ====================================
        // EXPORT EXCEL
        // ====================================
        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            var response = await _supabase.Client
                .From<Attendance>()
                .Get();

            var records = response.Models ?? new List<Attendance>();

            if (!string.IsNullOrEmpty(grade))
                records = records.Where(x => x.GradeLevel!.ToString() == grade).ToList();

            if (!string.IsNullOrEmpty(Section))
                records = records.Where(x => x.Section == Section).ToList();

            var sectionName = string.IsNullOrEmpty(Section) ? "AllSections" : Section;
            var gradeLevel = records.FirstOrDefault()?.GradeLevel ?? "N/A";
            var today = DateTime.Today.ToString("MMMM dd, yyyy");

            var boys = records.Where(x => (x.Gender ?? "").ToLower() == "boy").OrderBy(x => x.Name).ToList();
            var girls = records.Where(x => (x.Gender ?? "").ToLower() == "girl").OrderBy(x => x.Name).ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Attendance");

            ws.Cell("A1").Value = "ESTEBAN ABADA HIGH SCHOOL";
            ws.Cell("A2").Value = "ATTENDANCE REPORT";

            ws.Range("A1:D1").Merge();
            ws.Range("A2:D2").Merge();

            ws.Range("A1:A2").Style.Font.Bold = true;
            ws.Range("A1:A2").Style.Font.FontSize = 18;
            ws.Range("A1:A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("A4").Value = "Grade Level:";
            ws.Cell("B4").Value = gradeLevel;
            ws.Cell("A5").Value = "Section:";
            ws.Cell("B5").Value = sectionName;
            ws.Cell("A6").Value = "Date:";
            ws.Cell("B6").Value = today;

            int row = 8;

            ws.Cell(row, 1).Value = "BOYS";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 14;
            row++;

            ws.Cell(row, 1).Value = "#";
            ws.Cell(row, 2).Value = "Name";
            ws.Cell(row, 3).Value = "LRN";
            ws.Cell(row, 4).Value = "Status";

            var headerRange = ws.Range(row, 1, row, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;
            int count = 1;

            foreach (var b in boys)
            {
                ws.Cell(row, 1).Value = count++;
                ws.Cell(row, 2).Value = b.Name;
                ws.Cell(row, 3).Value = b.LRN;
                ws.Cell(row, 4).Value = b.Status;
                row++;
            }

            row += 2;

            ws.Cell(row, 1).Value = "GIRLS";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 14;
            row++;

            ws.Cell(row, 1).Value = "#";
            ws.Cell(row, 2).Value = "Name";
            ws.Cell(row, 3).Value = "LRN";
            ws.Cell(row, 4).Value = "Status";

            headerRange = ws.Range(row, 1, row, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;
            count = 1;

            foreach (var g in girls)
            {
                ws.Cell(row, 1).Value = count++;
                ws.Cell(row, 2).Value = g.Name;
                ws.Cell(row, 3).Value = g.LRN;
                ws.Cell(row, 4).Value = g.Status;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"Attendance_{sectionName}_{DateTime.Today:yyyy-MM-dd}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        // ====================================
        // LOAD PAGE
        // ====================================
        public async Task OnGetAsync(DateTime? date)
        {
            var today = DateTime.Today;

// check kung may record na today
var check = await _supabase.Client
    .From<RecordAttendance>()
    .Get();
    
    var todayRecords = check.Models
        .Where(x => x.Date.Date == today)
        .ToList();

    // ✅ FIX: kung WALANG record TODAY
    if (!todayRecords.Any())
    {
        await SaveDailyAttendanceAsync();  // save current status (present/absent)
        await ResetAttendanceAsync();      // reset for next day
    }

    
if (date.HasValue)
{
    var start = date.Value.Date;
    var end = start.AddDays(1);

    var history = await _supabase.Client
        .From<RecordAttendance>()
        .Get();

foreach (var x in history.Models)
{
    Console.WriteLine($"DB: {x.Date} | DATE ONLY: {x.Date.Date}");
}

    var filtered = history.Models
        .Where(x => x.Date.Date == start)
        .AsQueryable();

    if (!string.IsNullOrEmpty(grade))
    {
        filtered = filtered.Where(x =>
        !string.IsNullOrEmpty(x.GradeLevel) &&
        x.GradeLevel.Trim().ToLower() == grade!.Trim().ToLower()
);
    }

    if (!string.IsNullOrEmpty(Section))
    {
        filtered = filtered.Where(x => x.Section == Section);
    }

    AttendanceList = filtered
        .Select(x => new Attendance
        {
            GradeLevel = x.GradeLevel,
            Section = x.Section,
            Gender = x.Gender,
            Name = x.Name,
            LRN = x.LRN,
            Status = x.Status,
            Photo = x.Photo
        })
        .ToList();

    return;
}

var response = await _supabase.Client
    .From<Attendance>()
    .Get();

            var records = response.Models ?? new List<Attendance>();

            // RESET STATUS EVERY NEW DAY
            foreach (var student in records)
            {
                if (student.Date.Date < today)
                {
                    student.Status = "Absent";

                    await _supabase.Client
                        .From<Attendance>()
                        .Where(x => x.Id == student.Id)
                        .Update(student);
                }
            }

            if (!string.IsNullOrEmpty(grade))
                        records = records.Where(x =>
            !string.IsNullOrEmpty(x.GradeLevel) &&
            x.GradeLevel.Trim().ToLower() == grade!.Trim().ToLower()
        ).ToList();

            if (!string.IsNullOrEmpty(Section))
                records = records.Where(x => x.Section == Section).ToList();

            GradeLevels = records
                .Where(x => !string.IsNullOrEmpty(x.GradeLevel))
                .Select(x => x.GradeLevel!)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            Sections = records
                .Where(x => !string.IsNullOrEmpty(x.Section))
                .Select(x => x.Section!)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var totalRecords = records.Count;
            TotalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);

            AttendanceList = records
                .OrderByDescending(x => x.Id)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        // ====================================
        // ADD STUDENT WITH PHOTO
        // ====================================
        public async Task<IActionResult> OnPostAddAsync(
            string GradeLevel,
            string Section,
            string Gender,
            string Name,
            string LRN,
            string Status,
            IFormFile PhotoFile)
        {
            string photoPath = "";

            if (PhotoFile != null)
            {
                var folder = Path.Combine(_environment.WebRootPath, "studentphotos");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(PhotoFile.FileName);
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await PhotoFile.CopyToAsync(stream);
                }

                photoPath = "/studentphotos/" + fileName;
            }

            var newRecord = new Attendance
            {
                GradeLevel = GradeLevel,
                Section = Section,
                Gender = Gender,
                Name = Name,
                LRN = LRN,
                Status = Status,
                Photo = photoPath,
                Date = string.IsNullOrEmpty(Request.Form["Date"])
                ? DateTime.Today
                : DateTime.Parse(Request.Form["Date"]!)
            };

            await _supabase.Client
                .From<Attendance>()
                .Insert(newRecord);

            return RedirectToPage();
        }

        // ====================================
        // EDIT STUDENT + REPLACE PHOTO
        // ====================================
        public async Task<IActionResult> OnPostEditAsync(Attendance updated, IFormFile? PhotoFile)
        {
            if (PhotoFile != null)
            {
                var folder = Path.Combine(_environment.WebRootPath, "studentphotos");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(PhotoFile.FileName);
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await PhotoFile.CopyToAsync(stream);
                }

                updated.Photo = "/studentphotos/" + fileName;
            }

        var status = updated.Status ?? "Absent";
        var grade = updated.GradeLevel ?? "";
        var section = updated.Section ?? "";
        var gender = updated.Gender ?? "";
        var name = updated.Name ?? "";
        var lrn = updated.LRN ?? "";

        Console.WriteLine($"Status value: {status}");

    await _supabase.Client
        .From<Attendance>()
        .Where(x => x.Id == updated.Id)
        .Set(x => x.Status, status!)
        .Set(x => x.GradeLevel, grade!)
        .Set(x => x.Section, section!)
        .Set(x => x.Gender, gender!)
        .Set(x => x.Name, name!)
        .Set(x => x.LRN, lrn!)
        .Set(x => x.Date, DateTime.Now)
        .Update();

            return RedirectToPage();
        }

        // ====================================
        // DELETE
        // ====================================
        public async Task<IActionResult> OnPostDeleteAsync(int Id)
        {
            await _supabase.Client
                .From<Attendance>()
                .Where(x => x.Id == Id)
                .Delete();

            return RedirectToPage();
        }
private async Task SaveDailyAttendanceAsync()
{
    var today = DateTime.Today;

    var response = await _supabase.Client
        .From<Attendance>()
        .Get();

    var records = response.Models ?? new List<Attendance>();

    foreach (var student in records)
    {
        // 🔍 CHECK kung existing na
        var existing = await _supabase.Client
            .From<RecordAttendance>()
            .Get();

            var alreadyExists = existing.Models.Any(x =>
        x.LRN == student.LRN &&
        x.Date.Date == today
    );

    if (alreadyExists)
    {
        var updated = new RecordAttendance
        {
            Status = student.Status
        };

        await _supabase.Client
            .From<RecordAttendance>()
            .Where(x => x.LRN == student.LRN && x.Date.Date == today)
            .Update(updated);

        continue;
    }
    }
}
private async Task ResetAttendanceAsync()
{
    var response = await _supabase.Client
        .From<Attendance>()
        .Get();

    var records = response.Models ?? new List<Attendance>();

    foreach (var student in records)
    {
        student.Status = "Absent";

        await _supabase.Client
            .From<Attendance>()
            .Where(x => x.Id == student.Id)
            .Update(student);
    }
}

public async Task<IActionResult> OnGetFilterByDateAsync(DateTime date, string? section)
{
    var history = await _supabase.Client
        .From<RecordAttendance>()
        .Get();

    var records = history.Models ?? new List<RecordAttendance>();

    var start = date.Date;
    var end = start.AddDays(1);

    // ✅ FILTER BY DATE (SAFE)
    records = records
        .Where(x => x.Date.Date == date.Date)
        .ToList();

    // ✅ FILTER BY SECTION
    if (!string.IsNullOrEmpty(section))
    {
        records = records.Where(x => x.Section == section).ToList();
    }

    var result = records.Select(x => new
    {
        gradeLevel = x.GradeLevel,
        section = x.Section,
        gender = x.Gender,
        name = x.Name,
        lrn = x.LRN,
        status = x.Status,
        photo = string.IsNullOrEmpty(x.Photo)
        ? "/images/default.png"
        : (x.Photo.StartsWith("/studentphotos/")
            ? x.Photo
            : "/studentphotos/" + x.Photo)
    });

    return new JsonResult(result);
}


}
    }
