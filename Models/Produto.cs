using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConexaoMadeiraPCP.Models
{
    public class Produto
    {
        public string IdProduto { get; set; } = string.Empty;
        public string NomeProduto { get; set; } = string.Empty;
        public double EspessuraFinalDesejadaMM { get; set; }
        public int QuantidadeTotalLaminas { get; set; }
        public int NumeroLaminasCapa => 2;
        public int NumeroLaminasInternas => QuantidadeTotalLaminas - NumeroLaminasCapa;

    }
}
