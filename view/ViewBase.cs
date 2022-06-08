using AssemblyCommon;
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
	public class ResourceMonitor : ControllerBase
	{
		public void LoadAssets<T>(string path, Action<AddressablesLoader.LoadTask<T>> callback) where T : UnityEngine.Object
		{
			Action<AddressablesLoader.LoadTask<T>> callbackWrapper = (AddressablesLoader.LoadTask<T> loader) => {
				callback(loader);
				resourceLoader_.Add(loader);
			};

			Globals.resLoader.LoadAsync(path, callbackWrapper, progressOfLoading);
		}

		public override void Stop()
		{
			string path = "";
			try {
				foreach (var tsk in resourceLoader_) {
					path = tsk.path;
					tsk.Release();
				}
			}
			catch(Exception ex) {
				MyDebug.LogErrorFormat("Release Addressable Task Error:{0}", path);
			}

			resourceLoader_.Clear();
		}

		public override bool IsReady()
		{
			if (!base.IsReady()) return false;
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
		public class ViewLoadTask<T> where T : UnityEngine.Object
		{
			public string assetPath;
			public AddressablesLoader.LoadTask<T> loader;
			public Action<T> callback;
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

		public override void Start()
		{
			this.StartCor(DoStart_(), false);
		}

		public override bool IsReady()
		{
			if (!base.IsReady()) return false;
			return finished_;
		}

		public override void Stop()
		{
			RemoveInstance();
			base.Stop();
		}

		//每个View必须要调用这个Close才能正确的释放资源.
		//跳过这个直接清理了Canvas会造成资源泄露,
		//Adressable资源不能正确释放
		public virtual void Close()
		{
			AppController.ins.currentApp?.game?.OnViewClosed(this);
			//按加载顺序倒着释放
			objs.Reverse();
			foreach(var obj in objs) {
				GameObject.Destroy(obj);
			}
			objs.Clear();

			resNames_.Clear();
			resScenes_ = null;

			//停止本窗口所有协程
			this.StopCor(-1);
			Stop();
		}

		public IEnumerator LoadResources()
		{
			int willLoad = (resScenes_ == null? 0: 1) + resNames_.Count;
			int loaded = 0;
			progressOfLoading?.Desc(LangUITip.LoadingResource + $"(0/{willLoad})");

			foreach (var it in resNames_) {
				var tsk = it;
				Globals.resLoader.LoadAsync<GameObject>(it.assetPath, t => {
					tsk.loader = t;
					resourceLoader_.Add(tsk.loader);
					loaded++;
				}, progressOfLoading);
			}

			while (loaded < resNames_.Count) {
				progressOfLoading?.Desc(LangUITip.LoadingResource + $"({loaded}/{willLoad})");
				progressOfLoading?.Progress(loaded, willLoad);
				yield return 0;
			}

			progressOfLoading?.Desc(LangUITip.LoadingResource + $"({loaded}/{willLoad})");
			progressOfLoading?.Progress(loaded, willLoad);

			yield return 0;

			if(resScenes_ != null) {
				resScenes_.loader = Globals.resLoader.LoadAsync<AddressablesLoader.DownloadScene>(resScenes_.assetPath, t => {
					loaded++;
				}, progressOfLoading);


				while (!resScenes_.loader.SceneHandle.IsDone) {
					progressOfLoading?.Desc(LangUITip.LoadingResource + $"({loaded}/{willLoad})");
					progressOfLoading?.Progress((int)resScenes_.loader.SceneHandle.PercentComplete * 100, 100);
					yield return 0;
				}
				resScenes_ = null;
				yield return 0;
			}
		}

		protected abstract void SetLoader();

		protected virtual IEnumerator OnResourceReady() 
		{
			MyDebug.Log("ViewBase.OnResourceReady()");
			
			foreach (var it in resNames_) {
				var obj = it.loader.Instantiate();
				if(it.callback != null) it.callback(obj);
				objs.Add(obj);
			}
			resNames_.Clear();
			//这里很重要,要停一下
			yield return 0;
		}

		protected void LoadPrefab(string path, Action<GameObject> cb)
		{
			ViewLoadTask<GameObject> task = new ViewLoadTask<GameObject>();
			task.assetPath = path;
			task.callback = cb;
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
			yield return OnResourceReady();
			finished_ = true;
		}

		List<ViewLoadTask<GameObject>> resNames_ = new List<ViewLoadTask<GameObject>>();
		ViewLoadTask<AddressablesLoader.DownloadScene> resScenes_;
		List<GameObject> objs = new List<GameObject>();
		bool finished_ = false;
	}

	public abstract class ViewGameSceneBase : ViewBase
	{
		public ViewGameSceneBase(IShowDownloadProgress ip):base(ip)
		{

		}
		public virtual GamePlayer OnPlayerEnter(msg_player_seat msg)
		{
			if (AppController.ins.self.gamePlayer.uid == msg.uid_) {
				AppController.ins.self.gamePlayer.serverPos = int.Parse(msg.pos_);
				AppController.ins.self.gamePlayer.lv = int.Parse(msg.lv_);
				return AppController.ins.self.gamePlayer;
			}
			else {
				var game = AppController.ins.currentApp.game;
				var pp = game.CreateGamePlayer();
				pp.serverPos = int.Parse(msg.pos_);
				pp.nickName = msg.uname_;
				pp.headFrame = msg.headframe_id_;
				pp.headIco = msg.head_ico_;
				pp.lv = int.Parse(msg.lv_);
				game.AddPlayer(pp);
				return pp;
			}
		}
		public virtual void OnPlayerLeave(msg_player_leave msg)
		{
			var game = AppController.ins.currentApp.game;
			
		}
	}

	public abstract class ViewMultiplayerScene: ViewGameSceneBase
	{
		public ViewMultiplayerScene(IShowDownloadProgress ip) : base(ip)
		{

		}
		//网络消息回调,这是原始的网络消息
		public abstract void OnNetMsg(int cmd, string json);
		//通用错误回复
		public abstract void OnCommonReply(msg_common_reply msg);
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
		//玩家货币变币
		public abstract void OnGoldChange(msg_deposit_change2 msg);
		//玩家货币变币
		public abstract void OnGoldChange(msg_currency_change msg);
		//玩家申请上庄通知
		public abstract void OnApplyBanker(msg_new_banker_applyed msg);
		//玩家取消上庄通知
		public abstract void OnCancelBanker(msg_apply_banker_canceled msg);
		protected GameControllerBase.GameState st;
	}
}
