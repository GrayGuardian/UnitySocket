

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
/// <summary>
/// Socket服务端
/// </summary>
public class SocketServer
{
    public string IP;
    public int Port;

    private Socket _server;
    private Thread _connectThread;
    private DataBuffer _dataBuffer = new DataBuffer();

    private event Action _onConnect;
    private event Action<SocketDataPack> _onReceive;
    private event Action<Exception> _onError;

    private bool _isValid = true;

    public SocketServer(string ip, int port)
    {
        IP = ip;
        Port = port;

        _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _server.Bind(new IPEndPoint(IPAddress.Parse(IP), Port));  //绑定IP地址：端口  

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
            // Thread receiveThread = new Thread(ReceiveMessage);
            // receiveThread.Start(clientSocket);
            UnityEngine.Debug.Log("连接成功" + client.RemoteEndPoint.ToString());
            //GameManage.gameManageInstance.text.text = "";
            // SendConnectSucessMessage(clientSocket);
            // allClientSocket.Add(clientSocket);
            // if (!allReceiveThread.ContainsKey(clientSocket.RemoteEndPoint.ToString()))
            // {
            //     allReceiveThread.Add(clientSocket.RemoteEndPoint.ToString(), receiveThread);
            // }

        }

    }

    public void Close()
    {
        if (!_isValid) return;
        _isValid = false;
        if (_connectThread != null) _connectThread.Abort();
    }

}
