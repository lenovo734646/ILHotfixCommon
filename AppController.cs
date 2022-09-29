using AssemblyCommon;
using AssemblyCommon.Bridges;
using Hotfix.Lobby;
using Hotfix.Model;
using LitJson;
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
using static Hotfix.Common.ResourceMonitor;

namespace Hotfix.Common
{
	//热更入口类
	public class App : ResourceMonitor
	{
		public App()
		{
			ins = this;
			AddChild(network);
			AddChild(audio);
		}

		public IEnumerator DoCheckUpdate(GameConfig conf, IShowDownloadProgress ip)
		{
			bool succ = false;
			if (conf.contentCatalog.Length > 0) {
				var address = conf.GetCatalogAddress(AddressablesLoader.usingUpdateUrl, Globals.resLoader.GetPlatformString());
				//下载过的catalog不用再下了

				var handleCatalog = Globals.resLoader.LoadAsync<AddressablesLoader.DownloadCatalog>(address, ip);
				yield return handleCatalog;
				//失败继续,这里不用goto
				//if (handleCatalog.Current.status != AsyncOperationStatus.Succeeded) goto Clean;
				MyDebug.LogFormat("DownloadDependency:{0}", conf.folder);
				var handleDep = Globals.resLoader.LoadAsync<AddressablesLoader.DownloadDependency>(conf.folder, ip);
				yield return handleDep;

				if (handleDep.Current.status != AsyncOperationStatus.Succeeded) goto Clean;

				succ = true;
			}
			else {
				throw new Exception($"Game {conf.name} Config Error, contentCatalog not set.");
			}

		Clean:
			if (!succ) {
				MyDebug.LogFormat("CheckUpdateAndRun failed! will return to default game.", conf.name);
				yield return Result.Failure;
			}
			else
				yield return Result.Success;
			
		}

		IEnumerator CoDoCheckUpdateAndRun(GameConfig conf, IShowDownloadProgress ip, bool showLogin)
		{
			MyDebug.LogFormat("===================>CheckUpdateAndRun showlogin={0}", showLogin);

			var chkUpdate = DoCheckUpdate(conf, ip);
			yield return chkUpdate;
			if ((Result)chkUpdate.Current != Result.Success) {
				goto Clean;
			}

			AppBase oldApp = currentApp;
			currentApp?.AboutToStop();
			//清理本游戏声音资源
			audio.StopAll();
			currentApp = null;
			//确保连接
			if (!disableNetwork) {
				MyDebug.LogFormat("network.ValidSession");
				network.progressOfLoading = ip;
				var handleSess = network.CoValidSession();
				yield return handleSess;

				if ((Result)handleSess.Current != Result.Success) {
					if (ins.conf.defaultGame == conf) {
						MyDebug.LogFormat("network.ValidSession failed, will show login.");
						showLogin = true;
					}
					else {
						MyDebug.LogFormat("network.ValidSession failed goto Clean");
						goto Clean;
					}
				}
			}
			else {
				showLogin = true;
			}

			var entryClass = Type.GetType(conf.entryClass);
			currentApp = (AppBase)Activator.CreateInstance(entryClass);
			currentApp.progressOfLoading = ip;
			currentApp.Start();
			AddChild(currentApp);

			yield return currentApp.WaitingForReady();

			network.lastState = SessionBase.EnState.Initiation;
			
			currentGameConfig = conf;

			if (showLogin)
				yield return currentApp.game.ShowLogin();
			else {
				var loginHandle = network.CoEnterGame(conf);
				yield return loginHandle;
				//登录失败
				if ((Result)loginHandle.Current == Result.Failure) {
					//如果是登录大厅失败,返回登录界面
					if (conf == ins.conf.defaultGame) {
						yield return currentApp.game.ShowLogin();
					}
					//如果登录游戏失败,返回登录大厅
					else {
						yield return CoDoCheckUpdateAndRun(ins.conf.defaultGame, ip, false);
					}
				}
			}

			//清理旧游戏资源
			oldApp?.Stop();
			yield return Result.Success;
			yield break;

		Clean:
			if (ins.conf.defaultGame != conf) {
				yield return CoDoCheckUpdateAndRun(ins.conf.defaultGame, ip, false);
			}
			else {
				ip?.Desc(LangNetWork.GameStartFailed);
				yield return Result.Failure;
			}
		}

