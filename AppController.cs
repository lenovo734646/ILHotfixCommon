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
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

		public IEnumerator DoCheckUpdate(GameConfig conf, IShowDownloadProgress ip)
		{
			bool succ = false;
			if (conf.contentCatalog.Length > 0) {
				var address = conf.GetCatalogAddress(AddressablesLoader.usingUpdateUrl, Globals.resLoader.GetPlatformString());
				MyDebug.LogFormat("Get CatalogAddress:{0}, AddressablesLoader.usingUpdateUrl:{1}", address, AddressablesLoader.usingUpdateUrl);

				//下载过的catalog不用再下了
				if (!cachedCatalog.Contains(address)) {
					var handleCatalog = Globals.resLoader.LoadAsync<AddressablesLoader.DownloadCatalog>(address, ip);
					yield return handleCatalog;
					if (handleCatalog.Current.status != AsyncOperationStatus.Succeeded) goto Clean;
					cachedCatalog.Add(address);

					var handleDep = Globals.resLoader.LoadAsync<AddressablesLoader.DownloadDependency>(conf.folder, ip);
					yield return handleDep;

					if (handleDep.Current.status != AsyncOperationStatus.Succeeded) goto Clean;
					MyDebug.LogFormat("DownloadDependency:{0}", address);
				}
				succ = true;
			}
			else {
				throw new Exception($"Game {conf.name} Config Error, contentCatalog not set.");
			}

		Clean:
			if (!succ) {
				MyDebug.LogFormat("CheckUpdateAndRun failed! will return to default game.", conf.name);
				yield return -1;
			}
			else
				yield return 0;
			
		}

		IEnumerator DoCheckUpdateAndRun(GameConfig conf, IShowDownloadProgress ip, bool showLogin)
		{
			MyDebug.LogFormat("===================>CheckUpdateAndRun showlogin={0}", showLogin);

			var chkUpdate = DoCheckUpdate(conf, ip);
			yield return chkUpdate;
			if ((int)chkUpdate.Current != 0) {
				goto Clean;
			}

			if (currentApp != null) currentApp.Stop();
			currentApp = null;

			//确保连接
			MyDebug.LogFormat("network.ValidSession");
			network.progressOfLoading = ip;
			var handleSess = network.ValidSession();
			yield return handleSess;

			if ((int)handleSess.Current != 1) {
				if (ins.conf.defaultGame == conf) {
					MyDebug.LogFormat("network.ValidSession failed, will show login.");
					showLogin = true;
				}
				else {
					MyDebug.LogFormat("network.ValidSession failed goto Clean");
					goto Clean;
				}
			}

			var entryClass = Type.GetType(conf.entryClass);
			currentApp = (AppBase)Activator.CreateInstance(entryClass);
			currentApp.progressOfLoading = ip;
			currentApp.Start();
			yield return currentApp.WaitingForReady();

			network.lastState = SessionBase.EnState.Initiation;
			
			currentGameConfig = conf;

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
			if (ins.conf.defaultGame != conf) {
				yield return DoCheckUpdateAndRun(ins.conf.defaultGame, ip, false);
			}
			else {
				ip?.Desc(LangNetWork.GameStartFailed);
			}
		}

		public IEnumerator CheckUpdateAndRun(GameConfig conf, IShowDownloadProgress ip, bool showLogin)
		{
			yield return DoCheckUpdateAndRun(conf, ip, showLogin);
		}

		public override void Start()
		{
			MyDebug.LogFormat("Hotfix Module Begins.");
			//注册protobuf类
			ILRuntime_CLGT.Initlize();
			ILRuntime_CLPF.Initlize();
			ILRuntime_Global.Initlize();

			network.AddMsgHandler(OnNetMsg);

			runQueue.StartCor(DoStart_(), true);
		}

		public void OnNetMsg(object sender, NetEventArgs e)
		{
			if(e.cmd == (int)CommID.msg_sync_item) {
				var msgi = (msg_sync_item)e.msg;
				self.gamePlayer.items.SetKeyVal(int.Parse(msgi.item_id_), long.Parse(msgi.count_));
				self.gamePlayer.DispatchDataChanged();
			}
		}

		IEnumerator CachedResources_()
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
			yield return WaitingForReady();
		}

		IEnumerator DoStart_()
		{
			conf.Start();
			if (defaultGameFromHost != "") conf.defaultGameName = defaultGameFromHost;
			if (conf.defaultGame == null) {
				throw new Exception($"default game is not exist.{conf.defaultGameName}");
			}
			yield return CachedResources_();
			yield return CheckUpdateAndRun(conf.defaultGame, progressFromHost, !autoLoginFromHost);
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
		List<string> cachedCatalog = new List<string>();
		GameRunQueue runQueue = new GameRunQueue();
	}
}
