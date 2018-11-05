using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public static class Log
    {
        public static void LogError(Exception e)
        {
            LogText(e.ToString());
        }
        public static void LogText(string str)
        {
            //TODO: Write this to output / log file.

        }

    }
}
