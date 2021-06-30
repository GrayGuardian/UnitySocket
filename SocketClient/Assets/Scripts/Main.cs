using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    SocketClient _client;
    private void Awake()
    {
        _client = new SocketClient("127.0.0.1", 6854);
        _client.Connect();
    }
    private void OnDestroy()
    {
        _client.Close();
    }
}
