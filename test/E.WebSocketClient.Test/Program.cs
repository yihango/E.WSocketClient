using System;
using System.Net.Http;
using System.Threading.Tasks;
using DotNetty.Codecs.Http.WebSockets;
using E.WebSocketClient;
using Newtonsoft.Json;
using E;
using WebApiClient;
using System.Collections.Generic;

namespace E.WebSocketClient.Test
{
    class Program
    {
        static Guid imID;

        static WSocketClient wsClient;

        static IMWebApi apiClient;

        static void Main(string[] args)
        {
            /* 本示例需要配合以下项目运行
             * https://github.com/2881099/im 
             */
            HttpApi.Register<IMWebApi>().ConfigureHttpApiConfig(c =>
            {
                c.HttpHost = new Uri("http://localhost:5001/");
                c.FormatOptions.DateTimeFormat = DateTimeFormats.ISO8601_WithMillisecond;
            }); ;

            Run().Wait();
            Console.WriteLine("stop");
            Console.ReadKey();
        }

        static async Task Run()
        {
            apiClient = HttpApi.Resolve<IMWebApi>();
            var response = await apiClient.PreConnect(imID);
            var url = (string)response.server;
            imID = Guid.Parse((string)response.websocketId);

            var clientArguments = new WSocetClientArguments();
            clientArguments.Url = new Uri(url);
            clientArguments.Host = "127.0.0.1";
            clientArguments.Port = 6001;


            var client = new WSocketClient(clientArguments);
            client.OnOpen += Client_OnOpen;
            client.OnMessage += Client_OnMessage;
            client.OnClose += Client_OnClose;
            client.OnError += Client_OnError;
            client.OnPong += Client_OnPong;

            await client.Connection();

            wsClient = client;

            while (true)
            {
                string msg = Console.ReadLine();
                if (msg == null)
                {
                    break;
                }
                else if ("bye".Equals(msg.ToLower()))
                {
                    await client.Close();
                    break;
                }
                else if ("ping".Equals(msg.ToLower()))
                {
                    await client.Ping();
                }
                else
                {
                    await client.Send(msg);
                }
            }
        }

        private static void Client_OnPong(object sender, PongWebSocketFrame e)
        {
            Console.WriteLine($"Client_OnPong: pong");
        }

        private static void Client_OnError(object sender, Exception e)
        {
            Console.WriteLine($"Client_OnError: {e.Message}");
        }

        private static void Client_OnClose(object sender, CloseWebSocketFrame e)
        {
            Console.WriteLine("Client_OnClose");
        }

        private static void Client_OnMessage(object sender, TextWebSocketFrame e)
        {
            Console.WriteLine($"Client_OnMessage: {e.Text()}");
        }

        private static async void Client_OnOpen(object sender, object e)
        {
            Console.WriteLine("Client_OnOpen");
            var response = await apiClient.GetChannels();



        }
    }
}
