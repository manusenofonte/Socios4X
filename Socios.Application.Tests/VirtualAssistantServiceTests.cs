using Socios.Application.Services;

namespace Socios.Application.Tests;

public class VirtualAssistantServiceTests
{
    [Fact]
    public void RespuestaNormal_SeDevuelveTalCualYNoSeRegistra()
    {
        var outcome = VirtualAssistantService.InterpretModelOutput("  Para pagar la cuota, ingresá a **Pagos**.  ");

        Assert.Equal("Para pagar la cuota, ingresá a **Pagos**.", outcome.Answer);
        Assert.False(outcome.ShouldRegister);
    }

    [Theory]
    [InlineData("[SIN_INFO]")]
    [InlineData(" [sin_info] ")]
    [InlineData("[SIN_INFO] No encontré nada sobre eso.")]
    public void SinInfo_MuestraMensajeEstandarYSeRegistra(string modelOutput)
    {
        var outcome = VirtualAssistantService.InterpretModelOutput(modelOutput);

        Assert.Equal(VirtualAssistantService.NoInformationMessage, outcome.Answer);
        Assert.True(outcome.ShouldRegister);
    }

    [Fact]
    public void FueraDeTema_MuestraMensajeEstandarYNoSeRegistra()
    {
        var outcome = VirtualAssistantService.InterpretModelOutput("[FUERA_DE_TEMA]");

        Assert.Equal(VirtualAssistantService.NoInformationMessage, outcome.Answer);
        Assert.False(outcome.ShouldRegister);
    }

    [Fact]
    public void AmbasMarcas_GanaSinInfoPorqueAnteLaDudaSeRegistra()
    {
        var outcome = VirtualAssistantService.InterpretModelOutput("[FUERA_DE_TEMA] [SIN_INFO]");

        Assert.True(outcome.ShouldRegister);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Lo siento, no tengo información sobre ese tema. Te recomiendo contactarte con la administración del club o 4x.")]
    public void RespuestaVaciaOMensajeSinMarca_SeRegistraParaNoPerderLaPregunta(string modelOutput)
    {
        var outcome = VirtualAssistantService.InterpretModelOutput(modelOutput);

        Assert.Equal(VirtualAssistantService.NoInformationMessage, outcome.Answer);
        Assert.True(outcome.ShouldRegister);
    }
}
