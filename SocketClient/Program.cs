using Min_Helpers;
using Min_Helpers.PrintHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");

            try
            {
                PrintService = new Print();

                PrintService.WriteLine("App Start", Print.EMode.info);

                PrintService.Write("Mode (tcp / udp): ", Print.EMode.question);
                SocketMode mode = Console.ReadLine() == "tcp" ? SocketMode.Tcp : SocketMode.Udp;

                PrintService.Write("Server Ip: ", Print.EMode.question);
                IPAddress ip = IPAddress.Parse(Console.ReadLine());

                PrintService.Write("Server Port: ", Print.EMode.question);
                int port = Convert.ToInt32(Console.ReadLine());

                StartClient(ip, port, mode);
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

                        Task.Run(() =>
                        {
                            try
                            {
                                while (client.Connected && !token.IsCancellationRequested)
                                {
                                    string message = Console.ReadLine();
                                    if (message == "") continue;

                                    PrintService.Log($"Client: {message}", Print.EMode.message);

                                    byte[] byteData = Encoding.ASCII.GetBytes($"{message}\r\n");
                                    client.Send(byteData);
                                }
                            }
                            catch (Exception)
                            {
                                tokenSource.Cancel();
                            }
                        }, token);

                        try
                        {
                            while (client.Connected && !token.IsCancellationRequested)
                            {

                                byte[] bytes = new byte[1024];

                                int bytesRec = client.Receive(bytes);

                                string message = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                                message = message.Replace("\n", "").Replace("\r", "");

                                if (!string.IsNullOrEmpty(message))
                                {
                                    PrintService.Log($"Server: {message}", Print.EMode.message);
                                }
                            }

                            tokenSource.Cancel();

                            client.Shutdown(SocketShutdown.Both);
                            client.Close();
                        }
                        catch (Exception)
                        {
                            tokenSource.Cancel();
                        }
                        finally
                        {
                            PrintService.Log($"Server({remote}) was disconnected", Print.EMode.warning);
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
