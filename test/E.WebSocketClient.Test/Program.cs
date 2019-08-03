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

        static string imChannel;

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
            var response = await apiClient.PreConnect(null);
            Console.WriteLine($"reponse data: {response.ToString()}");
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
                else if ("ref".Equals(msg.ToLower()))
                {
                    var channels = await GetChannels();
                    foreach (var item in channels)
                    {
                        Console.WriteLine($"channel: {item.chan}  online:{item.online}");
                    }
                }
                else if (msg.ToLower().StartsWith("join"))
                {
                    var inputData = msg.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    imChannel = inputData[1].Trim();
                    await apiClient.SubscrChannel(imID, imChannel);
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

                    var time = ConvertToUnixOfTime(DateTime.UtcNow).ToString();
                    var message = new
                    {
                        type = "chanmsg",
                        sender = imID.ToString(),
                        senderNick = string.Empty,
                        chan = imChannel,
                        time = time,
                        msg = new
                        {
                            type = "text",
                            content = msg
                        }
                    };
                    await apiClient.SendChannelmsg(imID, imChannel, JsonConvert.SerializeObject(message));
                    //await client.Send(msg);
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
            Console.WriteLine($"Client_OnMessage: ");

            var dataText = e.Text();

            if (dataText.StartsWith("用户"))
            {
                Console.WriteLine(dataText);
                return;
            }

            var data = JsonConvert.DeserializeObject<dynamic>
                (JsonConvert.DeserializeObject<dynamic>(dataText));
            switch ((string)data.msg.type)
            {
                case "welcome":
                    Console.WriteLine($"{(string)data.sender} 上线了");
                    break;
                case "text":
                    if ((string)data.sender == imID.ToString())
                    {
                        break;
                    }
                    Console.WriteLine($"{(string)data.sender}:");
                    Console.WriteLine((string)data.msg.content);
                    break;
                default:
                    break;
            }
        }

        private static async void Client_OnOpen(object sender, object e)
        {
            Console.WriteLine("Client_OnOpen");
        }

        private static async Task<List<(string chan, long online)>> GetChannels()
        {
            var result = new List<(string chan, long online)>();

            var response = await apiClient.GetChannels();

            if (response.channels == null)
            {
                return result;
            }

            for (int i = 0; i < response.channels.Count; i++)
            {
                result.Add((response.channels[i].item1, response.channels[i].item2));
            }

            return result;
        }

        #region 转换时间为unix时间戳
        /// <summary>
        /// 转换时间为unix时间戳
        /// </summary>
        /// <param name="date">需要传递UTC时间,避免时区误差,例:DataTime.UTCNow</param>
        /// <returns></returns>
        public static double ConvertToUnixOfTime(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date - origin;
            //return Math.Floor(diff.TotalSeconds);
            return Math.Floor(diff.TotalMilliseconds);
        }
        #endregion

        #region 时间戳转换为时间

        public static DateTime StampToDateTime(string timeStamp)
        {
            // 1564811711911
            // 1564811705
            DateTime dateTimeStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(timeStamp + "0000000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dateTimeStart.Add(toNow);
        }

        #endregion 
    }
}
