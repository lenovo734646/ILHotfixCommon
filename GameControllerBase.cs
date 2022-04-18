using AssemblyCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix.Common
{

	//每个小游戏的GameController基类
	//用来管理每个小游戏的逻辑,包括视图管理,游戏逻辑,网络消息处理,流程处理等等.
	//总之,和小游戏相关的东西,都在这里开始
	//
	public abstract class GameControllerBase : ControllerBase
	{
		//创建和管理View
		public T OpenView<T>() where T : ViewBase, new()
		{
			T ret = new T();
			views_.Add(ret);
			ret.Start();
			return ret;
		}
		
		public void OnViewClosed(ViewBase view)
		{
			views_.Remove(view);
		}

		public virtual void OnEnterGameSucc()
		{
			prepared_ = true;
		}

		public void ReleaseWhenClose(AddressablesLoader.LoadTaskBase task)
		{
			resourceLoader_.Add(task);
		}
		
		public override void Update()
		{
			if (prepared_) {
				foreach (var view in views_) {
					view.Update();
				}
			}
		}

		public override void Stop()
		{
			List<ViewBase> cpl = new List<ViewBase>(views_);
			foreach (var view in cpl) {
				view.Close();
			}

			foreach (var tsk in resourceLoader_) {
				tsk.Release();
			}

			resourceLoader_.Clear();
		}

		//资源加载器,在半闭本窗口的时候,需要释放资源引用.
		List<AddressablesLoader.LoadTaskBase> resourceLoader_ = new List<AddressablesLoader.LoadTaskBase>();
		List<ViewBase> views_ = new List<ViewBase>();
		bool prepared_ = true;
	}

}
