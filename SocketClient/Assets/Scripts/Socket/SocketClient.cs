

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;



/// <summary>
/// Socket客户端
/// </summary>
public class SocketClient
{
    public string IP;
    public int Port;

    private const int HEAD_OFFSET = 2000; //心跳包发送间隔 毫秒
    private const int RECONN_MAX_SUM = 3;   //最大重连次数

    private Socket _client;
    private Thread _receiveThread;
    private System.Timers.Timer _headTimer;
    private DataBuffer _dataBuffer = new DataBuffer();

    public Action OnConnectSuccess;    // 连接成功
    public Action OnConnectError;    // 连接失败
    public event Action OnDisconnect;  // 断开回调
    public event Action<SocketDataPack> OnReceive;  // 接收回调
    public event Action<SocketException> OnError;   // 错误回调
    public event Action<int> OnReConnectSuccess; // 重连成功
    public event Action<int> OnReConnectError; // 重连失败
    public event Action<int> OnReconnecting;  // 重连中回调

    private bool _isConnect = false;
    private bool _isReConnect = false;
    private static object _reConnectLock = new object();

    public SocketClient(string ip, int port)
    {
        IP = ip;
        Port = port;
    }
    public void Connect(Action successEvent = null, Action errorEvent = null)
    {
        Action tsuccessEvent = null;
        Action terrorEvent = null;
        tsuccessEvent = () =>
        {
            if (successEvent != null) successEvent();
            Main.text += "清理一次连接事件" + "\n";
            OnConnectSuccess -= tsuccessEvent;
            OnConnectError -= terrorEvent;
        };
        terrorEvent = () =>
        {
            if (errorEvent != null) errorEvent();
            Main.text += "清理一次连接事件" + "\n";
            OnConnectSuccess -= tsuccessEvent;
            OnConnectError -= terrorEvent;
        };
        Main.text += "注册一次连接事件" + "\n";
        OnConnectSuccess += tsuccessEvent;
        OnConnectError += terrorEvent;
        try
        {
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建套接字
            IPAddress ipAddress = IPAddress.Parse(IP);//解析IP地址
            IPEndPoint ipEndpoint = new IPEndPoint(ipAddress, Port);
            IAsyncResult result = _client.BeginConnect(ipEndpoint, new AsyncCallback(onConnect), _client);//异步连接
        }
        catch (SocketException e)
        {
            if (OnConnectError != null) OnConnectError();
            // throw;
        }
    }
    /// <summary>
    /// 断线重连
    /// </summary>
    /// <param name="num"></param>
    public void ReConnect(int num = 0)
    {
        num++;
        if (num > RECONN_MAX_SUM)
        {
            _isReConnect = false;
            Close();
            return;
        }
        if (OnReconnecting != null) OnReconnecting(num);
        Connect(() =>
        {
            _isReConnect = false;
            if (OnReConnectSuccess != null) OnReConnectSuccess(num);
        }, () =>
        {
            if (OnReConnectError != null) OnReConnectError(num);
            ReConnect(num);
        });

    }
    public void Send(UInt16 e, byte[] buff = null)
    {
        buff = buff ?? new byte[] { };
        var data = new SocketDataPack(e, buff).Buff;
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
                if (!_isConnect) break;
                if (_client.Available <= 0) break;
                byte[] rbytes = new byte[8 * 1024];
                int len = _client.Receive(rbytes);
                if (len > 0)
                {
                    _dataBuffer.AddBuffer(rbytes, len); // 将收到的数据添加到缓存器中
                    var dataPack = new SocketDataPack();
                    if (_dataBuffer.TryUnpack(out dataPack)) // 尝试解包
                    {
                        if (dataPack.Type == (UInt16)eProtocalCommand.sc_kickout)
                        {
                            // 服务端踢出
                            UnityEngine.Debug.Log("服务器踢出连接");
                            Close();
                        }
                        else
                        {
                            onReceive(dataPack);
                        }


                    }
                }
            }
            catch (SocketException e)
            {
                onError(e);
                // throw;
            }
        }
    }
    /// <summary>
    /// 业务逻辑 - 客户端主动断开
    /// </summary>
    public void DisConnect()
    {
        Send((UInt16)eProtocalCommand.sc_disconn);
        Close();
    }
    /// <summary>
    /// 业务逻辑 - 被服务端断开
    /// </summary>
    public void Close()
    {

        Clear();

        onDisconnect();
    }
    /// <summary>
    /// 缓存数据清理
    /// </summary>
    public void Clear()
    {
        if (!_isConnect) return;
        _isConnect = false;
        if (_headTimer != null)
        {
            _headTimer.Stop();
            _headTimer = null;
        }
        // if (_receiveThread != null)
        // {
        //     _receiveThread.Abort();
        //     _receiveThread = null;
        // }
        if (_client != null)
        {
            _client.Close();
            _client = null;
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
            // 开始发送心跳包
            _headTimer = new System.Timers.Timer(HEAD_OFFSET);
            _headTimer.AutoReset = true;
            _headTimer.Elapsed += delegate (object sender, ElapsedEventArgs args)
            {
                UnityEngine.Debug.Log("发送心跳包");
                Send((UInt16)eProtocalCommand.sc_head);
            };
            _headTimer.Start();

            // 开始接收数据
            _receiveThread = new Thread(new ThreadStart(ReceiveEvent));
            _receiveThread.IsBackground = true;
            _receiveThread.Start();


            if (OnConnectSuccess != null) OnConnectSuccess();
        }
        catch (SocketException e)
        {
            if (OnConnectError != null) OnConnectError();
            // throw;
        }
    }

    /// <summary>
    /// 错误回调
    /// </summary>
    /// <param name="e"></param>
    private void onError(SocketException e)
    {
        Clear();
        if (OnError != null) OnError(e);

        // 尝试重连
        lock (_reConnectLock)
        {
            Main.text += "进入线程" + "\n";
            UnityEngine.Debug.Log("进入线程");
            if (!_isReConnect)
            {
                _isReConnect = true;
                ReConnect();
            }
            else
            {
                Main.text += "已经在重连了" + "\n";
                UnityEngine.Debug.Log("已经在重连了");
            }
        }
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
        catch (SocketException e)
        {
            onError(e);
            // throw;

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
    /// <summary>
    /// 断开连接/重连失败回调
    /// </summary>
    /// <param name="dataPack"></param>
    private void onDisconnect()
    {
        if (OnDisconnect != null) OnDisconnect();
    }
}
