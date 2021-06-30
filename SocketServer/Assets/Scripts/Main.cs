using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    SocketServer _server;
    private void Awake()
    {
        _server = new SocketServer("127.0.0.1", 6854);
    }
    private void OnDestroy()
    {
        _server.Close();
    }
}
