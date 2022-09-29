using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix.Common.MultiPlayer
{
	public enum GameMultiReqID
	{
		msg_set_bets_req=5,
		msg_pk_req=6,
		msg_open_card_req=7,
		msg_show_card_req=8,

		msg_turn_to_do =11,
		msg_cards=12,
		msg_player_setbet=13,
		msg_pk_result=14,
		msg_game_report=15,
		msg_card_match_result=16,
		msg_new_turn=17,
		msg_card_opened=18,
		msg_cards_complete=19,
		msg_promote_banker=20,

		
	}
	//需要客户端操作了。
	public class msg_turn_to_do:msg_room_msg
    {
		public string uid_;//玩家UID.
		public string wait_key_;//操作标识，需要带回服务器
		public string must_greater_;//最低下注额
		public string time_left_ ;//超时时间
		public string func_ ;//0请下注, 1.以后再说目前没有
	}

	//发牌消息
	public class msg_cards :  msg_room_msg
    {
		public string uid_;//玩家UID.
		public string cards_;//玩家牌
		public string type_;//1-亮牌

	}

	public class msg_cards_complete :  msg_room_msg
    {
		public string data_;//第几轮发牌结束。有些游戏发牌要发好几次的
	}
	//玩家下注
	public class msg_player_setbet :  msg_room_msg
    {
		public string uid_;//玩家UID
		public string bet_type_;//下注类型 0-底注,1-跟注, 2-加注, 3-allin. 4-弃牌
		public string bet_;//玩家下注额

	}
	//牌形结果
	public class msg_card_match_result : msg_room_msg
    {
		public string uid_;//哪个玩家
		public string card_type_;//0.散牌，1.对子，2.顺子，3.同花，4.同花顺，5.豹子
	}
	//PK比牌结果
	public class msg_pk_result :  msg_room_msg
    {
		public string from_;//发起人
		public string to_;//目标
		public string winner_;

	}
	//最后结果
	public class msg_game_report :  msg_room_msg
    {
		public string uid_;//谁
		public string win_;//赢了多少钱。负的表示输了

	}
	//新一轮下注开始，(超过最大下注轮数会强制结算游戏）
	public class msg_new_turn :  msg_room_msg
    {
		public string turn_; //第几轮下注了
	}
	//广播玩家看牌了
	public class msg_card_opened : msg_room_msg
    {
		public string uid_;//谁
	}
	//本轮庄家
	public class msg_promote_banker : msg_room_msg
    {
		public string pos_;
	}
	//下注
	public class msg_set_bets_req :  MsgBase
    {
		public string wait_key_;//事务标识，由msg_turn_to_do传来
		public string bet_type_;//0底注 1-跟注,2-加注,3-allin 4 弃牌, 5
		public string bet_;

	}
	//PK请求
	public class msg_pk_req : MsgBase
	{
		public string wait_key_;	//事务标识，由msg_turn_to_do传来
		public string target_;  //PK目标
	}
	//看牌请求
	public class msg_open_card_req : MsgBase
	{

    }

	public class msg_show_card_req : MsgBase
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


	

}
