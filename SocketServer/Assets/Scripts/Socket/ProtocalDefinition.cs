/// <summary>
/// 网络事件协议ID枚举
/// </summary>
public enum eProtocalCommand
{
    sc_data_image = 0x3000,//图片
    sc_data_video = 0x4000,//视频
    server_received_data = 0x6000,//收到消息
    sc_data_version_controls = 0x7000,//版本控制
    sc_data_connect_success = 0x8000,//连接成功
    //Object数据
    sc_data_obj_picturebook = 0xA100,//Ipad控制数据
    sc_data_obj_get_all_picturebook = 0xA200,//获取所有绘本数据
    sc_data_obj_get_process = 0xA300,//根据绘本ID和课程类型获取流程
    sc_data_obj_switch_process = 0xA400,//根据流程ID切换程序
    sc_data_obj_switch_question = 0xA500,//根据题目状态切换题目

    //桌面控制
    sc_data_desktop_first_connet = 0xB100,//小游戏第一次请求

    //小游戏接口
    sc_data_game_first_connet = 0xE100,//小游戏第一次请求
    sc_data_game_switch_process = 0xE200,//切换小游戏
    sc_data_game_switch_question = 0xE300,//切换题目
    sc_data_game_get_all_question = 0xE400,//获取所有题目

}
