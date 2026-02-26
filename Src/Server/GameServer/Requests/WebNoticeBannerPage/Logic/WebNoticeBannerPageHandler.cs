using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace Puniemu.Src.Server.GameServer.Requests.WebNoticeBannerPage.Logic
{
    public static class WebNoticeBannerPageHandler
    {
        public static async Task HandleAsync(HttpContext ctx)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Web", "Page", "NoticeBannerPage", "noticeBannerPage.html");
            if (File.Exists(filePath))
            {
                ctx.Response.ContentType = "text/html; charset=utf-8";
                await ctx.Response.SendFileAsync(filePath);
            }
            else
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("This file doesn't exist");
            }
        }
    }
}
