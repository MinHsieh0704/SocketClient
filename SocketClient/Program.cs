using Min_Helpers;
using Min_Helpers.PrintHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketClient
{
    class Program
    {
        static Print PrintService { get; set; } = null;

        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");

            try
            {
                PrintService = new Print();

                PrintService.WriteLine("App Start", Print.EMode.info);
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.GetReal(ex);
                PrintService.WriteLine($"App Error, {ex.Message}", Print.EMode.error);
            }
            finally
            {
                PrintService.WriteLine("App End", Print.EMode.info);
                Console.ReadKey();

                Environment.Exit(0);
            }
        }
    }
}
