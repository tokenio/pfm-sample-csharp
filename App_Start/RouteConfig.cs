using System.Web.Mvc;
using System.Web.Routing;

namespace pfm_sample_csharp
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            
            routes.MapRoute(
                "ReqBalance",
                "request-balances",
                new {controller = "Application", action = "RequestBalances"}
            );

            routes.MapRoute(
                "ReqBalancePopup",
                "request-balances-popup",
                new {controller = "Application", action = "RequestBalancesPopup"}
            );
            
            routes.MapRoute(
                "FchBalance",
                "fetch-balances",
                new {controller = "Application", action = "FetchBalances"}
            );

            routes.MapRoute(
                "FchBalancePopup",
                "fetch-balances-popup",
                new {controller = "Application", action = "FetchBalancesPopup"}
            );
            
            routes.MapRoute(
                "Application",
                "{action}/{id}",
                new {controller = "Application", action = "Index", id = UrlParameter.Optional}
            );
        }
    }
}