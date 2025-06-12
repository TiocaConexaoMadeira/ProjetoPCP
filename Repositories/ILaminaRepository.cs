using ConexaoMadeiraPCP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConexaoMadeiraPCP.Repositories
{
    public interface ILaminaRepository
    {
        Task<IEnumerable<LaminaEstoque>> ObterEstoqueDisponivelAsync();
        Task<ConfiguracaoTipos> ObterConfiguracaoTiposAsync();
        Task AtualizarEstoqueLaminasAsync(Dictionary<int, int> consumoPorId);
    }
}
