# Purpose
The purpose of this document is to describe the changes made to the sample project to demonstrate using an AuthorizationRequirement to validate membership of an Azure AD Group via the Microsft Graph Api.
This is intended for Microsoft Support in order to validate the approach is appropriate.

# Problem
Some users within our corporate Active Directory have more than 200 groups.  
This exceeds the limit that can be transported in the standard claims token and instead an "overage" claim is included to indicate this has occurred.
Although the "overage" claim appears to point to Graph endpoint where membership can be obtained the "out-the-box" Microsoft authentication/authorisation libraries do not automatically use this endpoint and resolve group membership.
As such group membership must be checked manually against Azure AD using the Microsoft Graph Api.

# Approach
In order to minimise the impact to our existing code base, that frequently uses the `[Authorize(policyname)]` attribute above controller classes and action methods, the approach taken has been to implement a solution that will the `[Authorize]` attribute to remain in place but ensure the check can be deferred to the Graph Api.

In order to do this an `AuthorizationRequirement` has been defined along with a custom `AuthorizationHandler`.  This allows any authorisation request for a policy to be handled by the custom `AuthorizationHandler` therefore allowing us to intercept the request and check Azure AD group membership against the Graph Api.  This approach is taken regardless of the number groups the user has (i.e. they do not need to exceed the 200 claim limit) - this has been done in order to simplify the code base.

# Explanation of Changes
The changes target the example solution found in the folder \5-WebApp-AuthZ\5-2-Groups

## Overview
Within that solution a new folder has been added to the `WebApp-OpenIDConnect-DotNet` project called `AuthorizationHandlers`.  Inside this folder are the main classes required to implement the solution.
- `IsGroupAuthorized`
- `HasGroupAuthorizationHandler`
- `IServiceCollectionAuthorizationExtensions`

The `startup` class is then updated with a call to the `IServiceCollectionAuthorizationExtensions` to add the policy into the application.

The `Home` controller has then been updated with a new action method `TestAuth`.  This action method is only made accessible to users with the policy "InAuthorizedGroup".  Entering this route triggers the authorization handler and on success redisplays the index page with a small amount of additional content to indicate success.

## Explanation of the Authorisation Handler classes
The `IsGroupAuthorized` class is defined as follows:
This class will later be used to define what group or groups a user must be a member of for a given policy
```
    public class IsGroupAuthorized : IAuthorizationRequirement
    {
        public IsGroupAuthorized(string groupId) : this(new[] {groupId}){}

        public IsGroupAuthorized(params string[] groupIds) : this(groupIds as IEnumerable<string>){}

        public IsGroupAuthorized(IEnumerable<string> groupIds)
        {
            AuthorizedGroups = groupIds ?? throw new ArgumentNullException(nameof(groupIds));
        }

        public IEnumerable<string> AuthorizedGroups { get; }

        public bool IsMemberOf(IEnumerable<Group> groups)
        {
            return groups.Any(group => this.AuthorizedGroups.Contains(group.Id));
        }
    }
 ```
 
 The `HasGroupAuthorizationHandler` class is used to handle authorisation requests.  It is injected with the `TokenAcquisition` and `MSGraphService` classes.  These two classes a defined within the `Microsoft.Identity.Web` and `WebApp-OpenIDConnect-DotNet` projects.  The dependency injection for these two classes is configured during the startup of the project in the sample code (see the `AddMsal()` and `AddMSGraphService()` calls on the `services` property).  The `HasGroupAuthorizationHandler` uses these two class in order to contact the Graph Api and retrieve and individual's group membership (see method `GetUserGroups()` in the code below).
 ```
     public class HasGroupAuthorizationHandler : AuthorizationHandler<IsGroupAuthorized>
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IMSGraphService _graphService;

        public HasGroupAuthorizationHandler(ITokenAcquisition tokenAcquisition, IMSGraphService MSGraphService)
        {
            _tokenAcquisition = tokenAcquisition;
            _graphService = MSGraphService;
        }


        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
            IsGroupAuthorized requirement)
        {
            if (requirement.IsMemberOf(await GetUserGroups()))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }

        private async Task<IList<Group>> GetUserGroups()
        {
            string accessToken =
                await _tokenAcquisition.GetAccessTokenOnBehalfOfUserAsync(new[] {"User.Read", "Directory.Read.All"});

            await _graphService.GetMeAsync(accessToken);

            IList<Group> groups = await _graphService.GetMyMemberOfGroupsAsync(accessToken);

            return groups;
        }
    }
 ```
 
 The `IServicesCollectionAuthorizationExtensions` class is used to simplify and encapsulate the necessary code required to define add our test authorization policy.  Here's the code:
 ```
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
 ```
 
 ## Hooking up the authorisation in Startup.cs
 A new line of code has been added to the `ConfigureServices()` method that Adds our test policy
 ```
             //Add our test authorisation policies.
            services.AddTestAuthorizationPolicies();
 ```
 
 ## Updating the Home controller
 A test method has been added to the `Home` controller using the standard `[Authorize(policyname)]` attribute allowing us to test the authorization handler in action.  This is the new action method:
 ```
         [Authorize("InAuthorizedGroup")]
        public IActionResult TestAuth()
        {
            ViewData["User"] = HttpContext.User;
            ViewData["TestAuthResult"] = "Authorised via custom requirement for the 'InAuthorizedGroup' policy authorised by Group Id";
            return View("Index");
        }
 ```
 
 In addition a small amount of markup has been added to the `Index` view of the `Home` controller to facilitate invoke the action method (via a button on the page).
