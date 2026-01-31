# 隐藏物体设置指南 (Hidden Objects Setup Guide)

本指南将详细说明如何在Unity场景中设置隐藏物体，以配合游戏中的滤镜系统(FilterSystem)和蒙版系统(MaskSystem)使用。

## 目录
- [系统概述](#系统概述)
- [前置准备](#前置准备)
- [创建隐藏物体](#创建隐藏物体)
- [配置HiddenObject组件](#配置hiddenobject组件)
- [设置标签和图层](#设置标签和图层)
- [颜色系统配置](#颜色系统配置)
- [测试和调试](#测试和调试)
- [最佳实践](#最佳实践)
- [常见问题](#常见问题)

## 系统概述

游戏中有两个主要的隐藏物体显示系统：

### FilterSystem (滤镜系统)
- 通过颜色滤镜显示特定颜色的隐藏物体
- 影响整个屏幕的渲染
- 适合大范围的物体显示

### MaskSystem (蒙版系统)
- 通过圆形蒙版在局部区域显示隐藏物体
- 只在蒙版范围内显示物体
- 适合探索和解谜玩法

## 前置准备

### 1. 确保GameConstants脚本存在
确保项目中有`GameConstants.cs`脚本，包含以下内容：

```csharp
public static class GameConstants
{
    // 颜色标签
    public const string TAG_RED_OBJECT = "RedObject";
    public const string TAG_GREEN_OBJECT = "GreenObject";
    public const string TAG_BLUE_OBJECT = "BlueObject";
    public const string TAG_YELLOW_OBJECT = "YellowObject";
    public const string TAG_PURPLE_OBJECT = "PurpleObject";
    public const string TAG_HIDDEN_OBJECT = "HiddenObject";

    // 图层名称
    public const string LAYER_INTERACTION = "Interaction";
    public const string LAYER_BACKGROUND = "Background";

    // 颜色常量
    public static readonly Color PURPLE_COLOR = new Color(1f, 0f, 1f); // 紫色

    // 根据FilterColor获取对应标签
    public static string GetColorTag(FilterColor color)
    {
        switch (color)
        {
            case FilterColor.Red: return TAG_RED_OBJECT;
            case FilterColor.Green: return TAG_GREEN_OBJECT;
            case FilterColor.Blue: return TAG_BLUE_OBJECT;
            case FilterColor.Yellow: return TAG_YELLOW_OBJECT;
            case FilterColor.Purple: return TAG_PURPLE_OBJECT;
            default: return TAG_RED_OBJECT;
        }
    }
}
```

### 2. 创建HiddenObject脚本
创建`HiddenObject.cs`组件脚本：

```csharp
using UnityEngine;

public class HiddenObject : MonoBehaviour
{
    [Header("Hidden Object Settings")]
    [SerializeField] public FilterColor objectColor = FilterColor.Red;
    [SerializeField] public bool enableColliderWhenRevealed = true;
    [SerializeField] public bool playRevealEffect = true;

    [Header("Visual Settings")]
    [SerializeField] private float revealAlpha = 1f;
    [SerializeField] private float hiddenAlpha = 0f;

    private SpriteRenderer spriteRenderer;
    private Collider2D objectCollider;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        objectCollider = GetComponent<Collider2D>();

        // 初始状态设为隐藏
        HideObject();
    }

    public void RevealObject()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            Color color = spriteRenderer.color;
            color.a = revealAlpha;
            spriteRenderer.color = color;
        }

        if (objectCollider != null && enableColliderWhenRevealed)
        {
            objectCollider.enabled = true;
        }
    }

    public void HideObject()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
            Color color = spriteRenderer.color;
            color.a = hiddenAlpha;
            spriteRenderer.color = color;
        }

        if (objectCollider != null)
        {
            objectCollider.enabled = false;
        }
    }
}
```

## 创建隐藏物体

### 步骤1：创建基础GameObject
1. 在Hierarchy中右键 → Create Empty
2. 重命名为描述性名称（如"HiddenPlatform_Red"）

### 步骤2：添加视觉组件
1. 添加`SpriteRenderer`组件
2. 设置Sprite（平台、道具、装饰等）
3. 设置颜色和材质

### 步骤3：添加物理组件（可选）
根据物体类型添加适当的Collider2D：
- **平台/地面**：BoxCollider2D
- **圆形物体**：CircleCollider2D
- **复杂形状**：PolygonCollider2D

### 步骤4：添加HiddenObject组件
1. 在Inspector中点击"Add Component"
2. 搜索并添加"HiddenObject"脚本
3. 配置组件参数（见下节）

## 配置HiddenObject组件

### 基本设置
- **Object Color**：选择物体对应的颜色（Red/Green/Blue/Yellow/Purple）
- **Enable Collider When Revealed**：显示时是否启用碰撞器
- **Play Reveal Effect**：是否播放显现特效

### 视觉设置
- **Reveal Alpha**：显示时的透明度（通常为1.0）
- **Hidden Alpha**：隐藏时的透明度（通常为0.0）

## 设置标签和图层

### 1. 创建标签 (Tags)
在Unity编辑器中：
1. 打开 Tags & Layers 面板（Edit → Project Settings → Tags and Layers）
2. 添加以下标签：
   - `RedObject`
   - `GreenObject`
   - `BlueObject`
   - `YellowObject`
   - `PurpleObject`
   - `HiddenObject`

### 2. 设置物体标签
为每个隐藏物体设置对应颜色的标签：
- 红色物体 → `RedObject`
- 绿色物体 → `GreenObject`
- 蓝色物体 → `BlueObject`
- 黄色物体 → `YellowObject`
- 紫色物体 → `PurpleObject`

### 3. 创建图层 (Layers)
1. 在Tags and Layers面板中添加图层：
   - `HiddenObjects`（用于隐藏物体检测）
   - `Interaction`（用于交互物体）
   - `Background`（用于背景物体）

### 4. 设置物体图层
将所有隐藏物体的Layer设置为`HiddenObjects`

## 颜色系统配置

### FilterColor枚举
确保项目中有以下枚举定义：

```csharp
public enum FilterColor
{
    Red,
    Green,
    Blue,
    Yellow,
    Purple
}
```

### 颜色对应关系
| FilterColor | 标签 | Unity颜色 | 用途 |
|-------------|------|-----------|------|
| Red | RedObject | Color.red | 红色主题物体 |
| Green | GreenObject | Color.green | 绿色主题物体 |
| Blue | BlueObject | Color.blue | 蓝色主题物体 |
| Yellow | YellowObject | Color.yellow | 黄色主题物体 |
| Purple | PurpleObject | PURPLE_COLOR | 紫色主题物体 |

## 测试和调试

### 1. 在编辑器中测试
1. 运行游戏
2. 使用右键激活技能系统
3. 使用R键切换FilterSystem和MaskSystem
4. 观察隐藏物体是否正确显示/隐藏

### 2. 调试工具
在MaskSystem和FilterSystem中启用Debug日志：
```csharp
Debug.Log($"Revealed {revealedObjects.Count} hidden objects with color: {currentMaskColor}");
```

### 3. 可视化调试
MaskSystem提供Gizmos可视化：
- 黄色圆圈：蒙版范围
- 青色圆圈：检测范围
- 绿色方框：已显示的隐藏物体

## 最佳实践

### 1. 命名规范
- 使用描述性名称：`HiddenPlatform_Red_01`
- 按颜色分组：`Red_Objects`、`Blue_Objects`等
- 按功能分类：`Platforms`、`Items`、`Decorations`

### 2. 场景组织
```
Scene Hierarchy:
├── Environment/
│   ├── Visible_Objects/
│   └── Hidden_Objects/
│       ├── Red_Objects/
│       ├── Green_Objects/
│       ├── Blue_Objects/
│       ├── Yellow_Objects/
│       └── Purple_Objects/
├── Player/
└── UI/
```

### 3. 性能优化
- 合理控制隐藏物体数量
- 使用对象池管理大量隐藏物体
- 考虑使用LOD系统

### 4. 设计建议
- **颜色主题一致性**：同颜色物体应有相似的视觉风格
- **渐进式难度**：从简单的单色物体到复杂的多色组合
- **视觉反馈**：显现时添加粒子特效或音效
- **可访问性**：考虑色盲玩家，添加形状或图案区分

## 常见问题

### Q: 隐藏物体不显示怎么办？
A: 检查以下项目：
1. 物体标签是否正确设置
2. 物体图层是否在检测范围内
3. HiddenObject组件是否正确配置
4. MaskSystem/FilterSystem的检测范围设置

### Q: 物体显示后无法交互？
A: 确保：
1. `enableColliderWhenRevealed`设置为true
2. Collider2D组件存在且配置正确
3. 物体在正确的交互图层上

### Q: 性能问题？
A: 优化建议：
1. 减少同时显示的隐藏物体数量
2. 使用更高效的检测方法
3. 考虑分帧处理大量物体

### Q: 颜色匹配不准确？
A: 检查：
1. GameConstants中的颜色定义
2. ColorApproximatelyEqual方法的阈值
3. UI颜色选择器的颜色值

## 示例场景设置

### 简单平台游戏示例
1. 创建红色隐藏平台（玩家需要红色滤镜才能看到）
2. 创建蓝色隐藏道具（需要蓝色蒙版探索发现）
3. 创建黄色隐藏装饰（增加视觉丰富度）

### 解谜游戏示例
1. 多色隐藏开关（需要特定颜色组合激活）
2. 隐藏路径指示（引导玩家探索）
3. 秘密区域入口（奖励探索行为）

---

**注意**：本指南基于当前的MaskSystem和FilterSystem实现。如果系统有更新，请相应调整配置方法。

**版本**：适用于Unity 2021.3+
**最后更新**：2026年1月