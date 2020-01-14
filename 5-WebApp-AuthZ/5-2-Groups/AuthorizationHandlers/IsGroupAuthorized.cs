using Microsoft.AspNetCore.Authorization;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApp_OpenIDConnect_DotNet.AuthorizationHandlers
{
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
}
