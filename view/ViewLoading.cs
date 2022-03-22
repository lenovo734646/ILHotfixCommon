using AssemblyCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Hotfix.Common
{
	class AShow : IShowDownloadProgress
	{
		public WeakReference<ViewLoading> wview_;
		public void Desc(string desc)
		{
// 			ViewLoading loading;
// 			if(wview_.TryGetTarget(out loading)) {
// 				loading.s
// 			}
		}

		public void Progress(long downed, long totalLength)
		{
			
		}

		public void SetState(DownloadState st)
		{
			
		}
	}

	public class ViewLoading : ViewBase
	{
		AShow shower = new AShow();
		public ViewLoading()
		{
			progress = new AShow();
			((AShow)progress).wview_.SetTarget(this);
		}

		protected override void SetLoader()
		{

			{
				LoadTask tsk = new LoadTask();
				tsk.assetPath = "Assets/AssetsFinal/Common/SmartLoadingUI.prefab";
				tsk.callback = AddToPopup;
				resNames_.Add(tsk);
			}
			
		}

		protected override void OnResourceReady()
		{
			var canvas = GameObject.Find("Canvas");
			var SmartLoadingUI = canvas.FindChildDeeply("SmartLoadingUI");
			var WaitResponseUI = SmartLoadingUI.FindChildDeeply("WaitResponseUI");
			WaitResponseUI.StartDoTweenAnim();

			var PopupMask = SmartLoadingUI.FindChildDeeply("PopupMask");
			PopupMask.StartAnim();

			var LoadlingIcon = SmartLoadingUI.FindChildDeeply("LoadlingIcon");
			LoadlingIcon.StartAnim();
		}
		
	}
}
