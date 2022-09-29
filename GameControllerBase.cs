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

	public abstract class ViewLoadingBase : ViewBase
	{
		public IShowDownloadProgress loading;
		public ViewLoadingBase(IShowDownloadProgress loadingProgress) : base(loadingProgress)
		{
		}
		public void CreateProgressShower()
		{
			loading = OnCreateProgressShower();
		}
		protected abstract IShowDownloadProgress OnCreateProgressShower();
	}

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

		ViewGameSceneBase mainView_;
		protected ViewLoadingBase loading_;
		void OnMainViewClose(object sender, EventArgs e)
		{
			mainView_ = null;
		}
		
		public override string GetDebugInfo()
		{
			return "GameController";
		}

		protected abstract ViewGameSceneBase OnCreateViewGameScene(IShowDownloadProgress loadingProgress);
		protected abstract ViewLoadingBase OnCreateLoading(IShowDownloadProgress loadingProgress);

		public ViewGameSceneBase CreateViewGameScene()
		{
			mainView = OnCreateViewGameScene(loading_.loading);
			return mainView;
		}

		public ViewLoadingBase CreateLoading()
		{
			loading_ = OnCreateLoading(progressOfLoading);
			loading_.CreateProgressShower();
			return loading_;
		}

		public ViewGameSceneBase mainView
		{
			get { return mainView_; }
			set { 
				if(value == null) {
					mainView_.ClosedEvent -= OnMainViewClose;
				}
				mainView_ = value;

				mainView_.ClosedEvent += OnMainViewClose;
			}
		}

		public bool isEntering = true;
		//创建和管理View
		public void OpenView(ViewBase view)
		{
			view.parent = new WeakReference<GameControllerBase>(this);
			AddChild(view);
			view.Start();
		}

		public void OnViewClosed(ViewBase view)
		{
			closing_.Add(view);
		}

		public virtual IEnumerator ShowLogin()
		{
			yield return 0;
		}

		protected virtual void InstallMsgHandler()
		{
			App.ins.network.RegisterMsgHandler((int)CommID.msg_common_reply, (cmd, json) => {
				msg_common_reply msg = JsonMapper.ToObject<msg_common_reply>(json);
				mainView?.OnCommonReply(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameRspID.msg_player_seat, (cmd, json) => {
				msg_player_seat msg = JsonMapper.ToObject<msg_player_seat>(json);
				mainView?.OnPlayerEnter(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameRspID.msg_player_leave, (cmd, json) => {
				msg_player_leave msg = JsonMapper.ToObject<msg_player_leave>(json);
				mainView?.OnPlayerLeave(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameRspID.msg_deposit_change2, (cmd, json) => {
				msg_deposit_change2 msg = JsonMapper.ToObject<msg_deposit_change2>(json);
				mainView?.OnGoldChange(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameRspID.msg_system_showdown, (cmd, json) => {
				msg_system_showdown msg = JsonMapper.ToObject<msg_system_showdown>(json);
				mainView?.OnServerShutdown(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameRspID.msg_currency_change, (cmd, json) => {
				msg_currency_change msg = JsonMapper.ToObject<msg_currency_change>(json);
				if (msg.why_ == "0" || msg.why_ == "5") {
					if(App.ins.currentApp.game.Self != null) {
						App.ins.currentApp.game.Self.items.SetKeyVal((int)ITEMID.GOLD, long.Parse(msg.credits_));
						App.ins.currentApp.game.Self.DispatchDataChanged();
					}
				}
				mainView?.OnGoldChange(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameRspID.msg_player_hint, (cmd, json) => {
				msg_player_hint msg = JsonMapper.ToObject<msg_player_hint>(json);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameRspID.msg_server_parameter, (cmd, json) => {
				msg_server_parameter msg = JsonMapper.ToObject<msg_server_parameter>(json);
				mainView?.OnServerParameter(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameRspID.msg_get_public_data_ret, (cmd, json) => {
				msg_get_public_data_ret msg = JsonMapper.ToObject<msg_get_public_data_ret>(json);
				mainView?.OnJackpotNumber(msg);
			}, this);
		}

		public IEnumerator GameLoginSucc()
		{
			yield return OnGameLoginSucc();
		}

		public IEnumerator PrepareGameRoom()
		{
			yield return OnPrepareGameRoom();
		}
		public IEnumerator GameRoomEnterSucc()
		{
			isEntering = false;
			yield return OnGameRoomSucc();
		}

		//登录游戏服务器成功,要求显示游戏大厅界面
		protected virtual IEnumerator OnGameLoginSucc()
		{
			yield return 0;
		}

		//进入房间阶段1,服务器位置已锁定,框架要求客户端加载房间资源.
		protected virtual IEnumerator OnPrepareGameRoom()
		{
			yield return 0;
		}

		//进入游戏房间成功,服务器已经把房间数据同步完成,可以安全使用服务器数据了.
		protected virtual IEnumerator OnGameRoomSucc()
		{
			yield return 0;
		}

		protected override IEnumerator OnStart()
		{
			UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
			InstallMsgHandler();
			yield return 0;
		}

		protected override void OnLazyUpdate()
		{
			
		}

		public override void LazyUpdate()
		{
			base.LazyUpdate();
			for (int i = 0; i < closing_.Count; i++) {
				closing_[i].Stop();
			}
			closing_.Clear();
		}

		public override void AboutToStop()
		{
			MyDebug.LogFormat("About To Stop GameController");
			App.ins.network.RemoveMsgHandler(this);
			base.AboutToStop();
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
			OnAddPlayer(p);
			AddChild(p);
			p.Start();
		}

		public virtual void OnAddPlayer(GamePlayer p)
		{

		}

		public GamePlayer GetPlayer(int serverPos)
		{
			var arr = players.ToArray();
			foreach(var pp in arr) {
				if(pp.Value.serverPos == serverPos) {
					return pp.Value;
				}
			}
			return null;
		}

		public GamePlayer GetPlayer(string uid)
		{
			var arr = players.ToArray();
			foreach (var pp in arr) {
				if (pp.Value.uid == uid) {
					return pp.Value;
				}
			}
			return null;
		}
		public GamePlayer Self
		{
			get {
				var arr = players.ToArray();
				foreach (var pp in arr) {
					if (pp.Value.uid == App.ins.self.uid) {
						return pp.Value;
					}
				}
				return null;
			}
			
		}

		public void RemovePlayer(int serverPos)
		{
			if (players.ContainsKey(serverPos)) {
				players[serverPos].Stop();
			}
			players.Remove(serverPos);
		}

		public DictionaryCached<int, GamePlayer> players = new DictionaryCached<int, GamePlayer>();
		
		List<ViewBase> closing_ = new List<ViewBase>();
	}

	public abstract class GameControllerMultiplayer : GameControllerBase
	{
		public abstract msg_random_result_base CreateRandomResult(string json);
		public abstract msg_last_random_base CreateLastRandom(string json);

		protected override void InstallMsgHandler()
		{
			base.InstallMsgHandler();

			App.ins.network.RegisterMsgHandler((int)GameMultiRspID.msg_state_change, (cmd, json) => {
				var mainViewThis = (ViewMultiplayerScene)mainView;
				msg_state_change msg = JsonMapper.ToObject<msg_state_change>(json);
				mainViewThis?.OnStateChange(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameMultiRspID.msg_rand_result, (cmd, json) => {
				var mainViewThis = (ViewMultiplayerScene)mainView;
				msg_random_result_base msg = CreateRandomResult(json);
				mainViewThis?.OnRandomResult(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameMultiRspID.msg_random_result_slwh, (cmd, json) => {
				var mainViewThis = (ViewMultiplayerScene)mainView;
				msg_random_result_base msg = CreateRandomResult(json);
				mainViewThis?.OnRandomResult(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameMultiRspID.msg_brnn_result, (cmd, json) => {
				var mainViewThis = (ViewMultiplayerScene)mainView;
				msg_random_result_base msg = CreateRandomResult(json);
				mainViewThis?.OnRandomResult(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameMultiRspID.msg_bjl_result, (cmd, json) => {
				var mainViewThis = (ViewMultiplayerScene)mainView;
				msg_random_result_base msg = CreateRandomResult(json);
				mainViewThis?.OnRandomResult(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameMultiRspID.msg_last_random, (cmd, json) => {
				var mainViewThis = (ViewMultiplayerScene)mainView;
				msg_last_random_base msg = CreateLastRandom(json);
				mainViewThis?.OnLastRandomResult(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameMultiRspID.msg_player_setbet, (cmd, json) => {
				var mainViewThis = (ViewMultiplayerScene)mainView;
				msg_player_setbet msg = JsonMapper.ToObject<msg_player_setbet>(json);
				mainViewThis?.OnPlayerSetBet(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameMultiRspID.msg_my_setbet, (cmd, json) => {
				var mainViewThis = (ViewMultiplayerScene)mainView;
				msg_my_setbet msg = JsonMapper.ToObject<msg_my_setbet>(json);
				mainViewThis?.OnMyBet(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameMultiRspID.msg_banker_deposit_change, (cmd, json) => {
				var mainViewThis = (ViewMultiplayerScene)mainView;
				msg_banker_deposit_change msg = JsonMapper.ToObject<msg_banker_deposit_change>(json);
				mainViewThis?.OnBankDepositChanged(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameMultiRspID.msg_banker_promote, (cmd, json) => {
				var mainViewThis = (ViewMultiplayerScene)mainView;
				msg_banker_promote msg = JsonMapper.ToObject<msg_banker_promote>(json);
				mainViewThis?.OnBankPromote(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameMultiRspID.msg_game_report, (cmd, json) => {
				var mainViewThis = (ViewMultiplayerScene)mainView;
				msg_game_report msg = JsonMapper.ToObject<msg_game_report>(json);
				mainViewThis?.OnGameReport(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameMultiRspID.msg_game_info, (cmd, json) => {
				var mainViewThis = (ViewMultiplayerScene)mainView;
				msg_game_info msg = JsonMapper.ToObject<msg_game_info>(json);
				mainViewThis?.OnGameInfo(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameMultiRspID.msg_new_banker_applyed, (cmd, json) => {
				var mainViewThis = (ViewMultiplayerScene)mainView;
				msg_new_banker_applyed msg = JsonMapper.ToObject<msg_new_banker_applyed>(json);
				mainViewThis?.OnApplyBanker(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameMultiRspID.msg_apply_banker_canceled, (cmd, json) => {
				var mainViewThis = (ViewMultiplayerScene)mainView;
				msg_apply_banker_canceled msg = JsonMapper.ToObject<msg_apply_banker_canceled>(json);
				mainViewThis?.OnCancelBanker(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameMultiRspID.msg_apply_banker_canceled, (cmd, json) => {
				var mainViewThis = (ViewMultiplayerScene)mainView;
				msg_apply_banker_canceled msg = JsonMapper.ToObject<msg_apply_banker_canceled>(json);
				mainViewThis?.OnCancelBanker(msg);
			}, this);
		}

		protected override IEnumerator OnGameLoginSucc()
		{
			yield return loading_.WaitingForReady();

			var view = CreateViewGameScene();
			OpenView(view);
			yield return view.WaitingForReady();

			//百人类游戏直接进游戏房间
			var handle1 = App.ins.network.CoEnterGameRoom(1, 0);
			yield return handle1;
			if ((int)handle1.Current == 0) {
				ViewToast.Create(LangNetWork.EnterRoomFailed);
			}
		}
	}

	public abstract class GameControllerSlot : GameControllerBase
	{
		public override string GetDebugInfo()
		{
			return ToString();
		}

		protected override void InstallMsgHandler()
		{
			base.InstallMsgHandler();
			App.ins.network.RegisterMsgHandler((int)GameSlotRspID.msg_random_present_ret, (cmd, json) => {
				var mainViewThis = (ViewSlotScene)mainView;
				msg_random_present_ret msg = JsonMapper.ToObject<msg_random_present_ret>(json);
				mainViewThis?.OnRandomResult(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameSlotRspID.msg_get_luck_player_ret, (cmd, json) => {
				var mainViewThis = (ViewSlotScene)mainView;
				msg_get_luck_player_ret msg = JsonMapper.ToObject<msg_get_luck_player_ret>(json);
				mainViewThis?.OnLuckResult(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameSlotRspID.msg_luck_player, (cmd, json) => {
				var mainViewThis = (ViewSlotScene)mainView;
				msg_luck_player msg = JsonMapper.ToObject<msg_luck_player>(json);
				mainViewThis?.OnLuckPlayer(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameSlotRspID.msg_player_setbet_slot, (cmd, json) => {
				var mainViewThis = (ViewSlotScene)mainView;
				msg_player_setbet_slot msg = JsonMapper.ToObject<msg_player_setbet_slot>(json);
				mainViewThis?.OnPlayerSetBet(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameSlotRspID.msg_random_present_ret_record, (cmd, json) => {
				var mainViewThis = (ViewSlotScene)mainView;
				msg_random_present_ret_record msg = JsonMapper.ToObject<msg_random_present_ret_record>(json);
				mainViewThis?.OnLuckPlayerPlayData(msg);
			}, this);

			App.ins.network.RegisterMsgHandler((int)GameSlotRspID.msg_hylj_gameinfo, (cmd, json) => {
				var mainViewThis = (ViewSlotScene)mainView;
				msg_hylj_gameinfo msg = JsonMapper.ToObject<msg_hylj_gameinfo>(json);
				mainViewThis?.OnLastFreeGame(msg);
			}, this);
		}
	}
}
