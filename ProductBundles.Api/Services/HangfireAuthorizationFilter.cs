using Hangfire.Dashboard;

namespace ProductBundles.Api.Services;

/// <summary>
/// Authorization filter for Hangfire dashboard access
/// Currently allows open access for development - should be secured in production
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // For development, allow access to everyone
        // In production, implement proper authentication
        // Example: return context.GetHttpContext().User.Identity.IsAuthenticated;
        return true;
    }
}
