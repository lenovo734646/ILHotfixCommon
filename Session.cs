
using AssemblyCommon;
using Hotfix.Common;
using Hotfix.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AssemblyCommon.MySocket;

namespace Hotfix.Lobby
{
	//FLLU3d项目的游戏连接会话管理
	public class FLLU3dSession: SessionBase
	{
		public FLLU3dSession(GameConfig game, bool resetNet)
		{
			resetNet_ = resetNet;
		}

		public override void Update()
		{
			if (pingTimer_.Elapse() > 5.0f) {
				pingTimer_.Restart();
				pingCostCounter_.Restart();

				CLGT.KeepAliveReq msg = new CLGT.KeepAliveReq();
				
				AppController.ins.network.Rpc<CLGT.KeepAliveAck>(msg, (ack)=> {
					if(ack != null) {
						pingTimeCost_ += pingCostCounter_.Elapse();
						pingCostCounter_.Restart();
						pingSucc_++;
					}
					else {
						pingFailed_++;
						MyDebug.LogFormat("Ping Failed,{0}", pingFailed_);
						//3次ping失败,重置网络
						if(pingFailed_ >= 3) {
							Globals.net.Stop();
						}
					}
				}, 5.0f);
			}
		}
		public void StartFLLU3dNetwork(Dictionary<string, int> hosts, float timeOut)
		{
			progress?.Desc(LangNetWork.InitNetwork);

			if (Globals.net != null) {
				Globals.net.Stop();
			}

			Globals.net = new NetManager(hosts, timeOut, MySocket.ProtocolParser.FLLU3dProtocol);
			Globals.net.Start();
		}

		IEnumerator Handshake_()
		{
			CLGT.HandReq msg = new CLGT.HandReq();
			msg.platform = (int)0;
			msg.product = 1;
			msg.version = 1;
			msg.device = AppController.ins.conf.GetDeviceID();
			msg.channel = 1;
			msg.language = "ZH-CN";
			msg.country = "CN";

			int result = -1;
			Action<IProtoMessage> cb = (ack) => {
				var msg1 = (CLGT.HandAck)ack;
				if (msg1 != null && msg1.errcode == 0) {
					Globals.net.sock().encrypt.SetRandomKey(msg1.random_key);
					MyDebug.LogFormat("randomkey:{0}", msg1.random_key);
					result = 1;
				}
				else {
					result = -2;
					if (msg1 != null) MyDebug.LogFormat("Handshake failed with:{0}", msg1.errcode);
				}

			};

			if (!AppController.ins.network.Rpc<CLGT.HandAck>(msg, cb, AppController.ins.conf.networkTimeout)) {
				MyDebug.LogFormat("Handshake failed with rpc failed.");
				yield return -1;
			}
			else {
				while (result == -1) {
					yield return new WaitForSeconds(0.1f);
				}
				yield return result;
			}
		}

		void OnSockEvent_(object sender, MySocket.SocketState st)
		{
			MySocket sock = (MySocket)sender;
			//如果事件已经过期了,忽略
			if (sock != Globals.net.sock()) return;

			//连接成功,这个事件每个socket会调一次,立即进行握手协议
			if (st == SocketState.Working) {
			}
			//网络连接完全失败,这个是所有连接都失败之后调用的
			else if (st == SocketState.ConnectFailed) {
				progress?.Desc(LangNetWork.ConnectFailed);
			}
			else if (st == SocketState.Resolving) {
				progress?.Desc(LangNetWork.ResovingDNS);
			}
			else if (st == SocketState.ResolveSucc) {
				progress?.Desc(LangNetWork.ResovingDNSSucc);
			}
			else if (st == SocketState.Connecting) {
				progress?.Desc(LangNetWork.Connecting);
			}
			else if (st == SocketState.ClosedByRemote) {
				progress?.Desc(LangNetWork.ConnectionCloseByRemote);
			}
			else if (st == SocketState.Closed) {
				progress?.Desc(LangNetWork.Closed);
			}
		}

