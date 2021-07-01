using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    public UnityEngine.UI.Text Text;
    SocketClient _client;
    private void Awake()
    {
        _client = new SocketClient("192.168.108.56", 6854);

        _client.OnDisconnect += () =>
        {
            Text.text += "断开连接" + "\n";
            UnityEngine.Debug.Log("断开连接");
        };

        _client.OnReceive += (data) =>
        {
            Text.text += string.Format("接收到数据>>>{0}", data.Buff.Length) + "\n";
            UnityEngine.Debug.LogFormat("接收到数据>>>{0}", data.Buff.Length);
        };
        _client.OnError += (ex) =>
        {
            Text.text += string.Format("出现异常>>>{0}", ex) + "\n";
            UnityEngine.Debug.LogFormat("出现异常>>>{0}", ex);
        };

        _client.OnReConnectSuccess += (num) =>
        {
            Text.text += string.Format("第{0}次重连成功", num) + "\n";
            UnityEngine.Debug.LogFormat("第{0}次重连成功", num);
        };
        _client.OnReConnectError += (num) =>
        {
            Text.text += string.Format("第{0}次重连失败", num) + "\n";
            UnityEngine.Debug.LogFormat("第{0}次重连失败", num);
        };
        _client.OnReconnecting += (num) =>
        {
            Text.text += string.Format("正在进行第{0}次重连", num) + "\n";
            UnityEngine.Debug.LogFormat("正在进行第{0}次重连", num);
        };



        _client.Connect(() =>
        {
            Text.text += "连接成功" + "\n";
            UnityEngine.Debug.Log("连接成功");
            // _client.DisConnect();
        }, () =>
        {
            Text.text += "连接失败" + "\n";
            UnityEngine.Debug.Log("连接失败");
        });

    }
    private void OnDestroy()
    {
        if (_client != null)
        {
            _client.Clear();
        }

    }
}
