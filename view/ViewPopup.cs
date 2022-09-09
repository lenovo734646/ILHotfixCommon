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

		public static ViewPopup Create(	string content, Flag flag, Action okCallback,
										float autoCloseTime = 0.0f, Action cancelCallback = null, string title = "")
		{
			ViewPopup popup = new ViewPopup(null);
			popup.SetParams(content, flag, okCallback, cancelCallback);
			if (title != "") popup.SetTitle(title);
			popup.SetAutoClose(autoCloseTime);
			popup.Start();
			return popup;
		}
		public ViewPopup(IShowDownloadProgress ip) : base(ip)
		{
			//不允许同时显示多
			if (opening != null) {
				if(opening.cancelCallback_ != null) opening.cancelCallback_();
				opening?.Close();
			}

			opening = this;
		}

		//Content, flag, okCallback, cancelCallback, title
		public void SetParams(string content, Flag flag, Action ok, Action cancel)
		{
			content_ = content;
			flag_ = flag;
			okCallback_ = ok;
			cancelCallback_ = cancel;
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
			LoadPrefab("Assets/AssetsFinal/Common/MessageBoxUI_CN.prefab", AddToPopup);
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
			cancelCallback_();
			Close();
		}

		protected override IEnumerator OnResourceReady()
		{
			canvas_ = GameObject.Find("MessageBoxUI_CN");
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
				btnOnlyOK.SetActive(true);
			}

			if (okCallback_ == null) okCallback_ = Close;
			if (cancelCallback_ == null) cancelCallback_ = Close;

			btnOK.GetComponent<Button>().onClick.AddListener(() => {
				okCallback_();
				Close();
			});

			btnOnlyOK.GetComponent<Button>().onClick.AddListener(() => {
				okCallback_();
				Close();
			});

			btnRelease.GetComponent<Button>().onClick.AddListener(() => {
				cancelCallback_();
				Close();
			});


			var txtTitle = canvas_.FindChildDeeply("txtTitle").GetComponent<Text>();
			txtTitle.text = title_;

			var txtContent = canvas_.FindChildDeeply("txtContent").GetComponent<Text>();
			txtContent.text = content_;

			if (autoCloseTime_ > 0.01f) {
				this.StartCor(DoAutoClose(), false);
			}

			var animNode = canvas_.FindChildDeeply("animNode");
			animNode.StartDoTweenAnim();
			yield return 0;
		}

		IEnumerator DoClose()
		{
			if(canvas_ != null) {
				var animNode = canvas_.FindChildDeeply("animNode");
				animNode.StartDoTweenAnim(true);
				yield return new WaitForSeconds(0.2f);
			}
		
			base.Close();
		}

		protected override void OnStop()
		{
			if(opening != null) {
				this.StartCor(DoClose(), true);
			}
			opening = null;
		}

		GameObject canvas_;
		Flag flag_ = Flag.BTN_OK_ONLY;
		Action okCallback_, cancelCallback_;
		string content_;
		string title_ = "提示";
		float autoCloseTime_ = 0.0f;
	}
}
