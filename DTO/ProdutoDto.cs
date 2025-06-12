using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConexaoMadeiraPCP.DTO
{
    public class ProdutoDto
    {
        public string IdProduto { get; set; }
        public string NomeProduto { get; set; }
        public double EspessuraFinalDesejadaMM { get; set; }
        public int QuantidadeTotalLaminas { get; set; }
    }
}
