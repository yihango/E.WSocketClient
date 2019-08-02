using System;
using System.Collections.Generic;
using System.Text;
using WebApiClient;
using WebApiClient.Attributes;

namespace E.WebSocketClient.Test
{
    public interface IMWebApi : IHttpApi
    {
        /// <summary>
        /// 获取websocket连接
        /// </summary>
        /// <param name="websocketId"></param>
        /// <returns></returns>
        [HttpPost("ws/pre-connect")]
        ITask<dynamic> PreConnect([FormContent] Guid? websocketId);

        /// <summary>
        /// 获取所有的聊天室
        /// </summary>
        /// <returns></returns>
        [HttpPost("ws/get-channels")]
        ITask<dynamic> GetChannels();


        /// <summary>
        /// 群聊，绑定消息频道
        /// </summary>
        /// <param name="websocketId">会话id</param>
        /// <param name="channel">频道key</param>
        /// <returns></returns>
        [HttpPost("ws/subscr-channel")]
        ITask<dynamic> SubscrChannel([FormContent] Guid websocketId, [FormContent] string channel);


        /// <summary>
        /// 群聊，发送频道消息，绑定频道的所有人将收到消息
        /// </summary>
        /// <param name="websocketId">会话id</param>
        /// <param name="channel">消息频道</param>
        /// <param name="content">发送内容</param>
        /// <returns></returns>
        [HttpPost("ws/send-channelmsg")]
        ITask<dynamic> SendChannelmsg([FormContent] Guid websocketId, [FormContent] string channel, [FormContent] string message);


        /// <summary>
        /// 单聊
        /// </summary>
        /// <param name="senderWebsocketId">发送者会话id</param>
        /// <param name="receiveWebsocketId">接收者会话id</param>
        /// <param name="message">消息内容</param>
        /// <param name="isReceipt">是否需要回执</param>
        /// <returns></returns>
        [HttpPost("ws/send-msg")]
        ITask<dynamic> SendMsg([FormContent] Guid senderWebsocketId, [FormContent] Guid receiveWebsocketId, [FormContent] string message, [FormContent] bool isReceipt = false);
    }
}
