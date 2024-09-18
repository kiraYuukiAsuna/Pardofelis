using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace PardofelisCore.Api;

public delegate void ApiServerCallback(string text);

public class ApiServer
{
    private static IHostApplicationLifetime AppLifetime;

    private static ApiServerCallback Callback;

    public class ApiJsonInput
    {
        public string text { get; set; }
    }

    public class ApiJsonOutput
    {
        public string status { get; set; }
    }

    public void StartBlocking(ApiServerCallback callback)
    {
        Callback = callback;

        var builder = WebApplication.CreateBuilder();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        AppLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        app.Urls.Add("http://127.0.0.1:14252");

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapPost("/api", async context =>
        {
            try
            {
                // 从请求中读取 JSON 数据
                using var reader = new StreamReader(context.Request.Body);
                var json = await reader.ReadToEndAsync();

                // 解析 JSON 数据
                var jsonInput = JsonConvert.DeserializeObject<ApiJsonInput>(json);
                if (jsonInput == null || string.IsNullOrEmpty(jsonInput.text))
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Invalid Json Input");
                }

                // 调用回调函数
                Callback(jsonInput.text);

                // 返回 JSON 响应
                var responseJson = JsonConvert.SerializeObject(new ApiJsonOutput
                {
                    status = "ok"
                });

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(responseJson);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync($"Internal server error: {ex.Message}");
            }
        });

        app.Run();
    }

    public void Stop()
    {
        AppLifetime.StopApplication();
    }
}