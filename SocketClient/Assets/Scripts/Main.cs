using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    public UnityEngine.UI.Text Text;
    SocketClient _client;
    public static string text;
    private void Awake()
    {
        _client = new SocketClient("127.0.0.1", 6854);
        _client.OnDisconnect += () =>
        {
            text += "断开连接" + "\n";
            UnityEngine.Debug.Log("断开连接");
        };

        _client.OnReceive += (data) =>
        {
            text += string.Format("接收到数据>>>{0}", data.Buff.Length) + "\n";
            UnityEngine.Debug.LogFormat("接收到数据>>>{0}", data.Buff.Length);
        };
        _client.OnError += (ex) =>
        {
            text += string.Format("出现异常>>>{0} {1}", ex, ex.Source) + "\n";
            UnityEngine.Debug.LogFormat("出现异常>>>{0}", ex);
        };

        _client.OnReConnectSuccess += (num) =>
        {
            text += string.Format("第{0}次重连成功", num) + "\n";
            UnityEngine.Debug.LogFormat("第{0}次重连成功", num);
        };
        _client.OnReConnectError += (num) =>
        {
            text += string.Format("第{0}次重连失败", num) + "\n";
            UnityEngine.Debug.LogFormat("第{0}次重连失败", num);
        };
        _client.OnReconnecting += (num) =>
        {
            text += string.Format("正在进行第{0}次重连", num) + "\n";
            UnityEngine.Debug.LogFormat("正在进行第{0}次重连", num);
        };


        _client.Connect(() =>
        {
            text += "连接成功" + "\n";
            UnityEngine.Debug.Log("连接成功");
            // _client.DisConnect();
        }, () =>
        {
            text += "连接失败" + "\n";
            UnityEngine.Debug.Log("连接失败");
        });

    }
    private void Update()
    {
        Text.text = text;
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
