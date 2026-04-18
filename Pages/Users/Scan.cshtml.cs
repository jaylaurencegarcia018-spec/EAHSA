using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EAHSA.Models;

[IgnoreAntiforgeryToken]

public class ScanModel : PageModel
{
    private readonly SupabaseService _supabase;

    public ScanModel(SupabaseService supabase)
    
    {
        _supabase = supabase;
    }

        public async Task<IActionResult> OnPostScanStudent([FromForm] string lrn)
    {
        var student = await _supabase.Client
            .From<Attendance>()
            .Where(x => x.LRN == lrn)
            .Single();

        if (student == null)
        {
            return new JsonResult(new { success = false });
        }

        // ✅ Update attendance
        student.Status = "Present";
        student.Date = DateTime.Now;

        await _supabase.Client
            .From<Attendance>()
            .Update(student);

        return new JsonResult(new
        {
            success = true,
            name = student.Name,
            grade = student.GradeLevel,
            lrn = student.LRN,
            gender = student.Gender,
            section = student.Section,
            photo = student.Photo
        });
    }
}