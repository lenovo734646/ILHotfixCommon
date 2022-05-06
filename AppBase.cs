using AssemblyCommon;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Hotfix.Common
{
	//每个小游戏都有一个MyApp,
	public class AppBase : ResourceMonitor
	{
		//每个小游戏都有一个GameController
		public GameControllerBase game = null;
		public override void Start()
		{
			game?.Start();
		}

		public override void Stop()
		{
			game?.Stop();
			base.Stop();
		}

		public override void Update()
		{
			game?.Update();
		}
		public virtual IEnumerator ShowLogin()
		{
			yield return 0;
		}
	}
}
