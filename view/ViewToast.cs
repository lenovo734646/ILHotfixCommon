using AssemblyCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Hotfix.Common
{
	public class ViewToast : ViewBase
	{
		static ViewToast opening = null;
		public static void Clear()
		{
			if (opening != null) {
				opening.Close();
			}
		}

		public static ViewToast Create(string content, float autoCloseTime = 3.0f)
		{
			if (opening != null) {
				opening.Close();
			}
			MyDebug.LogFormat("ViewToastCreate:{0}", content);
			ViewToast toast = new ViewToast(null);
			toast.SetParams(content, autoCloseTime);
			App.ins.currentApp?.game?.OpenView(toast);
			return toast;
		}

		public ViewToast(IShowDownloadProgress ip) : base(ip)
		{
			
		}

		protected override void OnStop()
		{
			GameObject.Destroy(mainObject_);
			opening = null;
		}

		protected override void SetLoader()
		{
			LoadPrefab("Assets/Res/prefabs/common/FX_UI_DlgTips.prefab", AddToPopup, true);
		}

		public void SetParams(string text, float autoCloseTime)
		{
			text_ = text;
			autoCloseTime_ = autoCloseTime;
		}

		protected override IEnumerator OnResourceReady()
		{

			var txt = mainObject_.FindChildDeeply("Text_tips").GetComponent<Text>();
			txt.text = text_;

			if (autoCloseTime_ > 0.01f) {
				App.ins.RunAction(autoCloseTime_, () => {
					Close();
				});
			}

			mainObject_?.DoPopup();
			opening = this;
			yield return 0;
		}

		string text_;
		float autoCloseTime_ = 0.0f;
	}
}
