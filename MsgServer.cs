using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix.Common
{
	public enum AccRspID
	{
		msg_user_login_ret = 1000,
		msg_player_info = 1002,
		msg_channel_server = 1009,
	}

	public class msg_common_reply : msg_base
	{
		public string rp_cmd_;    //回复的消息ID
		public string err_;       //错误码
		public string des_;           //描述
	}

	public enum CorRspID
	{
		msg_get_bank_info_ret = 1024,
	}

	public class msg_handshake_ret : msg_base
	{
		public string ret_;
	}
	public class msg_ping : msg_base
	{

	}

	public class msg_player_info : msg_base
	{
		public string uid_;
		public string nickname_;
		public string iid_;
		public string gold_;
		public string vip_level_;
		public string channel_;
		public string gender_;
		public string level_;
		public string isagent_;
		public string isinit_;
	}

	public class msg_user_login_ret : msg_player_info
	{
		public string token_;
		public string sequence_;
		public string email_;     //email
		public string phone;                  //手机号
		public string address_;           //地址
		public string game_gold_;                 //游戏币
		public string game_gold_free_;
		public string email_verified_;        //邮箱地址已验证 0未验证，1已验证
		public string phone_verified_;        //手机号验已验证 0未验证，1已验证
		public string byear_, bmonth_, bday_; //出生年月日
		public string region1_, region2_, region3_;
		public string age_;
		public string idcard_;                //身份证号
		public string app_key_;
		public string party_name_;
		public string headico_;
	}

	public class msg_get_bank_info_ret : msg_base
	{
		public string bank_gold_;
		public string bank_gold_game_;
		public string psw_set_;
	}

	public class msg_channel_server : msg_base
	{
		public string ip_;	//这个现在没用了,应该使用客户端检测出来的高防IP
		public string port_;	//服务器端口号
		public string for_game_;
		public string server_region_; //0点券服， 1正式服
	}

	public class msg_sync_item : msg_base
	{
		public string item_id_;
		public string count_;
	}
}
