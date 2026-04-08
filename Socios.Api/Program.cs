using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
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
// CAPAS (Clean Architecture)
// ==============================
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// ==============================
// CONFIGURACIÓN IA
// ==============================

// Configs tipadas
var grokConfig = builder.Configuration.GetSection("AI:Grok");
var openAiConfig = builder.Configuration.GetSection("AI:OpenAI");

// Validación (nivel pro)
var grokApiKey = grokConfig["ApiKey"] ?? throw new Exception("Falta Grok API Key");
var openAiApiKey = openAiConfig["ApiKey"] ?? throw new Exception("Falta OpenAI API Key");

// ==============================
// 1. CHAT (GROK)
// ==============================

builder.Services.AddHttpClient("Grok", client =>
{
    client.BaseAddress = new Uri(grokConfig["Endpoint"] ?? "https://api.x.ai/v1");
});

builder.Services.AddKernel()
    .AddOpenAIChatCompletion(
        modelId: grokConfig["ChatModel"] ?? "grok-beta",
        apiKey: grokApiKey
    );

// ==============================
// 2. MEMORIA SEMÁNTICA (SIN QDRANT)
// ==============================

builder.Services.AddSingleton<ISemanticTextMemory>(sp =>
{
    return new MemoryBuilder()
        .WithOpenAITextEmbeddingGeneration(
            modelId: "text-embedding-3-small",
            apiKey: openAiApiKey
        )
        .Build();
});

// ==============================
// BUILD APP
// ==============================
var app = builder.Build();

// Forzamos Swagger afuera del IF para que lo veas siempre mientras probamos
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Socios API V1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();