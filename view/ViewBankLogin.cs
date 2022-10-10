using AssemblyCommon;
using LitJson;
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
	public class ViewBankLogin : ViewBase
	{
		public ViewBankLogin(IShowDownloadProgress ip) : base(ip)
		{

		}
		protected override void SetLoader()
		{
			LoadPrefab("Assets/AssetsFinal/hall/Popup_BankLoginPanel.prefab", AddToPopup);
		}

		protected override IEnumerator OnResourceReady()
		{
			var layer = GameObject.Find("Popup_BankLoginPanel");
			var animNode = layer.FindChildDeeply("animNode");
			animNode.StartDoTweenAnim();

			var btn_OK = layer.FindChildDeeply("btn_OK").GetComponent<Button>();
			btn_OK.onClick.AddListener(() => {
				OnBtnOK();
			});

			var btn_Close = layer.FindChildDeeply("btn_Close").GetComponent<Button>();
			btn_Close.onClick.AddListener(() => {
				Close();
			});

			var btn_Forget = layer.FindChildDeeply("btn_Forget").GetComponent<Button>();
			btn_Forget.onClick.AddListener(() => {
				OnBtnForget();
			});

			//适配
			var canvas = ViewBase.GetCanvas();
			var scaler = canvas.GetComponent<CanvasScaler>();
			var rect = layer.GetComponent<RectTransform>();
			float ratio = scaler.referenceResolution.y / 1080;
			var scale = rect.localScale;
			scale *= ratio;
			rect.localScale = scale;
			yield return 0;
		}
		protected IEnumerator DoOnBtnOK()
		{
			var layer = GameObject.Find("Popup_BankLoginPanel");
			var psw = layer.FindChildDeeply("InputField").GetComponent<InputField>();
			if (psw.text == "") {
				ViewToast.Create(LangUITip.PleaseEnterPassword);
			}
			else {
				msg_set_bank_psw msg = new msg_set_bank_psw();
				msg.func_ = 2;
				msg.psw_ = psw.text;
				msg.old_psw_ = psw.text;
				var waitor = App.ins.network.BuildResponseWaitor((ushort)CommID.msg_common_reply, (ushort)CorReqID.msg_set_bank_psw, msg);
				yield return waitor.WaitResult();
				if (waitor.resultSetted) {
					var rpl = JsonMapper.ToObject<msg_common_reply>(waitor.result.json);
					if (rpl.err_ == "1") {
						Close();
						var view = new ViewBankMain(null);
						App.ins.currentApp.game.OpenView(view);
						App.ins.self.bankPsw = msg.psw_;
					}
					else {
						if (rpl.err_ == "-999") {
							ViewToast.Create(LangUITip.PasswordIncorrect);
						}
					}
				}
			}
			yield return 0;
		}
		protected void OnBtnOK()
		{
			this.StartCor(DoOnBtnOK(), false);
		}

		protected void OnBtnForget()
		{

		}
	}
}