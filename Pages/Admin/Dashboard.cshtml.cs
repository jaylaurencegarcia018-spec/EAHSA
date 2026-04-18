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
        public int TardyToday { get; set; }

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

            TotalStudents = students.Models.Count;

            PresentToday = todayRecords.Count(a => a.Status == "Present");
            AbsentToday = todayRecords.Count(a => a.Status == "Absent");
            TardyToday = todayRecords.Count(a => a.Status == "Tardy");

            var absences = todayRecords
                .Where(a => a.Status == "Absent")
                .Take(10);

            foreach (var item in absences)
            {
                RecentAbsences.Add(new RecentAbsence
                {
                    Name = item.Name,
                    Date = item.Date.ToString("MMM dd, yyyy"),
                    Status = item.Status
                });
            }

            // Chart sample data (since there is no Date column)
            ChartLabels.Add("Grade 7");
            ChartLabels.Add("Grade 8");
            ChartLabels.Add("Grade 9");
            ChartLabels.Add("Grade 10");

            ChartData.Add(todayRecords.Where(a => a.GradeLevel == "7" && a.Status == "Absent").Count());
            ChartData.Add(todayRecords.Where(a => a.GradeLevel == "8" && a.Status == "Absent").Count());
            ChartData.Add(todayRecords.Where(a => a.GradeLevel == "9" && a.Status == "Absent").Count());
            ChartData.Add(todayRecords.Where(a => a.GradeLevel == "10" && a.Status == "Absent").Count());
        }
    }
}