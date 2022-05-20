using AssemblyCommon;
using Hotfix.Model;
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
	public class ViewBankMain : ViewBase
	{
		GameObject ToggleGroup, GetView, PutView, TransferView, GoldCommonView, DetailsView, ChangePasswordView, TransferRecordView;
		Text goldText, bankGoldText;
		protected override void SetLoader()
		{
			LoadPrefab("Assets/AssetsFinal/hall/Popup_BankMainPanel.prefab", AddToPopup);
		}

		public ViewBankMain(IShowDownloadProgress ip) : base(ip)
		{

		}

		protected override IEnumerator OnResourceReady()
		{
			yield return base.OnResourceReady();
			var layer = GetPopupLayer();
			var animNode = layer.FindChildDeeply("animNode");
			animNode.StartDoTweenAnim();

			var btn_Close = layer.FindChildDeeply("btn_Close").GetComponent<Button>();
			btn_Close.onClick.AddListener(() => {
				Close();
			});

			ToggleGroup = layer.FindChildDeeply("ToggleGroup");
			GetView = layer.FindChildDeeply("GetView");
			PutView = layer.FindChildDeeply("PutView");
			TransferView = layer.FindChildDeeply("TransferView");
			GoldCommonView = layer.FindChildDeeply("GoldCommonView");
			DetailsView = layer.FindChildDeeply("DetailsView");
			ChangePasswordView = layer.FindChildDeeply("ChangePasswordView");
			TransferRecordView = layer.FindChildDeeply("TransferRecordView");

			goldText = GoldCommonView.FindChildDeeply("goldText").GetComponent<Text>();
			bankGoldText = GoldCommonView.FindChildDeeply("bankGoldText").GetComponent<Text>();


			HideAll();
			var obj = ToggleGroup.FindChildDeeply("tog_Get");
			var tog_Get = obj.GetComponent<Toggle>();
			tog_Get.onValueChanged.AddListener((enable) => {
				if (!enable) return;
				ShowFirstTab_();
			});

			var tog_Put = ToggleGroup.FindChildDeeply("tog_Put").GetComponent<Toggle>();
			tog_Put.onValueChanged.AddListener((enable) => {
				if (!enable) return;
				HideAll();
				ToggleGroup.SetActive(true);
				PutView.SetActive(true);
				GoldCommonView.SetActive(true);
				var img = ToggleGroup.FindChildDeeply("tog_Put").FindChildDeeply("Image");
				img.SetActive(true);
			});

			var tog_Transfer = ToggleGroup.FindChildDeeply("tog_Transfer").GetComponent<Toggle>();
			tog_Transfer.onValueChanged.AddListener((enable) => {
				if (!enable) return;
				HideAll();
				ToggleGroup.SetActive(true);
				TransferView.SetActive(true);
				GoldCommonView.SetActive(true);
				var img = ToggleGroup.FindChildDeeply("tog_Transfer").FindChildDeeply("Image");
				img.SetActive(true);
			});

			var tog_Deatils = ToggleGroup.FindChildDeeply("tog_Deatils").GetComponent<Toggle>();
			tog_Deatils.onValueChanged.AddListener((enable) => {
				if (!enable) return;
				HideAll();
				ToggleGroup.SetActive(true);
				DetailsView.SetActive(true);
				var img = ToggleGroup.FindChildDeeply("tog_Deatils").FindChildDeeply("Image");
				img.SetActive(true);
			});

			var tog_ChangePassword = ToggleGroup.FindChildDeeply("tog_ChangePassword").GetComponent<Toggle>();
			tog_ChangePassword.onValueChanged.AddListener((enable) => {
				if (!enable) return;
				HideAll();
				ToggleGroup.SetActive(true);
				ChangePasswordView.SetActive(true);

				var img = ToggleGroup.FindChildDeeply("tog_ChangePassword").FindChildDeeply("Image");
				img.SetActive(true);
			});

			{
				var btnOk = GetView.FindChildDeeply("btn_OK").GetComponent<Button>();
				var txt = GetView.FindChildDeeply("getTag").GetComponentInChildren<InputField>();
				btnOk.onClick.AddListener(() => {
					MyDebug.LogFormat("GetView onOK");
					if (txt.text == "") {
						ViewToast.Create(LangUITip.PleaseEnterValue);
					}
					else {

						msg_bank_op msg = new msg_bank_op();
						msg.op_ = 0;
						msg.psw_ = AppController.ins.self.bankPsw;
						msg.type_ = 1;
						msg.count_ = long.Parse(txt.text);

						bool succ = AppController.ins.network.Rpc((short)CorReqID.msg_bank_op, msg, (short)CommID.msg_common_reply, (rpl) => {
							if (rpl == null) return;
							if (rpl.err_ == 1) {
								ViewToast.Create(LangUITip.OperationSucc);
								GetBankInfo();
							}
							else if (rpl.err_ == -999) {
								ViewToast.Create(LangUITip.PasswordIncorrect);
							}
							else if (rpl.err_ == 3) {
								ViewToast.Create(LangUITip.NotEnough);
							}
						}, 3.0f);

						if (!succ) {
							ViewToast.Create(LangUITip.PleaseWait);
						}
					}
				});

				var slider = GetView.FindChildDeeply("Slider").GetComponent<Slider>();
				slider.onValueChanged.AddListener((val) => {
					txt.text = Utils.FormatGoldShow((long)(AppController.ins.self.gamePlayer.Item((int)ITEMID.BANK_GOLD) * val / 100.0f));
				});

				var btn_Reset = GetView.FindChildDeeply("btn_Reset").GetComponent<Button>();
				btn_Reset.onClick.AddListener(() => {
					txt.text = "";
					slider.value = 0;
				});

				var btn_Max = GetView.FindChildDeeply("btn_Max").GetComponent<Button>();
				btn_Max.onClick.AddListener(() => {
					slider.value = 100.0f;
				});
			}

			{
				var btnOk = PutView.FindChildDeeply("btn_OK").GetComponent<Button>();
				var txt = PutView.FindChildDeeply("putTag").GetComponentInChildren<InputField>();

				btnOk.onClick.AddListener(() => {
					MyDebug.LogFormat("PutView onOK");
					if (txt.text == "") {
						ViewToast.Create(LangUITip.PleaseEnterValue);
					}
					else {
						msg_bank_op msg = new msg_bank_op();
						msg.op_ = 1;
						msg.psw_ = AppController.ins.self.bankPsw;
						msg.type_ = 1;
						msg.count_ = long.Parse(txt.text);

						bool succ = AppController.ins.network.Rpc((short)CorReqID.msg_bank_op, msg, (short)CommID.msg_common_reply, (rpl) => {
							if (rpl == null) return;
							if (rpl.err_ == 1) {
								ViewToast.Create(LangUITip.OperationSucc);
								GetBankInfo();
							}
							else if (rpl.err_ == -999) {
								ViewToast.Create(LangUITip.PasswordIncorrect);
							}
							else if (rpl.err_ == 3) {
								ViewToast.Create(LangUITip.NotEnough);
							}
						}, 3.0f);

						if (!succ) {
							ViewToast.Create(LangUITip.PleaseWait);
						}
					}
				});

				var slider = PutView.FindChildDeeply("Slider").GetComponent<Slider>();
				slider.onValueChanged.AddListener((val) => {
					txt.text = Utils.FormatGoldShow((long)(AppController.ins.self.gamePlayer.Item((int)ITEMID.GOLD) * val / 100.0f));
				});

				var btn_Reset = PutView.FindChildDeeply("btn_Reset").GetComponent<Button>();
				btn_Reset.onClick.AddListener(() => {
					txt.text = "";
				});

				var btn_Max = PutView.FindChildDeeply("btn_Max").GetComponent<Button>();
				btn_Max.onClick.AddListener(() => {
					slider.value = 100.0f;
				});
			}

			{
				var btnOk = TransferView.FindChildDeeply("btn_OK").GetComponent<Button>();
				var txt = TransferView.FindChildDeeply("amountTag").GetComponentInChildren<InputField>();
				btnOk.onClick.AddListener(() => {
					MyDebug.LogFormat("TransferView onOK");
					if (txt.text == "") {
						ViewToast.Create(LangUITip.PleaseEnterValue);
						return;
					}

					var recverTag = TransferView.FindChildDeeply("recverTag").GetComponentInChildren<InputField>();
					if (recverTag.text == "") {
						ViewToast.Create(LangUITip.PleaseEnterValue);
						return;
					}
					else {
						msg_send_present msg = new msg_send_present();
						msg.present_id_ = (int)ITEMID.BANK_GOLD;
						msg.count_ = long.Parse(txt.text);
						msg.to_ = recverTag.text;
						bool succ = AppController.ins.network.Rpc((short)CorReqID.msg_send_present, msg, (short)CommID.msg_common_reply, (rpl) => {
							if (rpl == null) return;
							if (rpl.err_ == 1) {
								ViewToast.Create(LangUITip.OperationSucc);
								GetBankInfo();
							}
							else if(rpl.err_ == -995) {
								ViewToast.Create(LangUITip.CantFindPlayer);
							}
							else if(rpl.err_ == 3) {
								ViewToast.Create(LangUITip.NotEnough);
							}
						}, 3.0f);

						if (!succ) {
							ViewToast.Create(LangUITip.PleaseWait);
						}
					}
				});

				var btn_Reset = TransferView.FindChildDeeply("btn_Reset").GetComponent<Button>();
				btn_Reset.onClick.AddListener(() => {
					txt.text = "";
				});

				var btn_10W = TransferView.FindChildDeeply("btn_10W").GetComponent<Button>();
				btn_10W.onClick.AddListener(() => {
					txt.text = (long.Parse(txt.text) + 100000).ToString();
				});

				var btn_100W = TransferView.FindChildDeeply("btn_100W").GetComponent<Button>();
				btn_100W.onClick.AddListener(() => {
					txt.text = (long.Parse(txt.text) + 1000000).ToString(); ;
				});

				var btn_1000W = TransferView.FindChildDeeply("btn_1000W").GetComponent<Button>();
				btn_1000W.onClick.AddListener(() => {
					txt.text = (long.Parse(txt.text) + 10000000).ToString(); ;
				});

				var btn_1E = TransferView.FindChildDeeply("btn_1E").GetComponent<Button>();
				btn_1E.onClick.AddListener(() => {
					txt.text = (long.Parse(txt.text) + 100000000).ToString(); ;
				});
			}

			{
				var btnOk = ChangePasswordView.FindChildDeeply("btn_OK").GetComponent<Button>();
				btnOk.onClick.AddListener(() => {

					var oldTag = ChangePasswordView.FindChildDeeply("oldTag").GetComponentInChildren<InputField>();
					var newTag = ChangePasswordView.FindChildDeeply("newTag").GetComponentInChildren<InputField>();
					var confirmTag = ChangePasswordView.FindChildDeeply("confirmTag").GetComponentInChildren<InputField>();

					if (oldTag.text == "" || newTag.text == "" || confirmTag.text == "") {
						ViewToast.Create(LangUITip.PleaseEnterValue);
						return;
					}

					if (newTag.text != confirmTag.text) {
						ViewToast.Create(LangUITip.ConfirmFailed);
						return;
					}

					msg_set_bank_psw msg = new msg_set_bank_psw();
					msg.old_psw_ = oldTag.text;
					msg.psw_ = newTag.text;
					msg.func_ = 1;
					AppController.ins.network.Rpc((short)CorReqID.msg_set_bank_psw, msg, (short)CommID.msg_common_reply, (rpl) => {
						if (rpl == null) return;
						if (rpl.err_ == 1) {
							ViewToast.Create(LangUITip.OperationSucc);
						}
						else if (rpl.err_ == -999) {
							ViewToast.Create(LangUITip.PasswordIncorrect);
						}
						else {
							ViewToast.Create(LangUITip.OperationFailed);
						}
					}, 3.0f);

				});
			}

			GetBankInfo();

			ShowFirstTab_();
			AppController.ins.self.gamePlayer.onDataChanged += OnUserDataChanged;
		}

		public override void Close()
		{
			base.Close();
			AppController.ins.self.gamePlayer.onDataChanged -= OnUserDataChanged;
		}

		void OnUserDataChanged(object sender, System.EventArgs evt)
		{
			goldText.text = Utils.FormatGoldShow(AppController.ins.self.gamePlayer.items.GetVal((int)ITEMID.GOLD));
			bankGoldText.text = Utils.FormatGoldShow(AppController.ins.self.gamePlayer.items.GetVal((int)ITEMID.BANK_GOLD));
		}

		void ShowFirstTab_()
		{
			HideAll();
			ToggleGroup.SetActive(true);
			GetView.SetActive(true);
			GoldCommonView.SetActive(true);
			var img = ToggleGroup.FindChildDeeply("tog_Get").FindChildDeeply("Image");
			img.SetActive(true);
		}

		void GetBankInfo()
		{
			msg_get_bank_info msg = new msg_get_bank_info();
			AppController.ins.network.Rpc((short)CorReqID.msg_get_bank_info, msg, (short)CorRspID.msg_get_bank_info_ret, (rpl)=> {
				if(rpl != null && rpl.err_ == 0) {
					var info = (msg_get_bank_info_ret)rpl.msg_;
					AppController.ins.self.gamePlayer.items.SetKeyVal((int)ITEMID.BANK_GOLD,long.Parse(info.bank_gold_game_));
					AppController.ins.self.gamePlayer.DispatchDataChanged();
				}
			}, 3.0f);
		}

		void HideAll()
		{
			ToggleGroup.SetActive(false);
			GetView.SetActive(false);
			PutView.SetActive(false);
			TransferView.SetActive(false);
			GoldCommonView.SetActive(false);
			DetailsView.SetActive(false);
			ChangePasswordView.SetActive(false);
			TransferRecordView.SetActive(false);

			var img = ToggleGroup.FindChildDeeply("tog_Get").FindChildDeeply("Image");
			img.SetActive(false);

			img = ToggleGroup.FindChildDeeply("tog_Put").FindChildDeeply("Image");
			img.SetActive(false);

			img = ToggleGroup.FindChildDeeply("tog_Transfer").FindChildDeeply("Image");
			img.SetActive(false);

			img = ToggleGroup.FindChildDeeply("tog_Deatils").FindChildDeeply("Image");
			img.SetActive(false);

			img = ToggleGroup.FindChildDeeply("tog_ChangePassword").FindChildDeeply("Image");
			img.SetActive(false);
		}
	}
}
