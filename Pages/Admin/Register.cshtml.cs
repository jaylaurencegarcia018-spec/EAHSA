using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using EAHSA.Models;

namespace EAHSA.Pages.Admin
{
    public class RegisterModel : PageModel
    {
        private readonly SupabaseService _supabase;

        public RegisterModel(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        [BindProperty]
        public List<string> AllowedSections { get; set; } = new();
        
        [BindProperty]
        [EmailAddress]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        [BindProperty]
        public string ConfirmPassword { get; set; } = "";

        // Selected checkboxes
        [BindProperty]
        public List<string> Sections { get; set; } = new();

        // Grade -> Sections dictionary
        public Dictionary<string, List<string>> GradeSections { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadSections();
        }

        private async Task LoadSections()
        {
            var response = await _supabase.Client
                .From<Attendance>()
                .Get();

            var records = response.Models ?? new List<Attendance>();

            GradeSections = records
                .Where(x => !string.IsNullOrEmpty(x.GradeLevel) && !string.IsNullOrEmpty(x.Section))
                .GroupBy(x => x.GradeLevel!)
                .OrderBy(g => int.Parse(new string(g.Key.Where(char.IsDigit).ToArray())))
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Section!)
                          .Distinct()
                          .OrderBy(x => x)
                          .ToList()
                );
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadSections();

            Email = Email.Trim();

            if (Password != ConfirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return Page();
            }

            if (Sections == null || Sections.Count == 0)
            {
                TempData["Error"] = "Please select at least one section.";
                return Page();
            }

            try
            {
                var sections = string.Join(",", Sections);

            var allowedGrades = string.Join(",",
                Sections
                    .Select(section => GradeSections
                        .FirstOrDefault(g => g.Value.Contains(section)).Key)
                    .Where(g => g != null)
                    .Distinct()
            );


                // Save selected sections
                var userAccess = new UserAccess
                {
                    Email = Email,
                    Password = Password,
                    AllowedSections = string.Join(",", Sections),
                    Role = "User"
                };
                

                await _supabase.Client
                    .From<UserAccess>()
                    .Insert(userAccess);

                TempData["Success"] = "User Registered Successfully!";

                // Clear form
                Email = "";
                Password = "";
                ConfirmPassword = "";
                Sections.Clear();

                return Page();
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return Page();
            }
        }
    }
}