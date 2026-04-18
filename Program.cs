var builder = WebApplication.CreateBuilder(args);

// Add Razor Pages
builder.Services.AddRazorPages();

// Register Supabase service
builder.Services.AddSingleton<SupabaseService>();

// Enable Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

builder.Services.AddSession();
app.UseStaticFiles();
app.UseRouting();

// Enable Session middleware
app.UseSession();

app.MapRazorPages();

app.Run();