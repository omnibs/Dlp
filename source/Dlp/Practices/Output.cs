using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogPlayer.Master
{
    using System.IO;

    class Output
    {
        public static void Write(string s, bool writeToDisk = true)
        {
            Console.Write(s);
            
            bool retry;
            if (writeToDisk)
            do
            {
                try
                {
                    File.AppendAllText("output.txt", s);
                    retry = false;
                }
                catch
                {
                    retry = true;
                }
            }
            while (retry);

        }

        public static void WriteLine(string s, bool writeToDisk = true)
        {
            Console.WriteLine(s);
            bool retry;
            if (writeToDisk)
            do
            {
                try
                {
                    File.AppendAllText("output.txt", s + Environment.NewLine);
                    retry = false;
                }
                catch
                {
                    retry = true;
                }
            }
            while (retry);
        }

        public static void BlankLine()
        {
            Output.WriteLine(string.Empty);
        }
    }
}
