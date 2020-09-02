using Min_Helpers;
using Min_Helpers.LogHelper;
using Min_Helpers.PrintHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SocketClient
{
    class Program
    {
        enum SocketMode
        {
            Tcp,
            Udp
        }

        static Print PrintService { get; set; } = null;
        static Log LogService { get; set; } = null;

        public static Dictionary<string, string> Statuss { get; set; } = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");

            try
            {
                LogService = new Log();
                PrintService = new Print(LogService);

                LogService.Write("");
                PrintService.Log("App Start", Print.EMode.info);

                PrintService.Write("Mode (tcp / udp): ", Print.EMode.question);
                PrintService.WriteLine("tcp", ConsoleColor.Gray);
                SocketMode mode = SocketMode.Tcp;

                PrintService.Write("Server Ip: ", Print.EMode.question);
                IPAddress ip = IPAddress.Parse(Console.ReadLine());

                PrintService.Write("Server Port: ", Print.EMode.question);
                PrintService.WriteLine("12345", ConsoleColor.Gray);
                int port = 12345;

                StartClient(ip, port, mode);
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.GetReal(ex);
                PrintService.Log($"App Error, {ex.Message}", Print.EMode.error);
            }
            finally
            {
                PrintService.Log("App End", Print.EMode.info);
                Console.ReadKey();

                Environment.Exit(0);
            }
        }

        private static void StartClient(IPAddress ip, int port, SocketMode mode)
        {
            try
            {
                IPEndPoint iPEnd = new IPEndPoint(ip, port);
                SocketType socketType = mode == SocketMode.Tcp ? SocketType.Stream : SocketType.Dgram;
                ProtocolType protocolType = mode == SocketMode.Tcp ? ProtocolType.Tcp : ProtocolType.Udp;

                using (Socket client = new Socket(iPEnd.AddressFamily, socketType, protocolType))
                {
                    client.Connect(iPEnd);

                    EndPoint remote = client.RemoteEndPoint;

                    if (client.Connected)
                    {
                        PrintService.Log($"Server<{remote}> is connecting", Print.EMode.success);
                    }

                    using (CancellationTokenSource tokenSource = new CancellationTokenSource())
                    {
                        CancellationToken token = tokenSource.Token;

                        try
                        {
                            Task.Run(() =>
                            {
                                while (client.Connected)
                                {
                                    byte[] bytes = new byte[1024];

                                    int bytesRec = client.Receive(bytes);

                                    string message = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                                    if (!string.IsNullOrEmpty(message) && message != "ERROR")
                                    {
                                        string[] statuss = Regex.Split(message, "\r\n");
                                        foreach (var status in statuss)
                                        {
                                            if (string.IsNullOrEmpty(status)) continue;

                                            // "+STACH1:1,100000"
                                            string key = status.Substring(6, 1);
                                            string value = status.Substring(8, 1);
                                            Statuss[key] = value;
                                        }
                                    }
                                }
                            });

                            Observable
                                .Interval(TimeSpan.FromMilliseconds(1000 / 20))
                                .ObserveOn(NewThreadScheduler.Default)
                                .Subscribe((x) =>
                                {
                                    byte[] byteData = Encoding.ASCII.GetBytes($"AT+STACH0=?\r\n");
                                    client.Send(byteData);
                                });


                            Task.Run(() =>
                            {
                                while (true)
                                {
                                    foreach (var status in Statuss.ToList())
                                    {
                                        PrintService.WriteLine($"{status.Key}: {status.Value}", Print.EMode.message);
                                    }

                                    Thread.Sleep(10);
                                }
                            }).Wait();

                            client.Shutdown(SocketShutdown.Both);
                            client.Close();
                        }
                        catch (Exception)
                        {
                            tokenSource.Cancel();
                        }
                        finally
                        {
                            PrintService.Log($"Server<{remote}> was disconnected", Print.EMode.warning);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
