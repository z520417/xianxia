using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 战斗参与者
    /// </summary>
    [Serializable]
    public class BattleParticipant
    {
        [SerializeField] private string m_Name;
        [SerializeField] private CharacterStats m_Stats;
        [SerializeField] private bool m_IsPlayer;

        public string Name => m_Name;
        public CharacterStats Stats => m_Stats;
        public bool IsPlayer => m_IsPlayer;
        public bool IsAlive => m_Stats.IsAlive;

        public BattleParticipant(string _name, CharacterStats _stats, bool _isPlayer = false)
        {
            m_Name = _name;
            m_Stats = _stats.Clone();
            m_IsPlayer = _isPlayer;
        }
    }

    /// <summary>
    /// 战斗动作类型
    /// </summary>
    public enum BattleActionType
    {
        Attack,     // 普通攻击
        Defend,     // 防御
        UseItem,    // 使用物品
        Escape      // 逃跑
    }

    /// <summary>
    /// 战斗动作
    /// </summary>
    [Serializable]
    public class BattleAction
    {
        public BattleActionType ActionType;
        public BattleParticipant Actor;
        public BattleParticipant Target;
        public ItemData UsedItem;

        public BattleAction(BattleActionType _actionType, BattleParticipant _actor, BattleParticipant _target = null, ItemData _usedItem = null)
        {
            ActionType = _actionType;
            Actor = _actor;
            Target = _target;
            UsedItem = _usedItem;
        }
    }

    /// <summary>
    /// 战斗系统
    /// </summary>
    public class BattleSystem : MonoBehaviour
    {
        #region 事件
        public event Action<BattleParticipant, BattleParticipant> OnBattleStarted;
        public event Action<BattleResult, BattleParticipant> OnBattleEnded;
        public event Action<BattleAction, string> OnActionExecuted;
        public event Action<BattleParticipant> OnTurnStarted;
        public event Action<BattleParticipant, int, bool> OnDamageDealt;
        public event Action<BattleParticipant, int> OnHealthRestored;
        #endregion

        #region 战斗状态
        [Header("战斗状态")]
        [SerializeField] private bool m_IsBattleActive;
        [SerializeField] private BattleParticipant m_Player;
        [SerializeField] private BattleParticipant m_Enemy;
        [SerializeField] private BattleParticipant m_CurrentTurnActor;
        [SerializeField] private int m_TurnCount;
        #endregion

        #region 战斗配置
        [Header("战斗配置")]
        [SerializeField] private float m_ActionDelay = 1f;         // 动作延迟时间
        [SerializeField] private float m_EscapeChance = 0.7f;      // 逃跑成功率
        [SerializeField] private int m_DefenseReduction = 50;      // 防御时伤害减少百分比
        #endregion

        #region 关联系统
        [Header("关联系统")]
        [SerializeField] private InventorySystem m_InventorySystem;
        [SerializeField] private EquipmentManager m_EquipmentManager;
        #endregion

        #region 敌人数据
        [Header("敌人配置")]
        [SerializeField] private string[] m_EnemyNames = 
        {
            "野狼", "山贼", "妖狐", "石像鬼", "邪修", "恶灵",
            "魔狼", "黑衣人", "毒蛇", "骷髅兵", "幽灵", "恶魔"
        };

        [SerializeField] private string[] m_EnemyDescriptions = 
        {
            "一只饥饿的野狼，眼中闪烁着凶光。",
            "拦路抢劫的山贼，手持利刃。",
            "修炼成精的狐狸，拥有魅惑之术。",
            "古老的石像鬼，身体坚硬如铁。",
            "走火入魔的修炼者，实力不俗。",
            "怨气凝聚的恶灵，阴森可怖。"
        };
        #endregion

        #region 公共属性
        public bool IsBattleActive => m_IsBattleActive;
        public BattleParticipant Player => m_Player;
        public BattleParticipant Enemy => m_Enemy;
        public BattleParticipant CurrentTurnActor => m_CurrentTurnActor;
        public int TurnCount => m_TurnCount;
        #endregion

        #region Unity生命周期
        private void Start()
        {
            FindRelatedSystems();
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 查找相关系统
        /// </summary>
        private void FindRelatedSystems()
        {
            if (m_InventorySystem == null)
            {
                m_InventorySystem = FindObjectOfType<InventorySystem>();
            }

            if (m_EquipmentManager == null)
            {
                m_EquipmentManager = FindObjectOfType<EquipmentManager>();
            }
        }
        #endregion

        #region 战斗开始和结束
        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartBattle(CharacterStats _playerStats, int _enemyLevel = -1)
        {
            if (m_IsBattleActive)
            {
                Debug.LogWarning("战斗已经在进行中");
                return;
            }

            // 创建玩家参与者
            m_Player = new BattleParticipant("道友", _playerStats, true);

            // 生成敌人
            m_Enemy = GenerateRandomEnemy(_enemyLevel > 0 ? _enemyLevel : _playerStats.Level);

            // 初始化战斗状态
            m_IsBattleActive = true;
            m_TurnCount = 1;

            // 决定先手（速度高者先手）
            m_CurrentTurnActor = m_Player.Stats.Speed >= m_Enemy.Stats.Speed ? m_Player : m_Enemy;

            // 触发战斗开始事件
            OnBattleStarted?.Invoke(m_Player, m_Enemy);
            OnTurnStarted?.Invoke(m_CurrentTurnActor);

            Debug.Log($"战斗开始！{m_Player.Name} VS {m_Enemy.Name}");
            Debug.Log($"{m_CurrentTurnActor.Name} 先手！");

            // 如果是敌人先手，执行AI行动
            if (!m_CurrentTurnActor.IsPlayer)
            {
                StartCoroutine(ExecuteEnemyTurn());
            }
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        private void EndBattle(BattleResult _result)
        {
            if (!m_IsBattleActive) return;

            m_IsBattleActive = false;
            BattleParticipant winner = null;

            switch (_result)
            {
                case BattleResult.Victory:
                    winner = m_Player;
                    GiveVictoryRewards();
                    Debug.Log("战斗胜利！");
                    break;

                case BattleResult.Defeat:
                    winner = m_Enemy;
                    Debug.Log("战斗失败...");
                    break;

                case BattleResult.Escape:
                    Debug.Log("成功逃脱了战斗。");
                    break;
            }

            OnBattleEnded?.Invoke(_result, winner);

            // 清理战斗数据
            m_Player = null;
            m_Enemy = null;
            m_CurrentTurnActor = null;
            m_TurnCount = 0;
        }

        /// <summary>
        /// 给予胜利奖励
        /// </summary>
        private void GiveVictoryRewards()
        {
            if (m_Player == null || m_Enemy == null) return;

            // 经验值奖励
            int expReward = CalculateExperienceReward(m_Enemy.Stats.Level);
            bool leveledUp = m_Player.Stats.GainExperience(expReward);

            Debug.Log($"获得经验值：{expReward}" + (leveledUp ? "，等级提升！" : ""));

            // 物品奖励
            if (m_InventorySystem != null)
            {
                List<ItemData> rewards = GenerateBattleRewards(m_Enemy.Stats.Level, m_Player.Stats.Luck);
                
                foreach (ItemData reward in rewards)
                {
                    m_InventorySystem.AddItem(reward, 1);
                }

                if (rewards.Count > 0)
                {
                    Debug.Log($"战斗获得物品奖励：");
                    foreach (ItemData reward in rewards)
                    {
                        Debug.Log($"- {reward.FullName}");
                    }
                }
            }

            // 更新装备管理器的属性
            if (m_EquipmentManager != null)
            {
                m_EquipmentManager.UpdateBaseStats(m_Player.Stats);
            }
        }
        #endregion

        #region 回合系统
        /// <summary>
        /// 切换回合
        /// </summary>
        private void SwitchTurn()
        {
            if (!m_IsBattleActive) return;

            // 切换到下一个参与者
            m_CurrentTurnActor = m_CurrentTurnActor == m_Player ? m_Enemy : m_Player;
            
            // 如果回到玩家回合，增加回合数
            if (m_CurrentTurnActor.IsPlayer)
            {
                m_TurnCount++;
            }

            OnTurnStarted?.Invoke(m_CurrentTurnActor);

            // 如果是敌人回合，执行AI行动
            if (!m_CurrentTurnActor.IsPlayer)
            {
                StartCoroutine(ExecuteEnemyTurn());
            }
        }

        /// <summary>
        /// 执行敌人回合
        /// </summary>
        private IEnumerator ExecuteEnemyTurn()
        {
            yield return new WaitForSeconds(m_ActionDelay);

            if (!m_IsBattleActive || m_CurrentTurnActor.IsPlayer) yield break;

            // 简单AI：随机选择攻击或防御
            BattleActionType actionType = UnityEngine.Random.Range(0f, 1f) < 0.8f ? BattleActionType.Attack : BattleActionType.Defend;
            BattleAction enemyAction = new BattleAction(actionType, m_Enemy, m_Player);

            ExecuteAction(enemyAction);
        }
        #endregion

        #region 玩家行动
        /// <summary>
        /// 玩家攻击
        /// </summary>
        public void PlayerAttack()
        {
            if (!CanPlayerAct()) return;

            BattleAction action = new BattleAction(BattleActionType.Attack, m_Player, m_Enemy);
            ExecuteAction(action);
        }

        /// <summary>
        /// 玩家防御
        /// </summary>
        public void PlayerDefend()
        {
            if (!CanPlayerAct()) return;

            BattleAction action = new BattleAction(BattleActionType.Defend, m_Player);
            ExecuteAction(action);
        }

        /// <summary>
        /// 玩家使用物品
        /// </summary>
        public void PlayerUseItem(int _slotIndex)
        {
            if (!CanPlayerAct()) return;
            if (m_InventorySystem == null) return;

            ItemSlot slot = m_InventorySystem.GetSlot(_slotIndex);
            if (slot == null || slot.IsEmpty) return;

            BattleAction action = new BattleAction(BattleActionType.UseItem, m_Player, m_Player, slot.ItemData);
            ExecuteAction(action);
        }

        /// <summary>
        /// 玩家逃跑
        /// </summary>
        public void PlayerEscape()
        {
            if (!CanPlayerAct()) return;

            BattleAction action = new BattleAction(BattleActionType.Escape, m_Player);
            ExecuteAction(action);
        }

        /// <summary>
        /// 检查玩家是否可以行动
        /// </summary>
        private bool CanPlayerAct()
        {
            return m_IsBattleActive && m_CurrentTurnActor != null && m_CurrentTurnActor.IsPlayer;
        }
        #endregion

        #region 动作执行
        /// <summary>
        /// 执行战斗动作
        /// </summary>
        private void ExecuteAction(BattleAction _action)
        {
            if (!m_IsBattleActive || _action.Actor == null) return;

            string actionDescription = "";

            switch (_action.ActionType)
            {
                case BattleActionType.Attack:
                    actionDescription = ExecuteAttack(_action.Actor, _action.Target);
                    break;

                case BattleActionType.Defend:
                    actionDescription = ExecuteDefend(_action.Actor);
                    break;

                case BattleActionType.UseItem:
                    actionDescription = ExecuteUseItem(_action.Actor, _action.UsedItem);
                    break;

                case BattleActionType.Escape:
                    actionDescription = ExecuteEscape(_action.Actor);
                    break;
            }

            OnActionExecuted?.Invoke(_action, actionDescription);

            // 检查战斗是否结束
            if (m_IsBattleActive)
            {
                CheckBattleEnd();
            }

            // 如果战斗还在继续，切换回合
            if (m_IsBattleActive && _action.ActionType != BattleActionType.Escape)
            {
                StartCoroutine(DelayedSwitchTurn());
            }
        }

        /// <summary>
        /// 延迟切换回合
        /// </summary>
        private IEnumerator DelayedSwitchTurn()
        {
            yield return new WaitForSeconds(m_ActionDelay);
            SwitchTurn();
        }

        /// <summary>
        /// 执行攻击
        /// </summary>
        private string ExecuteAttack(BattleParticipant _attacker, BattleParticipant _target)
        {
            if (_target == null) return "";

            // 计算伤害
            int damage = CalculateDamage(_attacker.Stats, _target.Stats);
            bool isCritical = UnityEngine.Random.Range(0f, 1f) < _attacker.Stats.CriticalRate;

            if (isCritical)
            {
                damage = Mathf.RoundToInt(damage * _attacker.Stats.CriticalDamage);
            }

            // 应用伤害
            _target.Stats.TakeDamage(damage);

            OnDamageDealt?.Invoke(_target, damage, isCritical);

            string criticalText = isCritical ? "暴击！" : "";
            return $"{_attacker.Name} 攻击了 {_target.Name}，造成 {damage} 点伤害！{criticalText}";
        }

        /// <summary>
        /// 执行防御
        /// </summary>
        private string ExecuteDefend(BattleParticipant _defender)
        {
            // 防御状态会在受到攻击时减少伤害
            return $"{_defender.Name} 进入了防御状态！";
        }

        /// <summary>
        /// 执行使用物品
        /// </summary>
        private string ExecuteUseItem(BattleParticipant _user, ItemData _item)
        {
            if (_item == null || m_InventorySystem == null) return "";

            bool usedSuccessfully = _item.UseItem(_user.Stats);

            if (usedSuccessfully)
            {
                // 从背包移除物品
                m_InventorySystem.RemoveItem(_item, 1);
                return $"{_user.Name} 使用了 {_item.FullName}！";
            }

            return $"{_user.Name} 无法使用 {_item.FullName}。";
        }

        /// <summary>
        /// 执行逃跑
        /// </summary>
        private string ExecuteEscape(BattleParticipant _escaper)
        {
            if (UnityEngine.Random.Range(0f, 1f) < m_EscapeChance)
            {
                EndBattle(BattleResult.Escape);
                return $"{_escaper.Name} 成功逃脱了战斗！";
            }
            else
            {
                return $"{_escaper.Name} 逃跑失败！";
            }
        }
        #endregion

        #region 伤害计算
        /// <summary>
        /// 计算伤害
        /// </summary>
        private int CalculateDamage(CharacterStats _attacker, CharacterStats _defender)
        {
            // 基础伤害 = 攻击力 - 防御力
            int baseDamage = _attacker.Attack - _defender.Defense;
            
            // 确保最小伤害为1
            baseDamage = Mathf.Max(1, baseDamage);

            // 添加随机浮动 (80% - 120%)
            float randomFactor = UnityEngine.Random.Range(0.8f, 1.2f);
            int finalDamage = Mathf.RoundToInt(baseDamage * randomFactor);

            return Mathf.Max(1, finalDamage);
        }

        /// <summary>
        /// 计算经验值奖励
        /// </summary>
        private int CalculateExperienceReward(int _enemyLevel)
        {
            int baseExp = 25;
            int levelMultiplier = _enemyLevel;
            return baseExp * levelMultiplier;
        }
        #endregion

        #region 敌人生成
        /// <summary>
        /// 生成随机敌人
        /// </summary>
        private BattleParticipant GenerateRandomEnemy(int _level)
        {
            // 随机选择敌人名称
            string enemyName = m_EnemyNames[UnityEngine.Random.Range(0, m_EnemyNames.Length)];

            // 创建敌人属性
            CharacterStats enemyStats = new CharacterStats(_level);
            
            // 根据等级调整敌人属性
            float difficultyMultiplier = UnityEngine.Random.Range(0.8f, 1.2f);
            enemyStats.m_MaxHealth = Mathf.RoundToInt(enemyStats.MaxHealth * difficultyMultiplier);
            enemyStats.m_CurrentHealth = enemyStats.MaxHealth;
            enemyStats.m_Attack = Mathf.RoundToInt(enemyStats.Attack * difficultyMultiplier);
            enemyStats.m_Defense = Mathf.RoundToInt(enemyStats.Defense * difficultyMultiplier);
            enemyStats.m_Speed = Mathf.RoundToInt(enemyStats.Speed * difficultyMultiplier);

            return new BattleParticipant(enemyName, enemyStats, false);
        }
        #endregion

        #region 奖励生成
        /// <summary>
        /// 生成战斗奖励
        /// </summary>
        private List<ItemData> GenerateBattleRewards(int _enemyLevel, int _playerLuck)
        {
            List<ItemData> rewards = new List<ItemData>();

            // 50%概率获得物品奖励
            if (UnityEngine.Random.Range(0f, 1f) < 0.5f)
            {
                RandomItemGenerator itemGenerator = RandomItemGenerator.Instance;
                if (itemGenerator != null)
                {
                    ItemData reward = itemGenerator.GenerateRandomItem(_enemyLevel, _playerLuck);
                    if (reward != null)
                    {
                        rewards.Add(reward);
                    }
                }
            }

            return rewards;
        }
        #endregion

        #region 战斗检查
        /// <summary>
        /// 检查战斗是否结束
        /// </summary>
        private void CheckBattleEnd()
        {
            if (!m_Player.IsAlive)
            {
                EndBattle(BattleResult.Defeat);
            }
            else if (!m_Enemy.IsAlive)
            {
                EndBattle(BattleResult.Victory);
            }
        }
        #endregion

        #region 调试方法
#if UNITY_EDITOR
        [ContextMenu("开始测试战斗")]
        private void StartTestBattle()
        {
            CharacterStats testPlayerStats = new CharacterStats(5);
            StartBattle(testPlayerStats, 5);
        }

        [ContextMenu("玩家攻击")]
        private void TestPlayerAttack()
        {
            PlayerAttack();
        }

        [ContextMenu("玩家防御")]
        private void TestPlayerDefend()
        {
            PlayerDefend();
        }

        [ContextMenu("玩家逃跑")]
        private void TestPlayerEscape()
        {
            PlayerEscape();
        }
#endif
        #endregion
    }
}