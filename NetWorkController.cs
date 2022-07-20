using AssemblyCommon;
using Hotfix.Lobby;
using Hotfix.Model;
using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static AssemblyCommon.MySocket;

namespace Hotfix.Common
{
	public class MsgHandler
	{
		public int msgID;
		public Action<int, string> HandleMsg;
		public object owner;
		public float duation = float.MaxValue;
		public float start = Time.time;
	}

	public class MsgPbHandler
	{
		public string sMsgID;
		public Action<string,  byte[]> HandleMsg;
	}

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
			if (ret != null) {
				ret.Decode(new Google.Protobuf.CodedInputStream(data));
			}
			return ret;
		}
	}

	public class NetWorkController : ControllerBase
	{
		public enum PlatformType
		{
			Unknown = 0,
			IOS = 1,
			ANDROID = 2,
			WINDOWS = 3,
			LINUX = 4,
			MAC = 5,
			WebGL = 6,
		}

		public override string GetDebugInfo()
		{
			return $"Network:Time Since Last Ping:{TimeElapseSinceLastPing()}s, msgHandlers={msgHandlers.Count}, rpcWaiting={rpcWaiting.Count}";
		}

		public bool SendMessage(ushort subCmd, string json, int toserver)
		{
			sendStream_.Reset();
			MsgJsonForm msg = new MsgJsonForm();
			msg.subCmd = subCmd;
			msg.content = json;
			msg.toserver = toserver;
			msg.Write(sendStream_);
			bool succ = Globals.net.SendMessage(sendStream_);
			if (!succ) {
				MyDebug.LogWarningFormat("Json Message Send failed:{0},{1}", subCmd, json);
			}
			return succ;
		}

		public void SendPing()
		{
			lastPingSend_++;
			sendStream_.Reset();
			//先写个头长度占位
			sendStream_.SetCurentWrite(4);
			sendStream_.WriteInt((int)INT_MSGID.INTERNAL_MSGID_PING);
			sendStream_.WriteDataLengthHeader();
			Globals.net.SendMessage(sendStream_);
		}

		//做RPC调用,方便代码编写
		public IEnumerator Rpc(ushort msgid, MsgBase proto, ushort rspID, Func<int, string, int, MsgRpcRet> callback, float timeout = 3.0f)
		{
			bool responsed = false;
			
			MsgRpcRet ret = new MsgRpcRet();
			Action<int, string> wrapCallback = (cmd, json) => {
				if (responsed) return;
				var rpcRet = callback(cmd, json, msgid);
				if (rpcRet != null) {
					responsed = true;
					ret = rpcRet;
				}
			};

			var handler = RegisterMsgHandler(rspID, wrapCallback, this);
			var handlerCommonRpl = RegisterMsgHandler((int)CommID.msg_common_reply, wrapCallback, this);

			SendMessage(msgid, proto);

			TimeCounter tc = new TimeCounter("");
			while (!responsed && tc.Elapse() < timeout) {
				yield return new WaitForSeconds(0.1f);
			}
			//超时回调
			if (!responsed) {
				ret.err_ = -9999;
			}
			RemoveMsgHandler(handler);
			RemoveMsgHandler(handlerCommonRpl);
			yield return ret;
		}

		public bool Rpc(ushort msgid, MsgBase proto, Action<string> callback, float timeout = float.MaxValue)
		{
			MsgHandler handler = null;
			bool responsed = false;
			proto.rpc_sequence_ = Globals.Random_Range(1, 1000000).ToString();
			Action<int, string> wrapCallback = (cmd, json) => {
				if (responsed) return;

				var rpl = LitJson.JsonMapper.ToObject<msg_rpc_call_ret>(json);
				if (rpl.rpc_sequence_ == proto.rpc_sequence_) {
					responsed = true;
					callback(rpl.data_);
				}

				if (responsed) {
					RemoveMsgHandler(handler);
					rpcWaiting.Remove(handler);
				}
			};


			handler = RegisterMsgHandler((int)CommID.msg_rpc_call_ret, wrapCallback, this);
			handler.duation = timeout;
			rpcWaiting.Add(handler);


			return SendMessage(msgid, proto);
		}

		public bool Rpc(ushort msgid, MsgBase proto, ushort rspID, Action<int, string> callback, float timeout = float.MaxValue)
		{
			MsgHandler handler = null, handlerCommonRpl = null;
			bool responsed = false;

			Action<int, string> wrapCallback = (cmd, json) => {
				if (responsed) return;

				if (cmd == (int)CommID.msg_common_reply) {
					var rpl = LitJson.JsonMapper.ToObject<msg_common_reply>(json);
					if(int.Parse(rpl.rp_cmd_) == msgid) {
						responsed = true;
						callback(cmd, json);
					}
				}
				else {
					responsed = true;
					callback(cmd, json);
				}

				if (responsed) {
					RemoveMsgHandler(handler);
					RemoveMsgHandler(handlerCommonRpl);

					rpcWaiting.Remove(handler);
					rpcWaiting.Remove(handlerCommonRpl);
				}
			};


			handler = RegisterMsgHandler(rspID, wrapCallback, this);
			handlerCommonRpl = RegisterMsgHandler((int)CommID.msg_common_reply, wrapCallback, this);

			rpcWaiting.Add(handler);
			handler.duation = timeout;

			rpcWaiting.Add(handlerCommonRpl);
			handlerCommonRpl.duation = timeout;
			return SendMessage(msgid, proto);
		}


		public bool SendMessage(ushort subCmd, MsgBase content)
		{
			string json = JsonMapper.ToJson(content);
			return SendMessage(subCmd, json, content.to_server());
		}

		public bool SendMessage(string subCmd, IProtoMessage proto)
		{
			sendStream_.Reset();

			MsgPbForm msg = new MsgPbForm();
			msg.protoName = subCmd;
			msg.SetProtoMessage(proto);
			msg.Write(sendStream_);

			return Globals.net.SendMessage(sendStream_);
		}

		public bool SendMessage(IProtoMessage proto)
		{
			sendStream_.Reset();

			MsgPbFormStringHeader msg = new MsgPbFormStringHeader();
			msg.protoName = proto.GetType().FullName;
			msg.SetProtoMessage(proto);
			msg.Write(sendStream_);

			return Globals.net.SendMessage(sendStream_);
		}

		public void HandleRawData(object sender, BinaryStream evt)
		{
			MySocket sock = sender as MySocket;
			HandleDataFrame_(sock, evt);
		}

		//不论是登录,还是自动重连,走的都是一个流程.
		public void SetAutoLogin(AccountInfo.LoginType tp, string account, string psw)
		{
			//设置要登录的账号
			if (tp == AccountInfo.LoginType.Guest) {
				var token = App.ins.conf.GetDeviceID();
				var findit = App.ins.accounts.Find((acc) => { return acc.accountName == token; });
				if (findit != null) {
					App.ins.lastUseAccount = findit;
					App.ins.lastUseAccount.psw = psw;
				}
				else {
					AccountInfo inf = new AccountInfo();
					inf.accountName = token;
					inf.loginType = tp;
					inf.psw = psw;
					App.ins.accounts.Add(inf);
					App.ins.lastUseAccount = inf;
				}
			}
			else {
				var findit = App.ins.accounts.Find((acc) => { return acc.accountName == account; });
				if (findit != null) {
					App.ins.lastUseAccount = findit;
					App.ins.lastUseAccount.psw = psw;
				}
				else {
					AccountInfo inf = new AccountInfo();
					inf.accountName = account;
					inf.psw = psw;
					inf.loginType = tp;
					App.ins.accounts.Add(inf);
					App.ins.lastUseAccount = inf;
				}
			}
		}

		public IEnumerator ValidSession()
		{
			if (session == null || !session.IsWorking()) {
				lastPingSend_ = 0;
				if (session != null) session.Stop();
				
				isReconnecting_ = false;

				session = new KoKoSession();
				session.progressOfLoading = progressOfLoading;
				session.Start();

				while (session.closeByManual < 2) {
					yield return new WaitForSeconds(0.1f);
				}

				if (!session.IsWorking()) {
					yield return 0;
				}
				else {
					yield return 1;
				}
			}
			else
				yield return 1;
		}

		public MsgRpcRet RPCCallback<T>(int cmd, string json, int reqID) where T: MsgBase
		{
			MsgRpcRet msgr = new MsgRpcRet();
			if (cmd ==(int)CommID.msg_common_reply) {
				var msg = JsonMapper.ToObject<msg_common_reply>(json);
				if(int.Parse(msg.rp_cmd_) == reqID) {
					msgr.err_ = int.Parse(msg.err_);
					msgr.msg = msg;
				}
			}
			else {
				msgr.err_ = 0;
				var msg = JsonMapper.ToObject<T>(json);
				msgr.msg = msg;
			}
			if (msgr.msg == null) return null;
			return msgr;
		}

		public IEnumerator EnterGame(GameConfig toGame)
		{
			MyDebug.LogFormat("AutoLogin begin.");
			if (toGame == null) toGame = App.ins.conf.defaultGame;
			bool succ = false;
			var app = App.ins;
			//没有设置登录账号,使用游客登录
			if (app.lastUseAccount == null) {
				App.ins.network.SetAutoLogin(AccountInfo.LoginType.Guest, App.ins.conf.GetDeviceID(), "893NvalEW9od");
			}

			var handleSession = ValidSession();
			yield return handleSession;
			if ((int)handleSession.Current == 0) {
				MyDebug.LogFormat("AutoLogin failed on valid session fail.");
				goto Clean;
			}
			else {
				MyDebug.LogFormat("valid session succ.");
			}

			if (session.st <= SessionBase.EnState.HandShakeSucc) {
				MyDebug.LogFormat("will login.");
				//登录======================================
				if (app.lastUseAccount.loginType == AccountInfo.LoginType.Guest) {
					msg_user_register msg = new msg_user_register();
					msg.type_ = "7";
					msg.acc_name_ = app.lastUseAccount.accountName;
					msg.pwd_hash_ = Globals.Md5Hash(app.lastUseAccount.psw);
					msg.machine_mark_ = app.conf.GetDeviceID();
					msg.sign_ = Globals.Md5Hash(msg.acc_name_ + msg.pwd_hash_ + msg.machine_mark_ + "{51B539D8-0D9A-4E35-940E-22C6EBFA86A8}");
					var resultOfRpc = app.network.Rpc((ushort)AccReqID.msg_user_register, msg, (ushort)CommID.msg_common_reply,
						RPCCallback<msg_common_reply>);
					yield return resultOfRpc;

					MsgRpcRet rpcd = (MsgRpcRet)(resultOfRpc.Current);

					msg_common_reply r = (msg_common_reply)(rpcd.msg);
					if (r.err_ == "-994") {
						progressOfLoading?.Desc(LangUITip.ServerIsBusy);
						goto Clean;
					}
					else if (r.err_ != "0" && r.err_ != "-995") {
						progressOfLoading?.Desc(LangUITip.RegisterFailed);
						goto Clean;
					}
				}

				{
					msg_user_login msgReq = new msg_user_login();
					msgReq.acc_name_ = app.lastUseAccount.accountName;
					msgReq.pwd_hash_ = Globals.Md5Hash(app.lastUseAccount.psw);
					msgReq.machine_mark_ = app.conf.GetDeviceID();
					msgReq.sign_ = Globals.Md5Hash(msgReq.acc_name_ + msgReq.pwd_hash_ + msgReq.machine_mark_ + "{51B539D8-0D9A-4E35-940E-22C6EBFA86A8}");
					MyDebug.LogFormat($"Login Use:{msgReq.acc_name_},{msgReq.machine_mark_}");

					var resultOfRpc = app.network.Rpc((ushort)AccReqID.msg_user_login, msgReq,
						(ushort)AccRspID.msg_user_login_ret, RPCCallback<msg_user_login_ret>);
					yield return resultOfRpc;

					MsgRpcRet rpcd = (MsgRpcRet)(resultOfRpc.Current);
					if (rpcd.err_ != 0) {
						MyDebug.LogFormat("登录失败.{0}", rpcd.err_);
						progressOfLoading?.Desc(LangNetWork.AuthorizeFailed);
						goto Clean;
					}

					msg_user_login_ret r = (msg_user_login_ret)(rpcd.msg);
					var player = app.self.gamePlayer;
					player.iid = int.Parse(r.iid_);
					player.nickName = r.nickname_;
					player.uid = r.uid_;
					player.headIco = r.headico_;
					app.self.phone = r.phone;
				}
			}

			{
				msg_get_game_coordinate msg = new msg_get_game_coordinate();
				msg.gameid_ = ((int)toGame.gameID);
				msg.uid_ = app.self.gamePlayer.uid;

				var resultOfRpc = app.network.Rpc((ushort)AccReqID.msg_get_game_coordinate, msg, (ushort)AccRspID.msg_channel_server,
					RPCCallback<msg_channel_server>);
				yield return resultOfRpc;

				MsgRpcRet rpcd = (MsgRpcRet)(resultOfRpc.Current);
				if (rpcd.err_ != 0) {
					progressOfLoading?.Desc(LangNetWork.AuthorizeFailed);
					MyDebug.LogFormat("Get Coordinate failed,error:{0},game:{1}", rpcd.err_, toGame.gameID);
					goto Clean;
				}
				msg_channel_server r = (msg_channel_server)(rpcd.msg);
				MyDebug.LogFormat($"Get Coordinate:{r.ip_},{r.port_},game:{toGame.gameID}");

			}

			succ = true;
			progressOfLoading?.Desc(LangNetWork.InLobby);
			if (toGame.gameID == GameConfig.GameID.Lobby) {
				//如果只是登录到大厅.结束流程
				yield return app.currentApp.game.GameLoginSucc();
			}
			else {
				{
					//登录
					MyDebug.LogFormat("alloc game server...");
					msg_alloc_game_server msg = new msg_alloc_game_server();
					msg.game_id_ = (int)toGame.gameID;

					var resultOfRpc = app.network.Rpc((ushort)CorReqID.msg_alloc_game_server, msg, (ushort)CorRspID.msg_switch_game_server, 
						RPCCallback<msg_switch_game_server>);
					yield return resultOfRpc;
					MsgRpcRet rpcd = (MsgRpcRet)(resultOfRpc.Current);
					if (rpcd.err_ != 0) {
						progressOfLoading?.Desc(LangNetWork.AuthorizeFailed);
						MyDebug.LogFormat("alloc game server failed");
						goto Clean;
					}
					else {
						MyDebug.LogFormat("alloc game server succ.");

						msg_switch_game_server r = (msg_switch_game_server)(rpcd.msg);
					}

				}
				yield return app.currentApp.game.GameLoginSucc();
			}
		Clean:
			if (!succ) {
				MyDebug.LogFormat("auto login failed.");
				yield return 0;
			}
			else {
				yield return 1;
			}
		}
		//进入游戏房间,这个函数需要在服务器获取到玩家金钱数据之后进行,如果没有获取到金钱数据,可能会进入失败.
		public IEnumerator EnterGameRoom(int configid, int roomid)
		{
			bool succ = false;
			{
				msg_enter_game_req msg = new msg_enter_game_req();
				msg.room_id_ = configid << 24 | roomid;
				var resultOfRpc = App.ins.network.Rpc((ushort)GameReqID.msg_enter_game_req, msg, (ushort)GameRspID.msg_prepare_enter, RPCCallback<msg_prepare_enter>);
				yield return resultOfRpc;
				MsgRpcRet rpcd = (MsgRpcRet)(resultOfRpc.Current);
				if (rpcd.err_ != 0) {
					MyDebug.LogFormat("enter game room msg_enter_game_req failed");
					goto Clean;
				}
				MyDebug.LogFormat("PrepareGameRoom");
				yield return App.ins.currentApp.game.PrepareGameRoom();
			}

			{
				msg_prepare_enter_complete msg = new msg_prepare_enter_complete();
				var resultOfRpc = App.ins.network.Rpc((ushort)GameReqID.msg_prepare_enter_complete, msg, (ushort)CommID.msg_common_reply, RPCCallback<msg_common_reply>);
				yield return resultOfRpc;
				MsgRpcRet rpcd = (MsgRpcRet)(resultOfRpc.Current);
				if (rpcd.err_ == 0) {
					MyDebug.LogFormat("OnGameRoomSucc");
					yield return App.ins.currentApp.game.GameRoomEnterSucc();
				}
				else {
					MyDebug.LogFormat("msg_prepare_enter_complete msg_common_reply failed {0}", rpcd.err_);
					goto Clean;
				}
				succ = true;
			}
			lastState = SessionBase.EnState.Gaming;
			lastConfigid = configid;
			lastRoomid = roomid;
		Clean:
			if (!succ) {
				yield return 0;
			}
			else {
				yield return 1;
			}

		}
		public void LazyUpdate()
		{
			if (checkSeesionTc_.Elapse() > 5.0f && App.ins.network.session != null) {
				checkSeesionTc_.Restart();
				if (!App.ins.disableNetwork &&
					!App.ins.network.session.IsWorking() &&
					!App.ins.network.IsReconnecting()) {
					this.StartCor(App.ins.network.Recounnect(), true);
				}
			}

			//rpc超时数据包模拟
			tmpUse.Clear();
			tmpUse.AddRange(rpcWaiting);
			foreach(var h in tmpUse) {
				if(Time.time - h.start > h.duation) {
					msg_common_reply msg = new msg_common_reply();
					msg.rp_cmd_ = h.msgID.ToString();
					msg.err_ = "-99999";
					h.HandleMsg((int) CommID.msg_common_reply, LitJson.JsonMapper.ToJson(msg));
					rpcWaiting.Remove(h);
				}
			}
		}

		public override void Update()
		{
			Globals.net?.Update();
		}

		public bool IsReconnecting()
		{
			return isReconnecting_;
		}

		public IEnumerator Recounnect()
		{
			bool succ = false;
			isReconnecting_ = true;
			MyDebug.LogFormat("Reconnecting");
			ViewToast.Create(LangNetWork.Connecting, 10000.0f);
			//确认网络连接
			var handle1 = ValidSession();
			yield return handle1;

			if ((int)handle1.Current == 0) {
				MyDebug.LogFormat("ValidSession failed.");
				goto Clean;
			}

			//登录游戏服务器
			var handle2 = EnterGame(App.ins.currentGameConfig);
			yield return handle2;

			if ((int)handle2.Current == 0) {
				MyDebug.LogFormat("EnterGame failed.");
				goto Clean;
			}

			//如果之前是在房间里,则进入上次的房间
			if(lastState == SessionBase.EnState.Gaming) {
				var handle3 = EnterGameRoom(lastConfigid, lastRoomid);
				yield return handle3;
				if((int)handle3.Current == 0) { 
					MyDebug.LogFormat("EnterGameRoom failed.");
					goto Clean;
				}
			}

			succ = true;

			Clean:
			ViewToast.Clear();
			isReconnecting_ = false;
			if (succ) {
				yield return 1;
			}
			else {
				Globals.net.Stop();
				MyDebug.LogFormat("Reconnecting failed.");
				yield return 0;
			}
		}

		public int TimeElapseSinceLastPing()
		{
			return lastPingSend_;
		}

		public override void Start()
		{
			RegisterMsgHandler((int)INT_MSGID.INTERNAL_MSGID_PING, (cmd, json) => {
				HandlePing_();
			}, this);
		}

		public override void Stop()
		{
			session.Stop();
		}

		public MsgHandler RegisterMsgHandler(int msgID, Action<int, string> handler, object owner)
		{
			var msgH = new MsgHandler();
			msgH.msgID = msgID;
			msgH.HandleMsg = handler;
			msgH.owner = owner;
			List<MsgHandler> lst;
			if (msgHandlers.TryGetValue(msgID, out lst)) {
				lst.Add(msgH);
			}
			else {
				lst = new List<MsgHandler>();
				lst.Add(msgH);
				msgHandlers.Add(msgID, lst);
			}
			return msgH;
		}

		public void RemoveMsgHandler(MsgHandler handler)
		{
			List<MsgHandler> lst;
			if (msgHandlers.TryGetValue(handler.msgID, out lst)) {
				lst.Remove(handler);
			}
		}

		public void RemoveMsgHandler(object owner)
		{
			foreach(var lst in msgHandlers) {
				var cpl = new List<MsgHandler>(lst.Value);
				foreach(var h in cpl) {
					if(h.owner == owner) {
						lst.Value.Remove(h);
					}
				}
			}
		}

		private void HandlePing_()
		{
			lastPingSend_--;
			if (lastPingSend_ < 0) lastPingSend_ = 0;
		}

		private void HandleDataFrame_(MySocket sock, BinaryStream stm)
		{
			if (sock.useProtocolParser == ProtocolParser.KOKOProtocol) {
				int len = stm.ReadInt();
				int cmd = stm.ReadInt();
				int order = stm.ReadInt();

				switch (cmd) {
				//Json消息
				case (int)INT_MSGID.INTERNAL_MSGID_JSONFORM: {
					MsgJsonForm msg = new MsgJsonForm();
					msg.Read(stm);
					int lastI = msg.content.LastIndexOf('}');
					if (msg.subCmd != 0xFFFF) {
						MyDebug.LogWarningFormat("json Msg len{2} cmd:{0}, {1},", msg.subCmd, msg.content, len);
					}

					List<MsgHandler> handlers;
					var succ = msgHandlers.TryGetValue(msg.subCmd, out handlers);
					if (succ) {
						tmpUse.Clear(); tmpUse.AddRange(handlers);
						try {
							foreach (var handler in tmpUse) {
								handler.HandleMsg(msg.subCmd, msg.content);
							}
						}
						catch (Exception ex) {
							MyDebug.LogErrorFormat("HandleMsg Msg error tlen:{0} contentlen:{1}, unused char:{2}, err:{3}",
								len, msg.content.Length, msg.content.Length - 1 - lastI, ex.Message);
						}
					}
					else {
						MyDebug.LogWarningFormat("Msg is ignored:{0}, {1}", msg.subCmd, msg.content);
					}
				}
				break;
				//Protobuffer消息
				case (int)INT_MSGID.INTERNAL_MSGID_PB: {
					MsgPbForm msg = new MsgPbForm();
					msg.Read(stm);

					// 						List<MsgPbHandler> handlers;
					// 						var succ = msgPbHandlers.TryGetValue(msg.protoName, out handlers);
					// 						if (succ) {
					// 							tmpUse.Clear(); tmpUse.AddRange(handlers);
					// 							foreach (var handler in tmpUse) {
					// 								handler.HandleMsg(msg.protoName, msg.content);
					// 							}
					// 						}
					// 						else {
					// 							MyDebug.LogWarningFormat("Msg is ignored:{0}, {1}", msg.protoName, msg.content);
					// 						}
				}
				break;
				//二进制消息
				case (int)INT_MSGID.INTERNAL_MSGID_BINFORM: {

				}
				break;
				default: {
					if (cmd == (int)INT_MSGID.INTERNAL_MSGID_PING) {
						HandlePing_();
					}
					else {
						MyDebug.LogWarningFormat("Msg is ignored:{0}", cmd);
					}
				}
				break;
				}
			}
			else {
				throw new Exception("Unknown protocol parser.");
			}
		}

		public SessionBase session;
		public SessionBase.EnState lastState = SessionBase.EnState.Initiation;

		TimeCounter checkSeesionTc_ = new TimeCounter("");
		BinaryStream sendStream_ = new BinaryStream(0xFFFF);
		int lastPingSend_ = 0;
		bool isReconnecting_ = false;
		int lastConfigid, lastRoomid;
		Dictionary<int, List<MsgHandler>> msgHandlers = new Dictionary<int, List<MsgHandler>>();
		Dictionary<string, List<MsgPbHandler>> msgPbHandlers = new Dictionary<string, List<MsgPbHandler>>();
		List<MsgHandler> tmpUse = new List<MsgHandler>();

		List<MsgHandler> rpcWaiting = new List<MsgHandler>();
	}
}
