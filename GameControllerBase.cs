using AssemblyCommon;
using Hotfix.Common.MultiPlayer;
using Hotfix.Common.Slot;
using Hotfix.Model;
using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Hotfix.Common
{

	//每个小游戏的GameController基类
	//用来管理每个小游戏的逻辑,包括视图管理,游戏逻辑,网络消息处理,流程处理等等.
	//总之,和小游戏相关的东西,都在这里开始
	//
	public abstract class GameControllerBase : ControllerBase
	{
		public enum GameState
		{
			state_wait_start = 1,//等待开始
			state_do_random = 2,//发牌阶段
			state_rest_end = 3,//结算阶段
			state_vote_banker = 4,//选庄阶段
			state_gaming = 5,//打牌阶段
			state_wait_bet = 6,//等待下注阶段
			state_game_over = 7,//游戏结束阶段
			state_quadruple = 8, //超级加倍阶段
			state_force_vote = 9, //霸王叫阶段

			state_new_turn_begins = 100,//新一轮开始阶段
			state_delay_time = 987,
		}
		public ViewGameSceneBase mainView;
		public bool isEntering = true;
		//创建和管理View
		public void OpenView(ViewBase view)
		{
			views_.Add(view);
			view.Start();
		}

		public void OnViewClosed(ViewBase view)
		{
			views_.Remove(view);
		}

		public void CloseAllView()
		{
			List<ViewBase> cpl = new List<ViewBase>(views_);
			foreach (var view in cpl) {
				view.Close();
			}
			views_.Clear();
		}


		public virtual IEnumerator ShowLogin()
		{
			yield return 0;
		}

		protected virtual IEnumerator OnGameLoginSucc()
		{
			yield return 0;
		}

		public IEnumerator GameLoginSucc()
		{
			prepared_ = true;
			CloseAllView();
			yield return OnGameLoginSucc();
		}

		public void HandleNetMsg(object sender, NetEventArgs evt)
		{
			if (evt.payload == null && evt.msg == null && evt.msgProto == null) return;

			string json = "";
			if (evt.payload != null) json = Encoding.UTF8.GetString(evt.payload);
			if (mainView == null) return;

			switch (evt.cmd) {
				case (int)CommID.msg_common_reply: {
					msg_common_reply msg = (msg_common_reply)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_common_reply>(json);
					mainView.OnCommonReply(msg);
				}
				break;

				case (int)GameRspID.msg_player_seat: {
					msg_player_seat msg = (msg_player_seat)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_player_seat>(json);
					mainView.OnPlayerEnter(msg);
				}
				break;

				case (int)GameRspID.msg_player_leave: {
					msg_player_leave msg = (msg_player_leave)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_player_leave>(json);
					mainView.OnPlayerLeave(msg);
				}
				break;
				
				case (int)GameRspID.msg_deposit_change2: {
					msg_deposit_change2 msg = (msg_deposit_change2)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_deposit_change2>(json);
					mainView.OnGoldChange(msg);
				}
				break;
				case (int)GameRspID.msg_system_showdown: {
					msg_system_showdown msg = (msg_system_showdown)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_system_showdown>(json);
					mainView.OnServerShutdown(msg);
				}
				break;
				
				case (int)GameRspID.msg_currency_change: {
					msg_currency_change msg = (msg_currency_change)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_currency_change>(json);
					mainView.OnGoldChange(msg);
				}
				break;

				case (int)GameRspID.msg_server_parameter: {
					msg_server_parameter msg = (msg_server_parameter)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_server_parameter>(json);
					mainView.OnServerParameter(msg);
				}
				break;
				case (int)GameRspID.msg_get_public_data_ret: {
					msg_get_public_data_ret msg = (msg_get_public_data_ret)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_get_public_data_ret>(json);
					mainView.OnJackpotNumber(msg);
				}
				break;
				
				default:
				OnNetMsg(evt.msg, evt.cmd, json);
				break;
			}

		}
		protected abstract void OnNetMsg(msg_base evt, int cmd, string json);
		public IEnumerator PrepareGameRoom()
		{
			yield return OnPrepareGameRoom();
		}
		protected virtual IEnumerator OnPrepareGameRoom()
		{
			yield return 0;
		}

		public IEnumerator GameRoomEnterSucc()
		{
			isEntering = false;
			yield return OnGameRoomSucc();
		}

		protected virtual IEnumerator OnGameRoomSucc()
		{
			yield return 0;
		}

		public override void Start()
		{
			UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
			MyDebug.Log("GameControllerBase AddMsgHandler(HandleNetMsg)");
			AppController.ins.network.AddMsgHandler(HandleNetMsg);
			base.Start();
		}

		public override void Update()
		{
			if (prepared_) {
				foreach (var view in views_) {
					view.Update();
				}
			}
		}

		public override void Stop()
		{
			CloseAllView();
			MyDebug.Log("GameControllerBase RemoveMsgHandler(HandleNetMsg)");
			AppController.ins.network.RemoveMsgHandler(HandleNetMsg);
		}
		
		public virtual GamePlayer CreateGamePlayer()
		{
			return new GamePlayer();
		}

		public void AddPlayer(GamePlayer p)
		{
			if (players.ContainsKey(p.serverPos)) {
				players.Remove(p.serverPos);
			}
			players.Add(p.serverPos, p);
			p.DispatchDataChanged();
		}

		public GamePlayer GetPlayer(int serverPos)
		{
			foreach(var pp in players) {
				if(pp.Value.serverPos == serverPos) {
					return pp.Value;
				}
			}
			return null;
		}

		public GamePlayer GetPlayer(string uid)
		{
			foreach (var pp in players) {
				if (pp.Value.uid == uid) {
					return pp.Value;
				}
			}
			return null;
		}

		public void RemovePlayer(int serverPos)
		{
			players.Remove(serverPos);
		}

		List<ViewBase> views_ = new List<ViewBase>();
		Dictionary<int, GamePlayer> players = new Dictionary<int,GamePlayer>();
		bool prepared_ = true;
	}

	public abstract class GameControllerMultiplayer : GameControllerBase
	{

		public abstract msg_random_result_base CreateRandomResult(string json);
		public abstract msg_last_random_base CreateLastRandom(string json);
		protected override void OnNetMsg(msg_base evt, int cmd, string json)
		{

			var mainViewThis = (ViewMultiplayerScene)mainView;

			switch (cmd) {
				case (int)GameMultiRspID.msg_state_change: {
					msg_state_change msg = (msg_state_change)evt;
					if (msg == null) msg = JsonMapper.ToObject<msg_state_change>(json);
					mainViewThis.OnStateChange(msg);
				}
				break;

				case (int)GameMultiRspID.msg_rand_result:
				case (int)GameMultiRspID.msg_random_result_slwh:
				case (int)GameMultiRspID.msg_brnn_result:
				case (int)GameMultiRspID.msg_bjl_result: {
					msg_random_result_base msg = (msg_random_result_base)evt;
					if (msg == null) msg = CreateRandomResult(json);
					mainViewThis.OnRandomResult(msg);
				}
				break;
	
				case (int)GameMultiRspID.msg_last_random: {
					msg_last_random_base msg = (msg_last_random_base)evt;
					if (msg == null) msg = CreateLastRandom(json);
					mainViewThis.OnLastRandomResult(msg);
				}
				break;

				case (int)GameMultiRspID.msg_player_setbet: {
					msg_player_setbet msg = (msg_player_setbet)evt;
					if (msg == null) msg = JsonMapper.ToObject<msg_player_setbet>(json);
					mainViewThis.OnPlayerSetBet(msg);
				}
				break;

				case (int)GameMultiRspID.msg_my_setbet: {
					msg_my_setbet msg = (msg_my_setbet)evt;
					if (msg == null) msg = JsonMapper.ToObject<msg_my_setbet>(json);
					mainViewThis.OnMyBet(msg);
				}
				break;

				case (int)GameMultiRspID.msg_banker_deposit_change: {
					msg_banker_deposit_change msg = (msg_banker_deposit_change)evt;
					if (msg == null) msg = JsonMapper.ToObject<msg_banker_deposit_change>(json);
					mainViewThis.OnBankDepositChanged(msg);
				}
				break;

				case (int)GameMultiRspID.msg_banker_promote: {
					msg_banker_promote msg = (msg_banker_promote)evt;
					if (msg == null) msg = JsonMapper.ToObject<msg_banker_promote>(json);
					mainViewThis.OnBankPromote(msg);
				}
				break;

				case (int)GameMultiRspID.msg_game_report: {
					msg_game_report msg = (msg_game_report)evt;
					if (msg == null) msg = JsonMapper.ToObject<msg_game_report>(json);
					mainViewThis.OnGameReport(msg);
				}
				break;
				case (int)GameMultiRspID.msg_game_info: {
					msg_game_info msg = (msg_game_info)evt;
					if (msg == null) msg = JsonMapper.ToObject<msg_game_info>(json);
					mainViewThis.OnGameInfo(msg);
				}
				break;
				case (int)GameMultiRspID.msg_new_banker_applyed: {
					msg_new_banker_applyed msg = (msg_new_banker_applyed)evt;
					if (msg == null) msg = JsonMapper.ToObject<msg_new_banker_applyed>(json);
					mainViewThis.OnApplyBanker(msg);
				}
				break;
				case (int)GameMultiRspID.msg_apply_banker_canceled: {
					msg_apply_banker_canceled msg = (msg_apply_banker_canceled)evt;
					if (msg == null) msg = JsonMapper.ToObject<msg_apply_banker_canceled>(json);
					mainViewThis.OnCancelBanker(msg);
				}
				break;
				default: {
					if(mainView == null) {
						MyDebug.LogFormat("msg is ignored:{0},{1}", evt, json);
					}
					else {
						mainViewThis.OnNetMsg(cmd, json);
					}
				}
				
				break;
			}
		}
	}

	public abstract class GameControllerSlot : GameControllerBase
	{
		protected override void OnNetMsg(msg_base evt, int cmd, string json)
		{
			var mainViewThis = (ViewSlotScene)mainView;
			switch (cmd) {
				case (int)GameSlotRspID.msg_random_present_ret: {
					msg_random_present_ret msg = (msg_random_present_ret)evt;
					if (msg == null) msg = JsonMapper.ToObject<msg_random_present_ret>(json);
					mainViewThis.OnRandomResult(msg);
				}
				break;

				case (int)GameSlotRspID.msg_get_luck_player_ret: {
					msg_get_luck_player_ret msg = (msg_get_luck_player_ret)evt;
					if (msg == null) msg = JsonMapper.ToObject<msg_get_luck_player_ret>(json);
					mainViewThis.OnLuckResult(msg);
				}
				break;
				
				case (int)GameSlotRspID.msg_luck_player: {
					msg_luck_player msg = (msg_luck_player)evt;
					if (msg == null) msg = JsonMapper.ToObject<msg_luck_player>(json);
					mainViewThis.OnLuckPlayer(msg);
				}
				break;

				case (int)GameSlotRspID.msg_player_setbet_slot: {
					msg_player_setbet_slot msg = (msg_player_setbet_slot)evt;
					if (msg == null) msg = JsonMapper.ToObject<Slot.msg_player_setbet_slot>(json);
					mainViewThis.OnPlayerSetBet(msg);
				}
				break;

				case (int)GameSlotRspID.msg_random_present_ret_record: {
					msg_random_present_ret_record msg = (msg_random_present_ret_record)evt;
					if (msg == null) msg = JsonMapper.ToObject<msg_random_present_ret_record>(json);
					mainViewThis.OnLuckPlayerPlayData(msg);
				}
				break;

				default: {
					if (mainView == null) {
						MyDebug.LogFormat("msg is ignored:{0},{1}", cmd, json);
					}
					else {
						mainView.OnNetMsg(cmd, json);
					}
				}
				break;
			}
		}
	}
}
