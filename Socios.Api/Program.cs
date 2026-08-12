using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Socios.Application;
using Socios.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ==============================
// CONFIGURACIÓN BASE API
// ==============================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==============================
// RATE LIMITING (por IP)
// ==============================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("AssistantPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 15,
                QueueLimit = 0
            }));

    options.AddPolicy("UploadPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 5,
                QueueLimit = 0
            }));
});

// ==============================
// CORS (frontend Angular)
// ==============================
const string FrontendCorsPolicy = "FrontendCors";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ==============================
// CAPAS (Clean Architecture)
// ==============================
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// ==============================
// CONFIGURACIÓN IA (OpenAI)
// ==============================
var openAiConfig = builder.Configuration.GetSection("AI:OpenAI");
var openAiApiKey = openAiConfig["ApiKey"] ?? throw new InvalidOperationException("Falta configurar AI:OpenAI:ApiKey.");
var chatModel = openAiConfig["ChatModel"] ?? "gpt-5-nano";
var embeddingModel = openAiConfig["EmbeddingModel"] ?? "text-embedding-3-small";

builder.Services.AddKernel()
    .AddOpenAIChatCompletion(
        modelId: chatModel,
        apiKey: openAiApiKey
    )
    .AddOpenAITextEmbeddingGeneration(
        modelId: embeddingModel,
        apiKey: openAiApiKey
    );

// ==============================
// BUILD APP
// ==============================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Socios API V1");
        c.RoutePrefix = "swagger";
    });
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();