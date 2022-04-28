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
		public T OpenView<T>() where T : ViewBase, new()
		{
			T ret = new T();
			views_.Add(ret);
			ret.Start();
			return ret;
		}
		
		public void OnViewClosed(ViewBase view)
		{
			views_.Remove(view);
		}

		public virtual void OnGameLoginSucc()
		{
			prepared_ = true;
		}
		protected virtual void OnNetMsg(object sender, NetEventArgs evt)
		{

		}
		public virtual IEnumerator OnPrepareGameRoom()
		{
			yield return 0;
		}
		public virtual IEnumerator OnGameRoomSucc()
		{
			isEntering = true;
			yield return 0;
		}

		public override void Start()
		{
			UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
			AppController.ins.network.AddMsgHandler(OnNetMsg);
			base.Start();
		}

		public void ReleaseWhenClose(AddressablesLoader.LoadTaskBase task)
		{
			resourceLoader_.Add(task);
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

			foreach (var tsk in resourceLoader_) {
				tsk.Release();
			}

			resourceLoader_.Clear();
			AppController.ins.network.RemoveMsgHandler(OnNetMsg);
		}
		
		public virtual GamePlayer CreateGamePlayer()
		{
			return new GamePlayer();
		}


		//资源加载器,在半闭本窗口的时候,需要释放资源引用.
		List<AddressablesLoader.LoadTaskBase> resourceLoader_ = new List<AddressablesLoader.LoadTaskBase>();
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
			if (evt.payload == null) return;
			string json = Encoding.UTF8.GetString(evt.payload);
			switch (evt.cmd) {
				case (int)GameRspID.msg_player_seat: {
					var msg = JsonMapper.ToObject<msg_player_seat>(json);
					mainView.OnPlayerEnter(msg);
				}
				break;

				case (int)GameRspID.msg_player_leave: {
					var msg = JsonMapper.ToObject<msg_player_leave>(json);
					mainView.OnPlayerLeave(msg);
				}
				break;

				case (int)GameMultiID.msg_state_change: {
					var msg = JsonMapper.ToObject<msg_state_change>(json);
					mainView.OnStateChange(msg);
				}
				break;

				case (int)GameMultiID.msg_rand_result: {
					var msg = CreateRandomResult(json);
					mainView.OnRandomResult(msg);
				}
				break;

				case (int)GameMultiID.msg_last_random: {
					var msg = CreateLastRandom(json);
					mainView.OnLastRandomResult(msg);
				}
				break;

				case (int)GameMultiID.msg_player_setbet: {
					var msg = JsonMapper.ToObject<msg_player_setbet>(json);
					mainView.OnPlayerSetBet(msg);
				}
				break;

				case (int)GameMultiID.msg_my_setbet: {
					var msg = JsonMapper.ToObject<msg_my_setbet>(json);
					mainView.OnMyBet(msg);
				}
				break;

				case (int)GameMultiID.msg_banker_deposit_change: {
					var msg = JsonMapper.ToObject<msg_banker_deposit_change>(json);
					mainView.OnBankDepositChanged(msg);
				}
				break;

				case (int)GameMultiID.msg_banker_promote: {
					var msg = JsonMapper.ToObject<msg_banker_promote>(json);
					mainView.OnBankPromote(msg);
				}
				break;

				case (int)GameMultiID.msg_game_report: {
					var msg = JsonMapper.ToObject<msg_game_report>(json);
					mainView.OnGameReport(msg);
				}
				break;

				default:
				mainView?.OnNetMsg(evt.cmd, json); 
				break;
			}
		}
	}

}
