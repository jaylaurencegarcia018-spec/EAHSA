using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using Supabase.Postgrest;
using Microsoft.Extensions.Configuration;
using EAHSA.Models;

public class SupabaseService
{
    private readonly Supabase.Client _client;

    public SupabaseService(IConfiguration config)
    {
        var url = config["Supabase:Url"];
        var key = config["Supabase:Key"];

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
            throw new Exception("Supabase configuration is missing in appsettings.json");

        _client = new Supabase.Client(url, key);
        _client.InitializeAsync().Wait();
    }

    public async Task<Session?> Login(string email, string password)
    {
        try
        {
            var session = await _client.Auth.SignIn(email, password);
            return session;
        }
        catch (GotrueException ex)
        {
            if (ex.Message.Contains("Invalid login credentials"))
                return null;

            throw;
        }
    }

    public async Task<List<Attendance>> GetAttendance()
    {
        var response = await _client
            .From<Attendance>()
            .Get();

        return response.Models;
    }

    public async Task<User?> SignUp(string email, string password)
    {
        var session = await _client.Auth.SignUp(email, password);
        return session?.User;
    }

    // 🔥 ADD THIS PART
public async Task UpdateAttendance(Attendance data)
{
    await _client
        .From<Attendance>()
        .Where(x => x.Id == data.Id)
        .Set(x => x.Status, data.Status)
        .Set(x => x.Name, data.Name)
        .Set(x => x.Gender, data.Gender)
        .Set(x => x.Section, data.Section)
        .Set(x => x.GradeLevel, data.GradeLevel)
        .Set(x => x.LRN, data.LRN)
        .Set(x => x.Photo, data.Photo)
        .Update();
}

    public Supabase.Client Client => _client;
}