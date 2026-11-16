using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mytril.MaterialId.Api.Seguranca;

namespace Mytril.MaterialId.Tests.Api;

/// <summary>Ponta a ponta pela API: segurança, isolamento por empresa e fluxo de status com peso da tabela.</summary>
public class ApiTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly ApiFactory _api = new();
    private readonly Guid _empresa = Guid.NewGuid();

    public Task InitializeAsync() => _api.InitializeAsyncWrapper();
    public Task DisposeAsync() { _api.Dispose(); return Task.CompletedTask; }

    private HttpClient Operador(Guid? empresa = null) => _api.Cliente(empresa ?? _empresa, "joao", Permissoes.Capturar);
    private HttpClient Gestor() => _api.Cliente(_empresa, "maria", Permissoes.Capturar, Permissoes.Validar);
    private HttpClient Ia() => _api.Cliente(_empresa, "servidor-ia", Permissoes.ResultadoIa);

    private static async Task<JsonElement> Ler(HttpResponseMessage r)
    {
        var corpo = await r.Content.ReadAsStringAsync();
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode}: {corpo}");
        return JsonDocument.Parse(corpo).RootElement;
    }

    private async Task<Guid> ItemAsync(string busca)
    {
        var itens = await Ler(await Operador().GetAsync($"/api/catalogo/itens?familia=TuboRetangular&busca={Uri.EscapeDataString(busca)}"));
        return itens.EnumerateArray().Single(i => i.GetProperty("fabricante").GetString() == "Aço Cearense").GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Sem_token_recebe_401()
    {
        var r = await _api.CreateClient().GetAsync("/api/conferencias");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Operador_nao_valida_403()
    {
        var c = await Ler(await Operador().PostAsJsonAsync("/api/conferencias", new { operacao = "Recebimento" }, Json));
        var id = c.GetProperty("resumo").GetProperty("id").GetGuid();
        var r = await Operador().PostAsync($"/api/conferencias/{id}/validar", null);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Token_sem_empresa_recebe_403()
    {
        var semEmpresa = _api.Cliente(Guid.Empty, "x", Permissoes.Capturar);
        var r = await semEmpresa.GetAsync("/api/conferencias");
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Outra_empresa_recebe_404_ao_acessar_conferencia_pelo_id()
    {
        var c = await Ler(await Operador().PostAsJsonAsync("/api/conferencias", new { operacao = "Expedicao", contraparte = "Cliente X" }, Json));
        var id = c.GetProperty("resumo").GetProperty("id").GetGuid();
        var outra = Operador(Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, (await outra.GetAsync($"/api/conferencias/{id}")).StatusCode);
        Assert.Equal(0, (await Ler(await outra.GetAsync("/api/conferencias"))).GetArrayLength());
    }

    [Fact]
    public async Task Transicao_invalida_recebe_409_com_gatilhos_validos()
    {
        var c = await Ler(await Operador().PostAsJsonAsync("/api/conferencias", new { operacao = "Recebimento" }, Json));
        var id = c.GetProperty("resumo").GetProperty("id").GetGuid();
        var r = await Gestor().PostAsync($"/api/conferencias/{id}/validar", null);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        var problema = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Aberta", problema.GetProperty("statusAtual").GetString());
        Assert.Equal(["Encerrar", "Cancelar"], problema.GetProperty("gatilhosValidos").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task Fluxo_completo_com_correcao_do_operador_e_validacao_do_gestor()
    {
        var item40x20 = await ItemAsync("40x20 1,20");
        var item40x20Grosso = await ItemAsync("40x20 1,50");

        // Operador abre a conferência e fotografa um amarrado.
        var c = await Ler(await Operador().PostAsJsonAsync("/api/conferencias", new { operacao = "Recebimento", contraparte = "Aço Cearense" }, Json));
        var conferenciaId = c.GetProperty("resumo").GetProperty("id").GetGuid();
        c = await Ler(await Operador().PostAsync($"/api/conferencias/{conferenciaId}/analises", null));
        var analiseId = c.GetProperty("analises")[0].GetProperty("id").GetGuid();
        await Ler(await Operador().PostAsync($"/api/analises/{analiseId}/enviar-fotos", null));

        // Servidor de IA devolve o resultado; o peso sai da tabela do fabricante.
        c = await Ler(await Ia().PostAsJsonAsync($"/api/analises/{analiseId}/resultado-ia",
            new { medicao = new { itemCatalogoId = item40x20Grosso, quantidade = 98 }, confianca = 0.62, versaoModelo = "beta-0" }, Json));
        var analise = c.GetProperty("analises")[0];
        Assert.Equal("AguardandoConfirmacao", analise.GetProperty("status").GetString());

        // Operador nega e informa o valor correto (D6): era espessura 1,20.
        c = await Ler(await Operador().PostAsJsonAsync($"/api/analises/{analiseId}/corrigir", new { itemCatalogoId = item40x20, quantidade = 98 }, Json));
        analise = c.GetProperty("analises")[0];
        Assert.Equal("Corrigida", analise.GetProperty("status").GetString());
        Assert.Equal(642.096m, analise.GetProperty("medicaoAtual").GetProperty("pesoKg").GetDecimal());
        Assert.StartsWith("Tubo retangular 40×20 esp. 1,20", analise.GetProperty("medicaoAtual").GetProperty("descricao").GetString());

        // Operador encerra; gestor valida amarrado e conclui.
        await Ler(await Operador().PostAsync($"/api/conferencias/{conferenciaId}/encerrar", null));
        await Ler(await Gestor().PostAsync($"/api/analises/{analiseId}/validar", null));
        c = await Ler(await Gestor().PostAsync($"/api/conferencias/{conferenciaId}/validar", null));

        var resumo = c.GetProperty("resumo");
        Assert.Equal("Concluida", resumo.GetProperty("status").GetString());
        Assert.Equal(98, resumo.GetProperty("quantidadePecas").GetInt32());
        Assert.Equal(642.096m, resumo.GetProperty("pesoMedidoKg").GetDecimal());
        Assert.Equal(["Abrir", "Encerrar", "Validar"], c.GetProperty("transicoes").EnumerateArray().Select(t => t.GetProperty("gatilho").GetString()));
        Assert.Equal("maria", c.GetProperty("transicoes")[2].GetProperty("usuario").GetString());
    }

    [Fact]
    public async Task Calculo_de_peso_pela_api()
    {
        var item = await ItemAsync("40x20 1,20");
        var r = await Ler(await Operador().PostAsJsonAsync("/api/catalogo/peso", new { itemId = item, quantidade = 98 }, Json));
        Assert.Equal(642.096m, r.GetProperty("pesoKg").GetDecimal());
    }
}

internal static class ApiFactoryExtensions
{
    public static Task InitializeAsyncWrapper(this ApiFactory f) => f.InicializarAsync();
}
