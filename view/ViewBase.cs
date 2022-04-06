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
		public class ViewLoadTask<T> where T: UnityEngine.Object
		{
			public string assetPath;
			public AddressablesLoader.LoadTask<T> loader;
			public Action<T> callback;
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

		//每个View必须要调用这个Close才能正确的释放资源.
		//跳过这个直接清理了Canvas会造成资源泄露,
		//Adressable资源不能正确释放
		public virtual void Close()
		{
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

		protected void LoadPrefab(ViewLoadTask<GameObject> task)
		{
			resNames_.Add(task);
		}

		protected void LoadScene(ViewLoadTask<AddressablesLoader.DownloadScene> task)
		{
			resScenes_.Add(task);
		}

		protected IEnumerator DoStart_()
		{
			progress?.Desc("..");
			SetLoader();

			yield return LoadResources();
			finished_ = true;
			yield return OnResourceReady();
		}

		List<ViewLoadTask<GameObject>> resNames_ = new List<ViewLoadTask<GameObject>>();
		List<ViewLoadTask<AddressablesLoader.DownloadScene>> resScenes_ = new List<ViewLoadTask<AddressablesLoader.DownloadScene>>();
		List<GameObject> objs = new List<GameObject>();
		List<AddressablesLoader.LoadTaskBase> tasks_ = new List<AddressablesLoader.LoadTaskBase>();
		bool finished_ = false;
	}
}
