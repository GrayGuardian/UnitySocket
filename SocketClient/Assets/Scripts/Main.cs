using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    SocketClient _client;
    private void Awake()
    {

        _client = new SocketClient("127.0.0.1", 6854);
        _client.OnDisconnect += () =>
        {
            UnityEngine.Debug.Log("断开连接");
        };

        _client.OnReceive += (data) =>
        {
            UnityEngine.Debug.LogFormat("接收到数据>>>{0} {1}", (SocketEvent)data.Type, data.Buff.Length);
        };
        _client.OnError += (ex) =>
        {
            UnityEngine.Debug.LogFormat("出现异常>>>{0}", ex);
        };

        _client.OnReConnectSuccess += (num) =>
        {
            UnityEngine.Debug.LogFormat("第{0}次重连成功", num);
        };
        _client.OnReConnectError += (num) =>
        {
            UnityEngine.Debug.LogFormat("第{0}次重连失败", num);
        };
        _client.OnReconnecting += (num) =>
        {
            UnityEngine.Debug.LogFormat("正在进行第{0}次重连", num);
        };


        _client.Connect(() =>
        {
            UnityEngine.Debug.Log("连接成功");

            // _client.DisConnect();
        }, () =>
        {
            UnityEngine.Debug.Log("连接失败");
        });

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("我是测试数据");
            _client.Send((System.UInt16)SocketEvent.sc_test, bytes);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            _client.DisConnect();
        }
    }

    private void OnDestroy()
    {
        // 注意由于Unity编译器环境下，游戏开启/关闭只影响主线程的开关，游戏关闭回调时需要通过Close函数来关闭服务端/客户端的线程。
        if (_client != null)
        {
            _client.Close();
        }

    }
}
