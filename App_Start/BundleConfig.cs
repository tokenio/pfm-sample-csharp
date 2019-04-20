using System.Web.Optimization;

namespace pfm_sample_csharp
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/script").Include(
                "~/Scripts/script.js"));
            
            bundles.Add(new StyleBundle("~/Content/css").Include(
                "~/Content/style.css"));
        }
    }
}