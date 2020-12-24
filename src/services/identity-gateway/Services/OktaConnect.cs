// <copyright file="OktaConnect.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Mmm.Iot.IdentityGateway.Services.Models;

namespace Mmm.Iot.IdentityGateway.Services
{
    public class OktaConnect : IOktaConnect
    {
        public Task<bool> AcquireTokenAndValidate(ClientCredentialInput input)
        {
            throw new NotImplementedException();
        }

        public List<Claim> GetClaims(JwtSecurityToken jwt)
        {
            List<Claim> claims = new List<Claim>();
            claims = jwt.Claims.Where(t => new List<string> { "name", "sub" }.Contains(t.Type)).ToList();
            claims.Add(new Claim("sub", "534a9d20-04aa-4f42-9cf9-d82fca6cf7df"));
            return claims;
        }

        public UriBuilder GetLoginRedirectUri()
        {
            var uri = new UriBuilder("https://dev-3754605.okta.com/oauth2/default/v1/authorize?idp=0oa2u3ik1no4t01t15d6&client_id=0oa2u2rbmxmvtKiqS5d6&response_type=id_token&response_mode=form_post&&scope=openid&nonce=defaultNonce&state=state&redirect_uri=https%3a%2f%2flocalhost%3a5001%2fconnect%2fcallback");

            return uri;
        }

        public UriBuilder GetLogoutRedirectUri(string post_logout_redirect_uri)
        {
            var uri = new UriBuilder(post_logout_redirect_uri);
            return uri;
        }
    }
}