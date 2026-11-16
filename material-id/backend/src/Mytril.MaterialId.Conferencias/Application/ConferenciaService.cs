using Microsoft.EntityFrameworkCore;
using Mytril.MaterialId.Catalogo.Application;
using Mytril.MaterialId.Conferencias.Domain;
using Mytril.MaterialId.Conferencias.Infrastructure;
using Mytril.MaterialId.SharedKernel;
using Mytril.MaterialId.SharedKernel.Exceptions;
using Mytril.MaterialId.SharedKernel.Tenancy;

namespace Mytril.MaterialId.Conferencias.Application;

/// <summary>Casos de uso de Conferência e Análise (E3-01, E4-04). O peso vem sempre do catálogo (E4-03).</summary>
public sealed class ConferenciaService(ConferenciasDbContext db, ICatalogoConsulta catalogo, IContextoUsuario usuario, IRelogio relogio)
{
    public const string UsuarioSistemaIa = "sistema:ia";

    public async Task<ConferenciaDetalheDto> AbrirAsync(AbrirConferenciaCommand cmd, CancellationToken ct)
    {
        var conferencia = Conferencia.Abrir(usuario.EmpresaId, cmd.Operacao, cmd.Contraparte, usuario.Usuario, relogio.Agora);
        db.Conferencias.Add(conferencia);
        await db.SaveChangesAsync(ct);
        return await DetalharAsync(conferencia, ct);
    }

    public async Task<IReadOnlyList<ConferenciaResumoDto>> ListarAsync(StatusConferencia? status, CancellationToken ct)
    {
        var query = db.Conferencias.AsNoTracking().Include(c => c.Analises).AsSplitQuery();
        if (status is { } s) query = query.Where(c => c.Status == s);
        var lista = await query.OrderByDescending(c => c.CriadaEm).Take(500).ToListAsync(ct);
        return lista.Select(Resumo).ToList();
    }

    public async Task<ConferenciaDetalheDto> ObterAsync(Guid id, CancellationToken ct) => await DetalharAsync(await CarregarAsync(id, ct), ct);

    public Task<ConferenciaDetalheDto> EncerrarAsync(Guid id, CancellationToken ct) => AlterarAsync(id, c => c.Encerrar(usuario.Usuario, relogio.Agora), ct);
    public Task<ConferenciaDetalheDto> ValidarAsync(Guid id, CancellationToken ct) => AlterarAsync(id, c => c.Validar(usuario.Usuario, relogio.Agora), ct);
    public Task<ConferenciaDetalheDto> DevolverAsync(Guid id, string motivo, CancellationToken ct) => AlterarAsync(id, c => c.Devolver(motivo, usuario.Usuario, relogio.Agora), ct);
    public Task<ConferenciaDetalheDto> CancelarAsync(Guid id, string motivo, CancellationToken ct) => AlterarAsync(id, c => c.Cancelar(motivo, usuario.Usuario, relogio.Agora), ct);

    public async Task<ConferenciaDetalheDto> AdicionarAnaliseAsync(Guid conferenciaId, CancellationToken ct)
    {
        var conferencia = await CarregarAsync(conferenciaId, ct);
        var analise = conferencia.AdicionarAnalise(usuario.Usuario, relogio.Agora);
        db.Analises.Add(analise);
        await db.SaveChangesAsync(ct);
        return await DetalharAsync(conferencia, ct);
    }

    public Task<ConferenciaDetalheDto> EnviarFotosAsync(Guid analiseId, CancellationToken ct) => AlterarPorAnaliseAsync(analiseId, (c, a) => c.EnviarFotos(a, usuario.Usuario, relogio.Agora), ct);
    public Task<ConferenciaDetalheDto> ReprocessarAsync(Guid analiseId, CancellationToken ct) => AlterarPorAnaliseAsync(analiseId, (c, a) => c.Reprocessar(a, usuario.Usuario, relogio.Agora), ct);
    public Task<ConferenciaDetalheDto> RefazerFotosAsync(Guid analiseId, CancellationToken ct) => AlterarPorAnaliseAsync(analiseId, (c, a) => c.RefazerFotos(a, usuario.Usuario, relogio.Agora), ct);
    public Task<ConferenciaDetalheDto> ConfirmarAsync(Guid analiseId, CancellationToken ct) => AlterarPorAnaliseAsync(analiseId, (c, a) => c.Confirmar(a, usuario.Usuario, relogio.Agora), ct);
    public Task<ConferenciaDetalheDto> ValidarAnaliseAsync(Guid analiseId, CancellationToken ct) => AlterarPorAnaliseAsync(analiseId, (c, a) => c.ValidarAnalise(a, usuario.Usuario, relogio.Agora), ct);
    public Task<ConferenciaDetalheDto> DevolverAnaliseAsync(Guid analiseId, string motivo, CancellationToken ct) => AlterarPorAnaliseAsync(analiseId, (c, a) => c.DevolverAnalise(a, motivo, usuario.Usuario, relogio.Agora), ct);
    public Task<ConferenciaDetalheDto> CancelarAnaliseAsync(Guid analiseId, string motivo, CancellationToken ct) => AlterarPorAnaliseAsync(analiseId, (c, a) => c.CancelarAnalise(a, motivo, usuario.Usuario, relogio.Agora), ct);
    public Task<ConferenciaDetalheDto> FalharAsync(Guid analiseId, string motivo, CancellationToken ct) => AlterarPorAnaliseAsync(analiseId, (c, a) => c.FalharProcessamento(a, motivo, UsuarioSistemaIa, relogio.Agora), ct);

