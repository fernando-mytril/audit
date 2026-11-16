using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mytril.MaterialId.SharedKernel.Exceptions;

namespace Mytril.MaterialId.Api.Infraestrutura;

/// <summary>Converte exceções de negócio em Problem Details (RFC 9457).</summary>
public sealed class TratamentoDeErros(IProblemDetailsService problemDetails, ILogger<TratamentoDeErros> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        var (status, titulo) = exception switch
        {
            TransicaoInvalidaException => (StatusCodes.Status409Conflict, "Transição de status não permitida"),
            NaoEncontradoException => (StatusCodes.Status404NotFound, "Não encontrado"),
            RegraDeNegocioException => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Acesso negado"),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno")
        };

        if (status == StatusCodes.Status500InternalServerError) logger.LogError(exception, "Erro não tratado");

        var problem = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = status == StatusCodes.Status500InternalServerError ? "Ocorreu um erro inesperado." : exception.Message
        };
        if (exception is TransicaoInvalidaException t)
        {
            problem.Extensions["statusAtual"] = t.StatusAtual;
            problem.Extensions["gatilho"] = t.Gatilho;
            problem.Extensions["gatilhosValidos"] = t.GatilhosValidos;
        }

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = problem, Exception = exception });
    }
}
