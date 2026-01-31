# MaskSystem 设置指南

本文档详细说明如何在Unity场景中正确设置和配置MaskSystem（蒙版系统），用于实现局部区域的隐藏物体显示功能。

## 系统概述

MaskSystem是一个跟随玩家的圆形蒙版系统，可以在指定半径内显示特定颜色的隐藏物体。与FilterSystem的全屏滤镜不同，MaskSystem提供局部的、精确的物体显示控制。

## 前置要求

- Unity 2022.3+
- Universal Render Pipeline (URP)
- 已配置的Constants.cs文件（包含颜色枚举和标签定义）

## 第一步：场景层级设置

### 1.1 创建Sorting Layers
在Unity中设置以下Sorting Layers（按顺序）：
1. `Background` - 背景层
2. `Filter` - 滤镜层
3. `Interaction` - 交互层
4. `Mask` - 蒙版层
5. `UI` - UI层

**操作步骤：**
- 打开 `Edit → Project Settings → Tags and Layers`
- 在 `Sorting Layers` 部分添加上述层级

### 1.2 配置Physics2D Layers
确保以下Physics2D Layers存在：
- `Default` (Layer 0)
- `HiddenObjects` (推荐Layer 8)
- `Player` (推荐Layer 9)

## 第二步：玩家对象配置

### 2.1 玩家GameObject结构
```
Player (TempCharacter)
├── Sprite Renderer (玩家精灵)
├── Rigidbody2D (物理组件)
├── Collider2D (碰撞体)
├── PlayerController (移动控制)
├── FilterSystem (全局滤镜系统)
└── MaskSystem (蒙版系统) ← 本文档重点
```

### 2.2 添加MaskSystem组件
1. 选择Player GameObject
2. 点击 `Add Component`
3. 搜索并添加 `MaskSystem` 脚本

### 2.3 配置MaskSystem参数

#### Mask Settings（蒙版设置）
- **Current Mask Color**: 选择蒙版显示的颜色类型（Red/Blue/Green/Yellow/Purple）
- **Mask Radius**: 蒙版显示半径（推荐值：2-4）
- **Hidden Object Layer**: 设置为HiddenObjects层的LayerMask
- **Mask Prefab**: 可选，自定义蒙版预制体（留空则自动创建）

#### Detection Settings（检测设置）
- **Detection Radius**: 物体检测半径（通常与Mask Radius相同）
- **Detection Layer**: 检测层级（设置为HiddenObjects层）

## 第三步：隐藏物体设置

### 3.1 创建隐藏物体
1. 创建新的GameObject
2. 添加SpriteRenderer组件
3. 设置精灵图片
4. 添加HiddenObject组件

### 3.2 配置隐藏物体
```
Hidden Object GameObject
├── SpriteRenderer
│   ├── Sprite: 物体贴图
│   ├── Sorting Layer: Interaction
│   └── Order in Layer: 0
├── HiddenObject (脚本组件)
└── Tag: 对应颜色标签 (RedObject/BlueObject等)
```

#### 重要设置：
- **Layer**: 设置为 `HiddenObjects`
- **Tag**: 根据颜色设置对应标签：
  - 红色物体：`RedObject`
  - 蓝色物体：`BlueObject`
  - 绿色物体：`GreenObject`
  - 黄色物体：`YellowObject`
  - 紫色物体：`PurpleObject`
- **Sorting Layer**: `Interaction`

### 3.3 HiddenObject组件配置
- **Object Color**: 选择与Tag对应的颜色
- **Reveal Animation**: 可选的显示动画
- **Hide Animation**: 可选的隐藏动画

## 第四步：蒙版材质设置（可选）

### 4.1 自定义蒙版预制体
如果需要自定义蒙版外观：

1. 创建新的GameObject命名为"CustomMask"
2. 添加SpriteMask组件
3. 设置蒙版精灵（通常是圆形白色图片）
4. 配置SpriteMask参数：
   - **Alpha Cutoff**: 0.1
   - **Front Sorting Layer**: Interaction
   - **Back Sorting Layer**: Background

5. 将此GameObject制作成Prefab
6. 在MaskSystem的Mask Prefab字段中引用此Prefab

## 第五步：测试和调试

### 5.1 运行时测试
1. 进入Play模式
2. 使用R键切换到MaskSystem
3. 使用鼠标右键激活/关闭蒙版
4. 观察隐藏物体是否正确显示

### 5.2 常见问题排查

#### 问题1：蒙版不显示
**可能原因：**
- Sorting Layer设置错误
- SpriteMask组件缺失
- 蒙版GameObject未激活

**解决方案：**
- 检查Sorting Layer顺序
- 确保SpriteMask组件存在且配置正确
- 检查maskObject.SetActive(true)是否被调用

#### 问题2：隐藏物体不显示
**可能原因：**
- 物体Tag设置错误
- Layer设置不匹配
- HiddenObject组件缺失

**解决方案：**
- 确认Tag与MaskSystem的currentMaskColor匹配
- 检查物体Layer是否在hiddenObjectLayer中
- 添加HiddenObject组件

#### 问题3：检测范围不准确
**可能原因：**
- Detection Radius设置过小/过大
- Detection Layer配置错误
- 物体Collider缺失

**解决方案：**
- 调整detectionRadius参数
- 确认detectionLayer包含目标物体的Layer
- 为隐藏物体添加Collider2D组件

## 第六步：性能优化建议

### 6.1 对象池管理
- 对于大量隐藏物体，考虑使用对象池
- 避免频繁的GameObject创建和销毁

### 6.2 检测优化
- 合理设置检测半径，避免过大范围检测
- 使用LayerMask精确控制检测对象
- 考虑使用FixedUpdate而非Update进行检测

### 6.3 渲染优化
- 合理设置Sorting Layer避免过度绘制
- 使用Sprite Atlas减少Draw Call
- 对于静态隐藏物体，考虑使用Static Batching

## 示例场景配置

### 完整的测试场景设置：
```
Scene Hierarchy:
├── Main Camera
│   └── CameraController (跟随玩家)
├── GameManager
├── UIManager
├── Player (TempCharacter)
│   ├── PlayerController
│   ├── FilterSystem
│   └── MaskSystem ← 配置完成
├── Level Geometry
│   ├── Ground
│   ├── Platforms
│   └── Walls
├── Hidden Objects
│   ├── RedHiddenObject (Tag: RedObject, Layer: HiddenObjects)
│   ├── BlueHiddenObject (Tag: BlueObject, Layer: HiddenObjects)
│   └── GreenHiddenObject (Tag: GreenObject, Layer: HiddenObjects)
├── Respawn Points
└── Death Zones
```

## 总结

正确配置MaskSystem需要注意以下关键点：
1. **层级管理**：正确设置Sorting Layers和Physics2D Layers
2. **标签系统**：确保隐藏物体使用正确的颜色标签
3. **组件配置**：MaskSystem和HiddenObject组件参数正确设置
4. **性能考虑**：合理的检测范围和渲染优化

遵循本指南，您可以成功在Unity项目中实现功能完整的MaskSystem蒙版显示系统。