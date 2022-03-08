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
	public abstract class AppBase
	{
		public abstract void Start();
		public abstract void Update();
		public abstract void Stop();
	}

	//热更入口类
	public class AppController
	{
		public static AppController ins = null;
		public Config conf = new Config();
		public AppBase currentApp = null;
		//进度指示器,由调用者提供
		public IShowDownloadProgress ips;
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

		public void Start()
		{
			this.StartCor(DoStart_(), false);
		}

		IEnumerator DoStart_()
		{
			conf.Start();
			SwitchGame(conf.defaultGame);
			yield return 0;
		}

		public void Update()
		{
			net.Update();
			if(currentApp != null) currentApp.Update();
		}

		public void Stop()
		{
			if (currentApp != null) currentApp.Stop();
			net.Stop();
		}
	}
}
