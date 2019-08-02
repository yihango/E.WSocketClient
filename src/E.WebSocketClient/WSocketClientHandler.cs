using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace E
{
    public class WSocketClientHandler : SimpleChannelInboundHandler<object>
    {
        readonly WebSocketClientHandshaker _handshaker;
        readonly TaskCompletionSource _completionSource;

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
        /// pong 消息
        /// </summary>
        public event EventHandler<PongWebSocketFrame> OnPong;

        /// <summary>
        /// 异常
        /// </summary>
        public event EventHandler<Exception> OnError;


        public WSocketClientHandler(WebSocketClientHandshaker handshaker)
        {
            this._handshaker = handshaker;
            this._completionSource = new TaskCompletionSource();
        }

        public Task HandshakeCompletion => this._completionSource.Task;


        public override void ChannelActive(IChannelHandlerContext ctx)
        {
            this._handshaker.HandshakeAsync(ctx.Channel).LinkOutcome(this._completionSource);
        }


        public override void ChannelInactive(IChannelHandlerContext context)
        {
            this.OnClose?.Invoke(context, null);
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, object msg)
        {
            IChannel ch = ctx.Channel;

            #region 连接

            if (!this._handshaker.IsHandshakeComplete)
            {
                try
                {
                    this._handshaker.FinishHandshake(ch, (IFullHttpResponse)msg);
                    this._completionSource.TryComplete();
                    this.OnOpen?.Invoke(ctx, null);
                }
                catch (WebSocketHandshakeException e)
                {
                    this._completionSource.TrySetException(e);
                    this.OnError?.Invoke(ctx, e);
                }

                return;
            }

            #endregion


            if (msg is IFullHttpResponse response)
            {
                var exception = new InvalidOperationException(
                   $"Unexpected FullHttpResponse (getStatus={response.Status}, content={response.Content.ToString(Encoding.UTF8)})");
                this.OnError?.Invoke(ctx, exception);
            }

            if (msg is TextWebSocketFrame textFrame)
            {
                this.OnMessage?.Invoke(ctx, textFrame);
            }
            else if (msg is PongWebSocketFrame pong)
            {
                this.OnPong?.Invoke(ctx, pong);
            }
            else if (msg is CloseWebSocketFrame close)
            {
                ch.CloseAsync();
                this.OnClose?.Invoke(ctx, close);
            }
        }

        public override async void ExceptionCaught(IChannelHandlerContext ctx, Exception exception)
        {
            this._completionSource.TrySetException(exception);
            this.OnError?.Invoke(ctx, exception);

            await ctx.CloseAsync();
            this.OnClose?.Invoke(ctx, null);
        }
    }
}