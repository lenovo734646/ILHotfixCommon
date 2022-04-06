using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//由于类命名规则有点不一样,因为通信协议关系
namespace Hotfix.Common
{
	public enum CommID
	{
		msg_common_reply = 1001,
	}

	public enum GateReqID
	{
		msg_handshake = 99,
	}
	public enum GateRspID
	{
		msg_handshake_ret = 98,
	}
	
	public enum AccReqID
	{
		msg_user_login = 100,
		msg_user_register = 101,
		msg_change_account_info = 110,
		msg_get_verify_code = 111,
		msg_get_game_coordinate = 117,
		msg_change_psw = 1300,
	}

	public enum CorReqID
	{
		msg_join_channel = 102,
		msg_chat = 103,
		msg_leave_channel = 104,
		msg_user_login_channel = 107,

		msg_send_present = 114,
		msg_action_record = 116,
		msg_get_bank_info = 122,
		msg_set_bank_psw = 123,
		msg_bank_op = 124,
	}

	public class msg_base
	{
		public virtual int to_server() { return -1; }
		public string rpc_sequence_;
	}

	public class msg_rpc_ret
	{
		public int err_ = 0;
		public msg_base msg_;
	}

	public class msg_from_client : msg_base
	{
		public string sign_;
	}

	//网关握手
	public class msg_handshake_req : msg_from_client
	{
		public override int to_server() { return -1; }
		public string machine_id_;
	}

	public class msg_user_login_t : msg_from_client
	{
		public string acc_name_;
		public string pwd_hash_;
		public string machine_mark_;
	}

	//登录
	public class msg_user_login : msg_user_login_t
	{
		public override int to_server() { return 0; }
	}

	public class msg_user_register : msg_user_login
	{
		public string type_;  //0用户名,1手机，2邮箱 4重置密码 5手机绑定 6 银行密码修改 7游客登录
		public string verify_code;
		public string spread_from_; //渠道号。这里名称用得有点问题，先不改了
		public string spread_from2_; //推荐人号。
		public string nickname_;
		public string uid_;
		public string sn_;
		public string token_;
		public override int to_server() { return 0; }
	}

	public class msg_user_login_channel : msg_from_client
	{
		public string uid_;
		public string sn_;
		public string token_;
		public string device_;
		public string platform_;
		public string nickanme_;
		public override int to_server() { return 1; }
	}

	public class msg_get_game_coordinate : msg_from_client
	{
		public string uid_;
		public string gameid_;
		public override int to_server() { return 0; }
	}

	public class msg_chat : msg_from_client
	{
		public int channel_;       //频道id
		public string to_;            //密语对象,不是密语则为空
		public string content_;       //内容
		public override int to_server() { return 1; }
	}

	//更改用户数据
	public class msg_change_account_info : msg_from_client
	{
		public string gender_;
		public string byear_;
		public string bmonth_;
		public string bday_;
		public string address_;
		public string nick_name_;
		public string age_;
		public string mobile_;
		public string email_;
		public string idcard_;
		public string region1_, region2_, region3_;
		public override int to_server() { return 1; }
	}

	public class msg_get_verify_code : msg_from_client
	{
		public string type_;      //0图片验证码 1手机验证码 2 邮件验证码
		public string mobile_;
		public override int to_server() { return 0; }
	}

	public class msg_get_bank_info : msg_from_client
	{
		public override int to_server() { return 1; }
	}
	public class msg_bank_op : msg_from_client
	{
		public string psw_;       //密码
		public string op_;      //0,提取,1存入
		public string type_;        //0,K币,1K豆
		public string count_;
		public override int to_server() { return 1; }
	}

	public class msg_set_bank_psw : msg_from_client
	{
		public string func_;      //0-设置密码, 1-修改密码, 2-验证密码
		public string old_psw_;
		public string psw_;
		public override int to_server() { return 1; }
	}

	//送礼物给玩家,暂时只送钱
	public class msg_send_present : msg_from_client
	{
		public string channel_;
		public string present_id_;
		public string count_;
		public string to_;
		public override int to_server() { return 1; }
	}

	//加入聊天频道
	public class msg_join_channel : msg_from_client
	{
		public string channel_;
		public string sn_;
		public override int to_server() { return 1; }
	}

	//离开聊天频道
	public class msg_leave_channel : msg_from_client
	{
		public override int to_server() { return 1; }

	}

	//客户端行为埋点
	public class msg_action_record : msg_from_client
	{
		public enum opt
		{
			OP_TYPE_INSERT,
			OP_TYPE_UPDATE,
		};
		public string op_type_;       //0-新记录 1-更新
		public string action_id_;
		public string action_data_;
		public string action_data2_;
		public string action_data3_;
		public string action_data4_;
		public string action_data5_;
		public override int to_server() { return 0; }
	}

	public class msg_change_psw : msg_from_client
	{
		public string oldpsw_;
		public string psw_;
		public string uid_;
		public override int to_server() { return 0; }
	}

}
