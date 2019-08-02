using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Codecs.Http.WebSockets.Extensions.Compression;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;
using JetBrains.Annotations;
using System;
using System.Net;
using System.Net.Security;
using System.Threading.Tasks;

namespace E
{
    public class WSocketClient : IDisposable
    {

        #region 生命周期事件

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

        #endregion


        #region 私有字段


        /// <summary>
        /// 配置参数
        /// </summary>
        private WSocetClientArguments _arguments;

        #endregion


        #region 公开属性

        /// <summary>
        /// 状态
        /// </summary>
        public WSocketClientReadyState ReadyState { get; private set; }

        /// <summary>
        /// 客户端配置参数
        /// </summary>
        public virtual WSocetClientArguments Arguments
        {
            get => this._arguments;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Arguments cannot be null");
                }
                if (!IPAddress.TryParse(value.Host, out IPAddress iPAddress))
                {
                    throw new ArgumentException("Host value is not valid IP");
                }

                if (value.Port > 65536 || value.Port < 0)
                {
                    throw new ArgumentException("The maximum range of port Numbers is 0~65536");
                }

                if (value.Url == null)
                {
                    throw new ArgumentNullException("Url cannot be null");
                }

                this._arguments = value;
            }
        }

        #endregion


        #region 内部属性

        /// <summary>
        /// 处理通道
        /// </summary>
        protected virtual IChannel Channel { get; set; }

        /// <summary>
        /// 处理程序
        /// </summary>
        protected virtual WSocketClientHandler Handler { get; set; }

        #endregion




        public WSocketClient()
        { }

        public WSocketClient([NotNull]WSocetClientArguments arguments)
            : this()
        {
            this.Arguments = arguments;
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        public virtual async Task Connection([NotNull]WSocetClientArguments arguments)
        {
            this.Arguments = arguments;
            await this.Connection();
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        public virtual async Task Connection()
        {
            if (this.Arguments == null)
            {
                throw new ArgumentNullException("WSocketClient arguments cannot be null;");
            }

            // 状态为打开则跳过连接
            if (this.ReadyState == WSocketClientReadyState.Opened)
            {
                return;
            }

            try
            {

                #region 创建 Handler

                this.Handler = this.CreateHandler();
                this.BindEvent();

                #endregion


                #region 创建工作组和启动器

                var group = this.CreateEventGroup();
                var bootstrap = this.CreateBootstrap(group);

                #endregion


                #region 预先配置请求处理管道

                bootstrap.Handler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    // 自定义管道
                    if (this.Arguments.BootstrapInitializer != null)
                    {
                        this.Arguments.BootstrapInitializer.Invoke(this.Arguments, channel);
                        channel.Pipeline.AddLast(this.Handler);
                        return;
                    }

                    // 添加证书
                    if (this.Arguments.UseSsl)
                    {
                        channel.Pipeline.AddLast("tls", new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(this.Arguments.TargetHost)));
                    }

                    // 默认管道
                    channel.Pipeline.AddLast(new HttpClientCodec());
                    channel.Pipeline.AddLast(new HttpObjectAggregator(1024 * 1024 * 5));
                    channel.Pipeline.AddLast(WebSocketClientCompressionHandler.Instance);
                    channel.Pipeline.AddLast(this.Handler);
                }));

                #endregion


                #region 创建管道

                this.Channel = this.Arguments.CreateChannel?.Invoke(this.Arguments);
                if (this.Channel == null)
                {
                    this.Channel = await bootstrap.ConnectAsync(
                        new IPEndPoint(IPAddress.Parse(this.Arguments.Host), this.Arguments.Port)
                    );
                }

                #endregion


                // 握手成功
                await this.Handler.HandshakeCompletion;
            }
            catch (Exception e)
            {
                throw e;
            }
        }



        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public virtual async Task Send(string msg)
        {
            if (this.ReadyState != WSocketClientReadyState.Opened)
            {
                throw new Exception("WSocketClient state is not open");
            }
            await this.Channel.WriteAndFlushAsync(new TextWebSocketFrame(msg));
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <param name="closeWebSocketFrame"></param>
        /// <returns></returns>
        public virtual async Task Close(CloseWebSocketFrame closeWebSocketFrame = null)
        {
            if (closeWebSocketFrame != null)
            {
                await this.Channel?.WriteAndFlushAsync(closeWebSocketFrame);
            }
            else
            {
                await this.Channel?.CloseAsync();
            }

            await this.Channel?.CloseCompletion;
        }

        /// <summary>
        /// 发送Ping
        /// </summary>
        /// <param name="pingWebSocketFrame"></param>
        /// <returns></returns>
        public virtual async Task Ping(PingWebSocketFrame pingWebSocketFrame = null)
        {
            var frame = pingWebSocketFrame ?? new PingWebSocketFrame(Unpooled.WrappedBuffer(new byte[] { 8, 1, 8, 1 }));
            await this.Channel.WriteAndFlushAsync(frame);
        }


        #region 可被重写的内部函数

        /// <summary>
        /// 创建 ClientHandler
        /// </summary>
        /// <returns></returns>
        protected virtual WSocketClientHandler CreateHandler()
        {
            var handler = this.Arguments.CreateHandler?.Invoke(this.Arguments);
            if (handler != null)
            {
                return handler;
            }

            handler = new WSocketClientHandler(
                   WebSocketClientHandshakerFactory.NewHandshaker(
                           this.Arguments.Url,
                            WebSocketVersion.V13,
                           null,
                           true,
                           new DefaultHttpHeaders())
                    );

            return handler;
        }

        /// <summary>
        /// 创建工作组
        /// </summary>
        /// <returns></returns>
        protected virtual IEventLoopGroup CreateEventGroup()
        {
            IEventLoopGroup group;
            group = this.Arguments.CreateGroup?.Invoke(this.Arguments);
            if (group != null)
            {
                return group;
            }

            if (this.Arguments.UseLibv)
            {
                group = new EventLoopGroup();
            }
            else
            {
                group = new MultithreadEventLoopGroup();
            }

            return group;
        }

        /// <summary>
        /// 创建启动器
        /// </summary>
        /// <param name="eventLoopGroup"></param>
        /// <returns></returns>
        protected virtual Bootstrap CreateBootstrap(IEventLoopGroup eventLoopGroup)
        {
            var bootstrap = this.Arguments.CreateBootstrap?.Invoke(this.Arguments, eventLoopGroup);
            if (bootstrap != null)
            {
                return bootstrap;
            }

            bootstrap = new Bootstrap();
            bootstrap
                .Group(eventLoopGroup)
                .Option(ChannelOption.TcpNodelay, true);

            if (Arguments.UseLibv)
            {
                bootstrap.Channel<TcpChannel>();
            }
            else
            {
                bootstrap.Channel<TcpSocketChannel>();
            }

            return bootstrap;
        }

        #endregion


        #region 绑定/解绑 事件

        /// <summary>
        /// 绑定事件
        /// </summary>
        protected virtual void BindEvent()
        {
            if (this.Handler == null)
            {
                return;
            }

            this.Handler.OnOpen += Handler_OnOpen;
            this.Handler.OnClose += Handler_OnClose;
            this.Handler.OnError += Handler_OnError;
            this.Handler.OnMessage += Handler_OnMessage;
            this.Handler.OnPong += Handler_OnPong;
        }

        /// <summary>
        /// 解绑事件
        /// </summary>
        protected virtual void UnBindEvent()
        {
            if (this.Handler == null)
            {
                return;
            }

            this.Handler.OnOpen -= Handler_OnOpen;
            this.Handler.OnClose -= Handler_OnClose;
            this.Handler.OnError -= Handler_OnError;
            this.Handler.OnMessage -= Handler_OnMessage;
            this.Handler.OnPong -= Handler_OnPong;
        }

        #endregion


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
            this.UnBindEvent();
            this.Handler = null;
            this.ReadyState = WSocketClientReadyState.Closed;
            this.OnClose?.Invoke(sender, e);
            this.Channel = null;
        }

        private void Handler_OnOpen(object sender, object e)
        {
            this.ReadyState = WSocketClientReadyState.Opened;
            this.OnOpen?.Invoke(sender, e);
        }

        public void Dispose()
        {
            this.Close().Wait();
        }

        #endregion
    }
}
