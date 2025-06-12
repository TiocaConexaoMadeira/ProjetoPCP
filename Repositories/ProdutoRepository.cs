using ConexaoMadeiraPCP.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConexaoMadeiraPCP.Repositories
{
    public class ProdutoRepository : IProdutoRepository
    {
        private readonly string _connectionString;
        public ProdutoRepository(IConfiguration configuration) => _connectionString = configuration.GetConnectionString("PlywoodDb");

        public async Task<Produto> ObterPorIdAsync(string idProduto)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Produto>(
                "SELECT codigo AS IdProduto, nomepro AS NomeProduto, d_espessura AS EspessuraFinalDesejada FROM produtosc WHERE id_produto = @idProduto",
                new { idProduto });
        }

        public async Task<IEnumerable<Produto>> ListarTodosAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Produto>(
                "SELECT id_produto AS IdProduto, nome_produto AS NomeProduto, espessura_final_desejada_mm AS EspessuraFinalDesejadaMM, quantidade_total_laminas AS QuantidadeTotalLaminas FROM produtos_erp");
        }
    }

    public class LaminaRepository : ILaminaRepository
    {
        private readonly string _connectionString;
        public LaminaRepository(IConfiguration configuration) => _connectionString = configuration.GetConnectionString("PlywoodDb");

        public async Task<ConfiguracaoTipos> ObterConfiguracaoTiposAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var mapeamentos = await connection.QueryAsync<(string tipo_lamina_erp, string funcao_estrutural)>("SELECT tipo_lamina_erp, funcao_estrutural FROM configuracao_funcao_laminas");

            var config = new ConfiguracaoTipos();
            foreach (var map in mapeamentos)
            {
                if (map.funcao_estrutural.Equals("Capa", StringComparison.OrdinalIgnoreCase)) config.TiposParaCapa.Add(map.tipo_lamina_erp);
                else if (map.funcao_estrutural.Equals("Miolo", StringComparison.OrdinalIgnoreCase)) config.TiposParaMiolo.Add(map.tipo_lamina_erp);
                else if (map.funcao_estrutural.Equals("Enchimento", StringComparison.OrdinalIgnoreCase)) config.TiposParaEnchimento.Add(map.tipo_lamina_erp);
            }
            return config;
        }

        public async Task<IEnumerable<LaminaEstoque>> ObterEstoqueDisponivelAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<LaminaEstoque>(
                "SELECT id_lamina_estoque AS IdLaminaEstoque, tipo_lamina_erp AS TipoLaminaERP, espessura_mm AS EspessuraMM, quantidade_disponivel AS QuantidadeDisponivel FROM laminas_estoque_erp WHERE quantidade_disponivel > 0");
        }

        public async Task AtualizarEstoqueLaminasAsync(Dictionary<int, int> consumoPorId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                foreach (var item in consumoPorId)
                {
                    var idLaminaEstoque = item.Key;
                    var quantidadeConsumida = item.Value;
                    var rowsAffected = await connection.ExecuteAsync(
                        "UPDATE laminas_estoque_erp SET quantidade_disponivel = quantidade_disponivel - @quantidadeConsumida WHERE id_lamina_estoque = @idLaminaEstoque AND quantidade_disponivel >= @quantidadeConsumida",
                        new { quantidadeConsumida, idLaminaEstoque },
                        transaction: transaction);

                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException($"Falha ao atualizar estoque para a lâmina ID {idLaminaEstoque}. Estoque insuficiente ou lâmina não encontrada.");
                    }
                }
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
