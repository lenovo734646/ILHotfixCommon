using AssemblyCommon;
using Hotfix.Lobby;
using Hotfix.Model;
using LitJson;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using static AssemblyCommon.MySocket;

namespace Hotfix.Common
{
	public class RpcTaskBase
	{
		public float timeout = 3.0f;
		public bool callbackOnTimeout = false;
		public TimeCounter tc = new TimeCounter("");
		public RpcTaskBase()
		{
			tc.Restart();
		}
		public bool IsTimeout()
		{
			return tc.Elapse() >= timeout;
		}
	}

	public class RpcTask : RpcTaskBase
	{
		public Type tp;
		public Action<IProtoMessage> callback;
	}

	public class RpcTask2 : RpcTaskBase
	{
		public int rspID;
		public Action<msg_rpc_ret> callback;
	}

	//网络事情消息
	public class NetEventArgs
	{
		public int fromServer;
		public int cmd;
		public string strCmd;
		public byte[] payload;

		public IProtoMessage msgProto;
		public msg_base msg;
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

		public event EventHandler<NetEventArgs> MsgHandler;
		public override string GetDebugInfo()
		{
			return $"Network:Time Since Last Ping:{TimeElapseSinceLastPing()}s";
		}

		public void SendJson(short subCmd, string json, int toserver)
		{
			sendStream_.ClearUsedData();
			MsgJsonForm msg = new MsgJsonForm();
			msg.subCmd = subCmd;
			msg.content = json;
			msg.toserver = toserver;
			msg.Write(sendStream_);
			MyDebug.LogWarningFormat("Json Message Send:{0},{1}", subCmd, json);
			if (!Globals.net.SendMessage(sendStream_)) {
				MyDebug.LogWarningFormat("Json Message Send failed:{0},{1}", subCmd, json);
			}
		}

		public void SendPing()
		{
			sendStream_.ClearUsedData();
			//先写个头长度占位
			sendStream_.SetCurentWrite(4);
			sendStream_.WriteInt((int)INT_MSGID.INTERNAL_MSGID_PING);
			sendStream_.WriteDataLengthHeader();
			Globals.net.SendMessage(sendStream_);
		}


		//做RPC调用,方便代码编写
		public IEnumerator Rpc(short msgid, msg_base proto, int rspID, float timeout = 3.0f)
		{
			msg_rpc_ret Result = null;
			bool responsed = false;
			Action<msg_rpc_ret> callback = (msg) => {
				responsed = true;
				if (msg != null) Result = msg;
			};

			if (Rpc(msgid, proto, rspID, callback, timeout)) {
				TimeCounter tc = new TimeCounter("");
				while (!responsed && tc.Elapse() < timeout) {
					yield return new WaitForSeconds(0.01f);
				}
			}
			yield return Result;
		}

		public bool Rpc(short msgid, msg_base proto, int rspID, Action<msg_rpc_ret> callback, float timeout)
		{
			if (rpcHandler2.ContainsKey(rspID)) {
				return false;
			}

			if (rpcHandler2.ContainsKey(msgid)) {
				return false;
			}

			Action<msg_rpc_ret> wrapper = (msg) => {
				callback(msg);
				rpcHandler2.Remove(rspID);
				rpcHandler2.Remove(msgid);
			};

			RpcTask2 tsk = new RpcTask2();
			tsk.rspID = rspID;
			tsk.callback = wrapper;
			tsk.timeout = timeout;
			tsk.callbackOnTimeout = true;

			rpcHandler2.Add(tsk.rspID, tsk);
			rpcHandler2.Add(msgid, tsk);
			SendJson(msgid, proto);
			return true;
		}


		//做RPC调用,方便代码编写
		public IEnumerator Rpc<T>(IProtoMessage proto, float timeout = 3.0f) where T : IProtoMessage
		{
			IProtoMessage Result = null;
			bool responsed = false;
			Action<IProtoMessage> callback = (msg) => {
				responsed = true;
				if (msg != null) Result = (T)msg;
			};

			if (Rpc<T>(proto, callback, timeout)) {
				TimeCounter tc = new TimeCounter("");
				while (!responsed && tc.Elapse() < timeout) {
					yield return null;
				}
			}
			yield return Result;
		}

