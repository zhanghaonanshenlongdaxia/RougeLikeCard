using System;
using QFramework;

namespace CardGame
{
    /// <summary>
    /// 游戏时间模型 — 觅长生式年月日+时辰
    /// 1年=12月，1月=30日，1日=12时辰（子丑寅卯辰巳午未申酉戌亥）
    /// </summary>
    public interface IGameTimeModel : IModel
    {
        /// <summary>年（从1开始）</summary>
        BindableProperty<int> Year { get; }
        /// <summary>月（1-12）</summary>
        BindableProperty<int> Month { get; }
        /// <summary>日（1-30）</summary>
        BindableProperty<int> Day { get; }
        /// <summary>时辰（0-11，子=0 丑=1 寅=2 卯=3 辰=4 巳=5 午=6 未=7 申=8 酉=9 戌=10 亥=11）</summary>
        BindableProperty<int> Shichen { get; }

        /// <summary>纪元年号（如"乾元"）</summary>
        string EraName { get; set; }
        /// <summary>起始年偏移（显示用：Year + Offset = 纪元年数）</summary>
        int EraStartYearOffset { get; set; }

        /// <summary>游戏开始至今的总天数（用于事件时效比较）</summary>
        BindableProperty<int> TotalDays { get; }
    }

    public class GameTimeModel : AbstractModel, IGameTimeModel
    {
        public BindableProperty<int> Year { get; } = new BindableProperty<int>(1);
        public BindableProperty<int> Month { get; } = new BindableProperty<int>(1);
        public BindableProperty<int> Day { get; } = new BindableProperty<int>(1);
        public BindableProperty<int> Shichen { get; } = new BindableProperty<int>(4); // 辰时开局

        public string EraName { get; set; } = "乾元";
        public int EraStartYearOffset { get; set; } = 36; // 乾元三十七年 = Year(1) + 36

        public BindableProperty<int> TotalDays { get; } = new BindableProperty<int>(0);

        protected override void OnInit()
        {
        }

        /// <summary>重置到开局时间</summary>
        public void ResetToStart()
        {
            Year.Value = 1;
            Month.Value = 1;
            Day.Value = 1;
            Shichen.Value = 4;
            TotalDays.Value = 0;
        }
    }

    /// <summary>时辰工具</summary>
    public static class ShichenUtil
    {
        public static readonly string[] Names =
        {
            "子时", "丑时", "寅时", "卯时", "辰时", "巳时",
            "午时", "未时", "申时", "酉时", "戌时", "亥时"
        };

        /// <summary>时辰对应现代小时段描述</summary>
        public static readonly string[] Ranges =
        {
            "23:00-1:00", "1:00-3:00", "3:00-5:00", "5:00-7:00", "7:00-9:00", "9:00-11:00",
            "11:00-13:00", "13:00-15:00", "15:00-17:00", "17:00-19:00", "19:00-21:00", "21:00-23:00"
        };

        public static string GetName(int shichen)
        {
            return Names[Math.Clamp(shichen, 0, 11)];
        }
    }

    /// <summary>中文年月日显示工具</summary>
    public static class ChineseDateUtil
    {
        static readonly string[] MonthNames =
        {
            "正月", "二月", "三月", "四月", "五月", "六月",
            "七月", "八月", "九月", "十月", "冬月", "腊月"
        };

        /// <summary>中文数字（1-99足够）</summary>
        public static string ToChineseNumber(int n)
        {
            if (n <= 0) return "零";
            if (n < 10) return "一二三四五六七八九"[n - 1].ToString();
            if (n == 10) return "十";
            if (n < 20) return "十" + "一二三四五六七八九"[n % 10 - 1];
            if (n % 10 == 0) return "一二三四五六七八九"[n / 10 - 1] + "十";
            return "一二三四五六七八九"[n / 10 - 1] + "十" + "一二三四五六七八九"[n % 10 - 1];
        }

        public static string GetMonthName(int month)
        {
            return MonthNames[Math.Clamp(month, 1, 12) - 1];
        }

        /// <summary>日显示：初一~初十、十一~十九、二十、廿一~廿九、三十</summary>
        public static string GetDayName(int day)
        {
            day = Math.Clamp(day, 1, 30);
            if (day <= 10) return "初" + "一二三四五六七八九十"[day - 1];
            if (day < 20) return "十" + (day == 10 ? "" : "一二三四五六七八九"[day % 10 - 1].ToString());
            if (day == 20) return "二十";
            if (day < 30) return "廿" + "一二三四五六七八九"[day % 10 - 1];
            return "三十";
        }
    }
}
