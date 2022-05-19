using AssemblyCommon;
using Hotfix.Common;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

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
		public string headFrame;
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

		//设置头像
		public void SetHeadPic(Image img)
		{
			if (headIco == null) return;
			int ico = int.Parse(headIco);
			if (ico < 1) ico = 1;
			if (ico > 10) ico = 10;
			img.ChangeSprite(AppController.ins.headIcons[ico]);
		}

		public void SetHeadFrame(Image img)
		{
			if (headFrame == null) return;
			int ico = int.Parse(headFrame);
			if (ico < 1) ico = 1;
			if (ico > 10) ico = 10;
			img.ChangeSprite(AppController.ins.headFrames[ico]);
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
