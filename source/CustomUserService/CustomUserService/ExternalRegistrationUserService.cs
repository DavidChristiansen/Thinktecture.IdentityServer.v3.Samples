﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Thinktecture.IdentityServer.Core;
using Thinktecture.IdentityServer.Core.Extensions;
using Thinktecture.IdentityServer.Core.Models;
using Thinktecture.IdentityServer.Core.Services;

namespace SampleApp
{
    public class ExternalRegistrationUserService : IUserService
    {
        public class CustomUser
        {
            public string Subject { get; set; }
            public string Provider { get; set; }
            public string ProviderID { get; set; }
            public bool IsRegistered { get; set; }
            public List<Claim> Claims { get; set; }
        }
        
        public static List<CustomUser> Users = new List<CustomUser>();

        public Task<AuthenticateResult> AuthenticateExternalAsync(ExternalIdentity externalUser)
        {
            // look for the user in our local identity system from the external identifiers
            var user = Users.SingleOrDefault(x => x.Provider == externalUser.Provider && x.ProviderID == externalUser.ProviderId);
            string name = "Unknown";
            if (user == null)
            {
                // new user, so add them here
                var nameClaim = externalUser.Claims.First(x => x.Type == Constants.ClaimTypes.Name);
                if (nameClaim != null) name = nameClaim.Value;

                user = new CustomUser { 
                    Subject = Guid.NewGuid().ToString(),
                    Provider = externalUser.Provider,
                    ProviderID = externalUser.ProviderId,
                    Claims = new List<Claim> { new Claim(Constants.ClaimTypes.Name, name) }
                };
                Users.Add(user);
            }

            name = user.Claims.First(x => x.Type == Constants.ClaimTypes.Name).Value;

            if (user.IsRegistered)
            {
                // user is registered so continue
                var p = IdentityServerPrincipal.Create(
                    user.Subject, name,
                    Thinktecture.IdentityServer.Core.Constants.AuthenticationMethods.External,
                    user.Provider
                );
                return Task.FromResult<AuthenticateResult>(new AuthenticateResult(p));
            }
            else
            {
                // user not registered so we will issue a partial login and redirect them to our registration page
                var p = IdentityServerPrincipal.Create(
                    user.Subject, name,
                    Thinktecture.IdentityServer.Core.Constants.AuthenticationMethods.External,
                    user.Provider
                );
                return Task.FromResult<AuthenticateResult>(new AuthenticateResult("/core/externalregistration", p));
            }
        }

        public Task<AuthenticateResult> AuthenticateLocalAsync(string username, string password, SignInMessage message)
        {
            return Task.FromResult<AuthenticateResult>(null);
        }

        public Task<IEnumerable<System.Security.Claims.Claim>> GetProfileDataAsync(ClaimsPrincipal subject, IEnumerable<string> requestedClaimTypes = null)
        {
            // issue the claims for the user
            var user = Users.SingleOrDefault(x => x.Subject == subject.GetSubjectId());
            if (user == null)
            {
                return Task.FromResult<IEnumerable<Claim>>(null);
            }

            return Task.FromResult(user.Claims.Where(x => requestedClaimTypes.Contains(x.Type)));
        }

        public Task<bool> IsActiveAsync(ClaimsPrincipal subject)
        {
            var user = Users.SingleOrDefault(x => x.Subject == subject.GetSubjectId());
            return Task.FromResult(user != null && user.IsRegistered);
        }

        public Task<AuthenticateResult> PreAuthenticateAsync(IDictionary<string, object> env, SignInMessage message)
        {
            return Task.FromResult<AuthenticateResult>(null);
        }
    }
}
