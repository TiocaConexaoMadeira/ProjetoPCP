using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConexaoMadeiraPCP.Models
{
    public class LaminaEstoque
    {
        public int IdLaminaEstoque { get; set; } // PK da tabela de estoque
        public string TipoLaminaERP { get; set; } = string.Empty;
        public double EspessuraMM { get; set; }
        public int QuantidadeDisponivel { get; set; }
        public string ChaveUnica => $"{TipoLaminaERP}_{EspessuraMM}";
    }
}
