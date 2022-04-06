using AssemblyCommon;
using Hotfix.Model;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Hotfix.Common
{
	public static class ProtoMessageCreator
	{
		public static IProtoMessage CreateMessage(string protoName, byte[] data)
		{
			IProtoMessage ret = null;
			if (protoName == "CLGT.KeepAliveAck") {
				ret = new CLGT.KeepAliveAck();
			}
			else if (protoName == "CLGT.DisconnectNtf") {
				ret = new CLGT.DisconnectNtf();
			}
			else if (protoName == "CLGT.HandAck") {
				ret = new CLGT.HandAck();
			}
			else if (protoName == "CLGT.LoginAck") {
				ret = new CLGT.LoginAck();
			}
			MyDebug.LogFormat("msg:{0}", protoName);
			if(ret != null) {
				ret.Decode(new Google.Protobuf.CodedInputStream(data));
			}
			return ret;
		}
	}

	public class SessionBase : ControllerBase
	{
		public enum EnState
		{
			//
			Initiation = 100,
			HandShake,
			HandShakeSucc,
			//获取服务阶段
			AcquireService,
			AcquireServiceSucc,
			//
			PingBegin,
			//大厅中
			InLobby,
			//进入游戏房间阶段
			EnterRoom,
			//游戏中
			Gaming,
			//
			PingEnd,
			ExitRoomSucc,

			//失去部分连接
			Disconnected,
			HandShakeFailed,
			AcquireServiceFailed,
			EnterRoomFailed,
			AuthorizeFailed,
		}

		public static Dictionary<EnState, string> desc = new Dictionary<EnState, string>();
		public int closeByManual = 0;
		public bool isReconnect = false;
		public SessionBase(GameConfig game)
		{
			toGame = game;
			if(desc.Count == 0) {
				desc.Add(EnState.HandShake, LangNetWork.HandShake);
				desc.Add(EnState.HandShakeSucc, LangNetWork.HandShakeSucc);
				desc.Add(EnState.AcquireService, LangNetWork.AcquireService);
				desc.Add(EnState.AcquireServiceSucc, LangNetWork.AcquireServiceSucc);
				desc.Add(EnState.InLobby, LangNetWork.InLobby);
				desc.Add(EnState.EnterRoom, LangNetWork.EnterRoom);
				desc.Add(EnState.Gaming, LangNetWork.Gaming);
				desc.Add(EnState.Disconnected, LangNetWork.Disconnected);
				desc.Add(EnState.HandShakeFailed, LangNetWork.HandShakeFailed);
				desc.Add(EnState.AcquireServiceFailed, LangNetWork.AcquireServiceFailed);
				desc.Add(EnState.EnterRoomFailed, LangNetWork.EnterRoomFailed);
				desc.Add(EnState.AuthorizeFailed, LangNetWork.AuthorizeFailed);
			}
		}

		public IEnumerator WaitStopComplete()
		{
			while (closeByManual != 2) {
				yield return 0;
			}
			yield return 1;
		}

		protected GameConfig toGame;
	}
}
