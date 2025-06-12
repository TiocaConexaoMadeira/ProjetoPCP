using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConexaoMadeiraPCP.Models
{
    public class ConfiguracaoTipos
    {
        public HashSet<string> TiposParaCapa { get; set; } = new();
        public HashSet<string> TiposParaMiolo { get; set; } = new();
        public HashSet<string> TiposParaEnchimento { get; set; } = new();
    }
}
