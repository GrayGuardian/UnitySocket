# UnitySocket
该项目是纯C#实现的Socke网络模块，具备连接、发送、接收、重连等功能，项目基于Unity平台测试，包含两个Unity项目SocketServer、SocketClient，仅测试代码不同，Socket模块代码均是一套，目前仅测试过Windows端、Android端可用。

## 项目说明

### 代码说明
- ByteStreamBuff：Socket报文体序列化类，将需要传递的数据转成字节集，再进行装包；接收到的报文拆包后，将报文体序列化提取数据。此处为了简单考虑，仅使用了Binary，可自行改为Protobuf、Json等序列化形式。 
- DataBuff：Socket传输数据缓存区，此处主要处理Socket传输时粘包、分包的情况，Socket传输过程中将接收到数据放入缓存区，缓存区内尝试将报文一个个拆包出来。
- SocketEvent：Socket报文类型枚举，此处只枚举了网络底层需要发送的报文类型，业务逻辑层所使用的报文类型，建议封装至报文体序列化类中。 
- SocketDataPack：Socket报文类，处理具体的拆包、装包逻辑。
- SocketServer：Socket服务端，可new出多个对象
- SocketClient：Socket客户端，可new出多个对象