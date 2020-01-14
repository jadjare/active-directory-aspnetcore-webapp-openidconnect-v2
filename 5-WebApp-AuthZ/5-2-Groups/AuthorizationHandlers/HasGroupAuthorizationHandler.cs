using Microsoft.AspNetCore.Authorization;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApp_OpenIDConnect_DotNet.Services.MicrosoftGraph;

namespace WebApp_OpenIDConnect_DotNet.AuthorizationHandlers
{
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
}
