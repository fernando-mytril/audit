using System.Net.Http.Json;
using System.Text.Json;
using Mytril.MaterialId.Api.Seguranca;

namespace Mytril.MaterialId.Tests.Api;

/// <summary>A IA simulada (só no ambiente de teste) conclui os amarrados em processamento com um item real do catálogo.</summary>
public class IaSimuladaTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new(new Dictionary<string, string?>
    {
        ["IaSimulada:Habilitada"] = "true",
        ["IaSimulada:SegundosDeProcessamento"] = "0",
        ["IaSimulada:TaxaDeFalha"] = "0",
    });

    public Task InitializeAsync() => _api.InicializarAsync();
    public Task DisposeAsync() { _api.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Amarrado_enviado_recebe_resultado_com_peso_da_tabela()
    {
        var operador = _api.Cliente(Guid.NewGuid(), "joao", Permissoes.Capturar);
        var c = JsonDocument.Parse(await (await operador.PostAsJsonAsync("/api/conferencias", new { operacao = "Recebimento" })).Content.ReadAsStringAsync()).RootElement;
        var id = c.GetProperty("resumo").GetProperty("id").GetGuid();
        c = JsonDocument.Parse(await (await operador.PostAsync($"/api/conferencias/{id}/analises", null)).Content.ReadAsStringAsync()).RootElement;
        var analiseId = c.GetProperty("analises")[0].GetProperty("id").GetGuid();
        (await operador.PostAsync($"/api/analises/{analiseId}/enviar-fotos", null)).EnsureSuccessStatusCode();

        JsonElement analise = default;
        for (var i = 0; i < 40; i++)
        {
            await Task.Delay(250);
            analise = JsonDocument.Parse(await operador.GetStringAsync($"/api/conferencias/{id}")).RootElement.GetProperty("analises")[0];
            if (analise.GetProperty("status").GetString() != "Processando") break;
        }

        Assert.Equal("AguardandoConfirmacao", analise.GetProperty("status").GetString());
        Assert.Equal("simulador-dev", analise.GetProperty("versaoModelo").GetString());
        Assert.True(analise.GetProperty("medicaoAtual").GetProperty("pesoKg").GetDecimal() > 0);
        Assert.False(string.IsNullOrEmpty(analise.GetProperty("medicaoAtual").GetProperty("descricao").GetString()));
    }
}
