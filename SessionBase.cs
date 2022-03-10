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

			if(ret != null) {
				ret.Decode(new Google.Protobuf.CodedInputStream(data));
			}
			return ret;
		}
	}

	//连接握手验证流程,连接只有走完这个流程,才能正常运行
	public class FLLU3dHandshake
	{
		public enum State
		{
			//
			Initiation,
			//握手中
			Handshaking,
			//成功
			Succ,
			//失败
			Failed,
		}
		//事件派发
		public event EventHandler<int> Result;
		public FLLU3dHandshake(MySocket s)
		{
			sock_ = s;
			state_ = State.Initiation;
		}

		public bool IsTimeout()
		{
			if(state_ == State.Handshaking) {
				return timeOut.Elapse() > 5.0f;
			}
			return false;
		}

		public void TimeOut()
		{
			state_ = State.Failed;
			Result?.Invoke(this, (int)state_);
		}

		public void Start()
		{
			timeOut.Restart();
			state_ = State.Handshaking;

			Result?.Invoke(this, (int)state_);

 			CLGT.HandReq msg = new CLGT.HandReq();

			msg.platform = (int) 0;
			msg.product = 1;
			msg.version = 1;
			msg.device = AppController.ins.conf.GetDeviceID();
			msg.channel = 1;
			msg.language = "ZH-CN";
			msg.country = "CN";

			AppController.ins.network.Rpc<CLGT.HandAck>(msg, (pb)=>{
				var pbthis = (CLGT.HandAck)(pb);
				sock_.randomKey = pbthis.random_key;
				state_ = State.Succ;
				Result?.Invoke(this, (int)state_);
			});
		}

		public void Stop()
		{
			Result = null;
		}

		MySocket sock_;
		State state_;
		TimeCounter timeOut = new TimeCounter("");
	}

	public class SessionBase : ControllerBase
	{
		public enum EnState
		{
			//
			Initiation,
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

		public event EventHandler<int> Result;
		public SessionBase(string game)
		{
			gameName_ = game;
		}

		public void DispatchSessionEvent(int r)
		{
			Result.Invoke(this, r);
		}

		protected void SetState(EnState st)
		{
			state_ = st;
			DispatchSessionEvent((int)state_);
		}

		protected EnState state_;
		protected string gameName_ = "";
	}
}
