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
        [SerializeField] private int m_Level = 1;                  // 等级
        [SerializeField] private int m_Experience = 0;             // 经验值
        [SerializeField] private int m_ExperienceToNext = 100;     // 升级所需经验
        
        [SerializeField] private int m_MaxHealth = 100;            // 最大生命值
        [SerializeField] private int m_CurrentHealth = 100;        // 当前生命值
        [SerializeField] private int m_MaxMana = 50;               // 最大法力值
        [SerializeField] private int m_CurrentMana = 50;           // 当前法力值
        #endregion

        #region 战斗属性
        [Header("战斗属性")]
        [SerializeField] private int m_Attack = 10;                // 攻击力
        [SerializeField] private int m_Defense = 5;                // 防御力
        [SerializeField] private int m_Speed = 10;                 // 速度
        [SerializeField] private float m_CriticalRate = 0.05f;     // 暴击率
        [SerializeField] private float m_CriticalDamage = 1.5f;    // 暴击伤害倍数
        #endregion

        #region 修炼属性
        [Header("修炼属性")]
        [SerializeField] private int m_Cultivation = 0;            // 修为
        [SerializeField] private int m_Luck = 10;                  // 运气值
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
            // 使用配置管理器获取配置，如果配置不可用则使用默认值
            var config = ConfigManager.Instance?.Config?.CharacterStats;
            
            if (config != null)
            {
                m_MaxHealth = config.BaseHealth + (m_Level - 1) * config.HealthPerLevel;
                m_MaxMana = config.BaseMana + (m_Level - 1) * config.ManaPerLevel;
                m_Attack = config.BaseAttack + (m_Level - 1) * config.AttackPerLevel;
                m_Defense = config.BaseDefense + (m_Level - 1) * config.DefensePerLevel;
                m_Speed = config.BaseSpeed + (m_Level - 1) * config.SpeedPerLevel;
                m_ExperienceToNext = m_Level * config.ExperiencePerLevel;
                
                // 设置暴击相关属性
                m_CriticalRate = config.BaseCriticalRate;
                m_CriticalDamage = config.BaseCriticalDamage;
            }
            else
            {
                // 配置不可用时的默认值（保持向后兼容）
                m_MaxHealth = 100 + (m_Level - 1) * 20;
                m_MaxMana = 50 + (m_Level - 1) * 10;
                m_Attack = 10 + (m_Level - 1) * 3;
                m_Defense = 5 + (m_Level - 1) * 2;
                m_Speed = 10 + (m_Level - 1) * 1;
                m_ExperienceToNext = m_Level * 100;
                m_CriticalRate = 0.05f;
                m_CriticalDamage = 1.5f;
                
                Debug.LogWarning("CharacterStats: 配置管理器不可用，使用默认配置");
            }
        }
        #endregion

        #region 属性修改方法
        /// <summary>
        /// 添加属性加成
        /// </summary>
        public void AddStats(CharacterStats _bonusStats)
        {
            m_MaxHealth += _bonusStats.MaxHealth;
            m_MaxMana += _bonusStats.MaxMana;
            m_Attack += _bonusStats.Attack;
            m_Defense += _bonusStats.Defense;
            m_Speed += _bonusStats.Speed;
            m_CriticalRate += _bonusStats.CriticalRate;
            m_CriticalDamage += _bonusStats.CriticalDamage;
            m_Luck += _bonusStats.Luck;
        }

        /// <summary>
        /// 移除属性加成
        /// </summary>
        public void RemoveStats(CharacterStats _bonusStats)
        {
            m_MaxHealth -= _bonusStats.MaxHealth;
            m_MaxMana -= _bonusStats.MaxMana;
            m_Attack -= _bonusStats.Attack;
            m_Defense -= _bonusStats.Defense;
            m_Speed -= _bonusStats.Speed;
            m_CriticalRate -= _bonusStats.CriticalRate;
            m_CriticalDamage -= _bonusStats.CriticalDamage;
            m_Luck -= _bonusStats.Luck;
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
        /// 恢复法力值
        /// </summary>
        public void RestoreMana(int _amount)
        {
            m_CurrentMana = Mathf.Min(m_CurrentMana + _amount, m_MaxMana);
        }

        /// <summary>
        /// 消耗法力值
        /// </summary>
        public bool ConsumeMana(int _amount)
        {
            if (m_CurrentMana >= _amount)
            {
                m_CurrentMana -= _amount;
                return true;
            }
            return false;
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

        /// <summary>
        /// 设置属性值（用于内部修改）
        /// </summary>
        public void SetStats(int _maxHealth, int _maxMana, int _attack, int _defense, int _speed, 
            float _criticalRate, float _criticalDamage, int _luck)
        {
            m_MaxHealth = _maxHealth;
            m_MaxMana = _maxMana;
            m_Attack = _attack;
            m_Defense = _defense;
            m_Speed = _speed;
            m_CriticalRate = _criticalRate;
            m_CriticalDamage = _criticalDamage;
            m_Luck = _luck;
        }

        /// <summary>
        /// 调整属性（用于敌人生成等场景）
        /// </summary>
        public void ModifyStats(float _healthMultiplier, float _attackMultiplier, float _defenseMultiplier, float _speedMultiplier)
        {
            m_MaxHealth = Mathf.RoundToInt(m_MaxHealth * _healthMultiplier);
            m_CurrentHealth = m_MaxHealth; // 设置当前生命值为最大生命值
            m_Attack = Mathf.RoundToInt(m_Attack * _attackMultiplier);
            m_Defense = Mathf.RoundToInt(m_Defense * _defenseMultiplier);
            m_Speed = Mathf.RoundToInt(m_Speed * _speedMultiplier);
        }

        /// <summary>
        /// 添加单个属性值（用于消耗品增益等）
        /// </summary>
        public void AddSingleStat(int _statType, float _value)
        {
            switch (_statType)
            {
                case 0: // 攻击力
                    m_Attack += Mathf.RoundToInt(_value);
                    break;
                case 1: // 防御力
                    m_Defense += Mathf.RoundToInt(_value);
                    break;
                case 2: // 速度
                    m_Speed += Mathf.RoundToInt(_value);
                    break;
                case 3: // 暴击率
                    m_CriticalRate += _value;
                    break;
                case 4: // 运气
                    m_Luck += Mathf.RoundToInt(_value);
                    break;
            }
        }

        /// <summary>
        /// 设置经验值（用于存档加载等）
        /// </summary>
        public void SetExperience(int _experience)
        {
            m_Experience = _experience;
        }

        /// <summary>
        /// 设置等级（用于存档加载等）
        /// </summary>
        public void SetLevel(int _level)
        {
            m_Level = _level;
            CalculateStatsForLevel();
        }
        #endregion
    }
}