using System;
using System.Windows;

namespace AramBenchSwap.App
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            var application = new Application();
            application.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            using (var window = new MainWindow())
            {
                application.Run();
            }
        }
    }
}
