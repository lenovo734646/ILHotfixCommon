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
	public class ViewPopup : ViewBase
	{
		static ViewPopup opening = null;
		public enum Flag
		{
			BTN_OK_CANCEL = 1,
			BTN_OK_ONLY = 2,
		}

		public static ViewPopup Create(	string content, int flag, Action okCallback,
										float autoCloseTime = 0.0f, Action cancelCallback = null, string title = "")
		{
			ViewPopup popup = new ViewPopup();
			popup.SetParams(content, flag, okCallback, cancelCallback);
			if (title != "") popup.SetTitle(title);
			popup.SetAutoClose(autoCloseTime);
			popup.Start();
			return popup;
		}

		public ViewPopup()
		{
			//不允许同时显示多
			if(opening != null) {
				opening.cancelCallback();
				opening.Close();
			}

			opening = this;
		}

		//Content, flag, okCallback, cancelCallback, title
		public void SetParams(string content, int flag, Action ok, Action cancel)
		{
			content_ = content;
			flag_ = (Flag)flag;
			okCallback = ok;
			cancelCallback = cancel;
		}

		public void SetTitle(string t)
		{
			title_ = t;
		}

		public void SetAutoClose(float tm)
		{
			autoCloseTime_ = tm;
		}

		protected override void SetLoader()
		{
			{
				LoadTask tsk = new LoadTask();
				tsk.assetPath = "Assets/AssetsFinal/Common/MessageBoxUI_CN.prefab";
				tsk.callback = AddToPopup;
				resNames_.Add(tsk);
			}
		}

		IEnumerator DoAutoClose()
		{
			var txt = canvas_.FindChildDeeply("txtCountdown").GetComponent<Text>();
			
			txt.text = string.Format(LangUITip.PopupAutoCloseInTime, autoCloseTime_);

			TimeCounter tc = new TimeCounter("");
			float dt = tc.Elapse() - autoCloseTime_;
			while (dt >  0.01f) {
				yield return new WaitForSeconds(1.0f);
				txt.text = string.Format(LangUITip.PopupAutoCloseInTime, (int)dt);
				dt = tc.Elapse() - autoCloseTime_;
			}

			//自动关闭的算取消.
			cancelCallback();
			Close();
		}

		protected override void OnResourceReady()
		{
			canvas_ = GameObject.Find("Canvas");
			var btnOK = canvas_.FindChildDeeply("btnOK");
			var btnOnlyOK = canvas_.FindChildDeeply("btnOnlyOK");
			var btnRelease = canvas_.FindChildDeeply("btnRelease");
			btnOK.SetActive(false);
			btnOnlyOK.SetActive(false);
			btnRelease.SetActive(false);
			if (flag_ == Flag.BTN_OK_CANCEL) {
				btnOK.SetActive(true);
				btnRelease.SetActive(true);
			}
			else {
				btnOnlyOK.SetActive(false);
			}

			if (okCallback == null) okCallback = Close;
			if (cancelCallback == null) cancelCallback = Close;

			btnOK.GetComponent<Button>().onClick.AddListener(() => {
				okCallback();
				Close();
			});

			btnOnlyOK.GetComponent<Button>().onClick.AddListener(() => {
				okCallback();
				Close();
			});

			btnRelease.GetComponent<Button>().onClick.AddListener(() => {
				cancelCallback();
				Close();
			});


			var txtTitle = canvas_.FindChildDeeply("txtTitle").GetComponent<Text>();
			txtTitle.text = title_;

			var txtContent = canvas_.FindChildDeeply("txtTitle").GetComponent<Text>();
			txtContent.text = content_;

			if (autoCloseTime_ > 0.01f) {
				this.StartCor(DoAutoClose(), false);
			}

			var animNode = canvas_.FindChildDeeply("MessageBoxUI_CN").FindChildDeeply("animNode");
			animNode.StartDoTweenAnim();
		}

		IEnumerator DoClose()
		{
			var animNode = canvas_.FindChildDeeply("MessageBoxUI_CN").FindChildDeeply("animNode");
			animNode.StartDoTweenAnim(true);
			yield return new WaitForSeconds(0.2f);

			base.Close();
		}

		public override void Close()
		{
			if(opening != null) {
				this.StartCor(DoClose(), true);
			}
			opening = null;
		}

		GameObject canvas_;
		Flag flag_ = Flag.BTN_OK_ONLY;
		Action okCallback, cancelCallback;
		string content_;
		string title_ = "提示";
		float autoCloseTime_ = 0.0f;
	}
}
