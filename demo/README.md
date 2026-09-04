# 逐风之旅 · The Wandering City

开放世界探索战斗游戏的可交互界面 Demo。采用 React、Vinext、Shadcn/Base UI，所有游戏数据在前端模拟，没有业务后端。

## 运行

在此目录执行 `npm install`，然后执行 `npm run dev`，打开终端显示的地址。

## 验证

- `npm test`：验证完整任务链、战斗闪避、死亡重生、资源扣除、一次性奖励、建造回收与存档恢复。
- `npm run lint`：检查项目代码；脚手架原样提供的 UI 组件和配套 hook 不纳入项目规则检查。
- `npm run build`：生成可部署版本。

## 操作

点击场景目标采集或战斗。通过地图切换四个地点。背包查看材料、使用药剂；据点工作台制作、强化与建造。快捷键：M 地图、B 背包、C 工作台、E 交互、J 攻击、空格闪避、Q 药剂、Esc 关闭窗口。

战斗为回合模拟：闪避保护下一次攻击不受反击。进度仅保存在当前浏览器 localStorage，在设置中可以确认重置。本版不包含真实 3D 移动、实时战斗、碰撞或敌人寻路。

场景插画由 ImageGen 生成，保存在 `public/assets/fantasy-valley.png`。字体优先使用 Noto Sans SC / Noto Serif SC，网络不可用时使用系统后备字体。
