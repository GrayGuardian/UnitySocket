

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
/// <summary>
/// Socket客户端
/// </summary>
public class SocketClient
{
    public string IP;
    public int Port;

    private Socket _client;
    private Thread _receiveThread;
    private DataBuffer _dataBuffer = new DataBuffer();

    private event Action _onConnect;
    private event Action<SocketDataPack> _onReceive;
    private event Action<Exception> _onError;

    private bool _isConnect = false;

    public SocketClient(string ip, int port)
    {
        IP = ip;
        Port = port;
    }
    public void Connect()
    {
        try
        {
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建套接字
            IPAddress ipAddress = IPAddress.Parse(IP);//解析IP地址
            IPEndPoint ipEndpoint = new IPEndPoint(ipAddress, Port);
            IAsyncResult result = _client.BeginConnect(ipEndpoint, new AsyncCallback(onConnect), _client);//异步连接
            bool success = result.AsyncWaitHandle.WaitOne(5000, true);
            if (!success) //超时
            {
                onError(new Exception("连接超时"));
            }
        }
        catch (Exception e)
        {
            onError(e);
        }
    }
    public void Send(byte[] buff)
    {
        var data = new SocketDataPack((UInt16)eProtocalCommand.sc_data_obj_get_process, buff).Buff;
        _client.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(onSend), _client);
    }
    /// <summary>
    /// 线程内接收数据的函数
    /// </summary>
    private void ReceiveEvent()
    {
        while (true)
        {
            try
            {
                byte[] rbytes = new byte[8 * 1024];
                int len = _client.Receive(rbytes);
                if (len > 0)
                {
                    _dataBuffer.AddBuffer(rbytes, len); // 将收到的数据添加到缓存器中
                    var dataPack = new SocketDataPack();
                    if (_dataBuffer.TryUnpack(out dataPack)) // 尝试解包
                    {
                        onReceive(dataPack);
                    }
                }
            }
            catch (Exception e)
            {
                onError(e);
            }
        }
    }
    public void Close()
    {
        if (!_isConnect) return;
        UnityEngine.Debug.Log("关闭Socket连接" + _client.Connected);
        _isConnect = false;
        if (_receiveThread != null)
        {
            _receiveThread.Abort();
        }
        if (_client != null)
        {
            _client.Close();
        }


    }
    /// <summary>
    /// 连接成功回调
    /// </summary>
    /// <param name="iar"></param>
    private void onConnect(IAsyncResult iar)
    {
        try
        {
            Socket client = (Socket)iar.AsyncState;
            client.EndConnect(iar);

            _isConnect = true;

            _receiveThread = new Thread(new ThreadStart(ReceiveEvent));
            _receiveThread.IsBackground = true;
            _receiveThread.Start();

            if (_onConnect != null) _onConnect();
        }
        catch (Exception e)
        {
            onError(e);
        }
    }

    /// <summary>
    /// 错误回调
    /// </summary>
    /// <param name="e"></param>
    private void onError(Exception e)
    {
        Close();
        if (_onError != null) _onError(e);
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
        if (_onReceive != null) _onReceive(dataPack);
    }
}
