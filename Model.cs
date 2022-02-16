using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Hotfix.Model
{
	public enum ITEMID
	{
		GOLD,
		BANK_GOLD,
	}

	public class PlayerBase
	{
		public string nickName;
		public Dictionary<int, int> items = new Dictionary<int, int>();
		public int iid;
		public string uid;
		public int lv;
	}

	//玩家时数据
	public class GamePlayer : PlayerBase
	{
		public int serverPos;
	}

	public class SelfPlayer
	{
		public GamePlayer gamePlayer{
			set{
				pp_ = value;
			}
			get {
				return pp_;
			}
		} 
		GamePlayer pp_ = null;
	}
}
