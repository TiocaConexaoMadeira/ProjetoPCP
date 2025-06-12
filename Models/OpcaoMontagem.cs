using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConexaoMadeiraPCP.Models
{
    public class OpcaoMontagem
    {
        public Produto ProdutoAlvo { get; set; }
        public List<LaminaEstoque> LaminasSelecionadas { get; set; } = new List<LaminaEstoque>();
        public double EspessuraMontagemCalculadaMM => LaminasSelecionadas.Sum(l => l.EspessuraMM);
        public string AssinaturaUnica => string.Join("|", LaminasSelecionadas.Select(l => l.IdLaminaEstoque));

    }
}
