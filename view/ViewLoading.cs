using AssemblyCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Hotfix.Common
{
	class AShower : IShowDownloadProgress
	{
		public WeakReference<ViewLoading> wview_;
		public override void OnDesc(string desc)
		{
// 			ViewLoading loading;
// 			if(wview_.TryGetTarget(out loading)) {
// 				loading.s
// 			}
		}

		public override void OnProgress(long downed, long totalLength)
		{
			
		}

		public override void OnSetState(DownloadState st)
		{
			
		}
	}

	public class ViewLoading : ViewBase
	{
		AShower shower = new AShower();

		public ViewLoading(IShowDownloadProgress ip) : base(ip)
		{
			shower = new AShower();
			shower.wview_.SetTarget(this);
		}

		protected override void SetLoader()
		{
			LoadPrefab("Assets/AssetsFinal/Common/SmartLoadingUI.prefab", null);
		}

		protected override IEnumerator OnResourceReady()
		{
			yield return base.OnResourceReady();

			var canvas = GameObject.Find("Canvas");
			var SmartLoadingUI = canvas.FindChildDeeply("SmartLoadingUI");
			var WaitResponseUI = SmartLoadingUI.FindChildDeeply("WaitResponseUI");
			shower.SetUIRoot(SmartLoadingUI);

			WaitResponseUI.StartDoTweenAnim();

			var PopupMask = SmartLoadingUI.FindChildDeeply("PopupMask");
			PopupMask.StartAnim();

			var LoadlingIcon = SmartLoadingUI.FindChildDeeply("LoadlingIcon");
			LoadlingIcon.StartAnim();
		}
	}
}
