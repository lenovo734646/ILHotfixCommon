using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Hotfix.Model
{
	public enum ITEMID
	{
		GOLD = 1,
		BANK_GOLD = 5,
	}

	public class PlayerBase
	{
		public string nickName;
		public Dictionary<int, long> items = new Dictionary<int, long>();
		public int iid;
		public string uid;
		public int lv;
		public string headIco;
		public event System.EventHandler onDataChanged;
		public long Item(int id)
		{
			if (items.ContainsKey(id)) {
				return items[id];
			}
			return 0;
		}

		public void DispatchDataChanged()
		{
			System.EventArgs evt = new System.EventArgs();
			onDataChanged?.Invoke(this, evt);
		}
	}

	//玩家时数据
	public partial class GamePlayer : PlayerBase
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
		GamePlayer pp_ = new GamePlayer();
		public string bankPsw;
		public string phone;
	}
}
