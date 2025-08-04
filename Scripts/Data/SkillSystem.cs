using System.Collections.Generic;
using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 技能类型
    /// </summary>
    public enum SkillType
    {
        Active,    // 主动技能
        Passive,   // 被动技能
        Toggle,    // 切换技能
        Combo      // 连击技能
    }

    /// <summary>
    /// 技能目标类型
    /// </summary>
    public enum SkillTargetType
    {
        Self,        // 自身
        Enemy,       // 敌人
        Ally,        // 队友
        Area,        // 区域
        All          // 所有单位
    }

    /// <summary>
    /// 技能数据
    /// </summary>
    [CreateAssetMenu(fileName = "New Skill", menuName = "仙侠游戏/技能数据")]
    public class SkillData : ScriptableObject
    {
        [Header("基本信息")]
        public string SkillId;
        public string SkillName;
        [TextArea(3, 5)]
        public string Description;
        public Sprite Icon;
        public SkillType SkillType;
        public SkillTargetType TargetType;
        
        [Header("等级配置")]
        public int MaxLevel = 10;
        public int UnlockLevel = 1;
        public List<int> LevelUpCosts = new List<int>(); // 升级所需经验/金钱
        
        [Header("使用条件")]
        public int ManaCost = 0;
        public float CooldownTime = 0f;
        public float CastTime = 0f;
        public float Range = 1f;
        
        [Header("技能效果")]
        public List<SkillEffect> Effects = new List<SkillEffect>();
        
        [Header("动画和音效")]
        public string CastAnimation;
        public string ImpactAnimation;
        public string CastSound;
        public string ImpactSound;
        public ParticleSystem CastEffect;
        public ParticleSystem ImpactEffect;
        
        [Header("特殊属性")]
        public List<string> SkillTags = new List<string>();
        public bool CanCrit = true;
        public bool AffectedByGlobalCooldown = true;

        /// <summary>
        /// 获取指定等级的技能效果
        /// </summary>
        public List<SkillEffect> GetEffectsAtLevel(int level)
        {
            var scaledEffects = new List<SkillEffect>();
            
            foreach (var effect in Effects)
            {
                var scaledEffect = effect.ScaleToLevel(level);
                scaledEffects.Add(scaledEffect);
            }
            
            return scaledEffects;
        }

        /// <summary>
        /// 计算指定等级的法力消耗
        /// </summary>
        public int CalculateManaCost(int level)
        {
            // 可以根据等级调整法力消耗
            return Mathf.RoundToInt(ManaCost * (1f + level * 0.05f));
        }

        /// <summary>
        /// 计算指定等级的冷却时间
        /// </summary>
        public float CalculateCooldown(int level)
        {
            // 高等级技能冷却时间可能减少
            return CooldownTime * Mathf.Max(0.1f, 1f - level * 0.02f);
        }

        /// <summary>
        /// 检查是否可以使用技能
        /// </summary>
        public bool CanUse(CharacterStats caster, CharacterStats target = null)
        {
            // 检查法力值
            if (caster.CurrentMana < ManaCost)
                return false;

            // 检查目标类型
            if (TargetType == SkillTargetType.Enemy && target == null)
                return false;

            // 这里可以添加更多条件检查

            return true;
        }
    }

    /// <summary>
    /// 技能效果
    /// </summary>
    [System.Serializable]
    public class SkillEffect
    {
        [Header("效果类型")]
        public SkillEffectType EffectType;
        public string EffectName;
        
        [Header("数值配置")]
        public float BaseValue;
        public float ValuePerLevel = 0f;
        public AnimationCurve LevelScaling;
        
        [Header("持续时间")]
        public float Duration = 0f; // 0表示瞬间效果
        public int TickCount = 1;   // 生效次数
        public float TickInterval = 1f; // 间隔时间
        
        [Header("概率配置")]
        [Range(0f, 1f)]
        public float Chance = 1f;
        
        [Header("附加条件")]
        public List<string> RequiredConditions = new List<string>();
        public List<string> AppliedConditions = new List<string>();

        /// <summary>
        /// 缩放到指定等级
        /// </summary>
        public SkillEffect ScaleToLevel(int level)
        {
            var scaledEffect = new SkillEffect();
            scaledEffect.EffectType = this.EffectType;
            scaledEffect.EffectName = this.EffectName;
            scaledEffect.Duration = this.Duration;
            scaledEffect.TickCount = this.TickCount;
            scaledEffect.TickInterval = this.TickInterval;
            scaledEffect.Chance = this.Chance;
            scaledEffect.RequiredConditions = new List<string>(this.RequiredConditions);
            scaledEffect.AppliedConditions = new List<string>(this.AppliedConditions);
            
            // 计算缩放后的数值
            float scaledValue = BaseValue + ValuePerLevel * level;
            
            if (LevelScaling != null && LevelScaling.keys.Length > 0)
            {
                scaledValue *= LevelScaling.Evaluate(level);
            }
            
            scaledEffect.BaseValue = scaledValue;
            scaledEffect.ValuePerLevel = 0f; // 已经缩放过了
            
            return scaledEffect;
        }

        /// <summary>
        /// 应用效果
        /// </summary>
        public void Apply(CharacterStats caster, CharacterStats target)
        {
            if (Random.Range(0f, 1f) > Chance)
                return;

            switch (EffectType)
            {
                case SkillEffectType.Damage:
                    ApplyDamage(caster, target);
                    break;
                case SkillEffectType.Heal:
                    ApplyHeal(target);
                    break;
                case SkillEffectType.BuffStat:
                    ApplyStatBuff(target);
                    break;
                case SkillEffectType.DebuffStat:
                    ApplyStatDebuff(target);
                    break;
                case SkillEffectType.RestoreMana:
                    ApplyManaRestore(target);
                    break;
                // 可以添加更多效果类型
            }
        }

        private void ApplyDamage(CharacterStats caster, CharacterStats target)
        {
            int damage = Mathf.RoundToInt(BaseValue + caster.Attack * 0.1f);
            target.TakeDamage(damage);
            GameLog.Debug($"造成伤害: {damage}", "Skill");
        }

        private void ApplyHeal(CharacterStats target)
        {
            int healAmount = Mathf.RoundToInt(BaseValue);
            target.Heal(healAmount);
            GameLog.Debug($"治疗: {healAmount}", "Skill");
        }

        private void ApplyStatBuff(CharacterStats target)
        {
            // 这里需要实现状态效果系统
            GameLog.Debug($"应用增益: {EffectName}", "Skill");
        }

        private void ApplyStatDebuff(CharacterStats target)
        {
            // 这里需要实现状态效果系统
            GameLog.Debug($"应用减益: {EffectName}", "Skill");
        }

        private void ApplyManaRestore(CharacterStats target)
        {
            int manaAmount = Mathf.RoundToInt(BaseValue);
            target.RestoreMana(manaAmount);
            GameLog.Debug($"恢复法力: {manaAmount}", "Skill");
        }
    }

    /// <summary>
    /// 技能效果类型
    /// </summary>
    public enum SkillEffectType
    {
        Damage,         // 造成伤害
        Heal,           // 治疗
        BuffStat,       // 属性增益
        DebuffStat,     // 属性减益
        RestoreMana,    // 恢复法力
        Stun,           // 眩晕
        Poison,         // 中毒
        Burn,           // 燃烧
        Freeze,         // 冰冻
        Shield,         // 护盾
        Teleport,       // 传送
        Summon,         // 召唤
        Transform,      // 变身
        Special         // 特殊效果
    }

    /// <summary>
    /// 技能学习树节点
    /// </summary>
    [System.Serializable]
    public class SkillTreeNode
    {
        [Header("节点信息")]
        public string NodeId;
        public SkillData Skill;
        public Vector2 Position; // 在技能树中的位置
        
        [Header("学习条件")]
        public List<string> PrerequisiteNodes = new List<string>();
        public int RequiredLevel = 1;
        public int RequiredSkillPoints = 1;
        
        [Header("节点状态")]
        public bool IsUnlocked = false;
        public int CurrentLevel = 0;
        
        [Header("视觉配置")]
        public Sprite NodeIcon;
        public Color NodeColor = Color.white;

        /// <summary>
        /// 检查是否可以学习
        /// </summary>
        public bool CanLearn(int playerLevel, int skillPoints, List<string> learnedNodes)
        {
            // 检查等级要求
            if (playerLevel < RequiredLevel)
                return false;

            // 检查技能点要求
            if (skillPoints < RequiredSkillPoints)
                return false;

            // 检查前置条件
            foreach (var prerequisite in PrerequisiteNodes)
            {
                if (!learnedNodes.Contains(prerequisite))
                    return false;
            }

            // 检查是否已经学满
            if (Skill != null && CurrentLevel >= Skill.MaxLevel)
                return false;

            return true;
        }

        /// <summary>
        /// 学习技能
        /// </summary>
        public bool LearnSkill()
        {
            if (Skill == null)
                return false;

            if (CurrentLevel < Skill.MaxLevel)
            {
                CurrentLevel++;
                IsUnlocked = true;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 技能学习树
    /// </summary>
    [CreateAssetMenu(fileName = "New Skill Tree", menuName = "仙侠游戏/技能学习树")]
    public class SkillTree : ScriptableObject
    {
        [Header("技能树信息")]
        public string TreeId;
        public string TreeName;
        [TextArea(3, 5)]
        public string Description;
        public Sprite TreeIcon;
        
        [Header("技能节点")]
        public List<SkillTreeNode> Nodes = new List<SkillTreeNode>();
        
        [Header("学习配置")]
        public int MaxSkillPoints = 100;
        public int SkillPointsPerLevel = 1;

        /// <summary>
        /// 根据ID获取节点
        /// </summary>
        public SkillTreeNode GetNode(string nodeId)
        {
            return Nodes.Find(node => node.NodeId == nodeId);
        }

        /// <summary>
        /// 获取可学习的技能节点
        /// </summary>
        public List<SkillTreeNode> GetLearnableNodes(int playerLevel, int skillPoints, List<string> learnedNodes)
        {
            var learnableNodes = new List<SkillTreeNode>();
            
            foreach (var node in Nodes)
            {
                if (node.CanLearn(playerLevel, skillPoints, learnedNodes))
                {
                    learnableNodes.Add(node);
                }
            }
            
            return learnableNodes;
        }

        /// <summary>
        /// 学习技能
        /// </summary>
        public bool LearnSkill(string nodeId, int playerLevel, int skillPoints, List<string> learnedNodes)
        {
            var node = GetNode(nodeId);
            if (node == null)
                return false;

            if (!node.CanLearn(playerLevel, skillPoints, learnedNodes))
                return false;

            return node.LearnSkill();
        }

        /// <summary>
        /// 计算已使用的技能点
        /// </summary>
        public int CalculateUsedSkillPoints()
        {
            int usedPoints = 0;
            
            foreach (var node in Nodes)
            {
                if (node.IsUnlocked)
                {
                    usedPoints += node.CurrentLevel * node.RequiredSkillPoints;
                }
            }
            
            return usedPoints;
        }

        /// <summary>
        /// 重置技能树
        /// </summary>
        public void ResetSkillTree()
        {
            foreach (var node in Nodes)
            {
                node.IsUnlocked = false;
                node.CurrentLevel = 0;
            }
        }
    }

    /// <summary>
    /// 技能管理器
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        [Header("技能配置")]
        [SerializeField] private List<SkillTree> m_SkillTrees = new List<SkillTree>();
        [SerializeField] private int m_CurrentSkillPoints = 0;
        
        [Header("学习状态")]
        [SerializeField] private List<string> m_LearnedNodes = new List<string>();
        [SerializeField] private Dictionary<string, float> m_SkillCooldowns = new Dictionary<string, float>();

        public int SkillPoints => m_CurrentSkillPoints;
        public List<string> LearnedNodes => m_LearnedNodes;

        private void Update()
        {
            UpdateCooldowns();
        }

        /// <summary>
        /// 学习技能
        /// </summary>
        public bool LearnSkill(string treeId, string nodeId, int playerLevel)
        {
            var tree = m_SkillTrees.Find(t => t.TreeId == treeId);
            if (tree == null)
                return false;

            var node = tree.GetNode(nodeId);
            if (node == null || !node.CanLearn(playerLevel, m_CurrentSkillPoints, m_LearnedNodes))
                return false;

            if (tree.LearnSkill(nodeId, playerLevel, m_CurrentSkillPoints, m_LearnedNodes))
            {
                m_CurrentSkillPoints -= node.RequiredSkillPoints;
                
                if (!m_LearnedNodes.Contains(nodeId))
                {
                    m_LearnedNodes.Add(nodeId);
                }

                GameLog.Info($"学会技能: {node.Skill?.SkillName}", "Skill");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 使用技能
        /// </summary>
        public bool UseSkill(string skillId, CharacterStats caster, CharacterStats target = null)
        {
            var skill = GetSkillById(skillId);
            if (skill == null)
                return false;

            // 检查冷却时间
            if (IsOnCooldown(skillId))
                return false;

            // 检查使用条件
            if (!skill.CanUse(caster, target))
                return false;

            // 获取技能等级
            int skillLevel = GetSkillLevel(skillId);
            if (skillLevel <= 0)
                return false;

            // 消耗法力值
            int manaCost = skill.CalculateManaCost(skillLevel);
            if (!caster.ConsumeMana(manaCost))
                return false;

            // 应用技能效果
            var effects = skill.GetEffectsAtLevel(skillLevel);
            foreach (var effect in effects)
            {
                effect.Apply(caster, target);
            }

            // 设置冷却时间
            float cooldown = skill.CalculateCooldown(skillLevel);
            SetCooldown(skillId, cooldown);

            GameLog.Info($"使用技能: {skill.SkillName}", "Skill");
            return true;
        }

        /// <summary>
        /// 添加技能点
        /// </summary>
        public void AddSkillPoints(int points)
        {
            m_CurrentSkillPoints += points;
            GameLog.Debug($"获得技能点: {points}, 总计: {m_CurrentSkillPoints}", "Skill");
        }

        /// <summary>
        /// 获取技能等级
        /// </summary>
        public int GetSkillLevel(string skillId)
        {
            foreach (var tree in m_SkillTrees)
            {
                foreach (var node in tree.Nodes)
                {
                    if (node.Skill != null && node.Skill.SkillId == skillId && node.IsUnlocked)
                    {
                        return node.CurrentLevel;
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// 根据ID获取技能
        /// </summary>
        private SkillData GetSkillById(string skillId)
        {
            foreach (var tree in m_SkillTrees)
            {
                foreach (var node in tree.Nodes)
                {
                    if (node.Skill != null && node.Skill.SkillId == skillId)
                    {
                        return node.Skill;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 检查技能是否在冷却中
        /// </summary>
        private bool IsOnCooldown(string skillId)
        {
            return m_SkillCooldowns.ContainsKey(skillId) && m_SkillCooldowns[skillId] > 0f;
        }

        /// <summary>
        /// 设置冷却时间
        /// </summary>
        private void SetCooldown(string skillId, float cooldownTime)
        {
            m_SkillCooldowns[skillId] = cooldownTime;
        }

        /// <summary>
        /// 更新冷却时间
        /// </summary>
        private void UpdateCooldowns()
        {
            var keysToUpdate = new List<string>(m_SkillCooldowns.Keys);
            
            foreach (var skillId in keysToUpdate)
            {
                if (m_SkillCooldowns[skillId] > 0f)
                {
                    m_SkillCooldowns[skillId] -= Time.deltaTime;
                    if (m_SkillCooldowns[skillId] <= 0f)
                    {
                        m_SkillCooldowns[skillId] = 0f;
                    }
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("添加10技能点")]
        private void TestAddSkillPoints()
        {
            AddSkillPoints(10);
        }

        [ContextMenu("重置所有技能树")]
        private void TestResetAllTrees()
        {
            foreach (var tree in m_SkillTrees)
            {
                tree.ResetSkillTree();
            }
            m_LearnedNodes.Clear();
            Debug.Log("所有技能树已重置");
        }
#endif
    }
}