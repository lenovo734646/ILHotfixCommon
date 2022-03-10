using AssemblyCommon;
using Hotfix.Lobby;
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

	//FLLU3d项目的游戏连接会话管理
	public class FLLU3dSession: SessionBase
	{
		public int errCode = 0;

		public FLLU3dSession(string game):base(game)
		{
			SetState(EnState.Initiation);
		}

		public override void Update()
		{
			if (pingTimer.Elapse() > 5.0f && state_ > EnState.PingBegin && state_ < EnState.PingEnd) {
				pingTimer.Restart();
				pingTimeCounter.Restart();

				CLGT.KeepAliveReq msg = new CLGT.KeepAliveReq();
				AppController.ins.network.Rpc<CLGT.KeepAliveAck>(msg, (ack)=> {
					pingTimeCost += pingTimeCounter.Elapse();
					pingTimeCounter.Restart();
					pingCount++;
				});
			}
		}

		IEnumerator DoStart()
		{
			SetState(EnState.HandShake);
			NetWorkController.EnState st = AppController.ins.network.state();
			if (st != NetWorkController.EnState.HandshakeSucc) {
				//网络重置
				Globals.net.Stop();
				Globals.net.Start(AppController.ins.conf.timeOut, MySocket.ProtocolParser.FLLU3dProtocol);

				TimeCounter tc = new TimeCounter("");
				st = AppController.ins.network.state();
				while (st != NetWorkController.EnState.HandshakeSucc && tc.Elapse() < AppController.ins.conf.timeOut) {
					yield return new WaitForSeconds(0.1f);
					st = AppController.ins.network.state();
				}

				if(st != NetWorkController.EnState.HandshakeSucc) {
					SetState(EnState.HandShakeFailed);
					yield break;
				}
			}

			//这是开发人员犯错,抛出异常
			if (AppController.ins.lastUseAccount == null) {
				throw new Exception("AppController.ins.lastUseAccount == null,it must be settled before login.");
			}

			CLGT.LoginReq msg1 = new CLGT.LoginReq();

			msg1.login_type = AppController.ins.lastUseAccount.loginType;
			if (msg1.login_type == (int)CLGT.LoginReq.LoginType.Guest) {
				msg1.token = AppController.ins.conf.GetDeviceID();
			}
			else if(msg1.login_type == (int)CLGT.LoginReq.LoginType.GameCenter) {
				msg1.token = AppController.ins.lastUseAccount.accountName + "," + AppController.ins.lastUseAccount.psw;
			}

			var result1 = AppController.ins.network.Rpc<CLGT.LoginAck>(msg1);
			yield return result1;
			
			if(result1.Current == null) {
				SetState(EnState.AuthorizeFailed);
				yield break;
			}

			var self = AppController.ins.self.gamePlayer;
			self.iid = result1.Current.user_id;
			self.nickName = result1.Current.nickname;

			self.items[(int)ITEMID.GOLD] = result1.Current.currency;
			self.items[(int)ITEMID.BANK_GOLD] = result1.Current.bank_currency;

			SetState(EnState.InLobby);
			//如果只是登录到大厅.结束流程
			if (gameName_ == AppController.ins.conf.defaultGame) {
				AppController.ins.currentApp.game.OpenView<ViewLobby>();
			}
			else {
				SetState(EnState.AcquireService);

				CLGT.AccessServiceReq msg2 = new CLGT.AccessServiceReq();
				msg2.server_name = gameName_;
				msg2.action = 1;
				var result2 = AppController.ins.network.Rpc<CLGT.AccessServiceAck>(msg2);

				yield return result2;
				if (result2.Current == null) {
					SetState(EnState.AcquireServiceFailed);
					yield break;
				}

				//游戏流程继续,未完待续
			}
		}

		public override void Start()
		{
			AppController.ins.network.RegisterMsgHandler(OnMsg_);
			//这个协程进行排队.避免多个一起进行
			this.StartCor(DoStart(), true);
		}

		public override void Stop()
		{
			AppController.ins.network.RemoveMsgHandler(OnMsg_);
			this.StopCor(-1);
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

		//处理服务器主动推送的消息,没有rpc机制
		void HandleMessage_(string protoName, IProtoMessage pb)
		{
			//ping计时,统计服务器延时
			if (protoName == "CLGT.DisconnectNtf") {
				var pbthis = (CLGT.DisconnectNtf)(pb);
				errCode = pbthis.code;
				SetState(EnState.Disconnected);
			}
		}

		void OnMsg_(object sender, NetEventArgs evt)
		{
			var proto = ProtoMessageCreator.CreateMessage(evt.strCmd, evt.payload);
			if (proto == null) return;
			HandleMessage_(evt.strCmd, proto);
		}

		TimeCounter pingTimer = new TimeCounter("");
		TimeCounter pingTimeCounter = new TimeCounter("");
		float pingTimeCost; 
		long pingCount = 0;
	}

	public class KOKOSession : SessionBase
	{
		public KOKOSession(string name):base(name)
		{

		}
	}

}
