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
	public class AppBase : ControllerBase
	{
	}

	//热更入口类
	public class AppController : ControllerBase
	{
		public static AppController ins = null;
		public Config conf = new Config();
		public AppBase currentApp = null;
		//进度指示器,由宿主工程设置
		public IShowDownloadProgress progressFromHost;
		public NetWorkController net = new NetWorkController();
		public SelfPlayer self = new SelfPlayer();
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
			net.Update();
			if(currentApp != null) currentApp.Update();
		}

		public override void Stop()
		{
			if (currentApp != null) currentApp.Stop();
			net.Stop();
		}
	}
}
