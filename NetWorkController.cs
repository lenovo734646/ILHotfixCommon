using AssemblyCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static AssemblyCommon.MySocket;

namespace Hotfix.Common
{
	enum INT_MSGID
	{
		INTERNAL_MSGID_COMMONRP = 1001,
		INTERNAL_MSGID_JSONFORM = 0xCCCC,
		INTERNAL_MSGID_BINFORM = 0xBBBB,
		INTERNAL_MSGID_PB = 0xDDDD,
		INTERNAL_MSGID_PING = 0xFFFF,
	}

	//网络事情消息
	public class NetEventArgs
	{
		public int cmd;
		public string strCmd;
		public byte[] payload;
	}

	public class MsgPbBase
	{
		public byte[] content;

		public void SetProtoMessage(Google.Protobuf.IMessage proto)
		{
			int len = proto.CalculateSize();
			content = new byte[len];
			Google.Protobuf.CodedOutputStream stm = new Google.Protobuf.CodedOutputStream(content);
			proto.WriteTo(stm);
		}

	}

	//Protobuffer消息包
	public class MsgPbForm : MsgPbBase
	{
		public short subCmd = 0;
		public void Read(BinaryStream stm)
		{
			//跳2个int,
			stm.SetCurentRead(8);
			subCmd = stm.ReadShort();
			content = stm.ReadArray(0);
		}

		public void Write(BinaryStream stm)
		{
			stm.SetCurentWrite(4);
			stm.WriteInt((int)INT_MSGID.INTERNAL_MSGID_PB);
			stm.WriteShort(subCmd);
			stm.WriteArray(content, 0, content.Length);
			stm.WriteDataLengthHeader();
		}
	}

	//Protobuffer消息包
	public class MsgPbFormStringHeader : MsgPbBase
	{
		public string protoName;
		public int encrypted;
		public int controlFlag;

		byte[] randomKey_;
		public MsgPbFormStringHeader(byte[] key)
		{
			randomKey_ = key;
		}

		static void rc4Algorithm(byte[] key, Span<byte> data)
		{
			int[] s = new int[256], k = new int[256];
			int i = 0, j = 0, temp;

			for (i = 0; i < 256; i++) {
				s[i] = i;
				k[i] = key[i % key.Length];
			}
			for (i = 0; i < 256; i++) {
				j = (j + s[i] + k[i]) & 0xff;
				temp = s[i];
				s[i] = s[j];
				s[j] = temp;
			}

			int x = 0, y = 0, t = 0;
			for (i = 0; i < data.Length; i++) {
				x = (x + 1) & 0xff;
				y = (y + s[x]) & 0xff;
				temp = s[x];
				s[x] = s[y];
				s[y] = temp;
				t = (s[x] + s[y]) & 0xff;
				data[i] ^= (byte)s[t];
			}
		}

		public void Read(BinaryStream stm)
		{
			//跳2个int,
			stm.SetCurentRead(4);
			controlFlag = stm.ReadChar();

			encrypted = controlFlag & 0x3F;
			controlFlag = controlFlag >> 6;
			var span = new Span<byte>(stm.buffer(), stm.reader(), stm.BufferLeft());

			if (encrypted != 0) {
				rc4Algorithm(randomKey_, span);
			}
			protoName = stm.ReadString(false);
			content = stm.ReadArray(0);
		}
		public void Write(BinaryStream stm)
		{
			stm.SetCurentWrite(4);
			if(randomKey_.Length == 0) {
				encrypted = 0;
			}
			else {
				encrypted = 1;
			}

			stm.WriteByte((byte)encrypted);
			stm.WriteString(protoName);
			stm.WriteArray(content, 0, content.Length);
			//5字节以后的内容加密
			if (encrypted == 1) {
				var span = new Span<byte>(stm.buffer(), 5, stm.DataLeft());
				rc4Algorithm(randomKey_, span);
			}
			stm.WriteDataLengthHeader();
		}
	}
		//JSon消息包
	public class MsgJsonForm
	{
		public short subCmd = 0;
		public short isCompressed = 0;

		public byte[] content;

		public void Read(BinaryStream stm)
		{
			//跳2个int,
			stm.SetCurentRead(8);
			subCmd = stm.ReadShort();
			isCompressed = stm.ReadShort();
			content = stm.ReadArray(0);
		}

		public void Write(BinaryStream stm)
		{
			stm.SetCurentWrite(4);
			stm.WriteInt((int)INT_MSGID.INTERNAL_MSGID_JSONFORM);
			stm.WriteShort(subCmd);
			stm.WriteShort(0);
			stm.WriteArray(content, 0, content.Length);
			stm.WriteDataLengthHeader();
		}
	}

	public class NetWorkController
	{
		public event EventHandler<NetEventArgs> MsgHandler;
		public enum EnState
		{
			Init,
			HandshakeSucc,
			AllConnectionLost,
		}
		//数据发送使用同步方式,省事,实际应用中没有发现问题
		public void SendJson(short subCmd, string json, MySocket sock = null)
		{
			sendStream_.ClearUsedData();
			MsgJsonForm msg = new MsgJsonForm();
			msg.subCmd = subCmd;
			msg.content = Encoding.UTF8.GetBytes(json);
			msg.Write(sendStream_);
			Globals.net.SendMessage(sendStream_, sock);
		}

