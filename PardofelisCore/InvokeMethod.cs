using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PardofelisCore.Config;
using PardofelisCore.LlmController.LlamaSharpWrapper.FunctionCall;
using PardofelisCore.LlmController.LlamaSharpWrapper.Middleware;
using PardofelisCore.LlmController.LlamaSharpWrapper.Service;
using PardofelisCore.Logger;

namespace PardofelisCore;

public static class InvokeMethod
{
    private static IHostApplicationLifetime AppLifetime;

    public static void Run()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddControllers();
        builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
        builder.Services.AddEndpointsApiExplorer();

        var apiKey = "";

        builder.Services.AddSwaggerGen();
/*
 options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LLamaWorker",
        Version = "v1",
        Description = "LLamaWorker API",
        License = new OpenApiLicense
        {
            Name = "Apache License 2.0",
            Url = new System.Uri("https://github.com/sangyuxiaowu/LLamaWorker/blob/main/LICENSE.txt")
        },
        TermsOfService = new System.Uri("https://github.com/sangyuxiaowu/LLamaWorker"),
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    // ����ģ��ע��
    var xmlModelPath = Path.Combine(AppContext.BaseDirectory, "LLamaWorker.OpenAIModels.xml");
    options.IncludeXmlComments(xmlModelPath);

    if (string.IsNullOrEmpty(apiKey)) return;

    var securityScheme = new OpenApiSecurityScheme()
    {
        Description =
            "API Key Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {API_KEY}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    };
    options.AddSecurityDefinition("API_KEY", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "API_KEY"
                }
            },
            new string[] { }
        }
    });
}
 */

        builder.Services.AddSingleton<ILlmModelService, LLmModelDecorator>();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: "AllowCors",
                policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
            );
        });

// HttpClient
        builder.Services.AddHttpClient();

        var app = builder.Build();

        AppLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        app.Urls.Add("http://127.0.0.1:14251");

        app.UseCors();

// Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseSwagger();
        app.UseSwaggerUI();
        if (!string.IsNullOrEmpty(apiKey))
        {
            app.Use(async (context, next) =>
            {
                var found = context.Request.Headers.TryGetValue("Authorization", out var key);
                if (!found)
                {
                    found = context.Request.Headers.TryGetValue("api-key", out key);
                }

                key = key.ToString().Split(" ")[^1];

                if (found && key == apiKey)
                {
                    await next(context);
                }
                else
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
            });
        }


        app.UseMiddleware<TypeConversionMiddleware>();
        app.MapControllers();

        app.Run();
    }

    public static void Stop()
    {
        AppLifetime.StopApplication();
    }
}