		IEnumerator DoStart()
		{
			MyDebug.LogFormat("New FLLSession Runing:{0}", GetHashCode());
			if (isReconnect) {
				ViewToast.Create("网络断开,正在重连...");
			}
			else {
				closeByManual = 1;
			}

			progress?.Desc(LangNetWork.Connecting);

			var app = AppController.ins;
			TimeCounter tc = new TimeCounter("");

			bool netReseted = false;
			//如果网络模块不正常,则开始初始化网络============
			if (resetNet_ || Globals.net == null || !Globals.net.IsWorking()) {
				StartFLLU3dNetwork(app.conf.hosts, AppController.ins.conf.networkTimeout);
				netReseted = true;
			}

			Globals.net.RegisterSockEventHandler(OnSockEvent_);
			Globals.net.RegisterRawDataHandler(AppController.ins.network.HandleRawData);

			while (!Globals.net.IsWorking() && tc.Elapse() < AppController.ins.conf.networkTimeout) {
				yield return new WaitForSeconds(0.1f);
			}
			//网络没连接上,跳出
			if (!Globals.net.IsWorking()) {
				progress?.Desc(LangNetWork.ConnectFailed);
				MyDebug.LogFormat("FLLSession failed with !Globals.net.IsWorking()");
				goto Clean;
			}

			if (netReseted) {
				progress?.Desc(LangNetWork.HandShake);
				//握手===================================
				var handle1 = Handshake_();
				yield return handle1;
				//如果握手失败
				if ((int)handle1.Current != 1) {
					progress?.Desc(LangNetWork.HandShakeFailed);
					MyDebug.LogFormat("FLLSession failed with Handshake");
					goto Clean;
				}

				progress?.Desc(LangNetWork.HandShakeSucc);
				//这是开发人员犯错,抛出异常
				if (app.lastUseAccount == null) {
					throw new Exception("AppController.ins.lastUseAccount == null,it must be settled before login.");
				}

				//登录======================================
				CLGT.LoginReq msgReq = new CLGT.LoginReq();
				msgReq.login_type = (int)app.lastUseAccount.loginType;
				if (msgReq.login_type == (int)AccountInfo.LoginType.Guest) {
					msgReq.token = app.conf.GetDeviceID();
				}
				else if (msgReq.login_type == (int)AccountInfo.LoginType.GameCenter) {
					msgReq.token = app.lastUseAccount.accountName + "," + app.lastUseAccount.psw;
				}

				MyDebug.LogFormat("Login Use:{0},{1}", msgReq.login_type, msgReq.token);

				var resultOfRpc = app.network.Rpc<CLGT.LoginAck>(msgReq);
				yield return resultOfRpc;

				if (resultOfRpc.Current == null) {
					progress?.Desc(LangNetWork.AuthorizeFailed);
					goto Clean;
				}

				CLGT.LoginAck r = (CLGT.LoginAck)(resultOfRpc.Current);
				if (r.errcode != 0) {
					MyDebug.LogFormat("登录失败.{0}", r.errcode);
					progress?.Desc(LangNetWork.AuthorizeFailed);
					goto Clean;
				}

				progress?.Desc(LangNetWork.InLobby);

				var self = app.self.gamePlayer;
				self.iid = r.user_id;
				self.nickName = r.nickname;

				self.items[(int)ITEMID.GOLD] = r.currency;
				self.items[(int)ITEMID.BANK_GOLD] = r.bank_currency;
			}

			//如果只是登录到大厅.结束流程
// 			if (toGame == app.conf.defaultGame) {
// 				var lobby = AppController.ins.currentApp.game.OpenView<ViewLobby>();
// 				lobby.Start();
// 				lobby.progress = AppController.ins.progress;
// 			}
// 			else {
// 				CLGT.AccessServiceReq msg2 = new CLGT.AccessServiceReq();
// 				msg2.server_name = gameName_;
// 				msg2.action = 1;
// 				var result2 = app.network.Rpc<CLGT.AccessServiceAck>(msg2);
// 
// 				yield return result2;
// 				if (result2.Current == null) {
// 					progress?.Desc(LangNetWork.AcquireServiceFailed);
// 					goto Clean;
// 				}

				//游戏流程继续,未完待续

				//进入游戏了
//			}

			closeByManual = 2;
			while (Globals.net.IsWorking() && closeByManual == 0) {
				Update();
				yield return new WaitForSeconds(0.1f);
			}
			MyDebug.LogFormat("Session will exit! Globals.net.IsWorking():{0}, closeByManual:{1}", Globals.net.IsWorking(), closeByManual);
			Clean:
			Globals.net.RemoveRawDataHandler(AppController.ins.network.HandleRawData);
			Globals.net.RemoveSockEventHandler(OnSockEvent_);
			ViewToast.Clear();
			AppController.ins.network.RemoveMsgHandler(OnMsg_);

			closeByManual = 4;
		}

