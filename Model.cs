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

		LevelSet = 3000,
		EXP = 3001,
		Level = 3002,
	}

	public abstract class PlayerBase : ControllerBase
	{
		public string nickName;
		public Dictionary<int, long> items = new Dictionary<int, long>();
		public int iid;
		public string uid;
		public int lv;
		public string headIco;
		public string headFrame;
		public bool isBot = false;
		public event System.EventHandler onDataChanged;
		public long Item(ITEMID id)
		{
			if (items.ContainsKey((int)id)) {
				return items[(int)id];
			}
			return 0;
		}

		public void DispatchDataChanged()
		{
			System.EventArgs evt = new System.EventArgs();
			onDataChanged?.Invoke(this, evt);
		}
		public static void SetHeadPic(string headIco, Image img)
		{
			if (headIco == null || headIco == "") headIco = "1";
			int ico = int.Parse(headIco);
			if (ico < 1) ico = 1;
			if (ico > 10) ico = 10;

			var texture = App.ins.headIcons[ico];
			img.ChangeSprite(texture);
		}

		//设置头像
		public void SetHeadPic(Image img)
		{ 
			SetHeadPic(headIco, img);
		}

		public void SetHeadFrame(Image img)
		{
			if (headFrame == null) headFrame = "1";
			int ico = int.Parse(headFrame);
			if (ico < 1) ico = 1;
			if (ico > 8) ico = 8;

			var texture = App.ins.headFrames[ico];
			img.ChangeSprite(texture);
		}
	}

	//玩家时数据
	public class GamePlayer : PlayerBase
	{
		public int serverPos;

		public override string GetDebugInfo()
		{
			return "GamePlayer";
		}
	}

	public class SelfPlayer : PlayerBase
	{
		public string bankPsw;
		public string phone;

		public override string GetDebugInfo()
		{
			return "SelfPlayer";
		}
	}
}
