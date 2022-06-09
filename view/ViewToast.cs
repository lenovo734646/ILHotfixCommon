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
			ViewToast toast = new ViewToast(null);
			toast.SetParams(content, autoCloseTime);
			toast.Start();
			return toast;
		}

		public ViewToast(IShowDownloadProgress ip) : base(ip)
		{
			if (opening != null) {
				opening.Close();
			}
			opening = this;
		}

		public override void Close()
		{
			base.Close();
			opening = null;
		}

		protected override void SetLoader()
		{
			LoadPrefab("Assets/Res/prefabs/common/FX_UI_DlgTips.prefab", AddToPopup);
		}

		public void SetParams(string text, float autoCloseTime)
		{
			text_ = text;
			autoCloseTime_ = autoCloseTime;
		}

		protected override IEnumerator OnResourceReady()
		{
			yield return base.OnResourceReady();

			var canv = GameObject.Find("Canvas");
			if(canv != null) {
				var txt = canv.FindChildDeeply("Text_tips").GetComponent<Text>();
				txt.text = text_;

				if (autoCloseTime_ > 0.01f) {
					Globals.cor.RunAction(this, autoCloseTime_, () => {
						Close();
					});
				}
			}
		}

		string text_;
		float autoCloseTime_ = 0.0f;
	}
}
