using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tokenio;
using Tokenio.Proto.Common.AliasProtos;
using Tokenio.Proto.Common.MemberProtos;
using Tokenio.Proto.Common.SecurityProtos;
using Tokenio.Security;
using Member = Tokenio.Tpp.Member;
using TokenClient = Tokenio.Tpp.TokenClient;
using TokenRequest = Tokenio.TokenRequests.TokenRequest;
using Tokenio.Utils;
using static Tokenio.Proto.Common.TokenProtos.TokenRequestPayload.Types.AccessBody.Types;

namespace pfm_sample_csharp.Controllers
{
    public class ApplicationController : Controller
    {
        private static String rootLocation = AppDomain.CurrentDomain.BaseDirectory;
        
        // Connect to Token's development sandbox
        private static TokenClient tokenClient = InitializeSDK();

        // Create a Member (Token user account). A "real world" server would
        // use the same member instead of creating a new one for each run;
        // this demo creates a a new member for easier demos/testing.
        private static Member pfmMember;

        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Endpoint for requesting access to account balances via redirect
        /// </summary>
        /// <returns>RedirectResult</returns>
        [HttpGet]
        public Task<RedirectResult> RequestBalances()
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

            return GetPfmMember().FlatMap(mem => mem.GetFirstAlias()
                .FlatMap(alias => mem.StoreTokenRequest(
                    // Create a token request to be stored
                    TokenRequest.AccessTokenRequestBuilder(ResourceType.Accounts, ResourceType.Balances)
                        .SetToMemberId(mem.MemberId())
                        .SetToAlias(alias)
                        .SetRefId(refId)
                        .SetRedirectUrl(redirectUrl)
                        .SetCsrfToken(csrfToken)
                        .Build()))
                // generate the Token request URL to redirect to
                .FlatMap(requestId => tokenClient.GenerateTokenRequestUrl(requestId))
                .Map(url =>
                {
                    // send a 302 redirect
                    Response.StatusCode = 302;
                    return new RedirectResult(url);
                }));
        }

        /// <summary>
        /// Endpoint for requesting access to account balances via Popup
        /// </summary>
        /// <returns>Result</returns>
        [HttpPost]
        public Task<string> RequestBalancesPopup()
        {
            // generate CSRF token
            var csrfToken = Util.Nonce();

            // generate a reference ID for the token
            var refId = Util.Nonce();

            // generate Redirect Url
            var redirectUrl =
                string.Format("{0}://{1}/{2}", Request.Url.Scheme, Request.Url.Authority, "fetch-balances");

            // set CSRF token in browser cookie
            Response.Cookies.Add(new HttpCookie("csrf_token")
            {
                Value = csrfToken
            });

            return GetPfmMember().FlatMap(mem => mem.GetFirstAlias()
                .FlatMap(alias => mem.StoreTokenRequest(
                    // Create a token request to be stored
                    TokenRequest.AccessTokenRequestBuilder(ResourceType.Accounts, ResourceType.Balances)
                        .SetToMemberId(mem.MemberId())
                        .SetToAlias(alias)
                        .SetRefId(refId)
                        .SetRedirectUrl(redirectUrl)
                        .SetCsrfToken(csrfToken)
                        .Build()))
                // generate the Token request URL to redirect to
                .FlatMap(requestId => tokenClient.GenerateTokenRequestUrl(requestId)));
        }

