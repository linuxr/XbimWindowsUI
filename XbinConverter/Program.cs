using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XbinConverter.Export;
using Xbim.Ifc;

namespace XbinConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileName = args[0];
            
            var converter = new ConverterGLB();
            converter.Convert(fileName, fileName+".xbin");
        }
    }
}
