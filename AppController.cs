using AssemblyCommon;
using Hotfix.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Hotfix.Common
{
	//每个小游戏都有一个MyApp,
	public class AppBase : ControllerBase
	{
		//每个小游戏都有一个GameController
		public GameControllerBase game = null;
	}


	//每个小游戏的GameController基类
	//用来管理每个小游戏的逻辑,包括视图管理,游戏逻辑,网络消息处理,流程处理等等.
	//总之,和小游戏相关的东西,都在这里开始
	public class GameControllerBase : ControllerBase
	{
		//创建和管理View
		public ViewBase currentView = null;
		//additive true:直接加入当前canvas false:清除当前canvas之后再加入
		public T OpenView<T>() where T : ViewBase, new()
		{
			T ret = new T();
			currentView = ret;
			currentView.Start();
			return ret;
		}
	}
	
	//所有界面操作代码继承自这个类
	//画布命名使用Canvas
	public abstract class ViewBase : ControllerBase
	{
		public static void RemoveGameObject(string name)
		{
			var obj = GameObject.Find(name);
			if(obj != null)	GameObject.Destroy(obj);
		}

		public void SetAdditive()
		{
			additive_ = true;
		}

		public override void Start()
		{
			this.StartCor(DoStart_(), false);
		}

		public override bool IsReady()
		{
			return finished_;
		}
		
		protected abstract void SetLoader();

		protected abstract void OnResourceReady();

		protected IEnumerator DoStart_()
		{
			progress?.Desc("..");

			var canvas = GameObject.Find(canvas_);
			if (!additive_) {
				canvas?.RemoveAllChildren();
			}

			foreach (var it in resNames_) {
				var result = Globals.resLoader.LoadAsync<GameObject>(it, progress);
				yield return result;
				if (result.Current != null) {
					if(canvas == null) {
						canvas = GameObject.Find(canvas_);
					}
					canvas?.AddChild(result.Current);
				}
			}

			finished_ = true;
			OnResourceReady();
		}

		protected List<string> resNames_;
		protected string canvas_ = "Canvas";
		bool additive_ = false;
		bool finished_ = false;
	}

	//热更入口类
	public class AppController : ControllerBase
	{
		public static AppController ins = null;
		public Config conf = new Config();
		public AppBase currentApp = null;
		//进度指示器,由宿主工程设置
		public IShowDownloadProgress progressFromHost;
		public NetWorkController network = new NetWorkController();
		public SelfPlayer self = new SelfPlayer();
		public List<UserAccountInfo> accounts = new List<UserAccountInfo>();
		public UserAccountInfo lastUseAccount = null;
		public GameConfig lastGame = null;

		public AppController()
		{
			ins = this;
		}

		public void SwitchGame(string to)
		{
			if (currentApp != null) currentApp.Stop();
			var gmconf = conf.FindGameConfig(to);
			var entryClass = Type.GetType(gmconf.entryClass);
			currentApp = (AppBase)Activator.CreateInstance(entryClass);
			currentApp.Start();
		}

		public override void Start()
		{
			Debug.LogFormat("Hotfix Module Begins.");
			this.StartCor(DoStart_(), false);
		}

		IEnumerator DoStart_()
		{
			conf.Start();
			SwitchGame(conf.defaultGame);
			yield return 0;
		}

		public override  void Update()
		{
			network.Update();
			if(currentApp != null) currentApp.Update();
		}

		public override void Stop()
		{
			if (currentApp != null) currentApp.Stop();
			network.Stop();
		}
	}
}
