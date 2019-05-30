using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Tokenio;
using Tokenio.Proto.Common.AliasProtos;
using Tokenio.Proto.Common.MemberProtos;
using Tokenio.Proto.Common.SecurityProtos;
using Tokenio.Security;
using Member = Tokenio.Member;
using TokenRequest = Tokenio.TokenRequest;
using static Tokenio.Proto.Common.TokenProtos.TokenRequestPayload.Types.AccessBody.Types;

namespace pfm_sample_csharp.Controllers
{
    public class ApplicationController : Controller
    {
        // Connect to Token's development sandbox
        private static TokenClient tokenClient = InitializeSDK();

        // Create a Member (Token user account). A "real world" server would
        // use the same member instead of creating a new one for each run;
        // this demo creates a a new member for easier demos/testing.
        private static Member pfmMember = InitializeMember(tokenClient);
        
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Endpoint for requesting access to account balances
        /// </summary>
        /// <returns>RedirectResult</returns>
        [HttpPost]
        public string RequestBalances()
        {
            // generate CSRF token
            var csrfToken = Util.Nonce();

            // generate a reference ID for the token
            var refId = Util.Nonce();

            // generate Redirect Url
            var redirectUrl = string.Format("{0}://{1}/{2}", Request.Url.Scheme, Request.Url.Authority, "fetch-balances");
            
            // set CSRF token in browser cookie
            Response.Cookies.Add(new HttpCookie("csrf_token")
            {
                Value = csrfToken
            });
            // Create a token request to be stored
            var tokenRequest = TokenRequest.AccessTokenRequestBuilder(
                    ResourceType.Accounts, ResourceType.Balances)
                .SetToMemberId(pfmMember.MemberId())
                .SetToAlias(pfmMember.GetFirstAliasBlocking())
                .SetRefId(refId)
                .SetRedirectUrl(redirectUrl)
                .SetCsrfToken(csrfToken)
                .build();

            var requestId = pfmMember.StoreTokenRequestBlocking(tokenRequest);

            //generate the Token request URL to redirect to
            var tokenRequestUrl = tokenClient.GenerateTokenRequestUrlBlocking(requestId);

            //send Token Request URL
            return tokenRequestUrl;
        }

        /// <summary>
        /// Endpoint for transfer payment, called by client side after user approves payment
        /// </summary>
        /// <returns>Balances parsed in JSON</returns>
        [HttpGet]
        public string FetchBalances()
        {
            var queryParams = Request.QueryString.ToString();

            // retrieve CSRF token from browser cookie
            var csrfToken = Request.Cookies["csrf_token"];

            // check CSRF token and retrieve state and token ID from callback parameters
            var callback = tokenClient.ParseTokenRequestCallbackUrlBlocking(
                queryParams, csrfToken.Value);
            
            // use access token's permissions from now on, set true if customer initiated request
            var representable = pfmMember.ForAccessToken(callback.TokenId, false);
            
            var accounts = representable.GetAccountsBlocking();
            var balanceJsons = new List<string>();
            foreach (var account in accounts)
            {
                //for each account, get its balance
                var balance = account.GetBalanceBlocking(Key.Types.Level.Standard).Current;
                balanceJsons.Add(JsonConvert.SerializeObject(balance));
            }

            // respond to script.js with JSON
            return "{\"balances\":[" + string.Join(",", balanceJsons) + "]}";
        }

        /// <summary>
        /// Initializes the SDK, pointing it to the specified environment.
        /// </summary>
        /// <returns>TokenIO SDK instance</returns>
        [NonAction]
        private static TokenClient InitializeSDK()
        {
            var key = Directory.CreateDirectory("./keys");

            return TokenClient.NewBuilder()
                .ConnectTo(TokenCluster.SANDBOX)
                .WithKeyStore(new UnsecuredFileSystemKeyStore(key.FullName))
                .Build();
        }

        /// <summary>
        /// Log in existing member or create new member.
        /// </summary>
        /// <param name="tokenClient">tokenClient Token SDK client</param>
        /// <returns>Logged-in member</returns>
        [NonAction]
        private static Member InitializeMember(TokenClient tokenClient)
        {
            var keyDir = Directory.GetFiles("./keys");
            var memberIds = keyDir.Where(d => d.Contains("_")).Select(d => d.Replace("_", ":"));
            return !memberIds.Any() 
                ? CreateMember(tokenClient) 
                : LoadMember(tokenClient, Path.GetFileName(memberIds.First()));
        }
        /// <summary>
        /// Using a TokenClient SDK client, create a new Member.
        /// This has the side effect of storing the new Member's private
        /// keys in the ./keys directory.
        /// </summary>
        /// <param name="tokenClient">SDK</param>
        /// <returns>newly-created member</returns>
        [System.Web.Mvc.NonAction]
        private static Member CreateMember(TokenClient tokenClient)
        {
            // An alias is a human-readable way to identify a member, e.g., a domain or email address.
            // If a domain alias is used instead of an email, please contact Token
            // with the domain and member ID for verification.
            // See https://developer.token.io/sdk/#aliases for more information.
            var email = "ascsharp-" + Util.Nonce().ToLower() + "+noverify@example.com";
            var alias = new Alias
            {
                Type = Alias.Types.Type.Email,
                Value = email
            };
            var member = tokenClient.CreateMemberBlocking(alias);
            // A member's profile has a display name and picture.
            // The Token UI shows this (and the alias) to the user when requesting access.
            member.SetProfile(new Profile
            {
                DisplayNameFirst = "Demo PFM"
            });
            byte[] pict = System.IO.File.ReadAllBytes("Content/southside.png");
            member.SetProfilePictureBlocking("image/png", pict);
            return member;
        }
        
        /// <summary>
        /// Using a TokenClient SDK client and the member ID of a previously-created
        /// Member (whose private keys we have stored locally).
        /// </summary>
        /// <param name="tokenClient">SDK</param>
        /// <param name="memberId">ID of Member</param>
        /// <returns>Logged-in member</returns>
        [System.Web.Mvc.NonAction]
        private static Member LoadMember(TokenClient tokenClient, string memberId)
        {
            try
            {
                return tokenClient.GetMemberBlocking(memberId);
            }
            catch (KeyNotFoundException)
            {
                // it looks like we have a key but the member it belongs to does not exist in the DB
                throw new Exception("Couldn't log in saved member, not found. Remove keys dir and try again.");
            }
        }
    }
}
