﻿using System;
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
		msg_same_account_login = 1005,
	}

	public enum CorRspID
	{
		msg_switch_game_server = 1028,
		msg_get_bank_info_ret = 1024,
		msg_user_head_and_headframe = 1040,
		msg_rank_data = 1122,

		msg_common_recv_present = 1050,
		msg_user_login_prize_list = 1037,
	}

	public enum GameRspID
	{
		msg_prepare_enter = 1218,
		msg_currency_change = 1007,
		msg_player_seat = 1110,
		msg_player_leave = 1112,
		msg_deposit_change2 = 1113,
		msg_server_parameter = 1107,
		msg_system_showdown = 1221,
		msg_get_public_data_ret = 1304,
		msg_player_hint = 1499,
	}

	public class msg_player_hint : msg_room_msg
	{
		public string hint_type_;  //1断线重连
	}

	public class msg_system_showdown : MsgBase
	{
		public string desc_;
	}

	public class msg_common_reply : MsgBase
	{
		public string rp_cmd_;    //回复的消息ID
		public string err_;       //错误码
		public string des_;           //描述
	}

	public class msg_handshake_ret : MsgBase
	{
		public string ret_;
	}
	public class msg_ping : MsgBase
	{

	}

	public class msg_rpc_call_ret : MsgBase
	{
		public string data_;
	}

	public class msg_player_info : MsgBase
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

	public class msg_get_bank_info_ret : MsgBase
	{
		public string bank_gold_;
		public string bank_gold_game_;
		public string psw_set_;
	}

	public class msg_channel_server : MsgBase
	{
		public string ip_;  //这个现在没用了,应该使用客户端检测出来的高防IP
		public string port_;    //服务器端口号
		public string for_game_;
		public string server_region_; //0点券服， 1正式服
	}

	public class msg_switch_game_server : MsgBase
	{
		public string ip_;
		public string port_;
	}

	public class msg_sync_item : MsgBase
	{
		public string item_id_;
		public string count_;
	}
	public class msg_prepare_enter : MsgBase
	{

	}

	public class msg_currency_change : MsgBase
	{
		public string credits_;           //玩家资金数 8字节
		public string why_;               //0,同步(总量)，1,每日登录奖励(变化量)，2，等级装奖励(变化量), 3,财神奖金, 5金币兑换完毕
	}

	public class msg_room_msg : MsgBase
	{
		int roomid_asdf_ = 0;
	}

	public class msg_player_seat : msg_room_msg
	{
		public string uid_;   //谁
		public string head_ico_;
		public string headframe_id_;
		public string uname_;
		public string pos_;//坐在哪个位置
		public string iid_;
		public string lv_;
		public string gold_;
	}

	//玩家离开坐位
	public class msg_player_leave : msg_room_msg
	{
		public string pos_;       //哪个位置离开游戏
		public string why_;       //why = 0,玩家主动退出游戏, 1 换桌退出游戏 2 游戏结束清场退出游戏， 3，T出游戏, 1000变为观战者
	}

	public class msg_server_parameter : MsgBase
	{
		public string id_;
		public string balance_with_;      //结算货币的ID
		public string enter_cap_;         //进入场次的最低货币限制
		public string enter_cap_top;      //进入场次的最低货币限制
		public string banker_set_;        //抢庄保证金
		public string bet_set_;           //最小压注金额
		public string enter_scene_;       //是否进入场次信息。1是，0服务器参数，2， 开始 3服务器参数配置结束
		public string maxseat_;           //最多人数
		public string require_seat_;      //最少人数
		public string params_;            //参数 
	}

	//掉线重连时,如果玩家还在游戏中,会发这个消息来通知玩家
	public class msg_is_ingame : MsgBase
	{
		public string roomid_;    //0-普通房间 1-比赛房间 2-好友房
		public string data_;      //如果是普通或好友房,这里是房间ID,如果是比赛房,这里是比赛ID
	}

	//在排队中
	public class msg_is_inqueue : MsgBase
	{
		public string phase_;
		public string pos_;
	}


	//玩家保证金变化
	public class msg_deposit_change2 : msg_room_msg
	{
		public enum dp
		{
			display_type_sync_gold,
			display_type_show_gold,
			display_type_gold_change,
			display_type_sync_exp = 10,
			display_type_sync_score = 11,
			display_type_sync_pri_score = 12,
		};
		public string credits_;           //玩家资金数 8字节
		public string why_;
		public string pos_;
		public string display_type_;      //0，不需要飘字 1,要飘字, 2 保证金变化
	}

	public class msg_get_public_data_ret : MsgBase
	{
		public string ret;
	}

	public class msg_same_account_login : MsgBase
	{

	}

	public class msg_user_head_and_headframe : MsgBase
	{
		public string head_ico_;      //头像ID
		public string headframe_id_;  //头像框ID
		public string nickname_;
	}

	public class msg_rank_data : MsgBase
	{
		public string uname_;         //昵称	
		public string head_ico_;      //头像
		public string headframe_id_;  //请忽略
		public string rank_type_;     //排行榜类型,和请求一样
		public string tag_;           //0新的开始，1后续数据
		public string data_;           //排行数据
	}

	public class msg_user_login_prize_list : MsgBase
	{
		public string sign_days_;         //总签到天数
		public string serial_days_;       //连续登录天数
		public string serial_state_;      //最多三位数（个位-3天，十位-6天，百位-9天  0-未领取 1-已领取）
		public string login_day_;         //当前第几天(1~7)
		public string login_state_;       //登录奖励状态：0-未领取 1-已领取
		public string is_auto_;           //是否自动请求
	}

	public class msg_common_recv_present : MsgBase
	{
		public string type_;
		public string item_;
	}
}
