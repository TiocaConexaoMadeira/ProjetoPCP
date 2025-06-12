// Controllers/PlywoodController.cs
using ConexaoMadeiraPCP.DTO;
using ConexaoMadeiraPCP.Services;
using Microsoft.AspNetCore.Mvc;

namespace PlywoodProductionSystem.Controllers
{
    [ApiController]
    [Route("api")]
    public class PlywoodController : ControllerBase
    {
        private readonly PlywoodService _plywoodService;

        public PlywoodController(PlywoodService plywoodService)
        {
            _plywoodService = plywoodService;
        }

        [HttpGet("produtos")]
        public async Task<ActionResult<IEnumerable<ProdutoDto>>> GetProdutos()
        {
            // O serviço agora depende de um repositório, que é assíncrono
            var produtos = await _plywoodService.ListarProdutosAsync();
            var dtos = produtos.Select(p => new ProdutoDto {});
            return Ok(dtos);
        }

        [HttpGet("produtos/{idProdutoERP}/opcoes-montagem")]
        public async Task<ActionResult<IEnumerable<OpcaoMontagemDto>>> GetOpcoesDeMontagem(string idProdutoERP)
        {
            try
            {
                var opcoes = await _plywoodService.GerarOpcoesParaProdutoAsync(idProdutoERP);
                if (!opcoes.Any()) return NotFound("Nenhuma opção de montagem encontrada.");

                // Mapeia para DTOs
                var dtos = opcoes.Select(o => new OpcaoMontagemDto { /* ... mapeamento ... */ });
                return Ok(dtos);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Logar erro
                return StatusCode(500, "Erro interno no servidor.");
            }
        }

        [HttpPost("producoes")]
        public async Task<IActionResult> CriarProducao([FromBody] RegistrarProducaoRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // Encontra a opção escolhida pelo cliente usando a assinatura única
                var opcaoEscolhida = await _plywoodService.EncontrarOpcaoPorAssinaturaAsync(request.IdProdutoERP, request.AssinaturaOpcaoEscolhida);
                if (opcaoEscolhida == null)
                {
                    return BadRequest("A opção de montagem escolhida não é mais válida ou não existe. Tente gerar as opções novamente.");
                }

                await _plywoodService.RegistrarProducaoAsync(request.IdProdutoERP, opcaoEscolhida, request.QuantidadeAProduzir);

                return Ok(new { message = "Produção registrada com sucesso!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) // Captura erros de estoque
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Logar erro
                return StatusCode(500, "Erro interno ao registrar a produção.");
            }
        }
    }
}