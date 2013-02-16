using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmARestarter
{
    class Program
    {
        static void Main(string[] args)
        {
            var r = new Restarter();
            r.ServerName = "dayz1";

            r.Restart();
            
        }
    }
}
