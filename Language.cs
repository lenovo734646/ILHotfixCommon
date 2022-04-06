using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix.Common
{
	public static class LangNetWork
	{
		public const string InitNetwork = "正在初始化网络...";
		public const string ResovingDNS = "正在解析域名...";
		public const string ResovingDNSSucc = "解析成功.";
		public const string HandShake = "正在握手...";
		public const string HandShakeSucc = "握手成功.";
		public const string AcquireService = "正在获取游戏服务...";
		public const string AcquireServiceSucc = "游戏服务获取成功.";
		public const string InLobby = "进入大厅成功.";
		public const string EnterRoom = "正在进入游戏房间...";
		public const string Gaming = "进入房间成功.";
		public const string Disconnected = "网关连接被断开.";
		public const string HandShakeFailed = "握手失败.";
		public const string AcquireServiceFailed = "游戏服务获取失败!";
		public const string EnterRoomFailed = "进入房间失败!";
		public const string AuthorizeFailed = "登录失败!{0}";
		public const string Connecting = "正在连接...";
		public const string ConnectSucc = "连接成功.";
		public const string ConnectFailed = "连接失败.";
		public const string ConnectionCloseByRemote = "网络连接被远端关闭.";
		public const string Closed = "网络重置.";
	}

	public static class LangUITip
	{
		public const string PopupAutoCloseInTime = "{0}秒后自动关闭";
		public const string EnterGameFailed = "进入游戏失败.";
		public const string RegisterFailed = "注册失败.";
		public const string ServerIsBusy = "服务器正忙,请稍后再试.";

	}
}
