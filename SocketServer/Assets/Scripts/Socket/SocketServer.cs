

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class SocketInfo
{
    public Socket Client;
    public Thread ReceiveThread;
    public long HeadTime;
}

/// <summary>
/// Socket服务端
/// </summary>
public class SocketServer
{
    public string IP;
    public int Port;

    private const long HEAD_TIMEOUT = 5000;    //心跳超时 毫秒

    public Dictionary<Socket, SocketInfo> ClientInfoDic = new Dictionary<Socket, SocketInfo>();

    private Socket _server;
    private Thread _connectThread;
    private DataBuffer _dataBuffer = new DataBuffer();

    public Action<Socket> OnConnect;
    public event Action<SocketDataPack> OnReceive;
    public event Action<Exception> OnError;

    private bool _isValid = true;

    public SocketServer(string ip, int port)
    {
        IP = ip;
        Port = port;

        _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPAddress ipAddress = IPAddress.Parse(IP);//解析IP地址
        _server.Bind(new IPEndPoint(ipAddress, Port));  //绑定IP地址：端口  

        _server.Listen(10);    //设定最多10个排队连接请求

        _connectThread = new Thread(ListenClientConnect);
        _connectThread.Start();
    }
    /// <summary>  
    /// 监听客户端连接  
    /// </summary>  
    private void ListenClientConnect()
    {
        while (true)
        {
            if (!_isValid) break;
            Socket client = _server.Accept();
            Thread receiveThread = new Thread(ReceiveEvent);
            ClientInfoDic.Add(client, new SocketInfo() { Client = client, ReceiveThread = receiveThread, HeadTime = GetNowTime() });
            receiveThread.Start(client);
            UnityEngine.Debug.Log("连接成功" + client.RemoteEndPoint.ToString());
            if (OnConnect != null) OnConnect(client);
        }

    }
    /// <summary>
    /// 获取当前时间戳
    /// </summary>
    /// <returns></returns>
    private long GetNowTime()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalMilliseconds);
    }

    public void Send(Socket client, byte[] buff)
    {
        var data = new SocketDataPack((UInt16)eProtocalCommand.sc_data_obj_get_process, buff).Buff;
        client.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(onSend), client);
    }
    /// <summary>
    /// 线程内接收数据的函数
    /// </summary>
    private void ReceiveEvent(object client)
    {
        Socket tsocket = (Socket)client;
        while (true)
        {
            if (!_isValid) break;
            if (!ClientInfoDic.ContainsKey(tsocket)) break;
            try
            {
                byte[] rbytes = new byte[8 * 1024];
                int len = tsocket.Receive(rbytes);
                if (len > 0)
                {
                    _dataBuffer.AddBuffer(rbytes, len); // 将收到的数据添加到缓存器中
                    var dataPack = new SocketDataPack();
                    if (_dataBuffer.TryUnpack(out dataPack)) // 尝试解包
                    {
                        UnityEngine.Debug.Log("接收数据");
                        if (dataPack.Type == (UInt16)eProtocalCommand.sc_head)
                        {
                            // 接收到心跳包
                            ReceiveHead(tsocket);
                        }
                        else if (dataPack.Type == (UInt16)eProtocalCommand.sc_disconn)
                        {
                            // 客户端断开连接
                            UnityEngine.Debug.Log("客户端主动断开连接");
                            Clear(tsocket);

                        }
                        else
                        {
                            onReceive(dataPack);
                        }

                    }
                }
            }
            catch (Exception e)
            {
                onError(e);
            }
        }
    }
    /// <summary>
    /// 接收到心跳包
    /// </summary>
    private void ReceiveHead(Socket client)
    {
        SocketInfo info;
        if (ClientInfoDic.TryGetValue(client, out info))
        {
            long now = GetNowTime();
            long offset = now - info.HeadTime;
            UnityEngine.Debug.Log("更新心跳时间戳 >>>" + now + "  间隔>>>" + offset);
            if (offset > HEAD_TIMEOUT)
            {
                // 心跳包收到但超时逻辑
            }
            info.HeadTime = now;
        }
    }

    private void Clear(Socket client)
    {
        UnityEngine.Debug.Log("清理客户端连接");
        ClientInfoDic.Remove(client);

    }
    public void Close()
    {
        if (!_isValid) return;
        _isValid = false;
        // if (_connectThread != null) _connectThread.Abort();
        foreach (var socket in ClientInfoDic.Keys)
        {
            Clear(socket);
        }
    }

    /// <summary>
    /// 错误回调
    /// </summary>
    /// <param name="e"></param>
    private void onError(Exception e)
    {
        if (OnError != null) OnError(e);
    }
    /// <summary>
    /// 发送消息回调，可判断当前网络状态
    /// </summary>
    /// <param name="asyncSend"></param>
    private void onSend(IAsyncResult asyncSend)
    {
        try
        {
            Socket client = (Socket)asyncSend.AsyncState;
            client.EndSend(asyncSend);
        }
        catch (Exception e)
        {
            onError(e);
        }
    }
    /// <summary>
    /// 接收数据回调
    /// </summary>
    /// <param name="dataPack"></param>
    private void onReceive(SocketDataPack dataPack)
    {
        if (OnReceive != null) OnReceive(dataPack);
    }

}
