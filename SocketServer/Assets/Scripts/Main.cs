using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    SocketServer _server;
    private void Awake()
    {
        _server = new SocketServer("127.0.0.1", 6854);
        _server.OnConnect += (client) =>
        {
            UnityEngine.Debug.LogFormat("连接成功 >> IP:{0}", client.RemoteEndPoint.ToString());
        };
        _server.OnDisconnect += (client) =>
        {
            UnityEngine.Debug.LogFormat("连接断开 >> IP:{0}", client.RemoteEndPoint.ToString());
        };
        _server.OnReceive += (client, data) =>
        {
            UnityEngine.Debug.LogFormat("[{0}]接收到数据>>>{1}", client.RemoteEndPoint.ToString(), data.Buff.Length);
        };
        _server.OnError += (ex) =>
        {
            UnityEngine.Debug.Log("出现异常>>>>" + ex);
        };
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            // 踢出连接
            foreach (var item in _server.ClientInfoDic.Keys)
            {
                _server.KickOutAll();
            }

        }
    }
    private void OnDestroy()
    {
        // 注意由于Unity编译器环境下，游戏开启/关闭只影响主线程的开关，游戏关闭回调时需要通过Close函数来关闭服务端/客户端的线程。
        if (_server != null)
        {
            _server.Close();
        }
    }
}
