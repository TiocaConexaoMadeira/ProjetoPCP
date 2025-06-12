using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConexaoMadeiraPCP.DTO
{
    public class OpcaoMontagemDto
    {
        public string AssinaturaUnica { get; set; } // ID único para a opção
        public double EspessuraMontagemCalculadaMM { get; set; }
        public List<LaminaDto> Laminas { get; set; } = new();
    }
}
