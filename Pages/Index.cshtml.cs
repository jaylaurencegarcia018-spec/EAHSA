using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EAHSA.Models;

namespace EAHSA.Pages
{
public class IndexModel : PageModel
{
private readonly SupabaseService _supabase;

    public IndexModel(SupabaseService supabase)
    {
        _supabase = supabase;
    }

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter your email and password.";
            return Page();
        }

        // CLEAR OLD SESSION
        HttpContext.Session.Clear();

        // GET USER ACCESS FROM USERS TABLE
        var response = await _supabase.Client
            .From<UserAccess>()
        .Where(x => x.Email == Email)
            .Get();

        var user = response.Models.FirstOrDefault();

        if (user == null || user.Password != Password)
        {
            ErrorMessage = "User not found.";
            return Page();
        }

        // SAVE SESSION
        HttpContext.Session.SetString("UserEmail", user.Email ?? "");
        HttpContext.Session.SetString("AllowedSections", user.AllowedSections ?? "");

        // NORMALIZE ROLE
        var role = user.Role?.Trim().ToLower();

        HttpContext.Session.SetString("UserRole", role ?? "user");

        if (role == "admin")
        {
            return RedirectToPage("/Admin/Dashboard");
        }

        return RedirectToPage("/Users/Attendance");
    }
}
}