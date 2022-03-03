using AssemblyCommon;
using Hotfix.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix.Common
{
	public static class ProtoMessageCreator
	{
		public static Google.Protobuf.IMessage CreateMessage(string protoName)
		{

// 			if (protoName == "CLGT.HandAck") {
// 				return new CLGT.HandAck();
// 			}
// 			else if (protoName == "CLGT.HandReq") {
// 				return new CLGT.HandReq();
// 			}
// 			else if (protoName == "CLGT.KeepAliveAck") {
// 				return new CLGT.KeepAliveAck();
// 			}
// 			else if (protoName == "CLGT.LoginAck") {
// 				return new CLGT.LoginAck();
// 			}
// 			else if (protoName == "CLGT.DisconnectNtf") {
// 				return new CLGT.DisconnectNtf();
// 			}
// 			else if (protoName == "CLGT.AccessServiceAck") {
// 				return new CLGT.AccessServiceAck();
// 			}

			return null;
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

// 			CLGT.HandReq msg = new CLGT.HandReq();
// 			msg.Platform = CLGT.HandReq.Types.PlatformType.Windows;
// 			msg.Product = 1;
// 			msg.Version = 1;
// 			msg.Device = AppController.ins.conf.GetDeviceID();
// 			msg.Channel = 1;
// 			msg.Language = "ZH-CN";
// 			msg.Country = "CN";
// 			AppController.ins.net.SendPb2(msg.GetType().Name, msg, sock_);
		}

		void HandleMessage_(string protoName, Google.Protobuf.IMessage pb)
		{
			if (protoName == "CLGT.HandAck") {
// 				var pbthis = (CLGT.HandAck)(pb);
// 				sock_.randomKey = pbthis.RandomKey.ToByteArray();
// 
// 				CLGT.LoginReq msg = new CLGT.LoginReq();
// 				msg.Token = "";
// 				msg.LoginType = CLGT.LoginReq.Types.LoginType.Guest;
// 
// 				AppController.ins.net.SendPb2(msg.GetType().Name, msg, sock_);
			}
			else if (protoName == "CLGT.LoginAck") {

// 				var pbthis = (CLGT.LoginAck)(pb);
// 				var self = AppController.ins.self.gamePlayer;
// 				self.iid = pbthis.UserId;
// 				self.nickName = pbthis.Nickname;
// 
// 				self.items[(int)ITEMID.GOLD] = pbthis.Currency;
// 				self.items[(int)ITEMID.BANK_GOLD] = pbthis.BankCurrency;
// 				state_ = State.Succ;
// 				Result?.Invoke(this, (int)state_);
			}
		}

		void OnMsg_(object sender, NetEventArgs evt)
		{
			//不是我这个socket的消失就忽略
			if (sock_ != sender) return;

			var proto = ProtoMessageCreator.CreateMessage(evt.strCmd);
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
// 				CLGT.KeepAliveReq msg = new CLGT.KeepAliveReq();
// 				AppController.ins.net.SendPb2(msg.GetType().Name, msg, null);
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

		void HandleMessage_(string protoName, Google.Protobuf.IMessage pb)
		{
			//ping计时,统计服务器延时
			if (protoName == "CLGT.KeepAliveAck") {
				pingTimeCost += pingTimeCounter.Elapse();
				pingTimeCounter.Restart();
				pingCount++;
			}
			else if (protoName == "CLGT.DisconnectNtf") {
// 				var pbthis = (CLGT.DisconnectNtf)(pb);
// 				errCode = pbthis.Code;
// 				SetState(EnState.Disconnected);
// 				Result?.Invoke(this, (int)state_);
			}
			else if (protoName == "CLGT.AccessServiceAck") {
// 				var pbthis = (CLGT.AccessServiceAck)(pb);
// 				errCode = pbthis.Errcode;
// 				string gameData = pbthis.GameData;
// 				if (errCode == 0) {
// 					SetState(EnState.InLobby);
// 					Result?.Invoke(this, (int)state_);
// 				}
// 				else {
// 					SetState(EnState.AcquireServiceFailed);
// 					Result?.Invoke(this, (int)state_);
// 				}
			}
			else if (protoName == gameName_ + ".EnterRoomAck") {
// 				var pbthis = (CLSLWH.EnterRoomAck)(pb);
// 				if (pbthis.Errcode == 0) {
// 					SetState(EnState.Gaming);
// 					Result?.Invoke(this, (int)state_);
// 				}
// 				else {
// 					SetState(EnState.EnterRoomFailed);
// 					Result?.Invoke(this, (int)state_);
// 				}
			}
			else if (protoName == gameName_ + ".ExitRoomAck") {
				
			}
		}

		void OnMsg_(object sender, NetEventArgs evt)
		{
			var proto = ProtoMessageCreator.CreateMessage(evt.strCmd);
			if (proto == null) return;
			HandleMessage_(evt.strCmd, proto);
		}

		void AquireService_()
		{
			SetState(EnState.AcquireService);

// 			CLGT.AccessServiceReq msg = new CLGT.AccessServiceReq();
// 			msg.ServerName = gameName_;
// 			msg.Action = 1;
// 			AppController.ins.net.SendPb2(msg.GetType().Name, msg, null);
		}

		EnState state_;
		string gameName_ = "";
		TimeCounter pingTimer = new TimeCounter("");
		TimeCounter pingTimeCounter = new TimeCounter("");
		float pingTimeCost; 
		long pingCount = 0;
	}
}
