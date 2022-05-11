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
	public class AppController : ResourceMonitor
	{
		public class GameRunQueue { };
		public AppController()
		{
			ins = this;
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

				//确保连接
				MyDebug.LogFormat("network.ValidSession");
				var handleSess = network.ValidSession();
				yield return handleSess;

				if ((int)handleSess.Current == 0) {
					if(ins.conf.defaultGame == conf) {
						showLogin = true;
					}
					else
						goto Clean;
				}
				MyDebug.LogFormat("Run CommonEmptyScene->Game:{0}", conf.name);

				//开启新的场景,这里不需要进度指示是因为这个已经下载好了
				var sceneHandle = Globals.resLoader.LoadAsync<AddressablesLoader.DownloadScene>("Assets/Scenes/CommonEmptyScene.unity", null);
				yield return sceneHandle;
				if (sceneHandle.Current.succeed) {
					yield return sceneHandle.Current.ActiveScene();
				}
				else {
					goto Clean;
				}
				
				var entryClass = Type.GetType(conf.entryClass);
				currentApp = (AppBase)Activator.CreateInstance(entryClass);
				currentApp.Start();

				if ((int)handleSess.Current != 1) {
					MyDebug.LogFormat("network.ValidSession failed, will show login.");
					showLogin = true;
				}
				else {
					MyDebug.LogFormat("network.ValidSession succ.");
				}
				
				network.lastState = SessionBase.EnState.Initiation;

				if (showLogin)
					yield return currentApp.game.ShowLogin();
				else {
					var loginHandle = network.EnterGame(conf);
					yield return loginHandle;
					//登录失败
					if ((int)loginHandle.Current == 0) {
						//如果是登录大厅失败,返回登录界面
						if (conf == ins.conf.defaultGame) {
							yield return currentApp.game.ShowLogin();
						}
						//如果登录游戏失败,返回登录大厅
						else {
							yield return DoCheckUpdateAndRun(ins.conf.defaultGame, ip, false);
						}
					}
				}
				yield break;

			Clean:
				if (!succ) {
					MyDebug.LogFormat("CheckUpdateAndRun failed! will return to default game.", conf.name);
					yield return DoCheckUpdateAndRun(ins.conf.defaultGame, ip, false);
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

			network.AddMsgHandler(OnNetMsg);

			DoStart_();
		}

		public void OnNetMsg(object sender, NetEventArgs e)
		{
			if(e.cmd == (int)CommID.msg_sync_item) {
				var msgi = (msg_sync_item)e.msg;
				self.gamePlayer.items.SetKeyVal(int.Parse(msgi.item_id_), long.Parse(msgi.count_));
				self.gamePlayer.DispatchDataChanged();
			}
		}

		void CachedResources_()
		{
			for(int i = 1; i <= 10; i++) {
				int index = i;
				LoadAssets<Texture2D>($"Assets/ForReBuild/Res/PlazaUI/UserInfo/head/img_head_{i}.png", (task) => {
					headIcons.Add(index, task.Result);
				});
			}

			for (int i = 1; i <= 8; i++) {
				int index = i;
				LoadAssets<Texture2D>($"Assets/ForReBuild/Res/PlazaUI/UserInfo/headFrame/img_headframe_{i}.png", (task) => {
					headFrames.Add(index, task.Result);
				});
			}
		}

		void DoStart_()
		{
			conf.Start();
			if (defaultGameFromHost != "") conf.defaultGameName = defaultGameFromHost;
			if (conf.defaultGame == null) {
				throw new Exception($"default game is not exist.{conf.defaultGameName}");
			}
			CachedResources_();
			CheckUpdateAndRun(conf.defaultGame, progressFromHost, !autoLoginFromHost);
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
			base.Stop();
		}

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
		public string defaultGameFromHost;
		public bool autoLoginFromHost = true, disableNetwork = false;
		public Dictionary<int, Texture2D> headIcons = new Dictionary<int, Texture2D>();
		public Dictionary<int, Texture2D> headFrames = new Dictionary<int, Texture2D>();

		GameRunQueue runQueue = new GameRunQueue();
	}
}
