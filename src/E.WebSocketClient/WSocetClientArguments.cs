using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace E
{
    public class WSocetClientArguments
    {
        /// <summary>
        /// 地址
        /// </summary>
        public Uri Url { get; set; }

        /// <summary>
        /// 使用libv
        /// </summary>
        public bool UseLibv { get; set; }

        /// <summary>
        /// 服务端IP地址
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 服务端端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// WebSocket 版本号
        /// </summary>
        public WebSocketVersion WebSocketVersion { get; set; }

        /// <summary>
        /// 使用加密链接
        /// </summary>
        public bool UseSsl { get; set; }

        /// <summary>
        /// 证书
        /// </summary>
        public X509Certificate2 Cret { get; set; }

        /// <summary>
        /// 证书中 主题和发行者名称
        /// </summary>
        public string TargetHost { get; set; }

        /// <summary>
        /// 创建 WebSocketClientClientHandler
        /// </summary>
        public Func<WSocetClientArguments, WSocketClientHandler> CreateHandler { get; set; }

        /// <summary>
        /// 创建 事件组
        /// </summary>
        public Func<WSocetClientArguments, IEventLoopGroup> CreateGroup { get; set; }

        /// <summary>
        /// 创建 启动器
        /// </summary>
        public Func<WSocetClientArguments, IEventLoopGroup, Bootstrap> CreateBootstrap { get; set; }

        /// <summary>
        /// 启动器初始化配置
        /// </summary>
        public Action<WSocetClientArguments, IChannel> BootstrapInitializer { get; set; }

        /// <summary>
        /// 创建 Channel
        /// </summary>
        public Func<WSocetClientArguments, IChannel> CreateChannel { get; set; }


        public WSocetClientArguments()
        {
            this.UseLibv = true;
            this.Port = 80;
            this.WebSocketVersion = WebSocketVersion.V13;
            this.UseSsl = false;
        }
    }
}
