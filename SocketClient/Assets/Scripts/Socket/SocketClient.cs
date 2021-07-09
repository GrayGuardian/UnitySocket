

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
    /// <summary>
    /// 主线程
    /// </summary>
    private SynchronizationContext _mainThread;


    public string IP;
    public int Port;

    private const int TIMEOUT_CONNECT = 3000;   // 连接超时时间 毫秒
    private const int TIMEOUT_SEND = 3000;  // 发送超时时间 毫秒
    private const int TIMEOUT_RECEIVE = 3000;   //接收超时时间 毫秒

    private const int HEAD_OFFSET = 2000; //心跳包发送间隔 毫秒
    private const int RECONN_MAX_SUM = 3;   //最大重连次数

    private Socket _client;
    private Thread _receiveThread;
    private System.Timers.Timer _connTimeoutTimer;
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



    public SocketClient(string ip, int port)
    {
        _mainThread = SynchronizationContext.Current;

        IP = ip;
        Port = port;
    }
    public void Connect(Action successEvent = null, Action errorEvent = null)
    {
        Action tsuccessEvent = null;
        Action terrorEvent = null;
        tsuccessEvent = () =>
        {
            OnConnectSuccess -= tsuccessEvent;
            OnConnectError -= terrorEvent;
            if (_connTimeoutTimer != null)
            {
                _connTimeoutTimer.Stop();
                _connTimeoutTimer = null;
            }
            if (successEvent != null) successEvent();
        };
        terrorEvent = () =>
        {
            OnConnectSuccess -= tsuccessEvent;
            OnConnectError -= terrorEvent;
            if (_connTimeoutTimer != null)
            {
                _connTimeoutTimer.Stop();
                _connTimeoutTimer = null;
            }
            if (errorEvent != null) errorEvent();
        };
        OnConnectSuccess += tsuccessEvent;
        OnConnectError += terrorEvent;
        try
        {
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建套接字
            _client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, TIMEOUT_SEND);
            _client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, TIMEOUT_RECEIVE);
            IPAddress ipAddress = IPAddress.Parse(IP);//解析IP地址
            IPEndPoint ipEndpoint = new IPEndPoint(ipAddress, Port);
            IAsyncResult result = _client.BeginConnect(ipEndpoint, new AsyncCallback(onConnect), _client);//异步连接

            _connTimeoutTimer = new System.Timers.Timer(TIMEOUT_CONNECT);
            _connTimeoutTimer.AutoReset = false;
            _connTimeoutTimer.Elapsed += delegate (object sender, ElapsedEventArgs args)
            {
                PostMainThreadAction(OnConnectError);
            };
            _connTimeoutTimer.Start();

        }
        catch (SocketException ex)
        {
            PostMainThreadAction(OnConnectError);
            // throw;
        }
    }
    /// <summary>
    /// 断线重连
    /// </summary>
    /// <param name="num"></param>
    public void ReConnect(int num = RECONN_MAX_SUM, int index = 0)
    {
        num--;
        index++;
        if (num < 0)
        {
            onDisconnect();
            return;
        }
        PostMainThreadAction<int>(OnReconnecting, index);
        Connect(() =>
        {
            PostMainThreadAction<int>(OnReConnectSuccess, index);
        }, () =>
        {
            PostMainThreadAction<int>(OnReConnectError, index);
            ReConnect(num, index);
        });

    }
    public void Send(UInt16 e, byte[] buff = null, Action<SocketDataPack> onTrigger = null)
    {
        buff = buff ?? new byte[] { };
        var dataPack = new SocketDataPack(e, buff);
        var data = dataPack.Buff;
        try
        {
            _client.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback((asyncSend) =>
            {
                Socket c = (Socket)asyncSend.AsyncState;
                c.EndSend(asyncSend);
                PostMainThreadAction<SocketDataPack>(onTrigger, dataPack);
            }), _client);
        }
        catch (SocketException ex)
        {
            onError(ex);
        }

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
                if (_client.Available <= 0) continue;
                byte[] rbytes = new byte[8 * 1024];
                int len = _client.Receive(rbytes);
                if (len > 0)
                {
                    _dataBuffer.AddBuffer(rbytes, len); // 将收到的数据添加到缓存器中
                    var dataPack = new SocketDataPack();
                    if (_dataBuffer.TryUnpack(out dataPack)) // 尝试解包
                    {
                        if (dataPack.Type == (UInt16)SocketEvent.sc_kickout)
                        {
                            // 服务端踢出
                            onDisconnect();
                        }
                        else
                        {
                            // 收到消息
                            PostMainThreadAction<SocketDataPack>(OnReceive, dataPack);
                        }


                    }
                }
            }
            catch (SocketException ex)
            {
                onError(ex);
                // throw;
            }
        }
    }
    /// <summary>
    /// 业务逻辑 - 客户端主动断开
    /// </summary>
    public void DisConnect()
    {
        Send((UInt16)SocketEvent.sc_disconn);
        onDisconnect();
    }

    /// <summary>
    /// 缓存数据清理
    /// </summary>
    public void Close()
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
        if (_connTimeoutTimer != null)
        {
            _connTimeoutTimer.Stop();
            _connTimeoutTimer = null;
        }
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
                Send((UInt16)SocketEvent.sc_head);
            };
            _headTimer.Start();

            // 开始接收数据
            _receiveThread = new Thread(new ThreadStart(ReceiveEvent));
            _receiveThread.IsBackground = true;
            _receiveThread.Start();

            PostMainThreadAction(OnConnectSuccess);
        }
        catch (SocketException ex)
        {
            PostMainThreadAction(OnConnectError);
            // throw;
        }
    }

    /// <summary>
    /// 错误回调
    /// </summary>
    /// <param name="e"></param>
    private void onError(SocketException ex)
    {
        Close();

        PostMainThreadAction<SocketException>(OnError, ex);

        ReConnect();
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
        catch (SocketException ex)
        {
            onError(ex);
            // throw;

        }
    }
    /// <summary>
    /// 断开回调
    /// </summary>
    private void onDisconnect()
    {

        Close();

        PostMainThreadAction(OnDisconnect);
    }

    /// <summary>
    /// 通知主线程回调
    /// </summary>
    private void PostMainThreadAction(Action action)
    {
        _mainThread.Post(new SendOrPostCallback((o) =>
        {
            Action e = (Action)o.GetType().GetProperty("action").GetValue(o);
            if (e != null) e();
        }), new { action = action });
    }
    private void PostMainThreadAction<T>(Action<T> action, T arg1)
    {
        _mainThread.Post(new SendOrPostCallback((o) =>
        {
            Action<T> e = (Action<T>)o.GetType().GetProperty("action").GetValue(o);
            T t1 = (T)o.GetType().GetProperty("arg1").GetValue(o);
            if (e != null) e(t1);
        }), new { action = action, arg1 = arg1 });
    }
    public void PostMainThreadAction<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
    {
        _mainThread.Post(new SendOrPostCallback((o) =>
        {
            Action<T1, T2> e = (Action<T1, T2>)o.GetType().GetProperty("action").GetValue(o);
            T1 t1 = (T1)o.GetType().GetProperty("arg1").GetValue(o);
            T2 t2 = (T2)o.GetType().GetProperty("arg2").GetValue(o);
            if (e != null) e(t1, t2);
        }), new { action = action, arg1 = arg1, arg2 = arg2 });
    }
}