		public override void Start()
		{
			MyDebug.LogFormat("New FLLSession Start {0}", GetHashCode());
			AppController.ins.network.AddMsgHandler(OnMsg_);
			//这个协程进行排队.避免多个一起进行
			AppController.ins.StartCor(DoStart(), true);
		}

		public override void Stop()
		{
			if (closeByManual == 0)
				closeByManual = 1;
			MyDebug.LogFormat("====>Session Stop:{0}", closeByManual);
		}

		//获取消息延时
		public float GetLatency()
		{
			return pingTimeCost_ / pingSucc_;
		}

		//处理服务器主动推送的消息,没有rpc机制
		void HandleMessage_(string protoName, IProtoMessage pb)
		{
			//ping计时,统计服务器延时
			if (protoName == "CLGT.DisconnectNtf") {
				var pbthis = (CLGT.DisconnectNtf)(pb);
				closeByManual = 1;
			}
		}

		void OnMsg_(object sender, NetEventArgs evt)
		{
			var proto = ProtoMessageCreator.CreateMessage(evt.strCmd, evt.payload);
			if (proto == null) return;
			HandleMessage_(evt.strCmd, proto);
		}

		TimeCounter pingTimer_ = new TimeCounter("");
		TimeCounter pingCostCounter_ = new TimeCounter("");
		float pingTimeCost_; 
		long pingSucc_ = 0, pingFailed_ = 0;
		bool resetNet_ = false;
	}

	public class KoKoSession : SessionBase
	{
		public override void Update()
		{
			if (pingTimer_.Elapse() > 2.0f) {
				pingTimer_.Restart();
				pingCostCounter_.Restart();
				AppController.ins.network.SendPing();
			}
			float tmElapse = AppController.ins.network.TimeElapseSinceLastPing();
			if (tmElapse > 6.0f) {
				Globals.net.Stop();
			}
		}

		public void StartKoKoNetwork(Dictionary<string, int> hosts, float timeOut)
		{
			progress?.Desc(LangNetWork.InitNetwork);

			if (Globals.net != null) {
				Globals.net.Stop();
			}

			Globals.net = new NetManager(hosts, timeOut, ProtocolParser.KOKOProtocol);
			Globals.net.Start();
		}

		IEnumerator Handshake_()
		{
			msg_handshake_req msg = new msg_handshake_req();
			msg.machine_id_ = AppController.ins.conf.GetDeviceID();
			msg.sign_ = Globals.Md5Hash(msg.machine_id_ + "1EBE295C-BE45-45C0-9AA1-496C1CEE4BDB");
			int result = -1;
			Action<msg_rpc_ret> cb = (ack) => {
				if (ack != null) {
					var msg1 = (msg_handshake_ret)ack.msg_;
					if (msg1 != null && msg1.ret_ == "0") {
						result = 1;
					}
					else {
						result = -2;
						if (msg1 != null) MyDebug.LogFormat("Handshake failed with:{0}", msg1.ret_);
					}
				}
				else {
					result = -2;
				}
			};

			if (!AppController.ins.network.Rpc((short)GateReqID.msg_handshake, msg, (short)GateRspID.msg_handshake_ret, cb, AppController.ins.conf.networkTimeout)) {
				MyDebug.LogFormat("Handshake failed with rpc failed.");
				yield return -1;
			}
			else {
				while (result == -1) {
					yield return new WaitForSeconds(0.1f);
				}
				yield return result;
			}
		}

