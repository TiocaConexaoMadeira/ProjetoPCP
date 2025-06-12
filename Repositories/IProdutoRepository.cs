using ConexaoMadeiraPCP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConexaoMadeiraPCP.Repositories
{
    public interface IProdutoRepository
    {
        Task<IEnumerable<Produto>> ListarTodosAsync();
        Task<Produto> ObterPorIdAsync(string idProduto);
    }
}
