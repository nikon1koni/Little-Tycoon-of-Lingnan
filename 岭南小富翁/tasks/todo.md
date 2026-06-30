# Harvest 格子 + 卡池系统 待办

## 需求确认
- 把 `BoardTile.TileType.GainMoney` 改名为 `Harvest`（原地改名，序号不变，兼容已有场景）。
- 卡 = 复用现有 `ItemData`（稀有度用 `ItemData.ItemRarity`）。
- 每次踩 Harvest：必得少量基础铜钱；再按单格概率（默认 0.5）给"额外铜钱"或"一张卡"（二选一）。
- 抽卡：先按权重滚稀有度 → 该稀有度无卡则顺延到最近有卡稀有度 → 等概率抽该稀有度的卡；卡池空则回退给额外钱。
- 卡池：新建 `CardPool` ScriptableObject（列卡 + 4 个稀有度权重，全局共用）。

## 步骤
- [x] 1. 新建 `Assets/Script/Data/CardPool.cs`（ScriptableObject + DrawCard 逻辑），转 UTF-8 BOM 校验
- [x] 2. `ItemManager` 加 `cardPool` 字段与 `GiveRandomCardFromPool(player)`，发卡后刷新手牌
- [x] 3. `BoardTile` 枚举 `GainMoney`→`Harvest`，更新 case；`GameManager.cs:1006` 同步
- [x] 4. `BoardTile` 加每格字段并重写 `HandleGainMoneyTile`→`HandleHarvestTile`
- [x] 5. 校验所有改动文件仍为 UTF-8 BOM（4 个文件全部 BOM=True StrictUTF8=True；编译需用户在 Unity 验证，本会话无 Unity MCP）

## 需用户在 Unity 内完成
- 创建 CardPool 资源（菜单 Game/Card Pool），填入可获得卡牌与各稀有度权重
- 把 CardPool 资源拖到场景中 ItemManager 的 `Card Pool` 字段
- （可选）调整各 Harvest 格子的 `Harvest Card Chance` 等参数

## Review
- **CardPool.cs**（新建）：`DrawCard()` 先按 `rarityWeights` 权重滚稀有度；该稀有度无卡时 `TryFindNearestRarityWithCards` 按数值距离由近及远顺延（同距离优先低稀有度）；最终在目标稀有度的卡里等概率抽一张。未配置权重时退化为按卡池中实际存在的稀有度等概率。卡池为空返回 null。
- **ItemManager.cs**：新增 `public CardPool cardPool` 字段 + `GiveRandomCardFromPool(player)`，内部调 `DrawCard()` → `GiveItem()`（已弹"获得卡牌"提示）→ 刷新手牌；卡池未配置/为空时仅警告并返回 null。
- **BoardTile.cs**：枚举 `GainMoney`→`Harvest`（序号 14 不变，兼容旧场景）；`OnLanded` 的 case 同步；新增 5 个每格字段（基础钱上下限、卡牌概率、额外钱上下限）；`HandleHarvestTile` 逻辑＝先必给基础铜钱，再按 `harvestCardChance` 二选一：抽卡（失败则回退给额外铜钱）或直接给额外铜钱。注意 `TileEvent.GainMoney`/`SFXClip.EventGainMoney` 等同名项**未改动**。
- **GameManager.cs:1006**：`TileType.GainMoney`→`Harvest`。全局已无 `TileType.GainMoney` 残留引用。
- **编码**：Edit/Write 在本环境会把含中文文件重写为 GBK 无 BOM，已对 BoardTile.cs、GameManager.cs 做 GBK→UTF-8 BOM 转换；4 个改动文件全部 BOM=True、StrictUTF8=True，中文校验完好。
- **未验证项**：本会话无 Unity MCP，无法编译与创建资源，需用户在 Unity 内完成下方清单并重新编译确认无误。
