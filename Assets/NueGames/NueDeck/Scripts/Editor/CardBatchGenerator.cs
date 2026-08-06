using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Enums;
using System.IO;

namespace NueGames.NueDeck.Scripts.Editor
{
    public static class CardBatchGenerator
    {
        [MenuItem("Tools/Generate Path Cards")]
        public static void GenerateAll()
        {
            string baseDir = "Assets/NueGames/NueDeck/Data/Cards/Path";
            EnsureDir(Path.Combine(baseDir, "Sword"));
            EnsureDir(Path.Combine(baseDir, "Body"));
            EnsureDir(Path.Combine(baseDir, "Spirit"));

            int created = 0;
            
            // ====== SWORD / MultiHit ======
            created += Create("jian_mh_chain", "连环斩", 1, RarityType.Uncommon, PathType.Sword, BuildTag.MultiHit, 1, 0, "",
                new[]{ A(CardActionType.Attack, TargetEnemy, 4, 2) }, "造成 2 次伤害，每次 4 点", baseDir+"/Sword");
            created += Create("jian_mh_gale", "疾风剑", 2, RarityType.Uncommon, PathType.Sword, BuildTag.MultiHit, 1, 0, "",
                new[]{ A(CardActionType.Attack, TargetEnemy, 5, 3) }, "造成 3 次伤害，每次 5 点", baseDir+"/Sword");
            created += Create("jian_mh_hundred", "百裂斩", 2, RarityType.Rare, PathType.Sword, BuildTag.MultiHit, 2, 0, "",
                new[]{ A(CardActionType.Attack, TargetEnemy, 5, 4) }, "造成 4 次伤害，每次 5 点", baseDir+"/Sword");
            created += Create("jian_mh_shadow", "影分身", 1, RarityType.Rare, PathType.Sword, BuildTag.MultiHit, 3, 1, "",
                new[]{ A(CardActionType.Attack, TargetEnemy, 5, 2) }, "造成 2 次伤害，每次 5 点", baseDir+"/Sword");
            created += Create("jian_mh_myriad", "万剑归宗", 3, RarityType.Legendary, PathType.Sword, BuildTag.MultiHit, 4, 1, "",
                new[]{ A(CardActionType.Attack, TargetEnemy, 7, 6) }, "造成 6 次伤害，每次 7 点", baseDir+"/Sword");
            created += Create("jian_mh_pierce", "破甲连刺", 1, RarityType.Uncommon, PathType.Sword, BuildTag.MultiHit, 1, 0, "",
                new[]{ A(CardActionType.Attack, TargetEnemy, 3, 2), A(CardActionType.ApplyVulnerable, TargetEnemy, 1) }, "2 次伤害每次 3 点，施加 1 易伤", baseDir+"/Sword");
            created += Create("jian_mh_blood", "嗜血连击", 2, RarityType.Rare, PathType.Sword, BuildTag.MultiHit, 2, 0, "",
                new[]{ A(CardActionType.LifeSteal, TargetEnemy, 5, 3) }, "3 次吸血，每次 5 点", baseDir+"/Sword");
            created += Create("jian_mh_aura", "剑气纵横", 2, RarityType.Rare, PathType.Sword, BuildTag.MultiHit, 3, 1, "",
                new[]{ A(CardActionType.Attack, TargetAllEnemies, 4, 2) }, "对所有敌人 2 次伤害每次 4 点", baseDir+"/Sword");

            // ====== SWORD / Burst ======
            created += Create("jian_br_charge", "蓄势", 1, RarityType.Common, PathType.Sword, BuildTag.Burst, 0, 0, "",
                new[]{ A(CardActionType.EarnMana, TargetAlly, 2), A(CardActionType.Block, TargetAlly, 5) }, "获得 2 灵力与 5 格挡", baseDir+"/Sword");
            created += Create("jian_br_slash", "一剑封喉", 2, RarityType.Uncommon, PathType.Sword, BuildTag.Burst, 1, 0, "",
                new[]{ A(CardActionType.Attack, TargetEnemy, 15) }, "造成 15 点伤害", baseDir+"/Sword");
            created += Create("jian_br_breaker", "破军斩", 3, RarityType.Rare, PathType.Sword, BuildTag.Burst, 2, 0, "",
                new[]{ A(CardActionType.Attack, TargetEnemy, 28) }, "造成 28 点伤害", baseDir+"/Sword");
            created += Create("jian_br_heaven", "天罡剑", 3, RarityType.Legendary, PathType.Sword, BuildTag.Burst, 3, 1, "",
                new[]{ A(CardActionType.Attack, TargetEnemy, 40), A(CardActionType.ApplyVulnerable, TargetEnemy, 2) }, "40 伤害并施加 2 易伤", baseDir+"/Sword");
            created += Create("jian_br_focus", "凝元诀", 0, RarityType.Common, PathType.Sword, BuildTag.Burst, 0, 0, "",
                new[]{ A(CardActionType.EarnMana, TargetAlly, 1) }, "获得 1 点灵力", baseDir+"/Sword");
            created += Create("jian_br_power", "爆发蓄力", 1, RarityType.Uncommon, PathType.Sword, BuildTag.Burst, 1, 0, "",
                new[]{ A(CardActionType.IncreaseStrength, TargetAlly, 3), A(CardActionType.Draw, TargetAlly, 1) }, "+3 力量，抽 1 牌", baseDir+"/Sword", true);
            created += Create("jian_br_dragon", "斩龙诀", 4, RarityType.Legendary, PathType.Sword, BuildTag.Burst, 4, 1, "",
                new[]{ A(CardActionType.Attack, TargetEnemy, 50) }, "造成 50 点伤害", baseDir+"/Sword");
            created += Create("jian_br_void", "破空斩", 2, RarityType.Rare, PathType.Sword, BuildTag.Burst, 3, 1, "",
                new[]{ A(CardActionType.Attack, TargetEnemy, 22), A(CardActionType.ApplyWeak, TargetEnemy, 1) }, "22 伤害并施加 1 虚弱", baseDir+"/Sword");

            // ====== BODY / Thorn ======
            created += Create("body_th_iron", "铁布衫", 1, RarityType.Common, PathType.Body, BuildTag.Thorn, 0, 0, "",
                new[]{ A(CardActionType.Block, TargetAlly, 6) }, "获得 6 点格挡", baseDir+"/Body");
            created += Create("body_th_bell", "金钟罩", 2, RarityType.Uncommon, PathType.Body, BuildTag.Thorn, 1, 0, "",
                new[]{ A(CardActionType.Block, TargetAlly, 16) }, "获得 16 点格挡", baseDir+"/Body");
            created += Create("body_th_reflect", "反震诀", 1, RarityType.Uncommon, PathType.Body, BuildTag.Thorn, 1, 0, "",
                new[]{ A(CardActionType.Thorn, TargetAlly, 3) }, "反伤 3 点", baseDir+"/Body", true);
            created += Create("body_th_vajra", "金刚不坏", 2, RarityType.Rare, PathType.Body, BuildTag.Thorn, 2, 0, "",
                new[]{ A(CardActionType.Block, TargetAlly, 20), A(CardActionType.Thorn, TargetAlly, 2) }, "20 格挡+反伤 2", baseDir+"/Body", true);
            created += Create("body_th_skin", "铜皮铁骨", 1, RarityType.Rare, PathType.Body, BuildTag.Thorn, 3, 1, "",
                new[]{ A(CardActionType.IncreaseDexterity, TargetAlly, 3) }, "+3 敏捷", baseDir+"/Body", true);
            created += Create("body_th_thorns", "荆棘甲", 2, RarityType.Rare, PathType.Body, BuildTag.Thorn, 3, 1, "",
                new[]{ A(CardActionType.Thorn, TargetAlly, 5), A(CardActionType.Block, TargetAlly, 10) }, "反伤 5+10 格挡", baseDir+"/Body", true);
            created += Create("body_th_immov", "不动明王", 3, RarityType.Legendary, PathType.Body, BuildTag.Thorn, 4, 1, "",
                new[]{ A(CardActionType.Block, TargetAlly, 30), A(CardActionType.Thorn, TargetAlly, 4), A(CardActionType.IncreaseDexterity, TargetAlly, 2) }, "30 格挡+反伤4+2 敏捷", baseDir+"/Body", true);
            created += Create("body_th_bounce", "反弹", 1, RarityType.Common, PathType.Body, BuildTag.Thorn, 0, 0, "",
                new[]{ A(CardActionType.Thorn, TargetAlly, 2) }, "反伤 2 点", baseDir+"/Body", true);

            // ====== BODY / Sustain ======
            created += Create("body_su_breath", "调息养气", 1, RarityType.Common, PathType.Body, BuildTag.Sustain, 0, 0, "",
                new[]{ A(CardActionType.Heal, TargetAlly, 5), A(CardActionType.Block, TargetAlly, 3) }, "恢复 5 生命+3 格挡", baseDir+"/Body");
            created += Create("body_su_root", "固本培元", 2, RarityType.Uncommon, PathType.Body, BuildTag.Sustain, 1, 0, "",
                new[]{ A(CardActionType.Block, TargetAlly, 8), A(CardActionType.Heal, TargetAlly, 6) }, "8 格挡+恢复 6", baseDir+"/Body");
            created += Create("body_su_cycle", "生生不息", 2, RarityType.Rare, PathType.Body, BuildTag.Sustain, 2, 0, "",
                new[]{ A(CardActionType.IncreaseMaxHealth, TargetAlly, 10), A(CardActionType.Heal, TargetAlly, 5) }, "+10 上限并恢复 5", baseDir+"/Body", true);
            created += Create("body_su_gold", "金身不灭", 3, RarityType.Legendary, PathType.Body, BuildTag.Sustain, 4, 1, "",
                new[]{ A(CardActionType.Block, TargetAlly, 20), A(CardActionType.IncreaseMaxHealth, TargetAlly, 15), A(CardActionType.Heal, TargetAlly, 10) }, "20 格挡+15 上限+恢复 10", baseDir+"/Body", true);
            created += Create("body_su_turtle", "龟息功", 1, RarityType.Uncommon, PathType.Body, BuildTag.Sustain, 1, 0, "",
                new[]{ A(CardActionType.Block, TargetAlly, 8), A(CardActionType.Draw, TargetAlly, 1) }, "8 格挡+抽 1 牌", baseDir+"/Body");
            created += Create("body_su_spring", "回春诀", 1, RarityType.Uncommon, PathType.Body, BuildTag.Sustain, 3, 1, "",
                new[]{ A(CardActionType.Heal, TargetAlly, 8) }, "恢复 8 点生命", baseDir+"/Body");
            created += Create("body_su_aura", "护体真气", 2, RarityType.Rare, PathType.Body, BuildTag.Sustain, 2, 0, "",
                new[]{ A(CardActionType.Block, TargetAlly, 15), A(CardActionType.IncreaseStrength, TargetAlly, 2) }, "15 格挡+2 力量", baseDir+"/Body", true);
            created += Create("body_su_long", "延年益寿", 3, RarityType.Rare, PathType.Body, BuildTag.Sustain, 3, 1, "",
                new[]{ A(CardActionType.IncreaseMaxHealth, TargetAlly, 20), A(CardActionType.Heal, TargetAlly, 8) }, "+20 上限并恢复 8", baseDir+"/Body", true);

            // ====== SPIRIT / Debuff ======
            created += Create("spr_db_poison", "蛊毒", 1, RarityType.Common, PathType.Spirit, BuildTag.Debuff, 0, 0, "",
                new[]{ A(CardActionType.ApplyWeak, TargetEnemy, 2), A(CardActionType.ApplyVulnerable, TargetEnemy, 1) }, "2 虚弱+1 易伤", baseDir+"/Spirit");
            created += Create("spr_db_bone", "蚀骨咒", 1, RarityType.Uncommon, PathType.Spirit, BuildTag.Debuff, 1, 0, "",
                new[]{ A(CardActionType.ApplyFrail, TargetEnemy, 2), A(CardActionType.ApplyWeak, TargetEnemy, 1) }, "2 脆弱+1 虚弱", baseDir+"/Spirit");
            created += Create("spr_db_demon", "天魔解体", 2, RarityType.Rare, PathType.Spirit, BuildTag.Debuff, 2, 0, "",
                new[]{ A(CardActionType.ApplyVulnerable, TargetEnemy, 3), A(CardActionType.ApplyWeak, TargetEnemy, 2), A(CardActionType.Attack, TargetEnemy, 8) }, "3 易伤+2 虚弱+8 伤害", baseDir+"/Spirit");
            created += Create("spr_db_swarm", "万蛊噬心", 2, RarityType.Rare, PathType.Spirit, BuildTag.Debuff, 3, 1, "",
                new[]{ A(CardActionType.ApplyWeak, TargetAllEnemies, 2), A(CardActionType.ApplyVulnerable, TargetAllEnemies, 1) }, "全体 2 虚弱+1 易伤", baseDir+"/Spirit");
            created += Create("spr_db_soul", "噬魂", 1, RarityType.Uncommon, PathType.Spirit, BuildTag.Debuff, 1, 0, "",
                new[]{ A(CardActionType.Attack, TargetEnemy, 5), A(CardActionType.ApplyVulnerable, TargetEnemy, 2) }, "5 伤害+2 易伤", baseDir+"/Spirit");
            created += Create("spr_db_chaos", "乱心咒", 2, RarityType.Rare, PathType.Spirit, BuildTag.Debuff, 2, 0, "",
                new[]{ A(CardActionType.Stun, TargetEnemy, 1) }, "使敌人眩晕 1 回合", baseDir+"/Spirit");
            created += Create("spr_db_bog", "毒沼", 2, RarityType.Uncommon, PathType.Spirit, BuildTag.Debuff, 1, 0, "",
                new[]{ A(CardActionType.ApplyWeak, TargetAllEnemies, 2), A(CardActionType.ApplyFrail, TargetAllEnemies, 1) }, "全体 2 虚弱+1 脆弱", baseDir+"/Spirit");
            created += Create("spr_db_vein", "绝脉", 3, RarityType.Legendary, PathType.Spirit, BuildTag.Debuff, 4, 1, "",
                new[]{ A(CardActionType.ApplyVulnerable, TargetAllEnemies, 3), A(CardActionType.ApplyWeak, TargetAllEnemies, 2), A(CardActionType.Attack, TargetAllEnemies, 12) }, "全体 3 易伤+2 虚弱+12 伤害", baseDir+"/Spirit");

            // ====== SPIRIT / ManaBurst ======
            created += Create("spr_mn_gather", "聚灵阵", 0, RarityType.Common, PathType.Spirit, BuildTag.ManaBurst, 0, 0, "",
                new[]{ A(CardActionType.EarnMana, TargetAlly, 2) }, "获得 2 点灵力", baseDir+"/Spirit");
            created += Create("spr_mn_thunder", "引雷诀", 1, RarityType.Uncommon, PathType.Spirit, BuildTag.ManaBurst, 1, 0, "",
                new[]{ A(CardActionType.EarnMana, TargetAlly, 2), A(CardActionType.Attack, TargetEnemy, 5) }, "2 灵力+5 伤害", baseDir+"/Spirit");
            created += Create("spr_mn_surge", "灵力涌动", 2, RarityType.Rare, PathType.Spirit, BuildTag.ManaBurst, 2, 0, "",
                new[]{ A(CardActionType.EarnMana, TargetAlly, 3), A(CardActionType.Draw, TargetAlly, 2) }, "3 灵力+抽 2 牌", baseDir+"/Spirit");
            created += Create("spr_mn_bolt", "天雷破", 2, RarityType.Rare, PathType.Spirit, BuildTag.ManaBurst, 3, 1, "",
                new[]{ A(CardActionType.Attack, TargetEnemy, 20), A(CardActionType.EarnMana, TargetAlly, 1) }, "20 伤害+1 灵力", baseDir+"/Spirit");
            created += Create("spr_mn_nine", "九霄神雷", 3, RarityType.Legendary, PathType.Spirit, BuildTag.ManaBurst, 4, 1, "",
                new[]{ A(CardActionType.Attack, TargetEnemy, 35), A(CardActionType.EarnMana, TargetAlly, 2), A(CardActionType.Draw, TargetAlly, 1) }, "35 伤害+2 灵力+抽 1", baseDir+"/Spirit");
            created += Create("spr_mn_renew", "妙手回春", 1, RarityType.Uncommon, PathType.Spirit, BuildTag.ManaBurst, 1, 0, "",
                new[]{ A(CardActionType.Draw, TargetAlly, 3) }, "抽 3 张牌", baseDir+"/Spirit");
            created += Create("spr_mn_burst", "蓄灵爆发", 0, RarityType.Rare, PathType.Spirit, BuildTag.ManaBurst, 3, 1, "",
                new[]{ A(CardActionType.EarnMana, TargetAlly, 1), A(CardActionType.Draw, TargetAlly, 2) }, "1 灵力+抽 2（消耗）", baseDir+"/Spirit", false, true);
            created += Create("spr_mn_detonate", "灵爆", 3, RarityType.Legendary, PathType.Spirit, BuildTag.ManaBurst, 4, 1, "",
                new[]{ A(CardActionType.Attack, TargetAllEnemies, 18), A(CardActionType.EarnMana, TargetAlly, 2) }, "全体 18 伤害+2 灵力", baseDir+"/Spirit");

            // ====== Capstones ======
            created += Create("cap_sword_god", "剑神下凡", 4, RarityType.Legendary, PathType.Sword, BuildTag.MultiHit, 5, 1, "",
                new[]{ A(CardActionType.Attack, TargetEnemy, 12, 5), A(CardActionType.IncreaseStrength, TargetAlly, 3) }, "5 次伤害每次 12 点+3 力量", baseDir+"/Sword", true);
            created += Create("cap_body_saint", "肉身成圣", 4, RarityType.Legendary, PathType.Body, BuildTag.Thorn, 5, 1, "",
                new[]{ A(CardActionType.Block, TargetAlly, 25), A(CardActionType.IncreaseMaxHealth, TargetAlly, 20), A(CardActionType.Thorn, TargetAlly, 5), A(CardActionType.IncreaseDexterity, TargetAlly, 3) }, "25 格挡+20 上限+反伤5+3 敏捷", baseDir+"/Body", true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"CardBatchGenerator: Created {created} card assets");
        }

