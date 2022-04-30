using AssemblyCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Hotfix.Common {

	//所有界面操作代码继承自这个类
	//画布命名使用Canvas
	public abstract class ViewBase : ControllerBase
	{
		public class ViewLoadTask<T> where T : UnityEngine.Object
		{
			public string assetPath;
			public AddressablesLoader.LoadTask<T> loader;
			public Action<T> callback;
		}
		public ViewBase()
		{
		}

		public static GameObject GetPopupLayer()
		{
			var canv = GameObject.Find("Canvas");
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
			var canv = GameObject.Find("Canvas");
			if (canv != null) {
				canv.RemoveAllChildren();
				canv.AddChild(obj);
			}
		}

		public static void AddToCanvas(GameObject obj)
		{
			var canv = GameObject.Find("Canvas");
			if(canv != null) canv.AddChild(obj);
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
			return finished_;
		}

		public override void Stop()
		{
			RemoveInstance();
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
			resScenes_.Clear();

			foreach (var obj in tasks_) {
				obj.Release();
			}
			tasks_.Clear();
			//停止本窗口所有协程
			this.StopCor(-1);
			Stop();
		}

		public IEnumerator LoadResources()
		{
			foreach (var it in resScenes_) {
				var result = Globals.resLoader.LoadAsync<AddressablesLoader.DownloadScene>(it.assetPath, progress);
				yield return result;
				it.loader = result.Current;
			}

			foreach (var it in resNames_) {
				var result = Globals.resLoader.LoadAsync<GameObject>(it.assetPath, progress);
				yield return result;
				it.loader = result.Current;
				tasks_.Add(it.loader);
			}
		}

		protected abstract void SetLoader();

		protected virtual IEnumerator OnResourceReady() 
		{
			MyDebug.Log("ViewBase.OnResourceReady()");
			foreach (var it in resScenes_) {
				yield return it.loader.ActiveScene();
			}

			resScenes_.Clear();

			foreach (var it in resNames_) {
				var obj = it.loader.Instantiate();
				if(it.callback != null) it.callback(obj);
				objs.Add(obj);
			}
			resNames_.Clear();
			//这里很重要,要停一下
			yield return 0;
		}

		protected void LoadPrefab(string path, System.Action<GameObject> cb)
		{
			ViewLoadTask<GameObject> task = new ViewLoadTask<GameObject>();
			task.assetPath = path;
			task.callback = cb;
			resNames_.Add(task);
		}

		protected void LoadScene(string path, System.Action<AddressablesLoader.DownloadScene> cb)
		{
			ViewLoadTask<AddressablesLoader.DownloadScene> task = new ViewLoadTask<AddressablesLoader.DownloadScene>();
			task.assetPath = path;
			task.callback = cb;
			resScenes_.Add(task);
		}

		protected IEnumerator DoStart_()
		{
			progress?.Desc("..");
			SetLoader();

			yield return LoadResources();
			yield return OnResourceReady();
			finished_ = true;
		}

		List<ViewLoadTask<GameObject>> resNames_ = new List<ViewLoadTask<GameObject>>();
		List<ViewLoadTask<AddressablesLoader.DownloadScene>> resScenes_ = new List<ViewLoadTask<AddressablesLoader.DownloadScene>>();
		List<GameObject> objs = new List<GameObject>();
		List<AddressablesLoader.LoadTaskBase> tasks_ = new List<AddressablesLoader.LoadTaskBase>();
		bool finished_ = false;
	}

	public abstract class ViewGameSceneBase : ViewBase
	{
		public abstract void OnPlayerEnter(msg_player_seat msg);
		public abstract void OnPlayerLeave(msg_player_leave msg);
	}

	public abstract class ViewMultiplayerScene: ViewGameSceneBase
	{
		public abstract void OnNetMsg(int cmd, string json);
		public abstract void OnStateChange(msg_state_change msg);
		public abstract void OnPlayerSetBet(msg_player_setbet msg);
		public abstract void OnMyBet(msg_my_setbet msg);
		public abstract void OnRandomResult(msg_random_result_base msg);
		public abstract void OnLastRandomResult(msg_last_random_base msg);
		public abstract void OnBankDepositChanged(msg_banker_deposit_change msg);
		public abstract void OnBankPromote(msg_banker_promote msg);
		public abstract void OnGameReport(msg_game_report msg);
	}
}
