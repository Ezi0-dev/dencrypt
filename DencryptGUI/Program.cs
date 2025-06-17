using System;
using System.Windows.Forms;

namespace DencryptGUI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize(); // uses default WinForms settings
            Application.Run(new MainWindow());
        }
    }
}