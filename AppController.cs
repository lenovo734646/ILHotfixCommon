using AssemblyCommon;
using Hotfix.Lobby;
using Hotfix.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Hotfix.Common
{

	//热更入口类
	public class AppController : ControllerBase
	{
		public class GameRunQueue { };
		public static AppController ins = null;
		public Config conf = new Config();
		public AppBase currentApp = null;
		//进度指示器,由宿主工程设置
		public IShowDownloadProgress progressFromHost;
		public NetWorkController network = new NetWorkController();
		public SelfPlayer self = new SelfPlayer();
		public List<AccountInfo> accounts = new List<AccountInfo>();
		public AccountInfo lastUseAccount = null;
		public GameConfig currentGameConfig = null;
		public AppController()
		{
			ins = this;
		}

		public IEnumerator ShowLogin()
		{
			var vLogin = currentApp.game.OpenView<ViewLogin>();
			vLogin.progress = progress;
			vLogin.Start();
			yield return vLogin.WaitingForReady();
		}

		IEnumerator DoCheckUpdateAndRun(GameConfig conf, IShowDownloadProgress ip, bool showLogin)
		{
			MyDebug.LogFormat("===================>CheckUpdateAndRun showlogin={0}", showLogin);
			if (conf.scriptType == GameConfig.ScriptType.CSharp) {
				bool succ = false;

				if (conf.contentCatalog.Length > 0) {
					var address = conf.GetCatalogAddress(AddressablesLoader.usingUpdateUrl, Globals.resLoader.GetPlatformString());
					MyDebug.LogFormat("Get CatalogAddress:{0}, AddressablesLoader.usingUpdateUrl:{1}", address, AddressablesLoader.usingUpdateUrl);

					var handleCatalog = Globals.resLoader.LoadAsync<AddressablesLoader.DownloadCatalog>(address, ip);
					yield return handleCatalog;
					if (!handleCatalog.Current.succeed) goto Clean;

					var handleDep = Globals.resLoader.LoadAsync<AddressablesLoader.DownloadDependency>(conf.folder, ip);
					yield return handleDep;

					if (!handleDep.Current.succeed) goto Clean;
					MyDebug.LogFormat("DownloadDependency:{0}", address);
				}

				if (currentApp != null) currentApp.Stop();
				currentApp = null;

				//重置session,但不重置网络连接
				if (showLogin)
					yield return network.ResetSession(false, true);
				else
					yield return network.ResetSession(false, false);

				MyDebug.LogFormat("Run CommonEmptyScene->Game:{0}", conf.name);

				network.SetToGame(conf.name);

				//开启新的场景,这里不需要进度指示是因为这个已经下载好了
				var sceneHandle = Globals.resLoader.LoadAsync<AddressablesLoader.DownloadScene>("Assets/Scenes/CommonEmptyScene.unity", null);
				yield return sceneHandle;
				yield return sceneHandle.Current.ActiveScene();
				
				var entryClass = Type.GetType(conf.entryClass);
				currentApp = (AppBase)Activator.CreateInstance(entryClass);
				currentApp.Start();

				if (showLogin)
					yield return ShowLogin();
				else
					network.AutoLogin(false);

				succ = true;
			Clean:
				if (!succ) {
					ViewToast.Create(LangUITip.EnterGameFailed);
					MyDebug.LogFormat("CheckUpdateAndRun failed!", conf.name);
				}
			}
			else {

			}
		}

		public void CheckUpdateAndRun(GameConfig conf, IShowDownloadProgress ip, bool showLogin)
		{
			runQueue.StartCor(DoCheckUpdateAndRun(conf, ip, showLogin), true);
		}

		public override void Start()
		{
			MyDebug.LogFormat("Hotfix Module Begins.");
			//注册protobuf类
			ILRuntime_CLGT.Initlize();
			ILRuntime_CLPF.Initlize();
			ILRuntime_Global.Initlize();

			DoStart_();
		}

		void DoStart_()
		{
			conf.Start();
			CheckUpdateAndRun(conf.defaultGame, progressFromHost, true);

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
			this.StopAllCor();
		}

		GameRunQueue runQueue = new GameRunQueue();
	}
}
