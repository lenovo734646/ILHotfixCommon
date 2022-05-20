using AssemblyCommon;
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

		public virtual IEnumerator ShowLogin()
		{
			yield return 0;
		}

		public virtual IEnumerator OnGameLoginSucc()
		{
			prepared_ = true;
			yield return 0;
		}
		protected abstract void OnNetMsg(object sender, NetEventArgs evt);
		public virtual IEnumerator OnPrepareGameRoom()
		{
			yield return 0;
		}
		public virtual IEnumerator OnGameRoomSucc()
		{
			isEntering = false;
			yield return 0;
		}

		public override void Start()
		{
			UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
			AppController.ins.network.AddMsgHandler(OnNetMsg);
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
			List<ViewBase> cpl = new List<ViewBase>(views_);
			foreach (var view in cpl) {
				view.Close();
			}

			AppController.ins.network.RemoveMsgHandler(OnNetMsg);
		}
		
		public virtual GamePlayer CreateGamePlayer()
		{
			return new GamePlayer();
		}

		List<ViewBase> views_ = new List<ViewBase>();
		bool prepared_ = true;
	}

	public abstract class GameControllerMultiplayer : GameControllerBase
	{
		public ViewMultiplayerScene mainView;
		public abstract msg_random_result_base CreateRandomResult(string json);
		public abstract msg_last_random_base CreateLastRandom(string json);
		protected override void OnNetMsg(object sender, NetEventArgs evt)
		{
			if (evt.payload == null && evt.msg == null && evt.msgProto == null) return;

			string json = "";
			if (evt.payload != null) json = Encoding.UTF8.GetString(evt.payload);
			if (mainView == null) return;

			switch (evt.cmd) {
				case (int)GameRspID.msg_player_seat: {
					msg_player_seat msg = (msg_player_seat)evt.msg;
					if(msg == null) msg = JsonMapper.ToObject<msg_player_seat>(json);
					mainView.OnPlayerEnter(msg);
				}
				break;

				case (int)GameRspID.msg_player_leave: {
					msg_player_leave msg = (msg_player_leave)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_player_leave>(json);
					mainView.OnPlayerLeave(msg);
				}
				break;

				case (int)GameMultiRspID.msg_state_change: {
					msg_state_change msg = (msg_state_change)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_state_change>(json);
					mainView.OnStateChange(msg);
				}
				break;

				case (int)GameMultiRspID.msg_rand_result:
				case (int)GameMultiRspID.msg_random_result_slwh:
				case (int)GameMultiRspID.msg_brnn_result:
				case (int)GameMultiRspID.msg_bjl_result: {
					msg_random_result_base msg = (msg_random_result_base)evt.msg;
					if (msg == null) msg = CreateRandomResult(json);
					mainView.OnRandomResult(msg);
				}
				break;
	
				case (int)GameMultiRspID.msg_last_random: {
					msg_last_random_base msg = (msg_last_random_base)evt.msg;
					if (msg == null) msg = CreateLastRandom(json);
					mainView.OnLastRandomResult(msg);
				}
				break;

				case (int)GameMultiRspID.msg_player_setbet: {
					msg_player_setbet msg = (msg_player_setbet)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_player_setbet>(json);
					mainView.OnPlayerSetBet(msg);
				}
				break;

				case (int)GameMultiRspID.msg_my_setbet: {
					msg_my_setbet msg = (msg_my_setbet)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_my_setbet>(json);
					mainView.OnMyBet(msg);
				}
				break;

				case (int)GameMultiRspID.msg_banker_deposit_change: {
					msg_banker_deposit_change msg = (msg_banker_deposit_change)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_banker_deposit_change>(json);
					mainView.OnBankDepositChanged(msg);
				}
				break;

				case (int)GameMultiRspID.msg_banker_promote: {
					msg_banker_promote msg = (msg_banker_promote)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_banker_promote>(json);
					mainView.OnBankPromote(msg);
				}
				break;

				case (int)GameMultiRspID.msg_game_report: {
					msg_game_report msg = (msg_game_report)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_game_report>(json);
					mainView.OnGameReport(msg);
				}
				break;
				case (int)GameMultiRspID.msg_game_info: {
					msg_game_info msg = (msg_game_info)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_game_info>(json);
					mainView.OnGameInfo(msg);
				}
				break;
				case (int)GameRspID.msg_deposit_change2: {
					msg_deposit_change2 msg = (msg_deposit_change2)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_deposit_change2>(json);
					mainView.OnGoldChange(msg);
				}
				break;
				case (int)GameRspID.msg_currency_change: {
					msg_currency_change msg = (msg_currency_change)evt.msg;
					if (msg == null) msg = JsonMapper.ToObject<msg_currency_change>(json);
					mainView.OnGoldChange(msg);
				}
				break;
				default: {
					if(mainView == null) {
						MyDebug.LogFormat("msg is ignored:{0},{1}", evt.cmd, json);
					}
					else {
						mainView.OnNetMsg(evt.cmd, json);
					}
				}
				
				break;
			}
		}
	}

}
