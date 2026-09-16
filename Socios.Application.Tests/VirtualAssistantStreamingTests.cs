using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Socios.Application.DTOs;
using Socios.Application.Interfaces;
using Socios.Application.Services;
using Socios.Domain.Entities;

namespace Socios.Application.Tests;

public class VirtualAssistantStreamingTests
{
    [Fact]
    public async Task RespuestaNormal_LlegaEnPartesYNoSeRegistra()
    {
        var (service, questions) = CreateService("Para ", "pagar ", "la cuota...");

        var parts = await CollectAsync(service.AskQuestionStreamAsync("¿cómo pago?", null));

        Assert.Equal("Para pagar la cuota...", string.Concat(parts));
        Assert.True(parts.Count > 1);
        Assert.Empty(questions.Added);
    }

    [Fact]
    public async Task SinInfoEnPedazos_NuncaLlegaLaMarcaYSeRegistra()
    {
        var (service, questions) = CreateService("[SIN", "_IN", "FO]");

        var parts = await CollectAsync(service.AskQuestionStreamAsync("¿cómo cargo un profesor?", 7));

        Assert.Equal([VirtualAssistantService.NoInformationMessage], parts);
        var registered = Assert.Single(questions.Added);
        Assert.Equal("¿cómo cargo un profesor?", registered.Question);
        Assert.Equal(7, registered.ClubId);
    }

    [Fact]
    public async Task FueraDeTema_MuestraMensajeEstandarYNoSeRegistra()
    {
        var (service, questions) = CreateService("[FUERA", "_DE_TEMA]");

        var parts = await CollectAsync(service.AskQuestionStreamAsync("¿cómo hago una milanesa?", null));

        Assert.Equal([VirtualAssistantService.NoInformationMessage], parts);
        Assert.Empty(questions.Added);
    }

    [Fact]
    public async Task RespuestaQueEmpiezaConLinkMarkdown_SeMandaNormal()
    {
        var (service, _) = CreateService("[Mercado Pago]", "(https://4x.com.ar) permite pagar online.");

        var parts = await CollectAsync(service.AskQuestionStreamAsync("¿puedo pagar online?", null));

        Assert.Equal("[Mercado Pago](https://4x.com.ar) permite pagar online.", string.Concat(parts));
    }

    [Fact]
    public async Task SiFallaElRegistro_ElUsuarioIgualRecibeLaRespuesta()
    {
        var (service, questions) = CreateService("[SIN_INFO]");
        questions.ThrowOnAdd = true;

        var parts = await CollectAsync(service.AskQuestionStreamAsync("¿algo del club?", null));

        Assert.Equal([VirtualAssistantService.NoInformationMessage], parts);
    }

    [Theory]
    [InlineData("", "Undecided")]
    [InlineData("  ", "Undecided")]
    [InlineData("[SIN", "Undecided")]
    [InlineData("[SIN_INFO]", "Marker")]
    [InlineData(" [fuera_de_tema]", "Marker")]
    [InlineData("[Link](url)", "Answer")]
    [InlineData("Hola", "Answer")]
    public void DecideStreamStart_DecideCuandoEmpezarAMandar(string outputSoFar, string expected)
    {
        Assert.Equal(expected, VirtualAssistantService.DecideStreamStart(outputSoFar).ToString());
    }

    private static (VirtualAssistantService Service, FakeUnansweredQuestionRepository Questions) CreateService(params string[] modelChunks)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IChatCompletionService>(new FakeChatCompletionService(modelChunks));

        var questions = new FakeUnansweredQuestionRepository();
        var service = new VirtualAssistantService(
            builder.Build(),
            new EmptyFaqRepository(),
            new EmptyVectorRepository(),
            questions,
            Options.Create(new AssistantOptions()),
            NullLogger<VirtualAssistantService>.Instance);

        return (service, questions);
    }

    private static async Task<List<string>> CollectAsync(IAsyncEnumerable<string> stream)
    {
        var parts = new List<string>();
        await foreach (var part in stream)
            parts.Add(part);
        return parts;
    }

    private sealed class FakeChatCompletionService(string[] chunks) : IChatCompletionService
    {
        public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ChatMessageContent> result = [new ChatMessageContent(AuthorRole.Assistant, string.Concat(chunks))];
            return Task.FromResult(result);
        }

        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var chunk in chunks)
            {
                await Task.Yield();
                yield return new StreamingChatMessageContent(AuthorRole.Assistant, chunk);
            }
        }
    }

    private sealed class FakeUnansweredQuestionRepository : IUnansweredQuestionRepository
    {
        public List<UnansweredQuestion> Added { get; } = [];
        public bool ThrowOnAdd { get; set; }

        public Task AddAsync(UnansweredQuestion question, CancellationToken cancellationToken = default)
        {
            if (ThrowOnAdd)
                throw new InvalidOperationException("La base no responde");

            Added.Add(question);
            return Task.CompletedTask;
        }

        public Task<PagedResult<UnansweredQuestion>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class EmptyFaqRepository : IFAQRepository
    {
        public Task<IEnumerable<FAQMatch>> SearchRelevantFAQsAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<FAQMatch>());

        public Task<IEnumerable<FrequentlyQuestion>> GetMostFrequentAsync(int count, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<FrequentlyQuestion>());
    }

    private sealed class EmptyVectorRepository : IVectorKnowledgeRepository
    {
        public Task<IEnumerable<KnowledgeChunk>> SearchAsync(string query, int? clubId, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<KnowledgeChunk>());

        public Task UpsertAsync(int documentId, string source, int? clubId, IEnumerable<string> textChunks, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteByDocumentIdAsync(int documentId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateSourceAsync(int documentId, string newSource, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
