using ConexaoMadeiraPCP.Models;
using ConexaoMadeiraPCP.Repositories;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace ConexaoMadeiraPCP.Services
{
    public class PlywoodService
    {
        private readonly IProdutoRepository _produtoRepo;
        private readonly ILaminaRepository _laminaRepo;
        private readonly IConfiguration _config;
        private readonly double _perdaNaPrensaMM;
        private readonly double _toleranciaEspessuraMM;

        public PlywoodService(IProdutoRepository produtoRepo, ILaminaRepository laminaRepo, IConfiguration config)
        {
            _produtoRepo = produtoRepo;
            _laminaRepo = laminaRepo;
            _config = config;
            _perdaNaPrensaMM = _config.GetValue<double>("PlywoodSettings:PerdaNaPrensaMM");
            _toleranciaEspessuraMM = _config.GetValue<double>("PlywoodSettings:ToleranciaEspessuraMM");
        }

        // As GerarOpcoesParaProduto e RegistrarProducao usam os repositórios

        public async Task<List<OpcaoMontagem>> GerarOpcoesParaProdutoAsync(string idProdutoERP)
        {
            // 1. Obter dados usando repositórios
            var produtoAlvo = await _produtoRepo.ObterPorIdAsync(idProdutoERP);
            if (produtoAlvo == null) throw new KeyNotFoundException($"Produto com ID '{idProdutoERP}' não encontrado.");

            var estoqueDisponivel = await _laminaRepo.ObterEstoqueDisponivelAsync();
            var configTipos = await _laminaRepo.ObterConfiguracaoTiposAsync();

            // 2. Preparar para a lógica de negócio
            double targetMontagem = produtoAlvo.EspessuraFinalDesejadaMM + _perdaNaPrensaMM;
            var opcoesFinais = new List<OpcaoMontagem>();

            var laminasCapa = estoqueDisponivel.Where(l => configTipos.TiposParaCapa.Contains(l.TipoLaminaERP)).ToList();
            var laminasMiolo = estoqueDisponivel.Where(l => configTipos.TiposParaMiolo.Contains(l.TipoLaminaERP)).ToList();
            var laminasEnchimento = estoqueDisponivel.Where(l => configTipos.TiposParaEnchimento.Contains(l.TipoLaminaERP)).ToList();

            var estoqueSimulado = estoqueDisponivel.ToDictionary(l => l.IdLaminaEstoque, l => l.QuantidadeDisponivel);

            // 3. Executar a lógica de combinação recursiva
            ConstruirCompensadoRecursivo(new List<LaminaEstoque>(), 0, 0, 0, true, estoqueSimulado, produtoAlvo,
                laminasCapa, laminasMiolo, laminasEnchimento, targetMontagem, _toleranciaEspessuraMM, opcoesFinais);

            return opcoesFinais.DistinctBy(o => o.AssinaturaUnica).ToList();
        }

        public async Task<OpcaoMontagem> EncontrarOpcaoPorAssinaturaAsync(string idProdutoERP, string assinatura)
        {
            // Esta função é necessária para o registro da produção.
            // Ela regera as opções (o que é rápido se não houver muitas) para encontrar a correspondente.
            var opcoes = await GerarOpcoesParaProdutoAsync(idProdutoERP);
            return opcoes.FirstOrDefault(o => o.AssinaturaUnica == assinatura);
        }

        public async Task RegistrarProducaoAsync(string idProdutoERP, OpcaoMontagem opcaoEscolhida, int quantidadeDesejada)
        {
            if (opcaoEscolhida == null) throw new ArgumentNullException(nameof(opcaoEscolhida));

            var consumoPorId = opcaoEscolhida.LaminasSelecionadas
                .GroupBy(l => l.IdLaminaEstoque)
                .ToDictionary(g => g.Key, g => g.Count() * quantidadeDesejada);

            await _laminaRepo.AtualizarEstoqueLaminasAsync(consumoPorId);
        }

        private void ConstruirCompensadoRecursivo(
            List<LaminaEstoque> laminasAtuaisNaMontagem,
            int numCapasAdicionadas,
            int numMiolosAdicionados,
            int numEnchimentosAdicionados,
            bool proximaInternaDeveSerMiolo,
            Dictionary<int, int> estoqueSimulado, 
            Produto produtoAlvo,
            List<LaminaEstoque> todasCapas,
            List<LaminaEstoque> todosMiolos,
            List<LaminaEstoque> todosEnchimentos,
            double targetEspessuraMontagem,
            double toleranciaEspessura,
            List<OpcaoMontagem> opcoesFinais)
        {
            // --- CONDIÇÃO DE PARADA (BASE DA RECURSÃO) ---
            // Se atingimos o número total de lâminas definido na receita do produto...
            if (laminasAtuaisNaMontagem.Count == produtoAlvo.QuantidadeTotalLaminas)
            {
                // ...verificamos se a espessura total está dentro da tolerância.
                double espessuraMontagemAtual = laminasAtuaisNaMontagem.Sum(l => l.EspessuraMM);
                if (Math.Abs(espessuraMontagemAtual - targetEspessuraMontagem) <= toleranciaEspessura)
                {
                    // SUCESSO!
                    var novaOpcao = new OpcaoMontagem
                    {
                        ProdutoAlvo = produtoAlvo,
                        // criar uma NOVA lista e clonar cada lâmina para que esta opção
                        // não seja modificada pelo processo de backtracking da recursão.
                        LaminasSelecionadas = new List<LaminaEstoque>(laminasAtuaisNaMontagem.Select(ClonarLamina))
                    };
                    opcoesFinais.Add(novaOpcao);
                }
                
                return;
            }

            // --- CONDIÇÃO DE (OTIMIZAÇÃO) ---
            // Se a espessura atual já ultrapassou o alvo mais a tolerância,
            // não há sentido em adicionar mais lâminas. Poda este "ramo" da árvore de busca.
            double espessuraAtual = laminasAtuaisNaMontagem.Sum(l => l.EspessuraMM);
            if (espessuraAtual > targetEspessuraMontagem + toleranciaEspessura)
            {
                return;
            }

            // --- LÓGICA RECURSIVA (ADICIONAR PRÓXIMA CAMADA) ---

            // Passo 1: Adicionar as CAPAS
            if (numCapasAdicionadas < produtoAlvo.NumeroLaminasCapa)
            {
                // Itera sobre todas as lâminas que podem ser usadas como capa
                foreach (var capaCandidata in todasCapas)
                {
                    // Verifica se há estoque para a lâmina candidata
                    if (estoqueSimulado[capaCandidata.IdLaminaEstoque] > 0)
                    {
                        // Ação: Adiciona a lâmina à montagem atual e consome do estoque simulado
                        laminasAtuaisNaMontagem.Add(capaCandidata);
                        estoqueSimulado[capaCandidata.IdLaminaEstoque]--;

                        // Chamada Recursiva: Continua construindo o restante do compensado
                        ConstruirCompensadoRecursivo(laminasAtuaisNaMontagem, numCapasAdicionadas + 1,
                            numMiolosAdicionados, numEnchimentosAdicionados, proximaInternaDeveSerMiolo,
                            estoqueSimulado, produtoAlvo, todasCapas, todosMiolos, todosEnchimentos,
                            targetEspessuraMontagem, toleranciaEspessura, opcoesFinais);

                        // Backtrack: Desfaz a ação para poder explorar outras possibilidades
                        estoqueSimulado[capaCandidata.IdLaminaEstoque]++;
                        laminasAtuaisNaMontagem.RemoveAt(laminasAtuaisNaMontagem.Count - 1);
                    }
                }
            }
            // Passo 2: Adicionar as CAMADAS INTERNAS (após as capas já terem sido adicionadas)
            else
            {
                int numInternasAdicionadas = numMiolosAdicionados + numEnchimentosAdicionados;
                if (numInternasAdicionadas < produtoAlvo.NumeroLaminasInternas)
                {
                    List<LaminaEstoque> laminasParaIterar;
                    bool estaCamadaEhMiolo = false;

                    // Determina qual tipo de lâmina interna tentar primeiro (Miolo ou Enchimento)
                    if (proximaInternaDeveSerMiolo && todosMiolos.Any(m => estoqueSimulado[m.IdLaminaEstoque] > 0))
                    {
                        laminasParaIterar = todosMiolos;
                        estaCamadaEhMiolo = true;
                    }
                    else if (!proximaInternaDeveSerMiolo && todosEnchimentos.Any(e => estoqueSimulado[e.IdLaminaEstoque] > 0))
                    {
                        laminasParaIterar = todosEnchimentos;
                        estaCamadaEhMiolo = false;
                    }
                    // Fallback: Se o tipo esperado não tem estoque, tenta o outro tipo
                    else if (todosMiolos.Any(m => estoqueSimulado[m.IdLaminaEstoque] > 0))
                    {
                        laminasParaIterar = todosMiolos;
                        estaCamadaEhMiolo = true;
                    }
                    else if (todosEnchimentos.Any(e => estoqueSimulado[e.IdLaminaEstoque] > 0))
                    {
                        laminasParaIterar = todosEnchimentos;
                        estaCamadaEhMiolo = false;
                    }
                    else
                    {
                        return; // Não há mais lâminas internas com estoque para adicionar
                    }

                    // Itera sobre as candidatas para a camada interna
                    foreach (var internaCandidata in laminasParaIterar)
                    {
                        if (estoqueSimulado[internaCandidata.IdLaminaEstoque] > 0)
                        {
                            // Ação
                            laminasAtuaisNaMontagem.Add(internaCandidata);
                            estoqueSimulado[internaCandidata.IdLaminaEstoque]--;

                            // Chamada Recursiva
                            ConstruirCompensadoRecursivo(laminasAtuaisNaMontagem, numCapasAdicionadas,
                                estaCamadaEhMiolo ? numMiolosAdicionados + 1 : numMiolosAdicionados,
                                !estaCamadaEhMiolo ? numEnchimentosAdicionados + 1 : numEnchimentosAdicionados,
                                !proximaInternaDeveSerMiolo, // Alterna para a próxima camada
                                estoqueSimulado, produtoAlvo, todasCapas, todosMiolos, todosEnchimentos,
                                targetEspessuraMontagem, toleranciaEspessura, opcoesFinais);

                            // Backtrack
                            estoqueSimulado[internaCandidata.IdLaminaEstoque]++;
                            laminasAtuaisNaMontagem.RemoveAt(laminasAtuaisNaMontagem.Count - 1);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Função auxiliar para criar uma cópia de um objeto LaminaEstoque.
        /// Essencial para garantir que cada OpcaoMontagem tenha sua própria lista de objetos.
        /// </summary>
        private LaminaEstoque ClonarLamina(LaminaEstoque original)
        {
            return new LaminaEstoque
            {
                IdLaminaEstoque = original.IdLaminaEstoque,
                TipoLaminaERP = original.TipoLaminaERP,
                EspessuraMM = original.EspessuraMM,
                QuantidadeDisponivel = original.QuantidadeDisponivel
            };
        }

        internal async Task<IEnumerable<object>> ListarProdutosAsync()
        {
            throw new NotImplementedException();
        }
    }
}