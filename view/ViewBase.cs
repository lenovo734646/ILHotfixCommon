using AssemblyCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Hotfix.Common {

	//所有界面操作代码继承自这个类
	//画布命名使用Canvas
	public abstract class ViewBase : ControllerBase
	{
		public class LoadTask
		{
			public string assetPath;
			public bool dontInstantiate = false;
			public Action<GameObject> callback;
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
			canv.RemoveAllChildren();
			canv.AddChild(obj);
		}

		public static void AddToCanvas(GameObject obj)
		{
			var canv = GameObject.Find("Canvas");
			canv.AddChild(obj);
		}

		public static void AddToPopup(GameObject obj)
		{
			var canv = GetPopupLayer();
			canv.AddChild(obj);
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

		public virtual void Close()
		{
			//按加载顺序倒着释放
			objs.Reverse();
			foreach(var obj in objs) {
				GameObject.Destroy(obj);
			}

			//停止本窗口所有协程
			this.StopCor(-1);
		}
		public IEnumerator LoadResources()
		{
			foreach (var it in resNames_) {
				var result = Globals.resLoader.LoadAsync<GameObject>(it.assetPath, progress);
				yield return result;

				if (result.Current != null) {
					if (it.dontInstantiate) {
						if (it.callback != null) it.callback(result.Current);
						objs.Add(result.Current);
					}
					else {
						var obj = GameObject.Instantiate(result.Current);
						obj.name = result.Current.name;
						if (it.callback != null) it.callback(obj);
						objs.Add(obj);
					}
				}
			}
			resNames_.Clear();
		}

		protected abstract void SetLoader();

		protected abstract void OnResourceReady();

		protected IEnumerator DoStart_()
		{
			progress?.Desc("..");
			SetLoader();

			yield return LoadResources();

			finished_ = true;
			OnResourceReady();
		}

		protected List<LoadTask> resNames_ = new List<LoadTask>();
		protected List<GameObject> objs = new List<GameObject>();
		bool finished_ = false;
	}
}
