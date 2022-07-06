using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix.Common.Slot
{
	public enum GameSlotReqID
	{
		msg_set_bets_req = 5,
		msg_start_random_req = 12,
		msg_player_game_info_req = 13,
		msg_get_game_record = 14,
		msg_select_lines = 15,
		msg_get_luck_player = 16,
		msg_get_luck_player_data_req = 17,
	}

	public enum GameSlotRspID
	{
		msg_player_setbet_slot = 5,
		msg_random_present_ret = 6,
		msg_player_game_info_ret = 7,
		msg_bigsmall_result = 9,
		msg_cash_pool_sync = 10,
		msg_get_game_record_ret = 11,
		msg_smary_result = 12,
		msg_hylj_gameinfo = 13,
		msg_get_luck_player_ret = 14,
		msg_select_lines_2 = 15,
		msg_luck_player = 16,
		msg_random_present_ret_record = 17,
	};

	public class msg_start_random_req : MsgBase
	{
		public enum ReqType
		{
			rqt_normal_roll, // 正常开始
			rqt_bet_big,   // 押大
			rqt_bet_small, // 押小
			rqt_bet_same,  // 押和
			rqt_get_score,       // 收分
			rqt_get_rank,        // 收每日排行榜励
			rqt_recv_rank_reward, // 收每周排行榜奖励
			rqt_get_smary,  //小玛丽
			rqt_get_bigsmall_record,
		};

		public int view_present_; //0 正常开始， 1 押大，2 押小, 3 押和 4 收分, 5,收每日排行榜励,6 收每周排行榜奖励
	}

	public class msg_get_luck_player : MsgBase
	{
		public int type_; //0新入围,1历史排行,2获取子数据, 3.记录播放次数，4,记录喜欢, 5,获取播放和喜欢次数
		public string instance_;
	}

	public class msg_set_bets_req : MsgBase
	{
		public int pid_; // 奖项id,顺时针递增。从1开始，12点方向
		public int count_;
	}

	public class msg_get_luck_player_data_req : MsgBase
	{
		public string instance_;
	}

	/// <summary>
	/// 服务器回复消息
	/// </summary>
	public class msg_random_present_ret : msg_room_msg
	{
		public string pid_;
		public string result_;  //有额外翻牌的机会次数. 小马丽的机会次数,免费游戏次数
		public string result2nd_;  //特殊数据，九线777随机出来的倍数
		public string random_item;  //15个图标随机结果
		public string win_; //总共赢了多少
		public string max_rate_; //最大中奖倍数
		public string hiticons_, hitfactors_;
		public string vx, vy;
		public string winlines_;
	}
	
	public class msg_random_present_ret_record: msg_random_present_ret
	{
		public string issub_; //是不是子游戏项
	}

	public class msg_get_luck_player_ret : MsgBase
	{
		public string create_time_;
		public string nickname_;
		public string headico_;
		public string viplv_;
		public string type_;      //1 bigwin,2钻石win 3,宝箱win
		public string gold_init_; //当时金币
		public string lines_;     //当时选线数
		public string betset_;    //当时总下注
		public string instance_; //记录标识.
		public string issub_;     //1表示子数据，免费游戏里的每一次数据
		public string viewed_;
		public string my_favor_;
		public string total_favor_;
		public string win_;
	}

	public class msg_bigsmall_result : msg_room_msg
	{
		public string result_;    //0-大,1-和,2-小
		public string win_;     //赢了多少钱
	}

	public class msg_luck_player : MsgBase
	{
		public string nickname_;
		public string viplv_;
	}

	public class msg_player_setbet_slot : msg_room_msg
	{
		public string uid_;
		public string pid_;
		public string max_setted_; //这个注一共已下注
		public string present_id_;    //美女id
	}

	public class msg_select_lines : msg_room_msg
	{
		public string lines_;
	}

	public class msg_hylj_gameinfo:msg_room_msg
	{
		public string free_game_;
	}
}
