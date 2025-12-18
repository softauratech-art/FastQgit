using System;
using System.Web;
using Newtonsoft.Json;

namespace FastQ.Web.Api
{
    public static class HandlerUtil
    {
        public static void WriteJson(HttpContext context, object obj, int statusCode = 200)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;

            var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            context.Response.Write(json);
        }

        public static Guid? GetGuid(HttpRequest request, string key)
        {
            var raw = request[key];
            if (Guid.TryParse(raw, out var g)) return g;
            return null;
        }

        public static string GetString(HttpRequest request, string key) => request[key];
    }
}
