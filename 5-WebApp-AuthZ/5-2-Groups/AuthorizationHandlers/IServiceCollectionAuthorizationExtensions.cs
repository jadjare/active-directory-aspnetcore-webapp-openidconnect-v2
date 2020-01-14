using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp_OpenIDConnect_DotNet.AuthorizationHandlers
{
    public static class IServiceCollectionAuthorizationExtensions
    {
        public static void AddTestAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddScoped<IAuthorizationHandler, HasGroupAuthorizationHandler>();  //Note, Authorisation Handler has dependencies on MSGraphService and TokenAcquisition.  Dependency injection for these is configured when the "AddMsal" and "AddMSGraphService" methods are invoked on services above
            
            services.AddAuthorization(options => options.AddPolicy("InAuthorizedGroup", TestAuthPolicy()));
        }

        private static AuthorizationPolicy TestAuthPolicy()
        {
            var testAuthPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new IsGroupAuthorized("3ce959ca-b36f-4828-a1c6-b677cec3e7bd",
                    "3c479d63-45a6-4cc9-a2b5-9ce22ce90d13")) //For this example we'll just use some hardcoded group ids.
                .Build();

            return testAuthPolicy;
        }
    }
}
