using System;
using QFramework;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 时间推进接口 — 所有消耗时间的行动（走路/战斗/炼丹/修炼）都通过此系统推进
    /// </summary>
    public interface IGameTimeSystem : ISystem
    {
        /// <summary>推进N个时辰</summary>
        void AdvanceShichen(int count);

        /// <summary>推进N天</summary>
        void AdvanceDays(int days);

        /// <summary>完整格式："乾元三十七年正月初一 辰时"</summary>
        string GetDisplayString();

        /// <summary>短格式："三十七年正月初一"</summary>
        string GetDateString();

        /// <summary>当前时辰名</summary>
        string GetShichenName();

        /// <summary>是否在白天（辰时~酉时之前，即7:00-17:00）</summary>
        bool IsDaytime();

        /// <summary>是否夜晚（戌时~丑时，19:00-次日3:00）</summary>
        bool IsNight();

        /// <summary>游戏开始至今的总天数（时间窗口比较基准）</summary>
        int GetTotalDays();
    }

    /// <summary>
    /// 时间事件广播（NPC日程/事件时效监听）
    /// </summary>
    public static class GameTimeEvents
    {
        /// <summary>每推进1天广播一次（参数=新的总天数）</summary>
        public static event Action<int> OnDayAdvanced;
        /// <summary>跨月广播（参数=年,月）</summary>
        public static event Action<int, int> OnMonthAdvanced;
        /// <summary>跨年广播（参数=年）</summary>
        public static event Action<int> OnYearAdvanced;

        public static void FireDay(int totalDays) => OnDayAdvanced?.Invoke(totalDays);
        public static void FireMonth(int year, int month) => OnMonthAdvanced?.Invoke(year, month);
        public static void FireYear(int year) => OnYearAdvanced?.Invoke(year);
    }

    public class GameTimeSystem : AbstractSystem, IGameTimeSystem
    {
        IGameTimeModel _model;

        protected override void OnInit()
        {
            _model = this.GetModel<IGameTimeModel>();
        }

        public void AdvanceShichen(int count)
        {
            if (count <= 0) return;
            for (int i = 0; i < count; i++)
            {
                _model.Shichen.Value++;
                if (_model.Shichen.Value >= 12)
                {
                    _model.Shichen.Value = 0;
                    AdvanceDayInternal();
                }
            }
        }

        public void AdvanceDays(int days)
        {
            if (days <= 0) return;
            for (int i = 0; i < days; i++)
                AdvanceDayInternal();
        }

        void AdvanceDayInternal()
        {
            _model.Day.Value++;
            _model.TotalDays.Value++;
            GameTimeEvents.FireDay(_model.TotalDays.Value);

            if (_model.Day.Value > 30)
            {
                _model.Day.Value = 1;
                _model.Month.Value++;
                GameTimeEvents.FireMonth(_model.Year.Value, _model.Month.Value);

                if (_model.Month.Value > 12)
                {
                    _model.Month.Value = 1;
                    _model.Year.Value++;
                    GameTimeEvents.FireYear(_model.Year.Value);
                }
            }
        }

        public string GetDisplayString()
        {
            int eraYear = _model.Year.Value + _model.EraStartYearOffset;
            return $"{_model.EraName}{ChineseDateUtil.ToChineseNumber(eraYear)}年" +
                   $"{ChineseDateUtil.GetMonthName(_model.Month.Value)}" +
                   $"{ChineseDateUtil.GetDayName(_model.Day.Value)} " +
                   $"{ShichenUtil.GetName(_model.Shichen.Value)}";
        }

        public string GetDateString()
        {
            int eraYear = _model.Year.Value + _model.EraStartYearOffset;
            return $"{ChineseDateUtil.ToChineseNumber(eraYear)}年" +
                   $"{ChineseDateUtil.GetMonthName(_model.Month.Value)}" +
                   $"{ChineseDateUtil.GetDayName(_model.Day.Value)}";
        }

        public string GetShichenName()
        {
            return ShichenUtil.GetName(_model.Shichen.Value);
        }

        public bool IsDaytime()
        {
            int s = _model.Shichen.Value;
            return s >= 4 && s <= 8; // 辰巳午未申
        }

        public bool IsNight()
        {
            int s = _model.Shichen.Value;
            return s >= 10 || s <= 1; // 戌亥子丑
        }

        public int GetTotalDays()
        {
            return _model.TotalDays.Value;
        }
    }
}
