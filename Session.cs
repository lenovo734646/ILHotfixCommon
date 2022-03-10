
using AssemblyCommon;
using Hotfix.Common;
using Hotfix.Model;
using System;
using System.Collections;
using UnityEngine;

namespace Hotfix.Lobby
{
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
				//AppController.ins.currentApp.game.OpenView<ViewLobby>();
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
		public KOKOSession(string name) : base(name)
		{

		}
	}
}

