using System.Web;

namespace HR.Web.Helpers
{
    /// <summary>
    /// MVC 4 lacks HttpRequest.Unvalidated (ASP.NET 4.5+). Read raw form values directly.
    /// </summary>
    public static class RequestFormHelper
    {
        public static string GetFormValue(HttpRequestBase request, string key)
        {
            if (request == null || string.IsNullOrEmpty(key))
            {
                return null;
            }

            return request.Form[key];
        }
    }
}
