
using AssemblyCommon;
using Hotfix.Common;
using Hotfix.Model;
using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AssemblyCommon.MySocket;

namespace Hotfix.Lobby
{
	public class KoKoSession : SessionBase
	{
		public override void Update()
		{
			if (pingTimer_.Elapse() > 2.0f) {
				pingTimer_.Restart();
				pingCostCounter_.Restart();
				App.ins.network.SendPing();
			}
			int tmElapse = App.ins.network.TimeElapseSinceLastPing();
			if (tmElapse > 3) {
				MyDebug.LogFormat("!!!!!Ping Failed, call Globals.net.Stop().!!!!!");
				Globals.net.Stop();
			}
		}

		public void StartKoKoNetwork(Dictionary<string, int> hosts, float timeOut)
		{
			progressOfLoading?.Desc(LangNetWork.InitNetwork);

			if (Globals.net != null) {
				Globals.net.Stop();
			}

			Globals.net = new NetManager(hosts, timeOut, ProtocolParser.KOKOProtocol);
			Globals.net.Start();
		}

		IEnumerator Handshake_()
		{
			msg_handshake_req msg = new msg_handshake_req();
			msg.machine_id_ = App.ins.conf.GetDeviceID();
			msg.sign_ = Globals.Md5Hash(msg.machine_id_ + "1EBE295C-BE45-45C0-9AA1-496C1CEE4BDB");

			Func<int, string, int, MsgRpcRet> cb = (cmd, json, reqID) => {
				MsgRpcRet ret = new MsgRpcRet();
				ret.msg = JsonMapper.ToObject<msg_handshake_ret>(json);
				ret.err_ = 0;
				return ret;
			};

			var handle = App.ins.network.Rpc((ushort)GateReqID.msg_handshake, msg, (ushort)GateRspID.msg_handshake_ret, cb);
			yield return handle;

			int result = -2;
			var msgRet = (MsgRpcRet)handle.Current;
			if (msgRet.err_ == 0) {
				var msg1 = (msg_handshake_ret)msgRet.msg;
				if (msg1.ret_ == "0") {
					result = 1;
				}
				else {
					result = -2;
					if (msg1 != null) MyDebug.LogFormat("Handshake failed with:{0}", msg1.ret_);
				}
			}

			yield return result;
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
				progressOfLoading?.Desc(LangNetWork.ConnectFailed);
			}
			else if (st == SocketState.Resolving) {
				progressOfLoading?.Desc(LangNetWork.ResovingDNS);
			}
			else if (st == SocketState.ResolveSucc) {
				progressOfLoading?.Desc(LangNetWork.ResovingDNSSucc);
			}
			else if (st == SocketState.Connecting) {
				progressOfLoading?.Desc(LangNetWork.Connecting);
			}
			else if (st == SocketState.ClosedByRemote) {
				progressOfLoading?.Desc(LangNetWork.ConnectionCloseByRemote);
			}
			else if (st == SocketState.Closed) {
				progressOfLoading?.Desc(LangNetWork.Closed);
			}
		}

		IEnumerator DoStart()
		{
			MyDebug.LogFormat("New FLLSession Runing:{0}", GetHashCode());
			st = EnState.HandShake;

			progressOfLoading?.Desc(LangNetWork.Connecting);

			var app = App.ins;
			TimeCounter tc = new TimeCounter("");

			bool netReseted = false;
			//如果网络模块不正常,则开始初始化网络============
			if (Globals.net == null || !Globals.net.IsWorking()) {
				StartKoKoNetwork(app.conf.gameHosts, App.ins.conf.networkTimeout);
				netReseted = true;
			}
			
			yield return Globals.net.WaitingForReady(App.ins.conf.networkTimeout);

			Globals.net.RegisterSockEventHandler(OnSockEvent_);
			Globals.net.RegisterRawDataHandler(App.ins.network.HandleRawData);
			
			//网络没连接上,跳出
			if (!Globals.net.IsWorking()) {
				progressOfLoading?.Desc(LangNetWork.ConnectFailed);
				MyDebug.LogFormat("KoKoSession failed with !Globals.net.IsWorking()");
				goto Clean;
			}

			if (netReseted) {
				progressOfLoading?.Desc(LangNetWork.HandShake);
				//握手===================================
				var handle1 = Handshake_();
				yield return handle1;
				//如果握手失败
				if ((int)handle1.Current != 1) {
					progressOfLoading?.Desc(LangNetWork.HandShakeFailed);
					MyDebug.LogFormat("KoKoSession failed with Handshake");
					goto Clean;
				}

				progressOfLoading?.Desc(LangNetWork.HandShakeSucc);
				
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
			Globals.net.RemoveRawDataHandler(App.ins.network.HandleRawData);
			Globals.net.RemoveSockEventHandler(OnSockEvent_);
			ViewToast.Clear();
		}

		public override void Start()
		{
			MyDebug.LogFormat("New FLLSession Start {0}", GetHashCode());
			//这个协程进行排队.避免多个一起进行
			App.ins.StartCor(DoStart(), true);
		}

		//session手动关闭,不重连
		//Global.net.Stop 关闭网络连接,会自动重连
		public override void Stop()
		{
			if (closeByManual <= 2)
				closeByManual = 4;
			MyDebug.LogFormat("====>Session Stop:{0}", closeByManual);
			RemoveInstance();
		}

		//获取消息延时
		public float GetLatency()
		{
			return pingTimeCost_ / pingSucc_;
		}

		TimeCounter pingTimer_ = new TimeCounter("");
		TimeCounter pingCostCounter_ = new TimeCounter("");
		float pingTimeCost_;
		long pingSucc_ = 0;
	}
}

