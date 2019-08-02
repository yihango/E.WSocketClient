using System;
using System.Net.Http;
using System.Threading.Tasks;
using DotNetty.Codecs.Http.WebSockets;
using E.WebSocketClient;
using Newtonsoft.Json;
using E;

namespace E.WebSocketClient.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Run().Wait();
            Console.WriteLine("close");
            Console.ReadKey();
        }

        static async Task Run()
        {
            //HttpContent httpContent = new StringContent(string.Empty);
            //httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            //var result = await (new HttpClient()).GetAsync("http://localhost:5001/ws/geturl");
            //var content = await result.RequestMessage.Content.ReadAsStringAsync();
            //var obj = JsonConvert.DeserializeObject<dynamic>(content);

            var client = new WSocketClient();
            client.OnOpen += Client_OnOpen;
            client.OnMessage += Client_OnMessage;
            client.OnClose += Client_OnClose;
            client.OnError += Client_OnError;
            client.OnPong += Client_OnPong;

            await client.Connection(new Uri("ws://127.0.0.1:6001/ws?token=42956ada56334e7c8e5105fef0093a8fd135437cef71412da937b80f8cef1546b70e8937f9c644209fa8b0f9c2b5081e5a55cc4d2e8a42939e4ea06f172ef5ca"),
                "127.0.0.1", 6001);

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

        private static void Client_OnOpen(object sender, object e)
        {
            Console.WriteLine("Client_OnOpen");
        }
    }
}
