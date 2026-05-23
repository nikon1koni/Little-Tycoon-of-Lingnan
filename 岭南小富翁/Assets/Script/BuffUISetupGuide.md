# Buff UI 系统设置指南

## 组件说明
---

### 1. BuffIcon 组件
单个 Buff 图标的显示和悬停提示

### 2. BuffDisplayManager 组件
管理所有 Buff 显示的单例

## 实例
---

## 步骤 1：创建 Buff Icon 预制件

1. 在 Unity 中创建一个新的 Canvas 或使用现有的
2. 创建一个空 GameObject 作为 Buff 图标的预制件：
   - 名称：`BuffIcon`
   - 组件：
     - RectTransform
     - Image (作为背景)
     - BuffIcon 脚本
   - 子对象：
     - `Icon` (Image，用于显示 Buff 图标)
     - `StackText` (TextMeshPro - Text，用于显示堆叠数，可选)

## 步骤 2：创建 Tooltip 预制件

1. 创建一个新的 GameObject：
   - 名称：`BuffTooltip`
   - 组件：
     - RectTransform
     - Image (背景)
     - LayoutElement
     - Vertical Layout Group
     - Content Size Fitter
     - Canvas Group
   - 子对象：
     - `Text` (TextMeshPro - Text)

## 步骤 3：设置 BuffDisplayManager

1. 在场景中创建一个 GameObject，命名为 `BuffDisplayManager`
2. 添加 `BuffDisplayManager` 组件
3. 配置字段：
   - `Buff Icon Prefab`：刚才创建的 `BuffIcon` 预制件
   - `Buff Container`：在 Canvas 中创建一个空对象作为容器
   - `Buff Tooltip Prefab`：刚才创建的 `BuffTooltip` 预制件
   - 设置各种 Buff 图标资源（可以先留空，或使用占位图，稍后添加)

## 步骤 4：布局建议

### Buff Icon 预制件设置：
- RectTransform:
  - Size: 60x60
- Image (背景):
  - Color: 白色/金色
  - Sprite: 圆形/方形背景
- BuffIcon 组件:
  - Icon Image: 关联子对象的 Icon Image
  - Stack Text: 关联子对象的 StackText

### Tooltip 预制件设置：
- RectTransform:
  - Size: 200x150
- Image (背景):
  - Color: 深灰色 (0.1, 0.1, 0.1, 0.95)
- Vertical Layout Group:
  - Padding: 10, 10, 10, 10
  - Child Force Expand: Width, Height
- Content Size Fitter:
  - Horizontal Fit: Preferred Size
  - Vertical Fit: Preferred Size

## 使用方式

现在运行游戏，当玩家获得 Buff 时，Buff 图标会自动显示在 Buff Container 中。

## 自定义

- 鼠标悬停在 Buff 图标上会显示详细信息：
  - Buff 名称
  - 数值加成
  - 来源
  - 剩余时间/回合
