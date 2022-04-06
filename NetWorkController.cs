using AssemblyCommon;
using Hotfix.Lobby;
using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
		public int cmd;
		public string strCmd;
		public byte[] payload;
	}

	public class NetWorkController:ControllerBase
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
		public bool shouldReconnect = false;
		public void SendJson(short subCmd, string json, int toserver)
		{
			sendStream_.ClearUsedData();
			MsgJsonForm msg = new MsgJsonForm();
			msg.subCmd = subCmd;
			msg.content = json;
			msg.toserver = toserver;
			msg.Write(sendStream_);
			Globals.net.SendMessage(sendStream_);
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
					yield return null;
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
			Action<IProtoMessage> callback = (msg)=>{
				responsed = true;
				if (msg != null)	Result = (T)msg;
			};

			if(Rpc<T>(proto, callback, timeout)) {
				TimeCounter tc = new TimeCounter("");
				while (!responsed && tc.Elapse() < timeout) {
					yield return null;
				}
			}
			yield return Result;
		}

		public bool Rpc<T>(IProtoMessage proto, Action<IProtoMessage> callback, float timeout) where T: IProtoMessage
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

		public void SetToGame(string toGame)
		{
			GameToLogin_ = toGame;
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

		public void AutoLogin(bool isReconnect)
		{
			var gmconf = AppController.ins.conf.FindGameConfig(GameToLogin_);
			//如果两个游戏属于不同的阵营,网络需要重置
// 			if(AppController.ins.currentGame != null && AppController.ins.currentGame.module != gmconf.module) {
// 				Globals.net.Stop();
// 			}
			
//			if (gmconf.module == GameConfig.Module.FLLU3d) {
//				session = new FLLU3dSession(GameToLogin_, resetNet_);
// 			}
// 			else {
 				session = new KoKoSession(gmconf, resetNet_);
// 			}
			session.isReconnect = isReconnect;
			session.progress = progress;
			session.Start();
		}

		public void RegisterMsgHandler(EventHandler<NetEventArgs> handler)
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

			if (shouldReconnect) {
				shouldReconnect = false;
				
				if (AppController.ins.currentGameConfig != null)
					GameToLogin_ = AppController.ins.currentGameConfig.name;
				else
					GameToLogin_ = AppController.ins.conf.defaultGameName;

				AutoLogin(true);
			}
		}

		public IEnumerator ResetSession(bool autoReconnect, bool resetNet)
		{
			resetNet_ = resetNet;
			if (session != null) {
				if (!autoReconnect || resetNet_) {
					session.Stop();
					yield return session.WaitStopComplete();
				}
				session = null;
			}
		}

		public override void Start()
		{

		}

		public override void Stop()
		{
			ResetSession(false, true);
		}

		void DispatchNetMsgEvent_(MySocket s, NetEventArgs evt)
		{
			MsgHandler?.Invoke(s, evt);
		}

		msg_base CreateMsg_(short subMsg, string content)
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
				case (short)CommID.msg_common_reply: {
					return JsonMapper.ToObject<msg_common_reply>(content);
				}
			}
			return null;
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

						var msgRsp = CreateMsg_(msg.subCmd, msg.content);
						if (msgRsp != null) {
							if (rpcHandler2.ContainsKey(msg.subCmd)) {
								//如果是通用回复
								msg_rpc_ret rsp = new msg_rpc_ret();
								rsp.err_ = 0;
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
							}
						}
						else {
							NetEventArgs evt = new NetEventArgs();
							evt.cmd = msg.subCmd;
							evt.payload = Encoding.UTF8.GetBytes(msg.content);
							DispatchNetMsgEvent_(sock, evt);
						}
					}
					break;
					//系统PING
					case (int)INT_MSGID.INTERNAL_MSGID_PING: {
						if (rpcHandler2.ContainsKey((int)INT_MSGID.INTERNAL_MSGID_PING)) {
							msg_rpc_ret rpcd = new msg_rpc_ret();
							rpcd.err_ = 0;
							rpcHandler2[(int)INT_MSGID.INTERNAL_MSGID_PING].callback(rpcd);
						}
					}
					break;
					//Protobuffer消息
					case (int)INT_MSGID.INTERNAL_MSGID_PB: {
						MsgPbForm msg = new MsgPbForm();
						msg.Read(stm);

						NetEventArgs evt = new NetEventArgs();
						evt.cmd = msg.subCmd;
						evt.payload = msg.content;
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


		BinaryStream sendStream_ = new BinaryStream(0xFFFF);
		DictionaryCached<Type, RpcTask> rpcHandler = new DictionaryCached<Type, RpcTask>();
		DictionaryCached<int, RpcTask2> rpcHandler2 = new DictionaryCached<int, RpcTask2>();
		string GameToLogin_;
		SessionBase session;
		bool resetNet_ = false;
	}
}
