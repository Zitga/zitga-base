using Cysharp.Threading.Tasks;
using Zitga.LocalizeTools;

public static class R
{

    public static class mail
    {
        public static UniTask<string> key => Localization.Current.Get("mail", "key");

        public static UniTask<string> vip_reward_subject => Localization.Current.Get("mail", "vip_reward_subject");

        public static UniTask<string> vip_reward_content => Localization.Current.Get("mail", "vip_reward_content");

        public static UniTask<string> friend_boss_last_hit_reward_subject => Localization.Current.Get("mail", "friend_boss_last_hit_reward_subject");

        public static UniTask<string> friend_boss_last_hit_reward_content => Localization.Current.Get("mail", "friend_boss_last_hit_reward_content");

        public static UniTask<string> friend_boss_cleared_reward_subject => Localization.Current.Get("mail", "friend_boss_cleared_reward_subject");

        public static UniTask<string> friend_boss_cleared_reward_content => Localization.Current.Get("mail", "friend_boss_cleared_reward_content");

        public static UniTask<string> friend_season_ranking_reward_subject => Localization.Current.Get("mail", "friend_season_ranking_reward_subject");

        public static UniTask<string> friend_season_ranking_reward_content => Localization.Current.Get("mail", "friend_season_ranking_reward_content");

        public static UniTask<string> arena_season_ranking_reward_subject => Localization.Current.Get("mail", "arena_season_ranking_reward_subject");

        public static UniTask<string> arena_season_ranking_reward_content => Localization.Current.Get("mail", "arena_season_ranking_reward_content");

        public static UniTask<string> guild_boss_daily_reward_subject => Localization.Current.Get("mail", "guild_boss_daily_reward_subject");

        public static UniTask<string> guild_boss_daily_reward_content => Localization.Current.Get("mail", "guild_boss_daily_reward_content");

        public static UniTask<string> guild_boss_monthly_reward_subject => Localization.Current.Get("mail", "guild_boss_monthly_reward_subject");

        public static UniTask<string> guild_boss_monthly_reward_content => Localization.Current.Get("mail", "guild_boss_monthly_reward_content");

        public static UniTask<string> guild_dungeon_cleared_reward_subject => Localization.Current.Get("mail", "guild_dungeon_cleared_reward_subject");

        public static UniTask<string> guild_dungeon_cleared_reward_content => Localization.Current.Get("mail", "guild_dungeon_cleared_reward_content");

        public static UniTask<string> guild_dungeon_season_reward_subject => Localization.Current.Get("mail", "guild_dungeon_season_reward_subject");

        public static UniTask<string> guild_dungeon_season_reward_content => Localization.Current.Get("mail", "guild_dungeon_season_reward_content");

        public static UniTask<string> guild_war_battle_reward_subject => Localization.Current.Get("mail", "guild_war_battle_reward_subject");

        public static UniTask<string> guild_war_battle_reward_content => Localization.Current.Get("mail", "guild_war_battle_reward_content");

        public static UniTask<string> guild_war_season_reward_subject => Localization.Current.Get("mail", "guild_war_season_reward_subject");

        public static UniTask<string> guild_war_season_reward_content => Localization.Current.Get("mail", "guild_war_season_reward_content");

        public static UniTask<string> purchase_raw_pack_subject => Localization.Current.Get("mail", "purchase_raw_pack_subject");

        public static UniTask<string> purchase_raw_pack_content => Localization.Current.Get("mail", "purchase_raw_pack_content");

        public static UniTask<string> purchase_subscription_pack_instant_subject => Localization.Current.Get("mail", "purchase_subscription_pack_instant_subject");

        public static UniTask<string> purchase_subscription_pack_instant_content => Localization.Current.Get("mail", "purchase_subscription_pack_instant_content");

        public static UniTask<string> purchase_subscription_pack_daily_subject => Localization.Current.Get("mail", "purchase_subscription_pack_daily_subject");

        public static UniTask<string> purchase_subscription_pack_daily_content => Localization.Current.Get("mail", "purchase_subscription_pack_daily_content");

        public static UniTask<string> purchase_progress_pack_subject => Localization.Current.Get("mail", "purchase_progress_pack_subject");

        public static UniTask<string> purchase_progress_pack_content => Localization.Current.Get("mail", "purchase_progress_pack_content");

