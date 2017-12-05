using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SimpleMvc.Extensions
{
    public static class HtmlHelperExtensions
    {
        public static HtmlString GetActiveMenu(this IHtmlHelper<dynamic> html, string controller)
        {
            if (html.ViewContext.RouteData.Values["controller"].Equals(controller) &&
              html.ViewContext.RouteData.Values["area"] == null)
            {
                return new HtmlString("active");
            }

            return new HtmlString("");
        }

        public static HtmlString GetActiveMenuItem(this IHtmlHelper<dynamic> html, string controller, string action)
        {
            if (html.ViewContext.RouteData.Values["controller"].Equals(controller) &&
              html.ViewContext.RouteData.Values["action"].Equals(action) &&
              html.ViewContext.RouteData.Values["area"] == null)
            {
                return new HtmlString("active");
            }

            return new HtmlString("");
        }
    }
}
