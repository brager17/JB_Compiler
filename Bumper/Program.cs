using System;
using Parser.Bumping;

namespace Bumper
{
    class Program
    {
        static void Main(string[] args)
        {
            // Bumping.StatementsExecutionIdentical();
            M();
        }

        static int GetInt(uint q) => int.MinValue + (int) (q - int.MaxValue - 1);

        static void M()
        {

            uint w = uint.MaxValue;
            uint ww = 10;
           
            // var t = (int) (object) long.MaxValue-2;
            // long x = long.MaxValue;
            // long xx = long.MaxValue - 1;
            // long xxx = 1;

            // int xxxx = int.MaxValue;
            // int xxxxx = int.MinValue;
            // int xxxxxx = int.MaxValue - 1;
            // int xxxxxxx = int.MinValue + 1;
            // int xxxxxxxx = -2;

            // uint xxxxxxxxx = uint.MaxValue;
            // uint xxxxxxxxxx = uint.MaxValue - 1;
            // uint xxxxxxxxxxx = int.MaxValue + 1U;
            // uint xxxxxxxxxxxx = 4294967295 - 1000;
            // uint xxxxxxxxxxxxx = uint.MinValue + 5;


            Console.WriteLine(GetInt(w));
            Console.WriteLine(GetInt(ww));
            // Console.WriteLine(xxxxx);
            // Console.WriteLine(xxxxxx);
            // Console.WriteLine(xxxxxxx);
            // Console.WriteLine(xxxxxxxx);
            // Console.WriteLine(xxxxxxxxx);
            // Console.WriteLine(xxxxxxxxxx);
            // Console.WriteLine(xxxxxxxxxxx);
            // Console.WriteLine(xxxxxxxxxxxx);
        }
    }
}