		void OnSockEvent_(object sender, MySocket.SocketState st)
		{
			MySocket sock = (MySocket)sender;
			//如果事件已经过期了,忽略
			if (sock != Globals.net.sock()) return;

			//连接成功,这个事件每个socket会调一次,立即进行握手协议
			if (st == SocketState.Working) {
			}
			//网络连接完全失败,这个是所有连接都失败之后调用的
			else if (st == SocketState.ConnectFailed) {
				progress?.Desc(LangNetWork.ConnectFailed);
			}
			else if (st == SocketState.Resolving) {
				progress?.Desc(LangNetWork.ResovingDNS);
			}
			else if (st == SocketState.ResolveSucc) {
				progress?.Desc(LangNetWork.ResovingDNSSucc);
			}
			else if (st == SocketState.Connecting) {
				progress?.Desc(LangNetWork.Connecting);
			}
			else if (st == SocketState.ClosedByRemote) {
				progress?.Desc(LangNetWork.ConnectionCloseByRemote);
			}
			else if (st == SocketState.Closed) {
				progress?.Desc(LangNetWork.Closed);
			}
		}

		IEnumerator DoStart()
		{
			MyDebug.LogFormat("New FLLSession Runing:{0}", GetHashCode());
			st = EnState.HandShake;

			progress?.Desc(LangNetWork.Connecting);

			var app = AppController.ins;
			TimeCounter tc = new TimeCounter("");

			bool netReseted = false;
			//如果网络模块不正常,则开始初始化网络============
			if (Globals.net == null || !Globals.net.IsWorking()) {
				StartKoKoNetwork(app.conf.hosts, AppController.ins.conf.networkTimeout);
				netReseted = true;
			}

			Globals.net.RegisterSockEventHandler(OnSockEvent_);
			Globals.net.RegisterRawDataHandler(AppController.ins.network.HandleRawData);

			while (!Globals.net.IsWorking() && tc.Elapse() < AppController.ins.conf.networkTimeout) {
				yield return new WaitForSeconds(0.1f);
			}
			//网络没连接上,跳出
			if (!Globals.net.IsWorking()) {
				progress?.Desc(LangNetWork.ConnectFailed);
				MyDebug.LogFormat("KoKoSession failed with !Globals.net.IsWorking()");
				goto Clean;
			}

			if (netReseted) {
				progress?.Desc(LangNetWork.HandShake);
				//握手===================================
				var handle1 = Handshake_();
				yield return handle1;
				//如果握手失败
				if ((int)handle1.Current != 1) {
					progress?.Desc(LangNetWork.HandShakeFailed);
					MyDebug.LogFormat("KoKoSession failed with Handshake");
					goto Clean;
				}

				progress?.Desc(LangNetWork.HandShakeSucc);
				
			}
			st = EnState.HandShakeSucc;
			closeByManual = 2;
			while (Globals.net.IsWorking() && closeByManual == 2) {
				Update();
				yield return new WaitForSeconds(0.1f);
			}
			MyDebug.LogFormat("Session will exit! Globals.net.IsWorking():{0}, closeByManual:{1}", Globals.net.IsWorking(), closeByManual);
		Clean:
			closeByManual = 4;
			st = EnState.HandShakeFailed;
			Globals.net.RemoveRawDataHandler(AppController.ins.network.HandleRawData);
			Globals.net.RemoveSockEventHandler(OnSockEvent_);
			ViewToast.Clear();
		}

		public override void Start()
		{
			MyDebug.LogFormat("New FLLSession Start {0}", GetHashCode());
			//这个协程进行排队.避免多个一起进行
			AppController.ins.StartCor(DoStart(), true);
		}

		//session手动关闭,不重连
		//Global.net.Stop 关闭网络连接,会自动重连
		public override void Stop()
		{
			if (closeByManual <= 2)
				closeByManual = 4;
			MyDebug.LogFormat("====>Session Stop:{0}", closeByManual);
		}

		//获取消息延时
		public float GetLatency()
		{
			return pingTimeCost_ / pingSucc_;
		}

		TimeCounter pingTimer_ = new TimeCounter("");
		TimeCounter pingCostCounter_ = new TimeCounter("");
		float pingTimeCost_;
		long pingSucc_ = 0, pingFailed_ = 0;
	}
}

