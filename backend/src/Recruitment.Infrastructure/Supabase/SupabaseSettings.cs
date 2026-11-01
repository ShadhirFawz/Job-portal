namespace Recruitment.Infrastructure.Supabase;

public class SupabaseSettings
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;
    public string AnonKey { get; set; } = string.Empty;
    public string? ServiceRoleKey { get; set; }
    public string? JwtSecret { get; set; }
}

