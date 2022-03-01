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
		static Dictionary<string, ShowToast> Opening_ = new Dictionary<string, ShowToast>();
		GameObject obj;
		string text_;
		public ShowToast(string text, float dur = 2.0f, GameObject container = null)
		{
			if (container == null) {
				container = GameObject.Find("Canvas");
			}

			//如果已经存在相同的提示,就只是延长显示时间
			if (!Opening_.ContainsKey(text)) {

				Opening_.Add(text, this);
				text_ = text;
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
			else {
				this.StopCor(-1);
				//重新计时
				Globals.cor.RunAction(this, dur, () => {
					Close();
				});
			}
		}

		public void Close()
		{
			this.StopCor(-1);
			Opening_.Remove(text_);
			GameObject.Destroy(obj);
		}

		public static void CloseAll()
		{
			foreach(var it in Opening_) {
				it.Value.Close();
			}
			Opening_.Clear();
		}
	}
}
