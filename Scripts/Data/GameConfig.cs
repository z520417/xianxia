using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 游戏配置数据
    /// 统一管理所有游戏中的配置参数，避免硬编码
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "仙侠游戏/游戏配置")]
    public class GameConfig : ScriptableObject
    {
        [Header("角色属性配置")]
        [SerializeField] private CharacterStatsConfig m_CharacterStats;
        
        [Header("探索配置")]
        [SerializeField] private ExplorationConfig m_Exploration;
        
        [Header("战斗配置")]
        [SerializeField] private BattleConfig m_Battle;
        
        [Header("背包配置")]
        [SerializeField] private InventoryConfig m_Inventory;
        
        [Header("UI配置")]
        [SerializeField] private UIConfig m_UI;
        
        [Header("音效配置")]
        [SerializeField] private AudioConfig m_Audio;

        public CharacterStatsConfig CharacterStats => m_CharacterStats;
        public ExplorationConfig Exploration => m_Exploration;
        public BattleConfig Battle => m_Battle;
        public InventoryConfig Inventory => m_Inventory;
        public UIConfig UI => m_UI;
        public AudioConfig Audio => m_Audio;
    }

    [System.Serializable]
    public class CharacterStatsConfig
    {
        [Header("等级成长配置")]
        public int BaseHealth = 100;
        public int HealthPerLevel = 20;
        public int BaseMana = 50;
        public int ManaPerLevel = 10;
        public int BaseAttack = 10;
        public int AttackPerLevel = 3;
        public int BaseDefense = 5;
        public int DefensePerLevel = 2;
        public int BaseSpeed = 10;
        public int SpeedPerLevel = 1;
        public int ExperiencePerLevel = 100;
        
        [Header("暴击配置")]
        public float BaseCriticalRate = 0.05f;
        public float BaseCriticalDamage = 1.5f;
    }

    [System.Serializable]
    public class ExplorationConfig
    {
        [Header("探索事件概率")]
        public float TreasureChance = 30f;
        public float BattleChance = 40f;
        public float ItemChance = 20f;
        public float NothingChance = 10f;
        
        [Header("奖励配置")]
        public int MinGoldReward = 50;
        public int MaxGoldReward = 200;
        public int NothingFoundExp = 5;
    }

    [System.Serializable]
    public class BattleConfig
    {
        [Header("战斗时间配置")]
        public float ActionDelay = 1f;
        public float TurnSwitchDelay = 1f;
        
        [Header("战斗平衡配置")]
        public float EscapeChance = 0.7f;
        public int DefenseReduction = 50;
        public float EnemyDifficultyMin = 0.8f;
        public float EnemyDifficultyMax = 1.2f;
        public int EnemyLevelVariance = 2;
        
        [Header("奖励配置")]
        public int BaseExperienceReward = 25;
        public float ItemRewardChance = 0.5f;
        
        [Header("失败惩罚")]
        public float GoldLossPercentage = 0.1f;
        public int MaxGoldLoss = 100;
    }

    [System.Serializable]
    public class InventoryConfig
    {
        [Header("背包配置")]
        public int DefaultMaxSlots = 50;
        public int MaxUpgradeSlots = 100;
        
        [Header("物品堆叠配置")]
        public int DefaultStackSize = 99;
        public int ConsumableStackSize = 20;
        public int MaterialStackSize = 999;
        public int TreasureStackSize = 1;
    }

    [System.Serializable]
    public class UIConfig
    {
        [Header("消息系统")]
        public int MaxMessageLines = 20;
        public float MessageFadeTime = 0.5f;
        
        [Header("UI动画")]
        public float UIFadeSpeed = 2f;
        public float ButtonScaleEffect = 1.1f;
        
        [Header("刷新频率")]
        public float UIUpdateInterval = 0.1f;
        public float BattleUIUpdateInterval = 0.05f;
    }

    [System.Serializable]
    public class AudioConfig
    {
        [Header("音效开关")]
        public bool EnableSFX = true;
        public bool EnableMusic = true;
        
        [Header("音量配置")]
        public float MasterVolume = 1f;
        public float SFXVolume = 0.8f;
        public float MusicVolume = 0.6f;
    }
}