    /// <summary>O operador nega e informa o valor correto. O peso é recalculado pela tabela do fabricante.</summary>
    public async Task<ConferenciaDetalheDto> CorrigirAsync(Guid analiseId, MedicaoCommand cmd, CancellationToken ct)
    {
        var medicao = await MedirAsync(cmd, ct);
        return await AlterarPorAnaliseAsync(analiseId, (c, a) => c.Corrigir(a, medicao, usuario.Usuario, relogio.Agora), ct);
    }

    /// <summary>Resultado do servidor de IA. Só o sistema chama (permissão própria).</summary>
    public async Task<ConferenciaDetalheDto> RegistrarResultadoIaAsync(Guid analiseId, ResultadoIaCommand cmd, CancellationToken ct)
    {
        var medicao = await MedirAsync(cmd.Medicao, ct);
        return await AlterarPorAnaliseAsync(analiseId, (c, a) => c.ConcluirProcessamento(a, medicao, cmd.Confianca, cmd.VersaoModelo, UsuarioSistemaIa, relogio.Agora), ct);
    }

    private async Task<Medicao> MedirAsync(MedicaoCommand cmd, CancellationToken ct)
    {
        var peso = await catalogo.CalcularPesoAsync(cmd.ItemCatalogoId, cmd.Quantidade, cmd.ComprimentoMm, cmd.LarguraMm, ct);
        return Medicao.Criar(cmd.ItemCatalogoId, cmd.Quantidade, cmd.ComprimentoMm, cmd.LarguraMm, peso.PesoKg);
    }

    private async Task<ConferenciaDetalheDto> AlterarAsync(Guid id, Action<Conferencia> acao, CancellationToken ct)
    {
        var conferencia = await CarregarAsync(id, ct);
        acao(conferencia);
        await db.SaveChangesAsync(ct);
        return await DetalharAsync(conferencia, ct);
    }

    private async Task<ConferenciaDetalheDto> AlterarPorAnaliseAsync(Guid analiseId, Action<Conferencia, Guid> acao, CancellationToken ct)
    {
        var conferenciaId = await db.Analises.Where(a => a.Id == analiseId).Select(a => (Guid?)a.ConferenciaId).FirstOrDefaultAsync(ct)
                            ?? throw new NaoEncontradoException($"Amarrado {analiseId} não encontrado.");
        return await AlterarAsync(conferenciaId, c => acao(c, analiseId), ct);
    }

    private async Task<Conferencia> CarregarAsync(Guid id, CancellationToken ct) =>
        await db.Conferencias.Include(c => c.Analises).AsSplitQuery().FirstOrDefaultAsync(c => c.Id == id, ct)
        ?? throw new NaoEncontradoException($"Conferência {id} não encontrada.");

    private static ConferenciaResumoDto Resumo(Conferencia c) => new(
        c.Id, c.Codigo, c.Operacao, c.Contraparte, c.Status, c.CriadaEm, c.CriadaPor,
        c.AnalisesAtivas.Count(), c.QuantidadePecas, c.PesoMedidoKg, c.GatilhosValidos);

    private async Task<ConferenciaDetalheDto> DetalharAsync(Conferencia c, CancellationToken ct)
    {
        var descricoes = new Dictionary<Guid, string?>();
        foreach (var itemId in c.Analises.SelectMany(a => new[] { a.Resultado?.ItemCatalogoId, a.Correcao?.ItemCatalogoId }).OfType<Guid>().Distinct())
            descricoes[itemId] = (await catalogo.ObterItemAsync(itemId, ct))?.Descricao;

        MedicaoDto? Map(Medicao? m) => m is null ? null
            : new MedicaoDto(m.ItemCatalogoId, descricoes.GetValueOrDefault(m.ItemCatalogoId), m.Quantidade, m.ComprimentoMm, m.LarguraMm, m.PesoKg);

        var analises = c.Analises.OrderBy(a => a.Sequencia).Select(a => new AnaliseDto(
            a.Id, a.Sequencia, a.Status, Map(a.Resultado), Map(a.Correcao), Map(a.MedicaoAtual), a.Confianca, a.VersaoModelo,
            a.MotivoFalha, a.GatilhosValidos, a.Transicoes.OrderBy(t => t.Em).Select(TransicaoDto.From).ToList())).ToList();

        return new ConferenciaDetalheDto(Resumo(c), analises, c.Transicoes.OrderBy(t => t.Em).Select(TransicaoDto.From).ToList());
    }
}
