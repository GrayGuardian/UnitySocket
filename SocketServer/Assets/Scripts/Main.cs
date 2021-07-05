using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    SocketServer _server;
    private void Awake()
    {
        _server = new SocketServer("192.168.108.56", 6854);
        _server.OnConnect += (client) =>
        {
            UnityEngine.Debug.LogFormat("连接成功 >> IP:{0}", client.RemoteEndPoint.ToString());
        };
        _server.OnDisconnect += (client) =>
        {
            UnityEngine.Debug.LogFormat("连接断开 >> IP:{0}", client.RemoteEndPoint.ToString());
        };
        _server.OnReceive += (data) =>
        {
            UnityEngine.Debug.LogFormat("接收到数据>>>{0}", data.Buff.Length);
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
        _server.Close();
    }
}
