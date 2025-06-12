using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ConexaoMadeiraPCP.DTO
{
    public class RegistrarProducaoRequest
    {
        [Required]
        public string IdProdutoERP { get; set; }

        [Required]
        public string AssinaturaOpcaoEscolhida { get; set; }

        [Range(1, int.MaxValue)]
        public int QuantidadeAProduzir { get; set; }
    }
}