        /// <summary>
        /// Endpoint for transfer payment, called by client side after user approves payment from Redirect flow
        /// </summary>
        /// <returns>Balances parsed in JSON</returns>
        [HttpGet]
        public Task<string> FetchBalances()
        {
            var callbackUrl = Request.Url.ToString();

            // retrieve CSRF token from browser cookie
            var csrfToken = Request.Cookies["csrf_token"];

            return GetPfmMember()
                // check CSRF token and retrieve state and token ID from callback parameters
                .FlatMap(mem => tokenClient.ParseTokenRequestCallbackUrl(callbackUrl, csrfToken.Value) 
                    // use access token's permissions from now on
                    .Map(callback => mem.ForAccessToken(callback.TokenId))) 
                .FlatMap(representable => representable.GetAccounts())
                .FlatMap(async accounts =>
                { 
                    // for each account, get its balance
                    var balanceTasks = accounts.Select(account => account.GetBalance(Key.Types.Level.Standard));
                    var balances = await Task.WhenAll(balanceTasks); 
                    // respond to script.js with JSON
                    return "{\"balances\":["
                           + string.Join(",", balances.Select(bal => JsonConvert.SerializeObject(bal.Current)))
                           + "]}";
                });
        }

        /// <summary>
        /// Endpoint for transfer payment, called by client side after user approves payment from Popup flow
        /// </summary>
        /// <returns>Balances parsed in JSON</returns>
        [HttpGet]
        public Task<string> FetchBalancesPopup()
        {
            var queryParams = Request.QueryString;

            // retrieve CSRF token from browser cookie
            var csrfToken = Request.Cookies["csrf_token"];

            return GetPfmMember() 
                // check CSRF token and retrieve state and token ID from callback parameters
                .FlatMap(mem => tokenClient.ParseTokenRequestCallbackParams(queryParams, csrfToken.Value)
                    // use access token's permissions from now on
                    .Map(callback => mem.ForAccessToken(callback.TokenId))) 
                .FlatMap(representable => representable.GetAccounts())
                .FlatMap(async accounts =>
                { 
                    // for each account, get its balance
                    var balanceTasks = accounts.Select(account => account.GetBalance(Key.Types.Level.Standard));
                    var balances = await Task.WhenAll(balanceTasks); 
                    // respond to script.js with JSON
                    return "{\"balances\":["
                           + string.Join(",", balances.Select(bal => JsonConvert.SerializeObject(bal.Current)))
                           + "]}";
                });
        }

        [NonAction]
        private static Task<Member> GetPfmMember()
        {
            return pfmMember != null
                ? Task.FromResult(pfmMember)
                : InitializeMember(tokenClient)
                    .Map(mem =>
                    {
                        pfmMember = mem;
                        return mem;
                    });
        }

        /// <summary>
        /// Initializes the SDK, pointing it to the specified environment.
        /// </summary>
        /// <returns>TokenIO SDK instance</returns>
        [NonAction]
        private static TokenClient InitializeSDK()
        {
            var key = Directory.CreateDirectory(Path.Combine(rootLocation, "keys"));

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
        private static Task<Member> InitializeMember(TokenClient tokenClient)
        {
            var keyDir = Directory.GetFiles(Path.Combine(rootLocation, "keys"));
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
        [NonAction]
        private static Task<Member> CreateMember(TokenClient tokenClient)
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
            return tokenClient.CreateMember(alias)
                .FlatMap(async (mem) =>
                {
                    // A member's profile has a display name and picture.
                    // The Token UI shows this (and the alias) to the user when requesting access.
                    await mem.SetProfile(new Profile
                    {
                        DisplayNameFirst = "Demo PFM"
                    });
                    byte[] pict = System.IO.File.ReadAllBytes(Path.Combine(rootLocation, "Content/southside.png"));
                    await mem.SetProfilePicture("image/png", pict);
                    
                    return mem;
                });
        }

        /// <summary>
        /// Using a TokenClient SDK client and the member ID of a previously-created
        /// Member (whose private keys we have stored locally).
        /// </summary>
        /// <param name="tokenClient">SDK</param>
        /// <param name="memberId">ID of Member</param>
        /// <returns>Logged-in member</returns>
        [NonAction]
        private static Task<Member> LoadMember(TokenClient tokenClient, string memberId)
        {
            try
            {
                return tokenClient.GetMember(memberId);
            }
            catch (KeyNotFoundException)
            {
                // it looks like we have a key but the member it belongs to does not exist in the DB
                throw new Exception("Couldn't log in saved member, not found. Remove keys dir and try again.");
            }
        }
    }
}