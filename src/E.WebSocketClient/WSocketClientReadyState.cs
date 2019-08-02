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
        A = 0,
        /// <summary>
        /// 表示连接已建立，可以进行通信
        /// </summary>
        B = 1,
        /// <summary>
        /// 表示连接正在进行关闭
        /// </summary>
        C = 2,
        /// <summary>
        /// 表示连接已经关闭或者连接不能打开
        /// </summary>
        D = 3
    }
}
