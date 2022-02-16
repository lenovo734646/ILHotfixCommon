using AssemblyCommon;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

namespace Hotfix.Common
{
	public class ShowToast
	{
		static List<ShowToast> Opening_ = new List<ShowToast>();
		GameObject obj;
		public ShowToast(string text, float dur = 2.0f, GameObject container = null)
		{
			if (container == null) {
				container = GameObject.Find("Canvas");
			}
			Opening_.Add(this);
			Globals.resLoader.LoadAsync<GameObject>("Assets/Res/prefabs/common/FX_UI_DlgTips.prefab", (Result) => {
				obj = GameObject.Instantiate(Result);
				var txt = obj.FindChildDeeply("Text_tips").GetComponent<Text>();
				txt.text = text;
				obj.transform.SetParent(container.transform, false);
				obj.transform.position = obj.transform.position + new Vector3(0, (Opening_.Count - 1) * 30, 0);
			}, null);

			Globals.cor.RunAction(this, dur, () => {
				Close();
			});
		}

		public void Close()
		{
			this.StopCor(-1);
			Opening_.Remove(this);
			GameObject.Destroy(obj);
		}

		public static void CloseAll()
		{
			for (int i = 0; i < Opening_.Count; i++) {
				Opening_[i].Close();
			}
		}
	}
}
