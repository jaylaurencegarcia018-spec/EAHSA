using Microsoft.AspNetCore.Mvc.RazorPages;
using EAHSA.Models;

namespace EAHSA.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly SupabaseService _supabase;

        public DashboardModel(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        public int TotalStudents { get; set; }
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }

        public List<string> ChartLabels { get; set; } = new();
        public List<int> ChartData { get; set; } = new();

        public List<RecentAbsence> RecentAbsences { get; set; } = new();

public async Task OnGet()
{
    var students = await _supabase.Client
        .From<Profile>()
        .Get();

    var attendance = await _supabase.Client
        .From<Attendance>()
        .Get();

    var today = DateTime.Today;

    var todayRecords = attendance.Models
        .Where(a => a.Date.Date == today)
        .ToList();

TotalStudents = attendance.Models
    .Select(a => a.Name)
    .Distinct()
    .Count();

    // ✅ SAFE NULL CHECK
    PresentToday = todayRecords.Count(a => (a.Status ?? "") == "Present");
    AbsentToday = todayRecords.Count(a => (a.Status ?? "") == "Absent");

    // ✅ FIXED (no foreach)
    RecentAbsences = todayRecords
        .Where(a => (a.Status ?? "") == "Absent")
        .Take(10)
        .Select(a => new RecentAbsence
        {
            Name = a.Name ?? "Unknown",
            Grade = "Grade " + (a.GradeLevel ?? ""),
            Status = a.Status ?? "Absent"
        })
        .ToList();

    // ✅ CHART
    ChartLabels = new List<string> { "Grade 7", "Grade 8", "Grade 9", "Grade 10" };

    ChartData = new List<int>
    {
        todayRecords.Count(a => a.GradeLevel == "7" && (a.Status ?? "") == "Absent"),
        todayRecords.Count(a => a.GradeLevel == "8" && (a.Status ?? "") == "Absent"),
        todayRecords.Count(a => a.GradeLevel == "9" && (a.Status ?? "") == "Absent"),
        todayRecords.Count(a => a.GradeLevel == "10" && (a.Status ?? "") == "Absent")
    };
}
    }
}