using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EAHSA.Models;
using ClosedXML.Excel;
using System.IO;

namespace EAHSA.Pages.Users
{
    public class AttendanceModel : PageModel
    {
        private readonly SupabaseService _supabase;
        private readonly IWebHostEnvironment _environment;

        public List<Attendance> AttendanceList { get; set; } = new();

        public List<string> Grades { get; set; } = new();
        public List<string> Sections { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SelectedGrade { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedSection { get; set; }

    public AttendanceModel(SupabaseService supabase, IWebHostEnvironment environment)
        {
            _supabase = supabase;
             _environment = environment;
        }

public async Task OnGetAsync()
{
    var records = await _supabase.GetAttendance();

    var today = DateTime.Today;

    // RESET TO ABSENT IF DATE IS NOT TODAY
    foreach (var student in records)
    {
        if (student.Date != today)
        {
            student.Status = "Absent";
        }
    }

    var allowedSections = HttpContext.Session.GetString("AllowedSections");

    List<string> sectionList;

    if (string.IsNullOrEmpty(allowedSections))
    {
        sectionList = records.Select(x => x.Section!).Distinct().ToList();
    }
    else
    {
        sectionList = allowedSections.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    // FILTER ONLY ALLOWED SECTIONS
    records = records
        .Where(x => x.Section != null && sectionList.Contains(x.Section))
        .ToList();

    // LOAD GRADES
    Grades = records
        .Select(x => x.GradeLevel!)
        .Distinct()
        .OrderBy(x => x)
        .ToList();

    if (string.IsNullOrEmpty(SelectedGrade))
        SelectedGrade = Grades.FirstOrDefault();

    // LOAD SECTIONS
    Sections = records
        .Where(x => x.GradeLevel == SelectedGrade)
        .Select(x => x.Section!)
        .Distinct()
        .ToList();

    if (string.IsNullOrEmpty(SelectedSection))
        SelectedSection = Sections.FirstOrDefault();

    // FINAL TABLE
    AttendanceList = records
        .Where(x =>
            (string.IsNullOrEmpty(SelectedGrade) || x.GradeLevel == SelectedGrade) &&
            (string.IsNullOrEmpty(SelectedSection) || x.Section == SelectedSection))
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
                Photo = photoPath
            };

            await _supabase.Client
                .From<Attendance>()
                .Insert(newRecord);

            return RedirectToPage();
        }

// ====================================
// EXPORT EXCEL (SAME AS ADMIN)
// ====================================

public async Task<IActionResult> OnGetExportExcelAsync()
{
    var records = await _supabase.GetAttendance();

    var allowedSections = HttpContext.Session.GetString("AllowedSections");

    List<string> sectionList;

    if (string.IsNullOrEmpty(allowedSections))
    {
        sectionList = records.Select(x => x.Section!).Distinct().ToList();
    }
    else
    {
        sectionList = allowedSections.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    // FILTER ALLOWED SECTIONS
    records = records
        .Where(x => x.Section != null && sectionList.Contains(x.Section))
        .ToList();

    // FILTER GRADE
    if (!string.IsNullOrEmpty(SelectedGrade))
        records = records.Where(x => x.GradeLevel == SelectedGrade).ToList();

    // FILTER SECTION
    if (!string.IsNullOrEmpty(SelectedSection))
        records = records.Where(x => x.Section == SelectedSection).ToList();

    var sectionName = SelectedSection ?? "AllSections";
    var gradeLevel = SelectedGrade ?? "N/A";
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

            await _supabase.Client
                .From<Attendance>()
                .Where(x => x.Id == updated.Id)
                .Update(updated);

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
    }
}