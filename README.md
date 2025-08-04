# 仙侠探索挖宝文字游戏

一个基于Unity的仙侠主题探索挖宝文字游戏，具有随机装备生成系统和简单战斗机制。

## 游戏特色

### 核心玩法
- **随机装备生成**：基于稀有度的装备生成系统，包含6种稀有度等级
- **探索系统**：随机遭遇战斗、发现宝藏、获得物品
- **战斗系统**：回合制战斗，支持攻击、防御、使用物品、逃跑
- **背包管理**：完整的物品管理系统，支持物品堆叠和整理
- **装备系统**：6种装备类型，实时属性加成计算

### 游戏系统

#### 物品系统
- **装备类型**：武器、头盔、护甲、靴子、戒指、项链
- **稀有度等级**：普通、不凡、稀有、史诗、传说、神话
- **属性系统**：生命值、法力值、攻击力、防御力、速度、暴击率、运气
- **消耗品**：恢复类、增益类药品

#### 战斗系统
- **回合制战斗**：基于速度的先手判定
- **AI系统**：敌人自动战斗行为
- **伤害计算**：考虑攻击力、防御力、暴击等因素
- **经验奖励**：战斗胜利获得经验值和物品奖励

#### 探索系统
- **随机事件**：宝藏、战斗、物品、空手而归
- **运气影响**：运气值影响稀有物品获得概率
- **挖宝机制**：特殊的宝藏挖掘，获得更好的物品

## 代码架构

### 数据层
```
Scripts/Data/
├── Enums.cs                # 游戏枚举定义
├── CharacterStats.cs       # 角色属性系统
├── ItemData.cs            # 物品数据基类
├── EquipmentData.cs       # 装备数据
└── ConsumableData.cs      # 消耗品数据
```

### 系统层
```
Scripts/Systems/
├── RandomItemGenerator.cs  # 随机物品生成器
├── InventorySystem.cs      # 背包管理系统
├── EquipmentManager.cs     # 装备管理系统
└── BattleSystem.cs         # 战斗系统
```

### 管理层
```
Scripts/Managers/
└── GameManager.cs          # 游戏主管理器
```

### UI层
```
Scripts/UI/
└── MainGameUI.cs           # 主游戏界面
```

## 使用方法

### 基本设置
1. 在场景中创建空GameObject命名为"GameManager"
2. 添加`GameManager`组件
3. 创建UI Canvas并添加`MainGameUI`组件
4. 设置UI组件引用

### 核心组件使用

#### 游戏管理器
```csharp
// 获取游戏管理器实例
GameManager gameManager = GameManager.Instance;

// 开始探索
gameManager.Explore();

// 保存/加载游戏
gameManager.SaveGame();
gameManager.LoadGame();
```

#### 随机物品生成
```csharp
// 获取随机物品生成器
RandomItemGenerator generator = RandomItemGenerator.Instance;

// 生成随机物品
ItemData item = generator.GenerateRandomItem(playerLevel, luckValue);

// 挖宝生成物品
List<ItemData> treasures = generator.DigTreasure(playerLevel, luckValue);
```

#### 背包系统
```csharp
// 添加物品到背包
inventorySystem.AddItem(item, quantity);

// 使用物品
inventorySystem.UseItem(slotIndex, characterStats);

// 整理背包
inventorySystem.SortInventory();
```

#### 装备系统
```csharp
// 装备物品
equipmentManager.EquipItem(equipment);

// 卸下装备
equipmentManager.UnequipItem(equipmentType);

// 获取总属性
CharacterStats totalStats = equipmentManager.TotalStats;
```

#### 战斗系统
```csharp
// 开始战斗
battleSystem.StartBattle(playerStats, enemyLevel);

// 玩家行动
battleSystem.PlayerAttack();
battleSystem.PlayerDefend();
battleSystem.PlayerEscape();
```

## 配置说明

### 稀有度权重配置
在`RandomItemGenerator`中可以调整各种稀有度的生成概率：
```csharp
[SerializeField] private float[] m_RarityWeights = 
{
    50f,  // 普通
    25f,  // 不凡
    15f,  // 稀有
    7f,   // 史诗
    2.5f, // 传说
    0.5f  // 神话
};
```

### 探索事件概率
在`GameManager`中可以调整探索事件的发生概率：
```csharp
[SerializeField] private float[] m_ExploreEventChances = 
{
    30f,    // 发现宝藏
    40f,    // 遭遇战斗
    20f,    // 发现物品
    10f     // 什么都没有
};
```

## 扩展性

### 添加新物品类型
1. 在`ItemType`枚举中添加新类型
2. 继承`ItemData`创建新的物品数据类
3. 在`RandomItemGenerator`中添加生成逻辑

### 添加新装备类型
1. 在`EquipmentType`枚举中添加新类型
2. 在`EquipmentManager`中添加对应槽位
3. 更新UI显示逻辑

### 添加新战斗技能
1. 扩展`BattleActionType`枚举
2. 在`BattleSystem`中添加技能执行逻辑
3. 更新AI和玩家行动选择

## 游戏流程

1. **游戏开始**：创建角色，获得初始装备和物品
2. **探索阶段**：点击探索按钮，随机触发事件
3. **战斗阶段**：遭遇敌人时进入回合制战斗
4. **物品管理**：查看背包，装备物品，使用消耗品
5. **角色成长**：通过战斗获得经验值提升等级
6. **挖宝寻宝**：发现宝藏获得稀有物品和装备

## 技术特点

- **模块化设计**：各系统独立，易于扩展和维护
- **事件驱动**：基于Unity Event的松耦合架构
- **数据驱动**：使用ScriptableObject存储游戏数据
- **随机生成**：支持各种随机内容生成
- **保存系统**：简单的游戏进度保存加载
- **UI适配**：支持不同分辨率的UI显示

## 开发注意事项

1. 所有脚本都使用`XianXiaGame`命名空间
2. 遵循Unity C# 编程规范
3. 使用SerializeField暴露需要在Inspector中配置的字段
4. 所有UI文本建议使用TextMeshPro
5. 重要的游戏事件都有相应的事件通知

这是一个完整的仙侠主题文字游戏框架，可以作为基础进行更多功能的扩展和完善。