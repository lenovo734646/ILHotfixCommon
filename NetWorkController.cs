﻿using AssemblyCommon;
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
		INTERNAL_MSGID_PB_STRINGHEADER = 0xDDDD,
		INTERNAL_MSGID_PING = 0xFFFF,
	}

	//网络事情消息
	public class NetEventArgs
	{
		public int cmd;
		public string strCmd;
		public string payload;
	}

	//Protobuffer消息包
	public class MsgPbForm
	{
		public short subCmd = 0;
		public string content;
		public void Read(BinaryStream stm)
		{
			//跳2个int,
			stm.SetCurentRead(8);
			subCmd = stm.ReadShort();
			content = stm.ReadString();
		}

		public void Write(BinaryStream stm)
		{
			stm.SetCurentWrite(8);
			stm.WriteShort(subCmd);
			stm.WriteString(content);
			int len = stm.DataLeft();
			stm.SetCurentWrite(0);
			stm.WriteInt(len);
			stm.WriteInt((int)INT_MSGID.INTERNAL_MSGID_PB);
			stm.SetCurentWrite(len);
		}
	}

	//Protobuffer消息包
	public class MsgPbFormStringHeaderReader
	{
		public string protoName_;
		public string content;
		public int encrypted;
		public int controlFlag;

		string randomKey_;
		public MsgPbFormStringHeaderReader(string key)
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
				rc4Algorithm(Encoding.UTF8.GetBytes(randomKey_), span);
			}
			protoName_ = stm.ReadString(false);
			content = stm.ReadString();
		}
	}

	//JSon消息包
	public class MsgJsonForm
	{
		public short subCmd = 0;
		public short isCompressed = 0;

		public string content;

		public void Read(BinaryStream stm)
		{
			//跳2个int,
			stm.SetCurentRead(8);
			subCmd = stm.ReadShort();
			isCompressed = stm.ReadShort();
			content = stm.ReadString();
		}

		public void Write(BinaryStream stm)
		{
			stm.SetCurentWrite(8);
			stm.WriteShort(subCmd);
			stm.WriteShort(0);
			stm.WriteString(content);
			int len = stm.DataLeft();
			stm.SetCurentWrite(0);
			stm.WriteInt(len);
			stm.WriteInt((int)INT_MSGID.INTERNAL_MSGID_JSONFORM);
			stm.SetCurentWrite(len);
		}
	}

	public class NetWorkController
	{
		public event EventHandler<NetEventArgs> msgHandler;
		public event EventHandler<int> socketEventHandler;

		string randomKey = "";
		BinaryStream sendStream_ = new BinaryStream(0xFFFF);
		//数据发送使用同步方式,省事,实际应用中没有发现问题
		public void SendJson(short subCmd, string json)
		{
			sendStream_.ClearUsedData();
			MsgJsonForm msg = new MsgJsonForm();
			msg.subCmd = subCmd;
			msg.content = json;
			msg.Write(sendStream_);
			Globals.net.SendMessage(sendStream_);
		}

		public void SendPing()
		{
			sendStream_.ClearUsedData();
			sendStream_.WriteInt(8);
			sendStream_.WriteInt((int)INT_MSGID.INTERNAL_MSGID_PING);
			Globals.net.SendMessage(sendStream_);
		}

		public void SendPb(short subCmd, byte[] data)
		{
			sendStream_.ClearUsedData();
			sendStream_.WriteShort(subCmd);
			sendStream_.WriteArray(data, 0, data.Length);
			Globals.net.SendMessage(sendStream_);
		}

		public void HandleRawData(object sender, BinaryStream evt)
		{
			MySocket sock = sender as MySocket;
			HandleDataFrame_(sock, evt);
		}

		public void HandleSockEvent(object sender, MySocket.SocketState st)
		{
			//连接成功,这个事件每个socket会调一次
			if(st == SocketState.WORKING) {
				MySocket sock = (MySocket)sender;

			}
			//网络连接完全失败,这个是所有连接都失败之后调用的
			else if(st == SocketState.FAILED) {

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

		public void RegisterMsgHandler(EventHandler<NetEventArgs> handler)
		{
			msgHandler += handler;
		}

		public void RemoveMsgHandler(EventHandler<NetEventArgs> handler)
		{
			msgHandler -= handler;
		}
		public void Update()
		{
			Globals.net.Update();
		}

		public void Stop()
		{
			Globals.net.RemoveRawDataHandler(HandleRawData);
		}
		void DispatchNetEvent_(NetEventArgs evt)
		{
			msgHandler.Invoke(this, evt);
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
						DispatchNetEvent_(evt);
					}
					break;
					//系统PING
					case (int)INT_MSGID.INTERNAL_MSGID_PING: {
						SendPing();
					}
					break;
					//Protobuffer消息
					case (int)INT_MSGID.INTERNAL_MSGID_PB: {
						MsgPbForm msg = new MsgPbForm();
						msg.Read(stm);

						NetEventArgs evt = new NetEventArgs();
						evt.cmd = msg.subCmd;
						evt.payload = msg.content;
						DispatchNetEvent_(evt);
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
				Globals.net.SendMessage(stmAck);

				MsgPbFormStringHeaderReader msg = new MsgPbFormStringHeaderReader(randomKey);
				msg.Read(stm);

				NetEventArgs evt = new NetEventArgs();
				evt.strCmd = msg.protoName_;
				evt.payload = msg.content;
				DispatchNetEvent_(evt);
			}
			else {
				Debug.LogWarning("Unknown protocol parser.");
			}
		}
	}
}