		public void SendPing(MySocket sock = null)
		{
			sendStream_.ClearUsedData();
			//先写个头长度占位
			sendStream_.SetCurentWrite(4);
			sendStream_.WriteInt((int)INT_MSGID.INTERNAL_MSGID_PING);
			sendStream_.WriteDataLengthHeader();
			Globals.net.SendMessage(sendStream_, sock);
		}

		public void SendPb(short subCmd, Google.Protobuf.IMessage proto, MySocket sock = null)
		{
			sendStream_.ClearUsedData();
			
			MsgPbForm msg = new MsgPbForm();
			msg.subCmd = subCmd;
			msg.SetProtoMessage(proto);
			msg.Write(sendStream_);

			Globals.net.SendMessage(sendStream_, sock);
		}

		public void SendPb2(string protoName, Google.Protobuf.IMessage proto, MySocket sock)
		{
			sendStream_.ClearUsedData();
			
			MsgPbFormStringHeader msg = new MsgPbFormStringHeader(sock.randomKey);
			msg.protoName = protoName;
			msg.SetProtoMessage(proto);
			msg.Write(sendStream_);

			Globals.net.SendMessage(sendStream_, sock);
		}

		public void HandleRawData(object sender, BinaryStream evt)
		{
			MySocket sock = sender as MySocket;
			HandleDataFrame_(sock, evt);
		}
	
		public void HandleSockEvent(object sender, MySocket.SocketState st)
		{
			//连接成功,这个事件每个socket会调一次,立即进行握手协议
			if (st == SocketState.WORKING) {
				MySocket sock = (MySocket)sender;
				FLLU3dHandshake handShake = new FLLU3dHandshake(sock);

				handShake.Result += (obj, res) => {
					if (res == (int)FLLU3dHandshake.State.Succ) {
						state_ = EnState.HandshakeSucc;
						handShake.Stop();
					}
					else if(res == (int)FLLU3dHandshake.State.Failed) {
						handShake.Stop();
					}
				};
				
				handShake.Start();
			}
			//网络连接完全失败,这个是所有连接都失败之后调用的
			else if(st == SocketState.FAILED) {

				state_ = EnState.AllConnectionLost;
				session_ = null;
			}
		}

		public void Start(Dictionary<string, int> hosts, float timeOut)
		{
			Globals.net = new NetManager(ProtocolParser.PB_STRING_HEADER);
			Globals.net.RegisterRawDataHandler(HandleRawData);
			Globals.net.RegisterSockEventHandler(HandleSockEvent);
			Globals.net.SetHostList(hosts);
			Globals.net.Start(timeOut);
		}

		public void EnterGame(string game)
		{
			var gmconf = AppController.ins.conf.FindGameConfig(game);
			if(gmconf.scriptType == GameConfig.ScriptType.Lua) {
				
				if (session_ != null) {
					session_.Stop();
				}

				session_ = new FLLU3dSession();
				session_.Start();
				session_.EnterGame(game);
			}
		}

		public void RegisterMsgHandler(EventHandler<NetEventArgs> handler)
		{
			MsgHandler += handler;
		}

		public void RemoveMsgHandler(EventHandler<NetEventArgs> handler)
		{
			MsgHandler -= handler;
		}

		public void Update()
		{
			Globals.net.Update();
			session_?.Update();
		}

		public void Stop()
		{
			Globals.net.RemoveRawDataHandler(HandleRawData);
		}

		void DispatchNetMsgEvent_(MySocket s, NetEventArgs evt)
		{
			MsgHandler.Invoke(s, evt);
		}

		private void HandleDataFrame_(MySocket sock, BinaryStream stm)
		{
			stm.ReadInt();//跳过len
			if (sock.useProtocolParser == ProtocolParser.BIN_INT_HEADER) {
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
			else if (sock.useProtocolParser == ProtocolParser.PB_STRING_HEADER) {
				//跳过这个无效包
				if (stm.DataLeft() == 5 && stm.buffer()[4] == 0x40) return;

				//回复一个垃圾数据
				byte[] data = new byte[5];
				data[0] = 5; data[4] = 1 << 6;
				BinaryStream stmAck = new BinaryStream(data, data.Length);
				Globals.net.SendMessage(stmAck, sock);

				MsgPbFormStringHeader msg = new MsgPbFormStringHeader(sock.randomKey);
				msg.Read(stm);

				NetEventArgs evt = new NetEventArgs();
				evt.strCmd = msg.protoName;
				evt.payload = msg.content;
				DispatchNetMsgEvent_(sock, evt);
			}
			else {
				Debug.LogWarning("Unknown protocol parser.");
			}
		}
		BinaryStream sendStream_ = new BinaryStream(0xFFFF);
		FLLU3dSession session_;
		EnState state_ = EnState.Init;
	}
}
