using System.Collections.Generic;
using System.IO;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;
using UnityEditor;
using UnityEngine;

namespace CardGame.Editor
{
    public static class BuffBatchGenerator
    {
        private const string BuffDir = "Assets/NueGames/NueDeck/Data/Buffs";
        private const string DatabaseDir = "Assets/NueGames/NueDeck/Data/Containers";

        [MenuItem("Tools/Generate Buffs")]
        public static void GenerateAll()
        {
            if (!Directory.Exists(BuffDir))
                Directory.CreateDirectory(BuffDir);

            var created = 0;
            created += CreateBuff("Block", StatusType.Block, "格挡", "减少受到的伤害",
                clearAtNextTurn: true);
            created += CreateBuff("Poison", StatusType.Poison, "中毒", "每回合受到等量伤害，无视格挡",
                decreaseOverTurn: true, specialEffect: BuffSpecialEffect.Poison);
            created += CreateBuff("Strength", StatusType.Strength, "力量", "增加攻击伤害",
                isPermanent: true, canNegativeStack: true);
            created += CreateBuff("Dexterity", StatusType.Dexterity, "敏捷", "增加格挡值",
                isPermanent: true, canNegativeStack: true);
            created += CreateBuff("Stun", StatusType.Stun, "眩晕", "无法行动",
                decreaseOverTurn: true, specialEffect: BuffSpecialEffect.Stun);
            created += CreateBuff("Weak", StatusType.Weak, "虚弱", "攻击造成的伤害减少25%",
                decreaseOverTurn: true, damageDealtMult: 0.75f);
            created += CreateBuff("Frail", StatusType.Frail, "脆弱", "获得的格挡减少25%",
                decreaseOverTurn: true, blockMult: 0.75f);
            created += CreateBuff("Vulnerable", StatusType.Vulnerable, "易伤", "受到的伤害增加50%",
                decreaseOverTurn: true, damageTakenMult: 1.5f);
            created += CreateBuff("Thorn", StatusType.Thorn, "反伤", "受击时反弹伤害给攻击者",
                isPermanent: true, specialEffect: BuffSpecialEffect.Thorn);

            // Create or update BuffDatabase
            CreateOrUpdateDatabase();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BuffBatchGenerator] Done. Created/updated {created} buff assets + BuffDatabase.");
        }

        private static int CreateBuff(string fileName, StatusType statusType, string displayName,
            string description, bool decreaseOverTurn = false, bool isPermanent = false,
            bool canNegativeStack = false, bool clearAtNextTurn = false,
            float damageTakenMult = 1f, float damageDealtMult = 1f, float blockMult = 1f,
            BuffSpecialEffect specialEffect = BuffSpecialEffect.None)
        {
            var path = $"{BuffDir}/{fileName}.asset";
            var buff = AssetDatabase.LoadAssetAtPath<BuffData>(path);
            var isNew = buff == null;
            if (isNew)
            {
                buff = ScriptableObject.CreateInstance<BuffData>();
                AssetDatabase.CreateAsset(buff, path);
            }

            buff.EditStatusType(statusType);
            buff.EditDisplayName(displayName);
            buff.EditDescription(description);
            // Copy icon from existing Status Icons asset if available
            var existingIcon = GetExistingIcon(statusType);
            if (existingIcon != null)
                buff.EditIcon(existingIcon);
            buff.EditDecreaseOverTurn(decreaseOverTurn);
            buff.EditIsPermanent(isPermanent);
            buff.EditCanNegativeStack(canNegativeStack);
            buff.EditClearAtNextTurn(clearAtNextTurn);
            buff.EditDamageTakenMult(damageTakenMult);
            buff.EditDamageDealtMult(damageDealtMult);
            buff.EditBlockMult(blockMult);
            buff.EditSpecialEffect(specialEffect);

            EditorUtility.SetDirty(buff);
            return 1;
        }

        /// <summary>从现有的 Status Icons.asset 中提取已配置的图标</summary>
        private static Sprite GetExistingIcon(StatusType type)
        {
            var iconsAsset = AssetDatabase.LoadAssetAtPath<StatusIconsData>(
                "Assets/NueGames/NueDeck/Data/Containers/Status Icons.asset");
            if (iconsAsset == null) return null;
            foreach (var iconData in iconsAsset.StatusIconList)
            {
                if (iconData.IconStatus == type)
                    return iconData.IconSprite;
            }
            return null;
        }

        private static void CreateOrUpdateDatabase()
        {
            if (!Directory.Exists(DatabaseDir))
                Directory.CreateDirectory(DatabaseDir);

            var dbPath = $"{DatabaseDir}/Buff Database.asset";
            var db = AssetDatabase.LoadAssetAtPath<BuffDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<BuffDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            // Collect all BuffData assets
            var guids = AssetDatabase.FindAssets("t:BuffData", new[] { BuffDir });
            var buffList = new List<BuffData>();
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var buff = AssetDatabase.LoadAssetAtPath<BuffData>(assetPath);
                if (buff != null)
                    buffList.Add(buff);
            }
            db.EditBuffList(buffList);
            EditorUtility.SetDirty(db);
        }
    }
}