        public static UniTask<string> purchase_limited_pack_subject => Localization.Current.Get("mail", "purchase_limited_pack_subject");

        public static UniTask<string> purchase_limited_pack_content => Localization.Current.Get("mail", "purchase_limited_pack_content");

        public static UniTask<string> purchase_event_bundle_pack_subject => Localization.Current.Get("mail", "purchase_event_bundle_pack_subject");

        public static UniTask<string> purchase_event_bundle_pack_content => Localization.Current.Get("mail", "purchase_event_bundle_pack_content");

        public static UniTask<string> purchase_event_hot_deal_subject => Localization.Current.Get("mail", "purchase_event_hot_deal_subject");

        public static UniTask<string> purchase_event_hot_deal_content => Localization.Current.Get("mail", "purchase_event_hot_deal_content");

        public static UniTask<string> purchase_subscription_pack_trial_subject => Localization.Current.Get("mail", "purchase_subscription_pack_trial_subject");

        public static UniTask<string> purchase_subscription_pack_trial_content => Localization.Current.Get("mail", "purchase_subscription_pack_trial_content");

        public static UniTask<string> purchase_first_time_reward_subject => Localization.Current.Get("mail", "purchase_first_time_reward_subject");

        public static UniTask<string> purchase_first_time_reward_content => Localization.Current.Get("mail", "purchase_first_time_reward_content");

        public static UniTask<string> event_arena_subject => Localization.Current.Get("mail", "event_arena_subject");

        public static UniTask<string> event_arena_content => Localization.Current.Get("mail", "event_arena_content");

        public static UniTask<string> event_casino_subject => Localization.Current.Get("mail", "event_casino_subject");

        public static UniTask<string> event_casino_content => Localization.Current.Get("mail", "event_casino_content");

        public static UniTask<string> event_hero_collect_subject => Localization.Current.Get("mail", "event_hero_collect_subject");

        public static UniTask<string> event_hero_collect_content => Localization.Current.Get("mail", "event_hero_collect_content");

        public static UniTask<string> event_prophet_tree_subject => Localization.Current.Get("mail", "event_prophet_tree_subject");

        public static UniTask<string> event_prophet_tree_content => Localization.Current.Get("mail", "event_prophet_tree_content");

        public static UniTask<string> event_summon_subject => Localization.Current.Get("mail", "event_summon_subject");

        public static UniTask<string> event_summon_content => Localization.Current.Get("mail", "event_summon_content");

        public static UniTask<string> event_tavern_subject => Localization.Current.Get("mail", "event_tavern_subject");

        public static UniTask<string> event_tavern_content => Localization.Current.Get("mail", "event_tavern_content");

        public static UniTask<string> event_mid_autumn_subject => Localization.Current.Get("mail", "event_mid_autumn_subject");

        public static UniTask<string> event_mid_autumn_content => Localization.Current.Get("mail", "event_mid_autumn_content");

        public static UniTask<string> email_change_password_subject => Localization.Current.Get("mail", "email_change_password_subject");

        public static UniTask<string> email_change_password_content => Localization.Current.Get("mail", "email_change_password_content");

        public static UniTask<string> email_validation_subject => Localization.Current.Get("mail", "email_validation_subject");

        public static UniTask<string> email_validation_content => Localization.Current.Get("mail", "email_validation_content");

        public static UniTask<string> email_sender_name => Localization.Current.Get("mail", "email_sender_name");

        public static UniTask<string> welcome_mail_title => Localization.Current.Get("mail", "welcome_mail_title");

        public static UniTask<string> welcome_mail_content => Localization.Current.Get("mail", "welcome_mail_content");

        public static UniTask<string> event_halloween_subject => Localization.Current.Get("mail", "event_halloween_subject");

        public static UniTask<string> event_halloween_content => Localization.Current.Get("mail", "event_halloween_content");

        public static UniTask<string> event_black_friday_subject => Localization.Current.Get("mail", "event_black_friday_subject");

        public static UniTask<string> event_black_friday_content => Localization.Current.Get("mail", "event_black_friday_content");

        public static UniTask<string> event_new_year_subject => Localization.Current.Get("mail", "event_new_year_subject");

        public static UniTask<string> event_new_year_content => Localization.Current.Get("mail", "event_new_year_content");

    }
}