		public bool Rpc<T>(IProtoMessage proto, Action<IProtoMessage> callback, float timeout) where T : IProtoMessage
		{
			if (rpcHandler.ContainsKey(typeof(T))) {
				return false;
			}

			Action<IProtoMessage> wrapper = (msg) => {
				callback(msg);
				rpcHandler.Remove(typeof(T));
			};

			RpcTask tsk = new RpcTask();
			tsk.tp = typeof(T);
			tsk.callback = wrapper;
			tsk.timeout = timeout;
			tsk.callbackOnTimeout = true;
			rpcHandler.Add(tsk.tp, tsk);

			SendPb2(proto);
			return true;
		}

		public void SendJson(short subCmd, msg_base content)
		{
			string json = JsonMapper.ToJson(content);
			SendJson(subCmd, json, content.to_server());
		}

		public void SendPb(short subCmd, IProtoMessage proto)
		{
			sendStream_.ClearUsedData();

			MsgPbForm msg = new MsgPbForm();
			msg.subCmd = subCmd;
			msg.SetProtoMessage(proto);
			msg.Write(sendStream_);

			Globals.net.SendMessage(sendStream_);
		}

		public void SendPb2(IProtoMessage proto)
		{
			sendStream_.ClearUsedData();

			MsgPbFormStringHeader msg = new MsgPbFormStringHeader();
			msg.protoName = proto.GetType().FullName;
			msg.SetProtoMessage(proto);
			msg.Write(sendStream_);

			Globals.net.SendMessage(sendStream_);
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
				var token = AppController.ins.conf.GetDeviceID();
				var findit = AppController.ins.accounts.Find((acc) => { return acc.accountName == token; });
				if (findit != null) {
					AppController.ins.lastUseAccount = findit;
					AppController.ins.lastUseAccount.psw = psw;
				}
				else {
					AccountInfo inf = new AccountInfo();
					inf.accountName = token;
					inf.loginType = tp;
					inf.psw = psw;
					AppController.ins.accounts.Add(inf);
					AppController.ins.lastUseAccount = inf;
				}
			}
			else {
				var findit = AppController.ins.accounts.Find((acc) => { return acc.accountName == account; });
				if (findit != null) {
					AppController.ins.lastUseAccount = findit;
					AppController.ins.lastUseAccount.psw = psw;
				}
				else {
					AccountInfo inf = new AccountInfo();
					inf.accountName = account;
					inf.psw = psw;
					inf.loginType = tp;
					AppController.ins.accounts.Add(inf);
					AppController.ins.lastUseAccount = inf;
				}
			}
		}
		public IEnumerator ValidSession()
		{
			if (session == null || !session.IsWorking()) {
				lastPing_ = float.MaxValue;
				
				if (session != null) session.Stop();

				session = new KoKoSession();
				session.progress = progress;
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

		public IEnumerator EnterGame(GameConfig toGame)
		{
			MyDebug.LogFormat("AutoLogin begin.");
			if (toGame == null) toGame = AppController.ins.conf.defaultGame;
			bool succ = false;
			var app = AppController.ins;
			//没有设置登录账号,使用游客登录
			if (app.lastUseAccount == null) {
				AppController.ins.network.SetAutoLogin(AccountInfo.LoginType.Guest, AppController.ins.conf.GetDeviceID(), "893NvalEW9od");
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
					var resultOfRpc = app.network.Rpc((short)AccReqID.msg_user_register, msg, (short)CommID.msg_common_reply);
					yield return resultOfRpc;

					if (resultOfRpc.Current == null) {
						progress?.Desc(LangUITip.RegisterFailed);
						goto Clean;
					}

					msg_rpc_ret rpcd = (msg_rpc_ret)(resultOfRpc.Current);
					msg_common_reply r = (msg_common_reply)(rpcd.msg_);

					if (r.err_ == "-994") {
						progress?.Desc(LangUITip.ServerIsBusy);
						goto Clean;
					}
					else if (r.err_ != "0" && r.err_ != "-995") {
						progress?.Desc(LangUITip.RegisterFailed);
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

					var resultOfRpc = app.network.Rpc((short)AccReqID.msg_user_login, msgReq, (short)AccRspID.msg_user_login_ret);
					yield return resultOfRpc;

					if (resultOfRpc.Current == null) {
						progress?.Desc(LangNetWork.AuthorizeFailed);
						goto Clean;
					}

					msg_rpc_ret rpcd = (msg_rpc_ret)(resultOfRpc.Current);

					msg_user_login_ret r = (msg_user_login_ret)(rpcd.msg_);
					if (rpcd.err_ != 0) {
						MyDebug.LogFormat("登录失败.{0}", rpcd.err_);
						progress?.Desc(LangNetWork.AuthorizeFailed);
						goto Clean;
					}

					var player = app.self.gamePlayer;
					player.iid = int.Parse(r.iid_);
					player.nickName = r.nickname_;
					player.uid = r.uid_;
					player.items[(int)ITEMID.GOLD] = long.Parse(r.gold_);
					player.headIco = r.headico_;
					app.self.phone = r.phone;
				}
			}

			{
				msg_get_game_coordinate msg = new msg_get_game_coordinate();
				msg.gameid_ = ((int)toGame.gameID);
				msg.uid_ = app.self.gamePlayer.uid;

				var resultOfRpc = app.network.Rpc((short)AccReqID.msg_get_game_coordinate, msg, (short)AccRspID.msg_channel_server);
				yield return resultOfRpc;

				if (resultOfRpc.Current == null) {
					progress?.Desc(LangNetWork.AuthorizeFailed);
					MyDebug.LogFormat("Get Coordinate failed");
					goto Clean;
				}
				else {
					MyDebug.LogFormat("Get Coordinate succ.");
				}
				msg_rpc_ret rpcd = (msg_rpc_ret)(resultOfRpc.Current);
				msg_channel_server r = (msg_channel_server)(rpcd.msg_);
				if (rpcd.err_ != 0) {
					progress?.Desc(LangNetWork.AuthorizeFailed);
					MyDebug.LogFormat("Get Coordinate failed,error:{0},game:{1}", rpcd.err_, toGame.gameID);
					goto Clean;
				}
				MyDebug.LogFormat($"Get Coordinate:{r.ip_},{r.port_},game:{toGame.gameID}");

			}

			succ = true;
			progress?.Desc(LangNetWork.InLobby);
			if (toGame.gameID == GameConfig.GameID.Lobby) {
				//如果只是登录到大厅.结束流程
				yield return app.currentApp.game.OnGameLoginSucc();
			}
			else {
				{
					//登录
					MyDebug.LogFormat("alloc game server...");
					msg_alloc_game_server msg = new msg_alloc_game_server();
					msg.game_id_ = (int)toGame.gameID;

					var resultOfRpc = app.network.Rpc((short)CorReqID.msg_alloc_game_server, msg, (short)CorRspID.msg_switch_game_server);
					yield return resultOfRpc;
					if (resultOfRpc.Current == null) {
						progress?.Desc(LangNetWork.AuthorizeFailed);
						MyDebug.LogFormat("alloc game server failed");
						goto Clean;
					}
					else {
						MyDebug.LogFormat("alloc game server succ.");
						msg_rpc_ret rpcd = (msg_rpc_ret)(resultOfRpc.Current);
						msg_switch_game_server r = (msg_switch_game_server)(rpcd.msg_);
					}

				}
				yield return app.currentApp.game.OnGameLoginSucc();
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
				var resultOfRpc = AppController.ins.network.Rpc((short)GameReqID.msg_enter_game_req, msg, (short)GameRspID.msg_prepare_enter);
				yield return resultOfRpc;
				if (resultOfRpc.Current == null) {
					MyDebug.LogFormat("enter game room msg_enter_game_req failed");
					goto Clean;
				}
				MyDebug.LogFormat("PrepareGameRoom");
				yield return AppController.ins.currentApp.game.OnPrepareGameRoom();
			}

			{
				msg_prepare_enter_complete msg = new msg_prepare_enter_complete();
				var resultOfRpc = AppController.ins.network.Rpc((short)GameReqID.msg_prepare_enter_complete, msg, (short)CommID.msg_common_reply);
				yield return resultOfRpc;
				if (resultOfRpc.Current == null) {
					MyDebug.LogFormat("msg_prepare_enter_complete failed");
					goto Clean;
				}
				msg_rpc_ret rpcd = (msg_rpc_ret)(resultOfRpc.Current);
				msg_common_reply r = (msg_common_reply)(rpcd.msg_);
				if (r.err_ == "0") {
					MyDebug.LogFormat("OnGameRoomSucc");
					yield return AppController.ins.currentApp.game.OnGameRoomSucc();
				}
				else {
					MyDebug.LogFormat("msg_prepare_enter_complete msg_common_reply failed {0}", r.err_);
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

		public void AddMsgHandler(EventHandler<NetEventArgs> handler)
		{
			MsgHandler += handler;
		}

		public void RemoveMsgHandler(EventHandler<NetEventArgs> handler)
		{
			MsgHandler -= handler;
		}

		public override void Update()
		{
			Globals.net?.Update();
			{
				var arr = rpcHandler.ToArray();

				for (int i = 0; i < arr.Count; i++) {
					var tsk = arr[i].Value;
					if (tsk.IsTimeout()) {
						if (tsk.callbackOnTimeout) tsk.callback(null);
						rpcHandler.Remove(tsk.tp);
					}
				}
			}

			{
				var arr = rpcHandler2.ToArray();

				for (int i = 0; i < arr.Count; i++) {
					var tsk = arr[i].Value;
					if (tsk.IsTimeout()) {
						if (tsk.callbackOnTimeout) tsk.callback(null);
						rpcHandler2.Remove(tsk.rspID);
					}
				}
			}

			if (checkSeesionTc_.Elapse() > 5.0f) {
				checkSeesionTc_.Restart();
				if (!AppController.ins.network.session.IsWorking() && !AppController.ins.network.IsReconnecting()) {
					this.StartCor(AppController.ins.network.Recounnect(), true);
				}
			}
		}
		public bool IsReconnecting()
		{
			return isReconnecting_;
		}

		public IEnumerator Recounnect()
		{
			isReconnecting_ = true;
			MyDebug.LogFormat("Reconnecting");
			ViewToast.Create(LangNetWork.Connecting, 10000.0f);

			bool succ = false;
			//确认网络连接
			var handle1 = ValidSession();
			yield return handle1;

			if ((int)handle1.Current == 0) {
				MyDebug.LogFormat("ValidSession failed.");
				goto Clean;
			}

			//登录游戏服务器
			var handle2 = EnterGame(AppController.ins.currentGameConfig);
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

		public float TimeElapseSinceLastPing()
		{
			return Time.time - lastPing_;
		}

		public override void Start()
		{

		}

		public override void Stop()
		{
			session.Stop();
		}
		void DispatchNetMsgEvent_(MySocket s, NetEventArgs evt)
		{
			MsgHandler?.Invoke(s, evt);
		}

		msg_base CreateMsgFromJson_(short subMsg, string content)
		{
			switch(subMsg) {
				case (short)GateRspID.msg_handshake_ret: {
					return JsonMapper.ToObject<msg_handshake_ret>(content);
				}
				case (short)AccRspID.msg_user_login_ret: {
					return JsonMapper.ToObject<msg_user_login_ret>(content);
				}
				case (short)AccRspID.msg_channel_server: {
					return JsonMapper.ToObject<msg_channel_server>(content);
				}
				case (short)CommID.msg_sync_item: {
					return JsonMapper.ToObject<msg_sync_item>(content); 
				}
				case (short)CommID.msg_common_reply: {
					return JsonMapper.ToObject<msg_common_reply>(content);
				}
				case (short)CorRspID.msg_get_bank_info_ret: {
					return JsonMapper.ToObject<msg_get_bank_info_ret>(content);
				}
				case unchecked((short)INT_MSGID.INTERNAL_MSGID_PING): {
					return new msg_ping();
				}
				case (int)GameRspID.msg_prepare_enter: {
					return JsonMapper.ToObject<msg_prepare_enter>(content);
				}
				case (short)CorRspID.msg_switch_game_server: {
					return JsonMapper.ToObject<msg_switch_game_server>(content);
				}
			}
			return null;
		}

		IProtoMessage CreateMsgFromPb_(short subMsg, byte[] content)
		{
			switch (subMsg) {
				
			}
			return null;
		}
		private void HandlePing_()
		{
			if (rpcHandler2.ContainsKey((int)INT_MSGID.INTERNAL_MSGID_PING)) {
				msg_rpc_ret rpcd = new msg_rpc_ret();
				rpcd.err_ = 0;
				rpcHandler2[(int)INT_MSGID.INTERNAL_MSGID_PING].callback(rpcd);
			}
			lastPing_ = Time.time;
		}
		private void HandleDataFrame_(MySocket sock, BinaryStream stm)
		{
			if (sock.useProtocolParser == ProtocolParser.KOKOProtocol) {
				stm.SetCurentRead(4);
				int cmd = stm.ReadInt();

				switch (cmd) {
					//Json消息
					case (int)INT_MSGID.INTERNAL_MSGID_JSONFORM: {
						MsgJsonForm msg = new MsgJsonForm();
						msg.Read(stm);
						if (msg.subCmd != -1 && msg.toserver == 2) {
							MyDebug.LogFormat("Json message Recieved:{0},fromserver:{2},{1}", msg.subCmd, msg.content, msg.toserver);
						}
						var msgRsp = CreateMsgFromJson_(msg.subCmd, msg.content);
						if (msgRsp != null) {
							if (rpcHandler2.ContainsKey(msg.subCmd)) {
								//如果是通用回复
								msg_rpc_ret rsp = new msg_rpc_ret();
								rsp.err_ = 0;
								if(msgRsp.GetType() == typeof(msg_common_reply)) {
									var commRpl = (msg_common_reply)(msgRsp);
									rsp.err_ = int.Parse(commRpl.err_);
								}
								rsp.msg_ = msgRsp;
								rpcHandler2[msg.subCmd].callback(rsp);
							}
							else {
								if (msg.subCmd == (short)CommID.msg_common_reply) {
									var commRpl = (msg_common_reply)(msgRsp);
									if (rpcHandler2.ContainsKey(int.Parse(commRpl.rp_cmd_))) {

										msg_rpc_ret rsp = new msg_rpc_ret();
										rsp.err_ = int.Parse(commRpl.err_);
										//如果是通用回复
										rpcHandler2[int.Parse(commRpl.rp_cmd_)].callback(rsp);
									}
								}
								else if(msg.subCmd == unchecked((short)(INT_MSGID.INTERNAL_MSGID_PING))) {
									HandlePing_();
								}
								else {
									NetEventArgs evt = new NetEventArgs();
									evt.fromServer = msg.toserver;
									evt.cmd = msg.subCmd;
									evt.msg = msgRsp;
									DispatchNetMsgEvent_(sock, evt);
								}
							}
						}
						else {
							NetEventArgs evt = new NetEventArgs();
							evt.cmd = msg.subCmd;
							evt.fromServer = msg.toserver;
							evt.payload = Encoding.UTF8.GetBytes(msg.content);
							DispatchNetMsgEvent_(sock, evt);
						}
					}
					break;
					//系统PING
					case (int)INT_MSGID.INTERNAL_MSGID_PING: {
						HandlePing_();
					}
					break;
					//Protobuffer消息
					case (int)INT_MSGID.INTERNAL_MSGID_PB: {
						MsgPbForm msg = new MsgPbForm();
						msg.Read(stm);

						var msgRsp = CreateMsgFromPb_(msg.subCmd, msg.content);

						NetEventArgs evt = new NetEventArgs();
						evt.cmd = msg.subCmd;
						if (msgRsp != null) {
							evt.msgProto = msgRsp;
						}
						else {
							evt.payload = msg.content;
						}
						DispatchNetMsgEvent_(sock, evt);
					}
					break;
					//二进制消息
					case (int)INT_MSGID.INTERNAL_MSGID_BINFORM: {

					}
					break;
				}
			}
			else if (sock.useProtocolParser == ProtocolParser.FLLU3dProtocol) {
				//跳过这个无效包
				if (stm.DataLeft() == 5 && stm.buffer()[4] == 0x40) return;

				//回复一个垃圾数据
				byte[] data = new byte[5];
				BinaryStream stmAck = new BinaryStream(data, data.Length);
				stmAck.WriteInt(5);
				stmAck.WriteByte(1 << 6);
				Globals.net.SendMessage(stmAck, true);

				MsgPbFormStringHeader msg = new MsgPbFormStringHeader();
				msg.Read(stm);

				NetEventArgs evt = new NetEventArgs();
				evt.strCmd = msg.protoName;
				evt.payload = msg.content;
				var proto = ProtoMessageCreator.CreateMessage(evt.strCmd, evt.payload);
				if (proto != null) {
					if (rpcHandler.ContainsKey(proto.GetType())) {
						var handler = rpcHandler[proto.GetType()].callback;
						handler(proto);
					}
					else {
						DispatchNetMsgEvent_(sock, evt);
					}
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
		DictionaryCached<Type, RpcTask> rpcHandler = new DictionaryCached<Type, RpcTask>();
		DictionaryCached<int, RpcTask2> rpcHandler2 = new DictionaryCached<int, RpcTask2>();
		float lastPing_ = float.MaxValue;
		bool isReconnecting_ = false;
		int lastConfigid, lastRoomid;

	}
}
