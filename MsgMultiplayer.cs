using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix.Common
{
	public enum GameMultiReqID
	{
		msg_cancel_banker_req = 3,
		msg_apply_banker_req = 4,
		msg_set_bets_req = 5,
		msg_get_player_count = 6,
		msg_get_lottery_record =  7,
		msg_get_banker_ranking = 8,
		msg_clear_my_bets = 9,
	}

	public class msg_set_bets_req : msg_from_client
	{
		//筹码ID
		public int pid_;
		//压注项
		public int present_id_;
	}

	public class msg_clear_my_bets : msg_from_client
	{
		
	}

	public class msg_apply_banker_req : msg_from_client
	{

	}

	public class msg_cancel_banker_req : msg_from_client
	{

	}

	public enum GameMultiRspID
	{
		msg_state_change = 10,
		msg_rand_result = 11,
		msg_banker_promote = 12,
		msg_game_report = 13,
		msg_banker_deposit_change = 14,
		msg_new_banker_applyed = 15,
		msg_apply_banker_canceled = 16,
		msg_player_setbet = 17,
		msg_my_setbet = 18,
		msg_last_random = 19,
		msg_return_player_count = 20,
		msg_return_lottery_record = 21,
		msg_broadcast_msg = 22,
		msg_banker_ranking_ret = 23,
		msg_chat_result = 24,
		msg_server_parameter_2 = 25,
		msg_send_color = 26,
		msg_random_result_slwh = 27,
		msg_cards = 28,
		msg_card_match_result = 29,
		msg_player_win = 30,
		msg_last_random_2 = 31,
		msg_brnn_result = 32,
		msg_bjl_result = 33,
		msg_game_info_longhu = 34,
		msg_game_info = 1120,
	}


	public class msg_banker_promote : msg_player_seat
	{
		public string deposit_;                //资金池 8字节
		public string is_sys_banker_;         //是否是系统庄 1是
		public string banker_turn;            //连庄次数
		public string totalwin_;
	}

	public class msg_game_report : msg_room_msg
	{
		public string uid_;
		public string nickname_;
		public string pay_;               //付出多少钱
		public string actual_win_;        //实际赢多少	8字节
		public string should_win_;        //理论上赢多少，由于庄家爆庄，可能会少赔 8字节
		public string banker_win;         //庄家赢了多少

	}

	public class msg_banker_deposit_change : msg_currency_change
	{
		public string turn_left_;
		public string total_win_;
		public string this_win_;
	}

	//状态机变化
	public class msg_state_change : msg_room_msg
	{
		public string change_to_;         //0开始下注, 1,开始转转,2,休息时间
		public string time_left;
		public string time_total_;
		public string data_;              //状态机客外数据,比如"removplayer"
	}

	public class msg_player_setbet : msg_room_msg
	{
		public string uid_;
		public string pid_;           //筹码id
		public string present_id_;    //美女id
		public string max_setted_;    //这个注一共已下注
	}

	public class msg_my_setbet : msg_room_msg
	{
		public string pid_;           //筹码id
		public string present_id_;
		public string set_;
		public string my_total_set_;  //我的本注总计
		public string total_set_;     //本注总计 
	}


	public class msg_send_color : msg_room_msg
	{
		public string colors_;
		public string rates_;
	}

	public class msg_random_result_base : msg_room_msg
	{

	}

	public class msg_last_random_base : msg_room_msg
	{

	}

	public class msg_random_result_slwh : msg_random_result_base
	{
		public string turn_;
		public string animal_pid_;
		public string bigsmall_;
		public string color_;
		public string animals_;
	}


	public class msg_cards : msg_room_msg
	{
		public int step_;
		public int pos_;               //位置
		public string cards_;    //0-12，方块A-K,13-25, 梅花A-K, 26-38,红桃A-K, 39-51,黑桃A-K, 52 小王,53大王
	}

	//牌型匹配结果
	public class msg_card_match_result : msg_room_msg
	{
		public string pos_;               //位置
		public string niuniu_point_;  //牛牛点数, 
		public string niuniu_level_;  //牛牛级别, <0-无牛，0-普通牛牛,1-4花牛,2-5花牛,3-炸弹,4-5小牛
		public string card3_;  //配0的3张牌
		public string card2_;  //配点的2点张牌
		public string replace_id1_;   //大王替换成
		public string replace_id2_;   //小王替换成
	}

	public class msg_player_win : msg_room_msg
	{
		public string uid_;
		public string betid_;     //庄注项
		public string win_;       //赢多少
	}

	public class msg_brnn_result : msg_random_result_base
	{
		public string wins;
	}

	public class msg_bjl_result : msg_random_result_base
	{
		public string card_result_; //庄和闲点数
	}

	public class msg_game_info_longhu : msg_base
	{
		public string dafuhao_;       //大富豪位置
		public string shensuanz_;     //神算子位置
		public string accurate_;      //准确率
		public string pred_;          //预测龙赢概率
	}

	//开奖历史记录
	public class msg_last_random_slwh : msg_last_random_base
	{
		public string pids_;
		public string bigsmall_;
		public string color_;
		public string ani_;
		public string turn_;
		public string data_;
	}

	public class msg_game_info : msg_base
	{
		public string turn_;
		//出奖项ID
		public string pids_;
		//出奖数量
		public string counts_;
	}

}
