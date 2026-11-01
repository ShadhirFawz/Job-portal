using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Recruitment.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load local configuration (ignored by git)
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Fallback to environment variables if present
if (string.IsNullOrWhiteSpace(builder.Configuration["Supabase:Url"]))
{
    var envUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? Environment.GetEnvironmentVariable("VITE_SUPABASE_URL");
    if (!string.IsNullOrWhiteSpace(envUrl)) builder.Configuration["Supabase:Url"] = envUrl;
}
if (string.IsNullOrWhiteSpace(builder.Configuration["Supabase:AnonKey"]))
{
    var envKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? Environment.GetEnvironmentVariable("VITE_SUPABASE_ANON_KEY");
    if (!string.IsNullOrWhiteSpace(envKey)) builder.Configuration["Supabase:AnonKey"] = envKey;
}

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS (for Vue frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Infrastructure Services (SupabaseContext, OrganizationService, MemberService, ApplicationService, ProfileService, ResumeService)
builder.Services.AddInfrastructure(builder.Configuration);

// Supabase JWT Authentication
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseJwtSecret = builder.Configuration["Supabase:JwtSecret"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var hasValidSecret = !string.IsNullOrWhiteSpace(supabaseJwtSecret) && supabaseJwtSecret.Length >= 32;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl?.TrimEnd('/')}/auth/v1",
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = hasValidSecret,
            IssuerSigningKey = hasValidSecret ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseJwtSecret!)) : null,
            SignatureValidator = !hasValidSecret ? (token, _) => new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token) : null,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
