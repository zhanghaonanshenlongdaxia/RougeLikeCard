using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using QFramework;
using Yarn.Unity;

namespace CardGame.Story
{
    /// <summary>
    /// 时间条件对话路由 — 觅长生式境界×时间双轴触发
    ///
    /// Yarn节点命名约定（按优先级从高到低尝试）：
    ///   {npc}_r{境界}_d{起始日}_{结束日}   如 qingshi_mother_r0_d10_20  → 练气期,第10~20天限定
    ///   {npc}_r{境界}                      如 qingshi_mother_r0          → 练气期专属
    ///   {npc}_d{起始日}_{结束日}           如 qingshi_mother_d10_20      → 时间窗口限定
    ///   {npc}_night / {npc}_day            → 夜晚/白天专属
    ///   {npc}                              基础对话（兜底）
    ///
    /// 事件时效性：限定窗口外的节点视为不存在 → 回退下一优先级。
    /// 一次性剧情节点（once_前缀）播过即锁（记录到Yarn变量 $story_once_{node}）。
    /// </summary>
    public class TimeConditionalDialogue
    {
        readonly DialogueRunner _runner;

        public TimeConditionalDialogue(DialogueRunner runner)
        {
            _runner = runner;
        }

        /// <summary>
        /// 按当前境界+时间选择NPC应播放的节点。
        /// 返回null表示该NPC当前无对话（如限时事件已过期且无兜底）。
        /// </summary>
        public string ResolveNode(string npcId)
        {
            if (_runner == null) return npcId;

            var realm = CardGameArchitecture.Interface.GetModel<IRealmModel>().CurrentRealm.Value;
            var timeSys = CardGameArchitecture.Interface.GetSystem<IGameTimeSystem>();
            int totalDays = timeSys.GetTotalDays();
            bool night = timeSys.IsNight();

            // 显式解析（优先级从高到低）
            string node;

            // 1. 境界+时间窗口精确匹配
            node = FindWindowNode($"{npcId}_r{realm}_d");
            if (node != null && IsWindowValid(node, totalDays)) return node;

            // 2. 境界专属
            if (NodeExists($"{npcId}_r{realm}")) return $"{npcId}_r{realm}";

            // 3. 时间窗口（不限境界）
            node = FindWindowNode($"{npcId}_d");
            if (node != null && IsWindowValid(node, totalDays)) return node;

            // 4. 时辰专属
            if (night && NodeExists($"{npcId}_night")) return $"{npcId}_night";
            if (!night && NodeExists($"{npcId}_day")) return $"{npcId}_day";

            // 5. 基础兜底
            if (NodeExists(npcId)) return npcId;

            return null;
        }

        bool NodeExists(string node)
        {
            if (_runner?.Dialogue == null) return false;
            var ok = _runner.Dialogue.NodeExists(node);
            if (!ok) return false;

            // 一次性节点：播过就视为不存在
            if (node.StartsWith("once_"))
            {
                bool played = StoryService.GetVariable($"story_once_{node}", false);
                return !played;
            }
            return true;
        }

        /// <summary>在Yarn程序里查找形如 {prefix}{start}_{end} 的时间窗口节点（遍历所有节点名解析）</summary>
        string FindWindowNode(string prefix)
        {
            if (_runner?.Dialogue == null) return null;

            string best = null;
            int bestSpan = int.MaxValue;

            foreach (var nodeName in AllNodeNames())
            {
                if (!nodeName.StartsWith(prefix)) continue;
                if (!IsWindowValid(nodeName, CurrentTotalDays())) continue;

                // 选窗口最窄的（最精确匹配）
                var span = GetWindowSpan(nodeName);
                if (span < bestSpan)
                {
                    bestSpan = span;
                    best = nodeName;
                }
            }
            return best;
        }

        static int CurrentTotalDays()
        {
            return CardGameArchitecture.Interface.GetSystem<IGameTimeSystem>().GetTotalDays();
        }

        /// <summary>解析节点名尾部的 _{start}_{end} 并检查当前天数是否在窗口内（start段可能带d前缀，如"_d0_3"）</summary>
        bool IsWindowValid(string nodeName, int totalDays)
        {
            var parts = nodeName.Split('_');
            if (parts.Length < 2) return false;
            var second = parts[parts.Length - 1];
            var first = parts[parts.Length - 2];

            // "d0"格式：剥离d前缀
            if (first.StartsWith("d") && first.Length > 1) first = first.Substring(1);
            if (second.StartsWith("d") && second.Length > 1) second = second.Substring(1);

            if (!int.TryParse(first, out int start) || !int.TryParse(second, out int end))
                return false;

            return totalDays >= start && totalDays <= end;
        }

        int GetWindowSpan(string nodeName)
        {
            var parts = nodeName.Split('_');
            if (parts.Length < 2) return int.MaxValue;
            var first = parts[parts.Length - 2];
            var second = parts[parts.Length - 1];
            if (first.StartsWith("d") && first.Length > 1) first = first.Substring(1);
            if (second.StartsWith("d") && second.Length > 1) second = second.Substring(1);
            if (int.TryParse(first, out int start) && int.TryParse(second, out int end))
                return end - start;
            return int.MaxValue;
        }

        IEnumerable<string> AllNodeNames()
        {
            // Yarn 3.x运行时不暴露节点枚举API → 使用NodeNameRegistry（StoryService扫描.yarn注册）
            foreach (var n in NodeNameRegistry.All)
                yield return n;
        }
    }

    /// <summary>
    /// 节点名注册表 — 编辑器启动时扫描YarnProject记录所有节点名
    /// （Yarn 3.x运行时不暴露节点枚举，用注册表补齐）
    /// </summary>
    public static class NodeNameRegistry
    {
        static readonly HashSet<string> _names = new HashSet<string>();

        public static IEnumerable<string> All => _names;

        public static void Register(string nodeName)
        {
            if (!string.IsNullOrEmpty(nodeName)) _names.Add(nodeName);
        }

        public static void RegisterFromYarnText(params string[] yarnTexts)
        {
            foreach (var text in yarnTexts)
            {
                if (text == null) continue;
                foreach (var line in text.Split('\n'))
                {
                    var t = line.Trim();
                    if (t.StartsWith("title:", StringComparison.OrdinalIgnoreCase))
                        Register(t.Substring(6).Trim());
                }
            }
        }
    }
}
