using AssemblyCommon;
using Hotfix.Lobby;
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
	public class RpcTask
	{
		public Type tp;
		public Action<IProtoMessage> callback;
		public TimeCounter tc = new TimeCounter("");
		public float timeout = 3.0f;
		public bool callbackOnTimeout = false;
		public RpcTask()
		{
			tc.Restart();
		}
		public bool IsTimeout()
		{
			return tc.Elapse() >= timeout;
		}
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
		public enum LoginType
		{
			Unknown = 0,            //未知
			Guest = 1,              //游客
			Phone = 2,             //手机
			QQ = 3,                 //QQ
			Wechat = 4,             //微信
			Facebook = 5,           //Facebook
			GooglePlay = 6,         //GooglePlay
			GameCenter = 7,         //GameCenter
		}

		public event EventHandler<NetEventArgs> MsgHandler;
		public bool shouldReconnect = false;
		public void SendJson(short subCmd, string json, MySocket sock = null)
		{
			sendStream_.ClearUsedData();
			MsgJsonForm msg = new MsgJsonForm();
			msg.subCmd = subCmd;
			msg.content = Encoding.UTF8.GetBytes(json);
			msg.Write(sendStream_);
			Globals.net.SendMessage(sendStream_);
		}

		public void SendPing(MySocket sock = null)
		{
			sendStream_.ClearUsedData();
			//先写个头长度占位
			sendStream_.SetCurentWrite(4);
			sendStream_.WriteInt((int)INT_MSGID.INTERNAL_MSGID_PING);
			sendStream_.WriteDataLengthHeader();
			Globals.net.SendMessage(sendStream_);
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
		public void SetAutoLogin(LoginType tp, string account, string psw, string toGame)
		{
			GameToLogin_ = toGame;
			//设置要登录的账号
			if (tp == LoginType.Guest) {
				var token = AppController.ins.conf.GetDeviceID();
				var findit = AppController.ins.accounts.Find((acc) => { return acc.accountName == token; });
				if (findit != null) {
					AppController.ins.lastUseAccount = findit;
				}
				else {
					UserAccountInfo inf = new UserAccountInfo();
					inf.accountName = token;
					inf.loginType = (int)tp;
					AppController.ins.accounts.Add(inf);
					AppController.ins.lastUseAccount = inf;
				}
			}
			else {
				var findit = AppController.ins.accounts.Find((acc) => { return acc.accountName == account; });
				if (findit != null) {
					AppController.ins.lastUseAccount = findit;
				}
				else {
					UserAccountInfo inf = new UserAccountInfo();
					inf.accountName = account;
					inf.psw = psw;
					inf.loginType = (int)tp;
					AppController.ins.accounts.Add(inf);
					AppController.ins.lastUseAccount = inf;
				}
			}
		}

		public void AutoLogin(bool isReconnect)
		{
			var gmconf = AppController.ins.conf.FindGameConfig(GameToLogin_);
			//如果两个游戏属于不同的阵营,网络需要重置
			if(AppController.ins.lastGame != null && AppController.ins.lastGame.module != gmconf.module) {
				Globals.net.Stop();
			}
			
			if (gmconf.module == GameConfig.Module.FLLU3d) {
				session = new FLLU3dSession(GameToLogin_);
			}
			else {
				session = new KOKOSession(GameToLogin_);
			}
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

			var arr = rpcHandler.ToArray();

			for(int i = 0; i < arr.Count; i++) {
				var tsk = arr[i].Value;
				if (tsk.IsTimeout()) {
					if (tsk.callbackOnTimeout) tsk.callback(null);
					rpcHandler.Remove(tsk.tp);
				}
			}

			if (shouldReconnect) {
				shouldReconnect = false;
				AutoLogin(true);
			}
		}

		public IEnumerator Reset(bool autoReconnect)
		{
			if(Globals.net != null) Globals.net.Stop();
			if (session != null) {
				if (!autoReconnect) session.Stop();
				yield return session.WaitStop();
				session = null;
			}
		}

		public override void Start()
		{

		}

		public override void Stop()
		{
			Reset(false);
		}

		void DispatchNetMsgEvent_(MySocket s, NetEventArgs evt)
		{
			MsgHandler?.Invoke(s, evt);
		}

		private void HandleDataFrame_(MySocket sock, BinaryStream stm)
		{
			if (sock.useProtocolParser == ProtocolParser.KOKOProtocol) {
				int cmd = stm.ReadInt();
				switch (cmd) {
					//Json消息
					case (int)INT_MSGID.INTERNAL_MSGID_JSONFORM: {
						MsgJsonForm msg = new MsgJsonForm();
						msg.Read(stm);

						NetEventArgs evt = new NetEventArgs();
						evt.cmd = msg.subCmd;
						evt.payload = msg.content;
						DispatchNetMsgEvent_(sock, evt);
					}
					break;
					//系统PING
					case (int)INT_MSGID.INTERNAL_MSGID_PING: {
						
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
		string GameToLogin_;
		SessionBase session;
	}
}
