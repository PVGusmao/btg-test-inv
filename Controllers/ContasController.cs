using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ProjetoBtg.Api.Contracts;
using ProjetoBtg.Api.Events;
using ProjetoBtg.Api.Models;
using ProjetoBtg.Api.Repositories;

namespace ProjetoBtg.Api.Controllers;

[ApiController]
[Route("api/contas")]
public sealed class ContasController : ControllerBase
{
    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(5),
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };
    private const string TodasContasCacheKey = "contas:todas";

    private readonly IContaRepository _contas;
    private readonly IMemoryCache _cache;
    private readonly IContaCriadaEventPublisher _publicador;
    private readonly IRegistroNotificacoes _notificacoes;
    private readonly ILogger<ContasController> _logger;

    public ContasController(
        IContaRepository contas,
        IMemoryCache cache,
        IContaCriadaEventPublisher publicador,
        IRegistroNotificacoes notificacoes,
        ILogger<ContasController> logger)
    {
        _contas = contas;
        _cache = cache;
        _publicador = publicador;
        _notificacoes = notificacoes;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<Conta>> ObterTodas()
    {
        if (_cache.TryGetValue(TodasContasCacheKey, out IReadOnlyCollection<Conta>? contas))
        {
            _logger.LogInformation("Cache hit ao listar contas.");
            return Ok(contas);
        }

        _logger.LogInformation("Cache miss ao listar contas. Consultando o repositório em memória.");
        contas = _contas.ObterTodas();
        _cache.Set(TodasContasCacheKey, contas, CacheOptions);
        return Ok(contas);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Conta> ObterPorId(Guid id)
    {
        var cacheKey = $"conta:{id}";
        if (_cache.TryGetValue(cacheKey, out Conta? conta))
        {
            _logger.LogInformation("Cache hit para a conta {ContaId}.", id);
            return Ok(conta);
        }

        _logger.LogInformation(
            "Cache miss para a conta {ContaId}. Consultando o repositório em memória.", id);
        conta = _contas.ObterPorId(id);
        if (conta is null) return NotFound();

        _cache.Set(cacheKey, conta, CacheOptions);
        return Ok(conta);
    }

    [HttpPost]
    public async Task<ActionResult<Conta>> Criar(ContaRequest request, CancellationToken cancellationToken)
    {
        var conta = new Conta(request.NomeTitular, request.Cpf, request.Status);
        _contas.Adicionar(conta);
        InvalidarCache(conta.Id);

        await _publicador.PublicarAsync(
            new ContaCriadaEvent(conta.Id, DateTimeOffset.UtcNow), cancellationToken);

        return CreatedAtAction(nameof(ObterPorId), new { id = conta.Id }, conta);
    }

    [HttpPut("{id:guid}")]
    public ActionResult<Conta> Atualizar(Guid id, ContaRequest request)
    {
        var conta = _contas.ObterPorId(id);
        if (conta is null) return NotFound();

        conta.Atualizar(request.NomeTitular, request.Cpf, request.Status);
        _contas.Atualizar(conta);
        InvalidarCache(id);
        return Ok(conta);
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Remover(Guid id)
    {
        if (!_contas.Remover(id)) return NotFound();
        InvalidarCache(id);
        return NoContent();
    }

    [HttpGet("{id:guid}/notificacoes")]
    public ActionResult<IReadOnlyCollection<NotificacaoArea>> ObterNotificacoes(Guid id)
    {
        if (_contas.ObterPorId(id) is null) return NotFound();
        return Ok(_notificacoes.ObterPorConta(id));
    }

    [HttpPost("{id:guid}/confirmacoes/prevencao")]
    public ActionResult<Conta> ConfirmarPrevencao(Guid id)
    {
        var conta = _contas.ObterPorId(id);
        if (conta is null) return NotFound();

        conta.ConfirmarPrevencao();
        _contas.Atualizar(conta);
        InvalidarCache(id);
        return Ok(conta);
    }

    [HttpPost("{id:guid}/confirmacoes/cartao")]
    public ActionResult<Conta> ConfirmarCartao(Guid id)
    {
        var conta = _contas.ObterPorId(id);
        if (conta is null) return NotFound();

        conta.ConfirmarCartao();
        _contas.Atualizar(conta);
        InvalidarCache(id);
        return Ok(conta);
    }

    private void InvalidarCache(Guid id)
    {
        _cache.Remove(TodasContasCacheKey);
        _cache.Remove($"conta:{id}");
    }
}


