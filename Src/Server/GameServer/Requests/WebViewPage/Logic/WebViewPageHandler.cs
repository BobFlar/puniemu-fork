using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace Puniemu.Src.Server.GameServer.Requests.WebViewPage.Logic
{
    public static class WebViewPageHandler
    {
        public static async Task HandleAsync(HttpContext ctx)
        {
            string webviewId = ctx.Request.Query["webviewId"].ToString();
            string pageNo = ctx.Request.Query["pageNo"].ToString();
            
            if (string.IsNullOrEmpty(webviewId))
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("webviewId is empty");
                return;
            }
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Web", "Page", webviewId);
            
            string fileName;
            if (string.IsNullOrEmpty(pageNo) || pageNo == "1")
            {
                fileName = "viewpage.html";
            }
            else
            {
                fileName = $"viewpage{pageNo}.html";
            }
            
            string filePath = Path.Combine(folderPath, fileName);
            
            
            ctx.Response.ContentType = "text/html; charset=utf-8";
            
            if (!Directory.Exists(folderPath))
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync($"This page doesn't exist: <br><b>{webviewId}</b>");
                return;
            }
            
            if (File.Exists(filePath))
            {
                await ctx.Response.SendFileAsync(filePath);
            }
            else
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync($"This page was not found, please check that the file name is: <b>{fileName}</b>");
            }
        }
    }
}
