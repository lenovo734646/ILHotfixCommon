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

			AppController.ins.net.RegisterMsgHandler(OnMsg_);

 			CLGT.HandReq msg = new CLGT.HandReq();

			msg.platform = (int) 0;
			msg.product = 1;
			msg.version = 1;
			msg.device = AppController.ins.conf.GetDeviceID();
			msg.channel = 1;
			msg.language = "ZH-CN";
			msg.country = "CN";

  			AppController.ins.net.SendPb2("CLGT.HandReq", msg, sock_);
		}

		void HandleMessage_(string protoName, IProtoMessage pb)
		{
			if (protoName == "CLGT.HandAck") {
				var pbthis = (CLGT.HandAck)(pb);
				sock_.randomKey = pbthis.random_key;

				CLGT.LoginReq msg = new CLGT.LoginReq();
				msg.token = "";
				msg.login_type = (int)CLGT.LoginReq.LoginType.Guest;

				AppController.ins.net.SendPb2("CLGT.LoginReq", msg, sock_);
			}
			else if (protoName == "CLGT.LoginAck") {

				var pbthis = (CLGT.LoginAck)(pb);
				var self = AppController.ins.self.gamePlayer;
				self.iid = pbthis.user_id;
				self.nickName = pbthis.nickname;

				self.items[(int)ITEMID.GOLD] = pbthis.currency;
				self.items[(int)ITEMID.BANK_GOLD] = pbthis.bank_currency;

				state_ = State.Succ;
				Result?.Invoke(this, (int)state_);
			}
		}

		void OnMsg_(object sender, NetEventArgs evt)
		{
			//不是我这个socket的消失就忽略
			if (sock_ != sender) return;

			var proto = ProtoMessageCreator.CreateMessage(evt.strCmd, evt.payload);
			if (proto == null) return;
			HandleMessage_(evt.strCmd, proto);
		}

		public void Stop()
		{
			Result = null;
			AppController.ins.net.RemoveMsgHandler(OnMsg_);
		}

		MySocket sock_;
		State state_;
		TimeCounter timeOut = new TimeCounter("");
	}

	//FLLU3d项目的游戏连接会话管理
	public class FLLU3dSession
	{
		public enum EnState
		{
			//
			Initiation,
			//获取服务阶段
			AcquireService,
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
			//失去部分连接
			Disconnected,
			//
			AcquireServiceFailed,
			//
			EnterRoomFailed,

			ExitRoomSucc,
		}

		public int errCode = 0;
		public event EventHandler<int> Result;
		public FLLU3dSession()
		{
			SetState(EnState.Initiation);
		}

		public void Update()
		{
			if (pingTimer.Elapse() > 5.0f && state_ > EnState.PingBegin && state_ < EnState.PingEnd) {
				pingTimer.Restart();
				pingTimeCounter.Restart();
				CLGT.KeepAliveReq msg = new CLGT.KeepAliveReq();
				AppController.ins.net.SendPb2(msg.GetType().Name, msg, null);
			}
		}

		//进入游戏
		public void EnterGame(string game)
		{
			gameName_ = game;
			AquireService_();
		}


		public void Start()
		{
			AppController.ins.net.RegisterMsgHandler(OnMsg_);
		}

		public void Stop()
		{
			AppController.ins.net.RemoveMsgHandler(OnMsg_);
		}

		public EnState State()
		{
			return state_;
		}
		
		//获取消息延时
		public float GetLatency()
		{
			return pingTimeCost / pingCount;
		}

		void SetState(EnState st)
		{
			state_ = st;
		}

		void HandleMessage_(string protoName, IProtoMessage pb)
		{
			//ping计时,统计服务器延时
			if (protoName == "CLGT.KeepAliveAck") {
				pingTimeCost += pingTimeCounter.Elapse();
				pingTimeCounter.Restart();
				pingCount++;
			}
			else if (protoName == "CLGT.DisconnectNtf") {
				var pbthis = (CLGT.DisconnectNtf)(pb);
				errCode = pbthis.code;
				SetState(EnState.Disconnected);
				Result?.Invoke(this, (int)state_);
			}
			else if (protoName == gameName_ + ".ExitRoomAck") {
				SetState(EnState.ExitRoomSucc);
				Result?.Invoke(this, (int)state_);
			}
		}

		void OnMsg_(object sender, NetEventArgs evt)
		{
			var proto = ProtoMessageCreator.CreateMessage(evt.strCmd, evt.payload);
			if (proto == null) return;
			HandleMessage_(evt.strCmd, proto);
		}

		void AquireService_()
		{
			SetState(EnState.AcquireService);

			CLGT.AccessServiceReq msg = new CLGT.AccessServiceReq();
			msg.server_name = gameName_;
			msg.action = 1;
			AppController.ins.net.SendPb2("CLGT.AccessServiceReq", msg, null);
		}

		EnState state_;
		string gameName_ = "";
		TimeCounter pingTimer = new TimeCounter("");
		TimeCounter pingTimeCounter = new TimeCounter("");
		float pingTimeCost; 
		long pingCount = 0;
	}
}
