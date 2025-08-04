using System;
using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 角色属性数据结构
    /// </summary>
    [Serializable]
    public class CharacterStats
    {
        #region 基础属性
        [Header("基础属性")]
        [SerializeField] public int m_Level = 1;                  // 等级
        [SerializeField] public int m_Experience = 0;             // 经验值
        [SerializeField] public int m_ExperienceToNext = 100;     // 升级所需经验
        
        [SerializeField] public int m_MaxHealth = 100;            // 最大生命值
        [SerializeField] public int m_CurrentHealth = 100;        // 当前生命值
        [SerializeField] public int m_MaxMana = 50;               // 最大法力值
        [SerializeField] public int m_CurrentMana = 50;           // 当前法力值
        #endregion

        #region 战斗属性
        [Header("战斗属性")]
        [SerializeField] public int m_Attack = 10;                // 攻击力
        [SerializeField] public int m_Defense = 5;                // 防御力
        [SerializeField] public int m_Speed = 10;                 // 速度
        [SerializeField] public float m_CriticalRate = 0.05f;     // 暴击率
        [SerializeField] public float m_CriticalDamage = 1.5f;    // 暴击伤害倍数
        #endregion

        #region 修炼属性
        [Header("修炼属性")]
        [SerializeField] public int m_Cultivation = 0;            // 修为
        [SerializeField] public int m_Luck = 10;                  // 运气值
        #endregion

        #region 公共属性
        public int Level => m_Level;
        public int Experience => m_Experience;
        public int ExperienceToNext => m_ExperienceToNext;
        
        public int MaxHealth => m_MaxHealth;
        public int CurrentHealth => m_CurrentHealth;
        public int MaxMana => m_MaxMana;
        public int CurrentMana => m_CurrentMana;
        
        public int Attack => m_Attack;
        public int Defense => m_Defense;
        public int Speed => m_Speed;
        public float CriticalRate => m_CriticalRate;
        public float CriticalDamage => m_CriticalDamage;
        
        public int Cultivation => m_Cultivation;
        public int Luck => m_Luck;
        
        public bool IsAlive => m_CurrentHealth > 0;
        public float HealthPercentage => (float)m_CurrentHealth / m_MaxHealth;
        public float ManaPercentage => (float)m_CurrentMana / m_MaxMana;
        #endregion

        #region 构造函数
        public CharacterStats()
        {
            Initialize();
        }

        public CharacterStats(int _level)
        {
            m_Level = _level;
            Initialize();
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化角色属性
        /// </summary>
        private void Initialize()
        {
            CalculateStatsForLevel();
            m_CurrentHealth = m_MaxHealth;
            m_CurrentMana = m_MaxMana;
        }

        /// <summary>
        /// 根据等级计算属性
        /// </summary>
        private void CalculateStatsForLevel()
        {
            m_MaxHealth = 100 + (m_Level - 1) * 20;
            m_MaxMana = 50 + (m_Level - 1) * 10;
            m_Attack = 10 + (m_Level - 1) * 3;
            m_Defense = 5 + (m_Level - 1) * 2;
            m_Speed = 10 + (m_Level - 1) * 1;
            m_ExperienceToNext = m_Level * 100;
        }
        #endregion

        #region 属性修改方法
        /// <summary>
        /// 添加属性加成
        /// </summary>
        public void AddStats(CharacterStats _bonusStats)
        {
            m_MaxHealth += _bonusStats.m_MaxHealth;
            m_MaxMana += _bonusStats.m_MaxMana;
            m_Attack += _bonusStats.m_Attack;
            m_Defense += _bonusStats.m_Defense;
            m_Speed += _bonusStats.m_Speed;
            m_CriticalRate += _bonusStats.m_CriticalRate;
            m_CriticalDamage += _bonusStats.m_CriticalDamage;
            m_Luck += _bonusStats.m_Luck;
        }

        /// <summary>
        /// 移除属性加成
        /// </summary>
        public void RemoveStats(CharacterStats _bonusStats)
        {
            m_MaxHealth -= _bonusStats.m_MaxHealth;
            m_MaxMana -= _bonusStats.m_MaxMana;
            m_Attack -= _bonusStats.m_Attack;
            m_Defense -= _bonusStats.m_Defense;
            m_Speed -= _bonusStats.m_Speed;
            m_CriticalRate -= _bonusStats.m_CriticalRate;
            m_CriticalDamage -= _bonusStats.m_CriticalDamage;
            m_Luck -= _bonusStats.m_Luck;
        }
        #endregion

        #region 生命值管理
        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(int _amount)
        {
            m_CurrentHealth = Mathf.Min(m_CurrentHealth + _amount, m_MaxHealth);
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(int _damage)
        {
            m_CurrentHealth = Mathf.Max(0, m_CurrentHealth - _damage);
        }

        /// <summary>
        /// 完全治愈
        /// </summary>
        public void FullHeal()
        {
            m_CurrentHealth = m_MaxHealth;
            m_CurrentMana = m_MaxMana;
        }
        #endregion

        #region 经验值管理
        /// <summary>
        /// 获得经验值
        /// </summary>
        public bool GainExperience(int _exp)
        {
            m_Experience += _exp;
            
            if (m_Experience >= m_ExperienceToNext)
            {
                LevelUp();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 升级
        /// </summary>
        private void LevelUp()
        {
            m_Experience -= m_ExperienceToNext;
            m_Level++;
            
            int previousMaxHealth = m_MaxHealth;
            int previousMaxMana = m_MaxMana;
            
            CalculateStatsForLevel();
            
            // 升级时恢复生命值和法力值
            m_CurrentHealth += (m_MaxHealth - previousMaxHealth);
            m_CurrentMana += (m_MaxMana - previousMaxMana);
            
            Debug.Log($"等级提升！当前等级：{m_Level}");
        }
        #endregion

        #region 复制方法
        /// <summary>
        /// 创建属性副本
        /// </summary>
        public CharacterStats Clone()
        {
            CharacterStats clone = new CharacterStats();
            clone.m_Level = this.m_Level;
            clone.m_Experience = this.m_Experience;
            clone.m_ExperienceToNext = this.m_ExperienceToNext;
            clone.m_MaxHealth = this.m_MaxHealth;
            clone.m_CurrentHealth = this.m_CurrentHealth;
            clone.m_MaxMana = this.m_MaxMana;
            clone.m_CurrentMana = this.m_CurrentMana;
            clone.m_Attack = this.m_Attack;
            clone.m_Defense = this.m_Defense;
            clone.m_Speed = this.m_Speed;
            clone.m_CriticalRate = this.m_CriticalRate;
            clone.m_CriticalDamage = this.m_CriticalDamage;
            clone.m_Cultivation = this.m_Cultivation;
            clone.m_Luck = this.m_Luck;
            
            return clone;
        }
        #endregion
    }
}