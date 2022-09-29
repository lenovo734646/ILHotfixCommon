using AssemblyCommon;
using Hotfix.Common.MultiPlayer;
using Hotfix.Common.Slot;
using Hotfix.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Hotfix.Common 
{
	//监视游戏资源,在模块结束时释放
	//ViewBase, 资源生存周期和视图一样
	//AppBase,资源生存周期和游戏一样
	//AppController,资源生存周期与整个APP一样
	public abstract class ResourceMonitor : ControllerBase
	{
		public enum Result
		{
			Failure,
			Success,
		}

		public void LoadAssets<T>(string path, Action<AddressablesLoader.LoadTask<T>> callback) where T : UnityEngine.Object
		{
			Action<AddressablesLoader.LoadTask<T>> callbackWrapper = (AddressablesLoader.LoadTask<T> loader) => {
				if(loader.status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded){
					callback(loader);
				}
			};
			resourceLoader_.Add(Globals.resLoader.LoadAsync(path, callbackWrapper, progressOfLoading)); 
		}

		protected override void OnStop()
		{
			ClearResource();
		}

		public void ClearResource()
		{
			string path = "";
			try {
				foreach (var tsk in resourceLoader_) {
					path = tsk.path;
					tsk.Release();
				}
			}
			catch (Exception ex) {
				MyDebug.LogErrorFormat("Release Addressable Task Error:{0}", path);
			}

			resourceLoader_.Clear();
		}

		public override bool IsReady()
		{
			foreach(var tsk in resourceLoader_) {
				if(tsk.status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.None) {
					return false;
				}
			}
			return true;
		}
		//资源加载器,在半闭本窗口的时候,需要释放资源引用.
		protected List<AddressablesLoader.LoadTaskBase> resourceLoader_ = new List<AddressablesLoader.LoadTaskBase>();
	}

	//所有界面操作代码继承自这个类
	//画布命名使用Canvas
	public abstract class ViewBase : ResourceMonitor
	{
		public System.WeakReference<GameControllerBase> parent = null;
		public class ViewLoadTask<T> where T : UnityEngine.Object
		{
			public string assetPath;
			public AddressablesLoader.LoadTask<T> loader;
			public Action<T> callback;
			public bool isMain = false;
		}

		public ViewBase(IShowDownloadProgress loadingProgress)
		{
			progressOfLoading = loadingProgress;
		}

		public static GameObject GetCanvas()
		{
			var canv = GameObject.Find("Canvas");

			if (canv == null)
				canv = GameObject.Find("Canvas2D");

			if (canv == null) {
				var can = GameObject.FindObjectOfType<Canvas>();
				if(can != null)
					canv = can.gameObject;
			}
			return canv;
		}

		public override string GetDebugInfo()
		{
			return "ViewBase";
		}

		public static GameObject GetPopupLayer()
		{
			var canv = GetCanvas();
			if (canv == null) return canv;

			var top = canv.FindChildDeeply("UITopLayer");
			if(top == null) {
				return canv;
			}
			else {
				return top;
			}
		}

		public static void ClearAndAddToCanvas(GameObject obj)
		{
			var canv = GetCanvas();

			if (canv != null) {
				canv.RemoveAllChildren();
				canv.AddChild(obj);
			}
		}

		public static void AddToCanvas(GameObject obj)
		{
			var canv = GetCanvas();
			if (canv != null) canv.AddChild(obj);
		}

		public static void AddToPopup(GameObject obj)
		{
			var canv = GetPopupLayer();
			if(canv != null) canv.AddChild(obj);
		}

		public static void RemoveGameObject(string name)
		{
			var obj = GameObject.Find(name);
			if (obj != null) GameObject.Destroy(obj);
		}

		protected override IEnumerator OnStart()
		{
			yield return DoStart_();
		}

		public override bool IsReady()
		{
			if (!base.IsReady()) return false;
			return finished_;
		}

		public void Close()
		{
			GameControllerBase ctrl;
			if(parent != null && parent.TryGetTarget(out ctrl)) {
				ctrl.OnViewClosed(this);
			}
		}

		protected override void OnStop()
		{
			ClosedEvent?.Invoke(this, new EventArgs());
			//按加载顺序倒着释放
			objs.Reverse();
			foreach (var obj in objs) {
				GameObject.Destroy(obj);
			}
			objs.Clear();

			resNames_.Clear();
			resScenes_ = null;

			App.ins.network.RemoveMsgHandler(this);
		}

		IEnumerator LoadResources()
		{
			foreach (var it in resNames_) {
				var tsk = it;
				resourceLoader_.Add(Globals.resLoader.LoadAsync<GameObject>(it.assetPath, t => {
					tsk.loader = t;
				}, progressOfLoading));
			}

			Func<List<AddressablesLoader.LoadTaskBase>, int> counterFinished = (lst) => {
				int ret = 0;
				foreach(var tsk in lst) {
					if(tsk.status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.None) {
						ret++;
					}
				}
				return ret;
			};

			progressOfLoading?.Desc(LangUITip.LoadingResource + $"(0/{resourceLoader_.Count})");
			int loaded = 0;
			while (loaded < resourceLoader_.Count) {
				progressOfLoading?.Desc(LangUITip.LoadingResource + $"({loaded}/{resourceLoader_.Count})");
				progressOfLoading?.Progress(loaded, resourceLoader_.Count);
				yield return new WaitForSeconds(0.05f);
				loaded = counterFinished(resourceLoader_);
			}

			progressOfLoading?.Desc(LangUITip.LoadingResource + $"({loaded}/{resourceLoader_.Count})");
			progressOfLoading?.Progress(loaded, resourceLoader_.Count);

			yield return new WaitForSeconds(0.1f);

			if(resScenes_ != null) {
				resScenes_.loader = Globals.resLoader.LoadAsync<AddressablesLoader.DownloadScene>(resScenes_.assetPath, t => {
					loaded++;
				}, progressOfLoading);


				while (!resScenes_.loader.SceneHandle.IsDone) {
					progressOfLoading?.Desc(LangUITip.LoadingResource + $"({loaded}/{resourceLoader_.Count})");
					progressOfLoading?.Progress((int)resScenes_.loader.SceneHandle.PercentComplete * 100, 100);
					yield return 0;
				}
				yield return resScenes_.loader.ActiveScene();
				resScenes_ = null;
				yield return 0;
			}
		}

		protected abstract void SetLoader();

		protected IEnumerator ReadyResource()
		{
			bool hasFailed = false;
			foreach (var it in resNames_) {
				if(it.loader.status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded) {
					var obj = it.loader.Instantiate();
					if (it.isMain) mainObject_ = obj;
					if (it.callback != null) it.callback(obj);
					objs.Add(obj);
				}
				else {
					hasFailed = true;
				}
			}

			if (!hasFailed)
				//这里很重要,要停一下
				yield return OnResourceReady();
			else {
				MyDebug.LogWarningFormat("some resouce of the view load failed.{0}", resNames_[0].assetPath);
				yield return 0;
			}
			resNames_.Clear();
		}

		protected abstract IEnumerator OnResourceReady();

		protected void LoadPrefab(string path, Action<GameObject> cb, bool mainObj = false)
		{
			ViewLoadTask<GameObject> task = new ViewLoadTask<GameObject>();
			task.assetPath = path;
			task.callback = cb;
			task.isMain = mainObj;
			resNames_.Add(task);
		}

		protected void LoadScene(string path, Action<AddressablesLoader.DownloadScene> cb)
		{
			ViewLoadTask<AddressablesLoader.DownloadScene> task = new ViewLoadTask<AddressablesLoader.DownloadScene>();
			task.assetPath = path;
			task.callback = cb;
			resScenes_ = task;
		}

		protected IEnumerator DoStart_()
		{
			progressOfLoading?.Desc(LangUITip.LoadingResource);
			SetLoader();

			yield return LoadResources();
			yield return ReadyResource();
			finished_ = true;
		}

		public event EventHandler ClosedEvent;

		protected GameObject mainObject_;

		List<ViewLoadTask<GameObject>> resNames_ = new List<ViewLoadTask<GameObject>>();
		ViewLoadTask<AddressablesLoader.DownloadScene> resScenes_;
		List<GameObject> objs = new List<GameObject>();
		protected bool finished_ = false;
	}

	public abstract class ViewGameSceneBase : ViewBase
	{
		public ViewGameSceneBase(IShowDownloadProgress ip):base(ip)
		{

		}

		public virtual GamePlayer OnPlayerEnter(msg_player_seat msg)
		{
			var game = App.ins.currentApp.game;
			var pp = game.CreateGamePlayer();
			pp.uid = msg.uid_;
			pp.serverPos = int.Parse(msg.pos_);
			pp.nickName = msg.uname_;
			pp.headFrame = msg.headframe_id_;
			pp.headIco = msg.head_ico_;
			pp.lv = int.Parse(msg.lv_);
			game.AddPlayer(pp);
			return pp;
		}

		public virtual void OnPlayerLeave(msg_player_leave msg)
		{
			var game = App.ins.currentApp.game;
			game.RemovePlayer(int.Parse(msg.pos_));
		}

		public virtual void OnCommonReply(msg_common_reply msg)
		{

		}

		//玩家货币变币
		public virtual void OnGoldChange(msg_deposit_change2 msg)
		{
			if (int.Parse(msg.display_type_) == (int)msg_deposit_change2.dp.display_type_sync_gold ||
				int.Parse(msg.display_type_) == (int)msg_deposit_change2.dp.display_type_gold_change) {
				var pp = App.ins.currentApp.game.GetPlayer(int.Parse(msg.pos_));
				if (pp != null) {
					pp.items.SetKeyVal((int)ITEMID.GOLD, long.Parse(msg.credits_));
					pp.DispatchDataChanged();
				}

				if(pp.uid == App.ins.self.uid) {
					App.ins.self.items.SetKeyVal((int)ITEMID.GOLD, long.Parse(msg.credits_));
					App.ins.self.DispatchDataChanged();
				}
			}
		}

		//玩家货币变币
		public virtual void OnGoldChange(msg_currency_change msg)
		{
			if (msg.why_ == "0") {
				App.ins.currentApp.game.Self?.items.SetKeyVal((int)ITEMID.GOLD, long.Parse(msg.credits_));
				App.ins.currentApp.game.Self?.DispatchDataChanged();
			}
		}

		public void OnServerShutdown(msg_system_showdown msg)
		{
			ViewToast.Create(msg.desc_);
		}

		public abstract void OnServerParameter(msg_server_parameter msg);
		public abstract void OnJackpotNumber(msg_get_public_data_ret msg);
		public virtual IEnumerator OnRoomEnterSucc() { yield return 0; }
	}

	public abstract class ViewSlotScene : ViewGameSceneBase
	{
		public ViewSlotScene(IShowDownloadProgress ip) : base(ip)
		{

		}

		public abstract void OnRandomResult(msg_random_present_ret msg);
		public abstract void OnLuckResult(msg_get_luck_player_ret msg);
		public abstract void OnLuckPlayer(msg_luck_player msg);
		public abstract void OnPlayerSetBet(msg_player_setbet_slot msg);
		public abstract void OnLuckPlayerPlayData(msg_random_present_ret_record msg);
		public abstract void OnLastFreeGame(msg_hylj_gameinfo msg);

	}

	public abstract class ViewMultiplayerScene: ViewGameSceneBase
	{
		public ViewMultiplayerScene(IShowDownloadProgress ip) : base(ip)
		{

		}
		public override void OnServerParameter(msg_server_parameter msg) { }
		//状态机变化
		public abstract void OnStateChange(msg_state_change msg);
		//其它玩家下注通知
		public abstract void OnPlayerSetBet(msg_player_setbet msg);
		//我的下注通知
		public abstract void OnMyBet(msg_my_setbet msg);
		//开奖结果
		public abstract void OnRandomResult(msg_random_result_base msg);
		//历史开奖记录
		public abstract void OnLastRandomResult(msg_last_random_base msg);
		//庄家货币变化
		public abstract void OnBankDepositChanged(msg_banker_deposit_change msg);
		//玩家上庄
		public abstract void OnBankPromote(msg_banker_promote msg);
		//每个玩家输赢结果通知
		public abstract void OnGameReport(msg_game_report msg);
		//游戏信息
		public abstract void OnGameInfo(msg_game_info msg);

		//玩家申请上庄通知
		public abstract void OnApplyBanker(msg_new_banker_applyed msg);
		//玩家取消上庄通知
		public abstract void OnCancelBanker(msg_apply_banker_canceled msg);
		protected GameControllerBase.GameState st;
	}

	public class ViewCommon : ViewBase
	{
		public ViewCommon(string path):base(null)
		{
			path_ = path;
		}

		protected override IEnumerator OnResourceReady()
		{
			mainObject_.DoPopup();
			var btn_close = mainObject_.FindChildDeeply("btn_close");
			if(btn_close == null) {
				btn_close = mainObject_.FindChildDeeply("btnClose");
			}
			btn_close.OnClick(() => {
				Close();
			});
			yield return 0;
		}

		protected override void SetLoader()
		{
			LoadPrefab(path_, AddToCanvas, true);
		}
		string path_;
	}
}
