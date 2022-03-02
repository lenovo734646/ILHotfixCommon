using AssemblyCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix.Common
{
	public class ReconnectRoutine
	{
		MySocket sock_;
		public ReconnectRoutine(MySocket s)
		{
			sock_ = s;
		}

		public IEnumerator Start()
		{
			AppController.ins.net
			yield return 0;
		}
	}
}
