using System;
using System.Collections.Generic;
using System.Text;

namespace E
{
    public enum WSocketClientReadyState
    {
        /// <summary>
        /// 表示连接尚未建立
        /// </summary>
        None = 0,
        /// <summary>
        /// 表示连接已建立，可以进行通信
        /// </summary>
        Opened = 1,
        /// <summary>
        /// 表示连接已经关闭
        /// </summary>
        Closed = 2
    }
}
