

## Codely Structured Memories

### User

### Feedback

### Project
- [2026-07-30 18:01:33] User is building a card game similar to Slay the Spire in Unity. Using QFramework for architecture + NueDeck (open-source MIT) as card game template. Project switched from Built-in to URP render pipeline. Unity 2022.3.62f2c1.
- [2026-07-30 18:24:31] QFramework + NueDeck integration complete (2026-07-30). Three Singleton Managers (CombatManager, CollectionManager, GameManager) converted to QFramework IController. All combat data (mana, card piles, turn state) now managed by IBattleModel/ICardModel via BindableProperty. CardSystem and BattleSystem handle logic. Combat scene tested: mana 3/3, 5 cards drawn, pile counts correct, zero runtime errors.
- [2026-07-30 18:41:12] All NueDeck UI scripts now converted to QFramework IController (2026-07-30). InformationCanvas, MapManager, EncounterButton, RewardCanvas, InventoryCanvas added. Project pushed to GitHub: https://github.com/zhanghaonanshenlongdaxia/RougeLikeCard.git.
- [2026-07-30 18:59:29] Combat system enhanced (2026-07-30): Added Weak/Frail/Vulnerable debuffs (Weak reduces attack damage 25%, Frail reduces block 25%, Vulnerable increases damage taken 50%). Added card upgrade system (CardData.IsUpgraded/Upgrade()/Downgrade()). Added Power card persistence (isPowerCard field, Power cards exhaust instead of discard). Created 4 new cards (Demoralize/Expose/Rupture/Crippling Strike), 1 new enemy (Enemy 4 debuff caster), 3 new status icon entries. Total: 13 cards, 5 enemies, 8 status types, 13 card actions, 7 enemy actions. Compile zero errors.

### Reference
- [2026-07-30 18:01:33] NueDeck analysis: Singleton-based managers (GameManager/CombatManager/CollectionManager/UIManager/FxManager/AudioManager). CardData SO with CardActionData list (type/target/value/delay). CardActionProcessor auto-discovers CardActionBase subclasses via reflection. CharacterStats handles HP/Block/Poison/Strength/Dexterity/Stun. EnemyBase with intent system (show next ability icon+value). EnemyCharacterData SO with ability list (pattern or random). CollectionManager manages draw/discard/exhaust piles. CombatManager turn flow: AllyTurn(draw cards+restore mana) → EnemyTurn(discard hand+enemy actions) → EndCombat. No QFramework usage - pure Singleton pattern. Need to integrate with our QFramework data-driven architecture (IBattleModel/ICardModel).
- [2026-07-30 18:41:12] Combat system gap analysis (vs Slay the Spire): Missing Debuffs (Weak/Frail/Vulnerable), card upgrade system, multi-hit attacks, Power card persistence, relic hooks, potion system. Existing: 10 card actions, 5 status types (Block/Poison/Strength/Dexterity/Stun), 4 enemy actions, 9 cards, 4 enemies. Architecture issue: CombatManager.BuildEnemies still uses PersistentGameplayData for encounter lookup. Suggested priority: Weak/Frail/Vulnerable → card upgrade → Power persistence → relic hooks.
