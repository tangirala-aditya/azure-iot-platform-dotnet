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
using System.Web;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.IdentityGateway.Services.Helpers;
using Mmm.Iot.IdentityGateway.Services.Models;

namespace Mmm.Iot.IdentityGateway.Services
{
    public class OktaConnect : IOktaConnect
    {
        private IJwtHelpers jwtHelper;

        public OktaConnect(IJwtHelpers jwtHelper)
        {
            this.jwtHelper = jwtHelper;
        }

        public Task<bool> AcquireTokenAndValidate(ClientCredentialInput input)
        {
            throw new NotImplementedException();
        }

        public List<Claim> GetClaims(JwtSecurityToken jwt)
        {
            List<Claim> claims = new List<Claim>();
            claims = jwt.Claims.Where(t => new List<string> { "name" }.Contains(t.Type)).ToList();
            var oidClaim = jwt.Claims.FirstOrDefault(c => c.Type == "objectSid");
            if (oidClaim != null)
            {
                claims.Add(new Claim("sub", oidClaim.Value));
            }

            var oktaSub = jwt.Claims.FirstOrDefault(c => c.Type == "sub");
            if (oktaSub != null)
            {
                claims.Add(new Claim("oktaSub", oktaSub.Value));
            }

            var oktaiss = jwt.Claims.FirstOrDefault(c => c.Type == "iss");
            if (oktaiss != null)
            {
                claims.Add(new Claim("oktaiss", oktaiss.Value));
            }

            var oktaaud = jwt.Claims.FirstOrDefault(c => c.Type == "aud");
            if (oktaaud != null)
            {
                claims.Add(new Claim("oktaaud", oktaSub.Value));
            }

            var oktaiat = jwt.Claims.FirstOrDefault(c => c.Type == "iat");
            if (oktaiat != null)
            {
                claims.Add(new Claim("oktaiat", oktaiat.Value));
            }

            var oktaidp = jwt.Claims.FirstOrDefault(c => c.Type == "idp");
            if (oktaidp != null)
            {
                claims.Add(new Claim("oktaidp", oktaidp.Value));
            }

            var oktaexp = jwt.Claims.FirstOrDefault(c => c.Type == "exp");
            if (oktaexp != null)
            {
                claims.Add(new Claim("oktaexp", oktaexp.Value));
            }

            var oktajti = jwt.Claims.FirstOrDefault(c => c.Type == "jti");
            if (oktajti != null)
            {
                claims.Add(new Claim("oktajti", oktajti.Value));
            }

            var oktaamr = jwt.Claims.FirstOrDefault(c => c.Type == "amr");
            if (oktaamr != null)
            {
                claims.Add(new Claim("oktaamr", oktaamr.Value));
            }

            /*
            var jwtHandler = new JwtSecurityTokenHandler();
            claims.Add(new Claim("oktaToken", jwtHandler.WriteToken(jwt)));
            */
            return claims;
        }

        public UriBuilder GetLoginRedirectUri()
        {
            var uri = new UriBuilder("https://dev-8554852.okta.com/oauth2/default/v1/authorize?idp=0oa36uimemZwn3JDB5d6&client_id=0oa36tbvgtpc9p7Nv5d6&response_type=id_token&response_mode=form_post&&scope=openid%20profile%20email&nonce=defaultNonce&state=state&redirect_uri=https%3a%2f%2flocalhost%3a5001%2fconnect%2fcallback");

            return uri;
        }

        public UriBuilder GetLogoutRedirectUri(string post_logout_redirect_uri, string token = null)
        {
            // Extract Bearer token
            string encodedToken = token;
            var jwtHandler = new JwtSecurityTokenHandler();
            if (!this.jwtHelper.TryValidateToken("IoTPlatform", encodedToken, null, out JwtSecurityToken jwt))
            {
                throw new NoAuthorizationException("The given token could not be read or validated.");
            }

            List<Claim> claims = jwt.Claims.Where(c => new List<string>() { "oktaSub", "oktaiss", "oktaaud", "oktaiat", "oktaidp", "oktaexp", "oktajti", "oktaamr", "name", "email", "oktaToken" }.Contains(c.Type)).ToList();

            // Everything checks out so you can mint a new token
            var tokenString = jwtHandler.WriteToken(this.jwtHelper.GetOktaIdentityToken(claims, string.Empty, jwt.Audiences.First(), jwt.ValidTo));

            var postredirectUri = new UriBuilder("https://dev-8554852.okta.com/oauth2/default/v1/logout?post_logout_redirect_uri=redirect&id_token_hint=id_token");

            var query = HttpUtility.ParseQueryString(postredirectUri.Query);
            query["id_token_hint"] = claims.First(c => c.Type == "oktaToken").Value;
            query["post_logout_redirect_uri"] = post_logout_redirect_uri;

            postredirectUri.Query = query.ToString();

            var uri = new UriBuilder(postredirectUri.Uri.ToString());
            return uri;
        }
    }
}