using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix.Common
{
	public class AccountInfo
	{
		public enum LoginType
		{
			Unknown = 0,            //未知
			Guest = 1,              //游客
			Phone = 2,             //手机
			QQ = 3,                 //QQ
			Wechat = 4,             //微信
			Facebook = 5,           //Facebook
			GooglePlay = 6,         //GooglePlay
			GameCenter = 7,         //GameCenter
		}

		public LoginType loginType;
		public int iid;

		public string accountName;
		public string psw;
		public string nickName;
		public string headPic;
	}
}