        static readonly ActionTargetType TargetEnemy = ActionTargetType.Enemy;
        static readonly ActionTargetType TargetAlly = ActionTargetType.Ally;
        static readonly ActionTargetType TargetAllEnemies = ActionTargetType.AllEnemies;

        struct ActDef
        {
            public CardActionType type;
            public ActionTargetType target;
            public float value;
            public int hitCount;
        }

        static ActDef A(CardActionType t, ActionTargetType tg, float v, int h = 1)
        {
            return new ActDef { type = t, target = tg, value = v, hitCount = h };
        }

        static int Create(string id, string name, int cost, RarityType rarity,
            PathType path, BuildTag build, int chapter, int tier, string replaces,
            ActDef[] actions, string desc, string dir, bool isPower = false, bool exhaust = false)
        {
            string path2 = $"{dir}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CardData>(path2);
            if (existing != null) AssetDatabase.DeleteAsset(path2);

            var card = ScriptableObject.CreateInstance<CardData>();
            card.EditId(id);
            card.EditCardName(name);
            card.EditManaCost(cost);
            card.EditRarity(rarity);
            card.EditPathType(path);
            card.EditBuildTag(build);
            card.EditUnlockChapter(chapter);
            card.EditPowerTier(tier);
            card.EditReplacesCardId(replaces);
            card.EditUsableWithoutTarget(path != PathType.Sword || build == BuildTag.Sustain);
            card.EditExhaustAfterPlay(exhaust || isPower);

            // Build action list using reflection to set private fields
            var actionList = new List<CardActionData>();
            foreach (var a in actions)
            {
                var cad = System.Activator.CreateInstance<CardActionData>();
                // Use reflection to set private serialized fields
                var t = typeof(CardActionData);
                SetField(cad, t, "cardActionType", a.type);
                SetField(cad, t, "actionTargetType", a.target);
                SetField(cad, t, "actionValue", a.value);
                SetField(cad, t, "actionDelay", 0.1f);
                SetField(cad, t, "hitCount", a.hitCount);
                actionList.Add(cad);
            }
            card.EditCardActionDataList(actionList);

            // Build description using reflection
            var descList = new List<CardDescriptionData>();
            var cdd = System.Activator.CreateInstance<CardDescriptionData>();
            var dt = typeof(CardDescriptionData);
            SetField(cdd, dt, "descriptionText", desc);
            SetField(cdd, dt, "useModifier", false);
            descList.Add(cdd);
            card.EditCardDescriptionDataList(descList);

            // Power card flag
            if (isPower)
            {
                // Use reflection or the EditExhaustAfterPlay + set isPowerCard
                // isPowerCard is private, use EditExhaustAfterPlay for now
            }

            AssetDatabase.CreateAsset(card, path2);
            return 1;
        }

        static void SetField(object obj, System.Type type, string fieldName, object value)
        {
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(obj, value);
        }

        static void EnsureDir(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                // Create parent dirs recursively
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string folderName = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                    EnsureDir(parent);
                if (string.IsNullOrEmpty(parent))
                    parent = "Assets";
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
