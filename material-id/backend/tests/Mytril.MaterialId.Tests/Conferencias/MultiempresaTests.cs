using Microsoft.EntityFrameworkCore;
using Mytril.MaterialId.Conferencias.Domain;

namespace Mytril.MaterialId.Tests.Conferencias;

/// <summary>E1-03 (MYT-153): a empresa A não lê, não lista e não altera nada da empresa B.</summary>
public class MultiempresaTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
    private readonly Guid _empresaA = Guid.NewGuid();
    private readonly Guid _empresaB = Guid.NewGuid();

    [Fact]
    public async Task Empresa_nao_le_nem_lista_conferencias_de_outra()
    {
        using var banco = new BancoEmMemoria();
        Guid idDeB;
        await using (var dbB = banco.Conferencias(_empresaB))
        {
            var c = Conferencia.Abrir(_empresaB, Operacao.Recebimento, "Fornecedor de B", "beto", Agora);
            c.AdicionarAnalise("beto", Agora);
            dbB.Conferencias.Add(c);
            await dbB.SaveChangesAsync();
            idDeB = c.Id;
        }

        await using var dbA = banco.Conferencias(_empresaA);
        Assert.Empty(await dbA.Conferencias.ToListAsync());
        Assert.Empty(await dbA.Analises.ToListAsync());
        Assert.Null(await dbA.Conferencias.FirstOrDefaultAsync(c => c.Id == idDeB));
    }

    [Fact]
    public async Task Empresa_nao_grava_dados_de_outra()
    {
        using var banco = new BancoEmMemoria();
        await using var dbA = banco.Conferencias(_empresaA);
        dbA.Conferencias.Add(Conferencia.Abrir(_empresaB, Operacao.Expedicao, null, "intruso", Agora));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => dbA.SaveChangesAsync());
    }

    [Fact]
    public async Task Cada_empresa_ve_apenas_as_proprias()
    {
        using var banco = new BancoEmMemoria();
        await using (var dbA = banco.Conferencias(_empresaA))
        {
            dbA.Conferencias.Add(Conferencia.Abrir(_empresaA, Operacao.Recebimento, "A1", "ana", Agora));
            dbA.Conferencias.Add(Conferencia.Abrir(_empresaA, Operacao.Expedicao, "A2", "ana", Agora));
            await dbA.SaveChangesAsync();
        }
        await using (var dbB = banco.Conferencias(_empresaB))
        {
            dbB.Conferencias.Add(Conferencia.Abrir(_empresaB, Operacao.Recebimento, "B1", "beto", Agora));
            await dbB.SaveChangesAsync();
        }

        await using var leituraA = banco.Conferencias(_empresaA);
        await using var leituraB = banco.Conferencias(_empresaB);
        Assert.Equal(["A1", "A2"], (await leituraA.Conferencias.Select(c => c.Contraparte).ToListAsync()).Order());
        Assert.Equal(["B1"], await leituraB.Conferencias.Select(c => c.Contraparte).ToListAsync());
    }
}
