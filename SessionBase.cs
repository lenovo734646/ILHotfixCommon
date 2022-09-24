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
	public abstract class SessionBase : ControllerBase
	{
		public enum EnState
		{
			//失去部分连接
			Disconnected,
			HandShakeFailed,
			AcquireServiceFailed,
			EnterRoomFailed,
			AuthorizeFailed,
			//
			Initiation = 100,
			HandShake,
			HandShakeSucc,
			Login,
			LoginSucc,
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
		}

		public static Dictionary<EnState, string> desc = new Dictionary<EnState, string>();
		public int closeByManual = 0;
		public SessionBase()
		{
			if(desc.Count == 0) {
				desc.Add(EnState.HandShake, LangNetWork.HandShake);
				desc.Add(EnState.HandShakeSucc, LangNetWork.HandShakeSucc);
				desc.Add(EnState.Login, LangNetWork.Login);
				desc.Add(EnState.LoginSucc, LangNetWork.LoginSucc);
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
			while (closeByManual <= 2) {
				yield return new WaitForSeconds(0.1f);
			}
			yield return 1;
		}

		public bool IsWorking()
		{
			return closeByManual != 4;
		}

		public EnState st = EnState.Initiation;
	}
}
