using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Recruitment.Application.Interfaces;
using Recruitment.Infrastructure.Services;
using Recruitment.Infrastructure.Supabase;

namespace Recruitment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SupabaseSettings>(configuration.GetSection(SupabaseSettings.SectionName));

        services.AddHttpContextAccessor();

        services.AddHttpClient<ISupabaseContext, SupabaseContext>();

        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IResumeService, ResumeService>();
        services.AddScoped<IJobService, JobService>();

        return services;
    }
}

