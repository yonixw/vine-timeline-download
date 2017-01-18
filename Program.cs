using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Config = VineDownlaodJSONParser.Properties.Settings;

namespace VineDownlaodJSONParser
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("*** Console Start ***\n");
            VineJSONDownload vine = new VineJSONDownload(Config.Default.sessionID, Config.Default.savePath, Config.Default.errorPath);
            vine.Start();

            Console.WriteLine("*** Console End ***");
        }
    }
}