		public IEnumerator CoCheckUpdateAndRun(GameConfig conf, IShowDownloadProgress ip, bool showLogin)
		{
			if (!runningGame_) {
				runningGame_ = true;
				yield return CoDoCheckUpdateAndRun(conf, ip, showLogin);
				runningGame_ = false;
			}
		}

		//只是为了ILRuntime能调用到函数
		public override void Start()
		{
			base.Start();
		}
		//只是为了ILRuntime能调用到函数
		public override void Update()
		{
			base.Update();
		}
		//只是为了ILRuntime能调用到函数
		public override void Stop()
		{
			base.Stop();
		}

		protected override IEnumerator OnStart()
		{
			MyDebug.LogFormat("Hotfix Module Begins.");
			//注册protobuf类
			ILRuntime_CLGT.Initlize();
			ILRuntime_CLPF.Initlize();
			ILRuntime_Global.Initlize();
			InstallMsgHandler();
			yield return DoStart_();
		}

		public void InstallMsgHandler()
		{
			network.RegisterMsgHandler((int)AccRspID.msg_same_account_login, (cmd, json) => {
				ins.disableNetwork = true;
				ViewPopup.Create(LangUITip.SameAccountLogin, ViewPopup.Flag.BTN_OK_ONLY, () => {
					ins.StartCor(ins.CoCheckUpdateAndRun(ins.conf.defaultGame, null, true), false);
				});
			}, this);

			network.RegisterMsgHandler((int)CommID.msg_sync_item, (cmd, json) => {
				msg_sync_item msg = JsonMapper.ToObject<msg_sync_item>(json);
				int itemId = int.Parse(msg.item_id_);
				//刷新大厅里的我
				if (self.items.ContainsKey(itemId)) {
					self.items[itemId] = int.Parse(msg.count_);
				}
				else {
					self.items.Add(itemId, int.Parse(msg.count_));
				}

				//刷新游戏里的我
				var gself = App.ins.currentApp.game.Self;
				if (gself != null) {
					if (gself.items.ContainsKey(itemId)) {
						gself.items[itemId] = int.Parse(msg.count_);
					}
					else {
						gself.items.Add(itemId, int.Parse(msg.count_));
					}
				}
			}, this);
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
			progressOfLoading = progressFromHost;
			progressOfLoading.Reset();

			StartCor(DoLazyUpdate(), false);

			yield return CachedResources_();
			yield return CoCheckUpdateAndRun(conf.defaultGame, progressFromHost, !autoLoginFromHost);
		}

		IEnumerator DoLazyUpdate()
		{
			while (true) {
				var arr = longPress.ToArray();
				for (int i = 0; i < arr.Count; i++) {
					var longp = arr[i];
					if (longp.Value.triggered) continue;
					if (!longp.Value.IsTimeout()) continue;
					longp.Value.Trigger();
				}

				LazyUpdate();

				yield return new WaitForSeconds(0.1f);
			}
		}

		public override string GetDebugInfo()
		{
			return "AppController";
		}

		public static App ins = null;
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

		public DictionaryCached<GameObject, LongPressData> longPress = new DictionaryCached<GameObject, LongPressData>();
		public Dictionary<int, Texture2D> headIcons = new Dictionary<int, Texture2D>();
		public Dictionary<int, Texture2D> headFrames = new Dictionary<int, Texture2D>();

		public AudioManager audio = new AudioManager();

		List<string> cachedCatalog = new List<string>();
		ControllerDefault runQueue = new ControllerDefault();
		bool runningGame_ = false;

	}
}
