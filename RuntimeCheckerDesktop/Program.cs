using System;

namespace RuntimeCheckerDesktop
{
    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            var wpfLabel = new System.Windows.Controls.Label();
            var formLabel = new System.Windows.Forms.Label();
            var retVal = (formLabel.Height << 8) + (int)wpfLabel.FontSize;
            Console.WriteLine(retVal);
            return retVal;
        }
    }
}
