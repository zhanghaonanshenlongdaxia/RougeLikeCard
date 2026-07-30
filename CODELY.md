

## Codely Structured Memories

### User

### Feedback

### Project
- [2026-07-30 18:01:33] User is building a card game similar to Slay the Spire in Unity. Using QFramework for architecture + NueDeck (open-source MIT) as card game template. Project switched from Built-in to URP render pipeline. Unity 2022.3.62f2c1.
- [2026-07-30 18:24:31] QFramework + NueDeck integration complete (2026-07-30). Three Singleton Managers (CombatManager, CollectionManager, GameManager) converted to QFramework IController. All combat data (mana, card piles, turn state) now managed by IBattleModel/ICardModel via BindableProperty. CardSystem and BattleSystem handle logic. Combat scene tested: mana 3/3, 5 cards drawn, pile counts correct, zero runtime errors.

### Reference
- [2026-07-30 18:01:33] NueDeck analysis: Singleton-based managers (GameManager/CombatManager/CollectionManager/UIManager/FxManager/AudioManager). CardData SO with CardActionData list (type/target/value/delay). CardActionProcessor auto-discovers CardActionBase subclasses via reflection. CharacterStats handles HP/Block/Poison/Strength/Dexterity/Stun. EnemyBase with intent system (show next ability icon+value). EnemyCharacterData SO with ability list (pattern or random). CollectionManager manages draw/discard/exhaust piles. CombatManager turn flow: AllyTurn(draw cards+restore mana) → EnemyTurn(discard hand+enemy actions) → EndCombat. No QFramework usage - pure Singleton pattern. Need to integrate with our QFramework data-driven architecture (IBattleModel/ICardModel).
