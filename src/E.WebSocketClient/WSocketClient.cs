using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Codecs.Http.WebSockets.Extensions.Compression;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv;
using System;
using System.Net;
using System.Threading.Tasks;

namespace E
{
    public class WSocketClient
    {
        /// <summary>
        /// 开启
        /// </summary>
        public event EventHandler<object> OnOpen;

        /// <summary>
        /// 关闭
        /// </summary>
        public event EventHandler<CloseWebSocketFrame> OnClose;

        /// <summary>
        /// 消息
        /// </summary>
        public event EventHandler<TextWebSocketFrame> OnMessage;

        /// <summary>
        /// 异常
        /// </summary>
        public event EventHandler<Exception> OnError;

        /// <summary>
        /// pong 消息
        /// </summary>
        public event EventHandler<PongWebSocketFrame> OnPong;

        /// <summary>
        /// 状态
        /// </summary>
        public WSocketClientReadyState ReadyState { get; private set; }

        protected IChannel Channel { get; set; }

        protected WSocketClientHandler Handler { get; set; }

        public bool Alive
        {
            get
            {
                return this.Channel != null && this.Channel.Active;
            }
        }


        public WSocketClient()
        {


        }

        public async Task Connection(Uri uri, string ip, int port, WebSocketVersion version = null, HttpHeaders headers = null)
        {

            #region 新增 Handler

            this.Handler = new WSocketClientHandler(
                WebSocketClientHandshakerFactory.NewHandshaker(
                        uri,
                        version ?? WebSocketVersion.V13,
                        null,
                        true,
                        headers ?? new DefaultHttpHeaders())
                 );

            #endregion

            #region 绑定 Handler 生命周期事件

            this.Handler.OnOpen += Handler_OnOpen;
            this.Handler.OnClose += Handler_OnClose;
            this.Handler.OnError += Handler_OnError;
            this.Handler.OnMessage += Handler_OnMessage;
            this.Handler.OnPong += Handler_OnPong;

            #endregion


            IEventLoopGroup group = new EventLoopGroup();
            var bootstrap = new Bootstrap();
            bootstrap
                .Group(group)
                .Option(ChannelOption.TcpNodelay, true)
                .Channel<TcpChannel>();

            bootstrap.Handler(new ActionChannelInitializer<IChannel>(channel =>
            {
                IChannelPipeline pipeline = channel.Pipeline;
                pipeline.AddLast(
                    new HttpClientCodec(),
                    new HttpObjectAggregator(1024 * 1024 * 5),
                    WebSocketClientCompressionHandler.Instance,
                      this.Handler);
            }));

            this.Channel = await bootstrap.ConnectAsync(
                new IPEndPoint(IPAddress.Parse(ip), port)
                );

            await this.Handler.HandshakeCompletion;
        }



        public async Task Send(string msg)
        {
            if (!this.Alive)
            {
                return;
            }
            await this.Channel.WriteAndFlushAsync(new TextWebSocketFrame(msg));
        }

        public async Task Close(CloseWebSocketFrame closeWebSocketFrame = null)
        {
            closeWebSocketFrame = closeWebSocketFrame ?? new CloseWebSocketFrame();

            await this.Channel?.WriteAndFlushAsync(closeWebSocketFrame);
            await this.Channel?.CloseAsync();
            await this.Channel?.CloseCompletion;
            this.OnClose?.Invoke(this, closeWebSocketFrame);
        }

        public async Task Ping(PingWebSocketFrame pingWebSocketFrame = null)
        {
            var frame = pingWebSocketFrame ?? new PingWebSocketFrame(Unpooled.WrappedBuffer(new byte[] { 8, 1, 8, 1 }));
            await this.Channel.WriteAndFlushAsync(frame);
        }


        #region 私有事件触发

        private void Handler_OnPong(object sender, PongWebSocketFrame e)
        {
            this.OnPong?.Invoke(sender, e);
        }

        private void Handler_OnMessage(object sender, TextWebSocketFrame e)
        {
            this.OnMessage?.Invoke(sender, e);
        }

        private void Handler_OnError(object sender, Exception e)
        {
            this.OnError?.Invoke(sender, e);
        }

        private void Handler_OnClose(object sender, CloseWebSocketFrame e)
        {
            this.OnClose?.Invoke(sender, e);
        }

        private void Handler_OnOpen(object sender, object e)
        {
            this.OnOpen?.Invoke(sender, e);
        }

        #endregion
    }
}
