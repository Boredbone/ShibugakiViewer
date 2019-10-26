using System;

namespace RuntimeCheckerCore
{
    class Program
    {
        static int Main(string[] args)
        {
            var ver = Environment.Version;
            var retVal = (ver.Major << 16) + (ver.Minor << 8) + ver.Build;
            Console.WriteLine(retVal);
            return retVal;
        }
    }
}
