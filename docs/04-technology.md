# 技术方案 v0.1

## 选型与边界
Unity 6000.3.29f1（本机已有）+ URP 17.3.0 + C# + Blender。iOS/iPadOS，Metal，IL2CPP ARM64；候选最低 iOS 17、A12/3GB，待真机测量后锁定。Android 仅保留输入和资源平台隔离，不计首批交付。

对比：Unity 可复用现有赛车和枪战的真实工具链，骨骼动画、LOD、粒子、移动渲染与编辑工具成熟；Unreal 适合高保真但移动包体和工程负担较大；Godot 开源但现有 Apple 发布经验较少；Swift/RealityKit 原生整合好，但跨平台和完整游戏内容生产需新增工具。选择 Unity 的依据是当前资产管线与开发条件，不承诺画质由引擎自动实现。

## 模块与数据流
InputAdapter → FishingSession 状态机 → FishingSimulation → CaptureSettlement → SaveStore；FishPresentation/WaterPresentation/CameraDirector 只消费快照与事件。UI 不直接加积分，Animator 不决定捕获结果，物种数据不耦合场景名。Bootstrap 仅装配依赖。

模块：Core（时钟/RNG/稳定 ID）、Fishing（状态/抛投/鱼线/张力）、Ecology（鱼群/水层/偏好）、Progression（积分/任务/解锁）、Persistence（迁移/恢复）、Presentation（模型/镜头/音效）、UI（适配/无障碍）、Platform（生命周期/触觉/发布）、Editor（内容校验/构建）。不加入首版联网服务。

## 钓鱼状态机与物理
Ready → Casting → Waiting → BiteWindow → Fighting → Landed → Released → Ready；超时走 Escaped → Ready。任意活跃态可进入 Paused，恢复原态；主菜单退出取消未结算会话。输入以动作开始/结束表达，收到失焦必须释放所有 held flags。

固定模拟 30Hz，表现插值到 30/60fps。张力由鱼的爆发力、收线输入、线弹性和泄力近似计算，clamp 到合法范围；安全带持续收线增加进度，超载积累风险，松线降张力但鱼恢复体力。采用可解释的弹簧阻尼近似，不以完整软体求解作为 P0 前置。鱼线用 verlet 链或分段曲线视觉模拟，只有必要节点碰撞；竿弯曲由张力驱动。逻辑真值与视觉线长度有明确映射。

鱼 AI：巡游 → 注意拟饵 → 接近 → 试探 → 咬钩 → 被钓/逃离；周围鱼群用低频 flocking 和距离裁剪。可钓概率由物种/水层/饵/时间决定，教程强制确定性；不能后台无限创建鱼对象。所有对象池有上限。

## 存档与恢复
本地 JSON schemaVersion=1，临时文件写完 flush 后原子替换，保留上一份 .bak。启动校验结构/范围/ID，主文件损坏则恢复备份，均失败提示新建且保留旧文件。迁移先备份再写，禁止静默清空。结算使用 catchId 去重；崩溃重开不重复奖励。积分变更与捕获记录同一次写入。

## 渲染与性能目标
URP Forward，烘焙环境、受控单主光、候选 2x MSAA，动态分辨率/质量等级；不默认开启 SSAO、实时平面反射或大面积多层透明。水面/水下 shader 分级，高档焦散/反射与低档近似保持玩法可见性一致。透明鳍和水面 overdraw 专项检查。

初始预算（非实测结果）：中档 A15 60fps，最低档 30fps；P95 帧耗分别 ≤16.7/33.3ms；峰值内存 <900MB；首场景冷启动 <8s；首包 <500MB。场景可见三角面初始 <500k、draw calls <150、主要阴影 <50m。真机 20 分钟含温升和后台恢复，记录 CPU/GPU、内存、掉帧，不用 Editor fps 代替。

## 测试与工具
纯逻辑测试：合法状态迁移、超时、张力边界、分数公式、重复结算、存档恢复、教程种子、非法数据。PlayMode：实际触控流程、镜头、模型实例/动画、场景切换、中断。模拟器验证布局和启动；真机验证触控、Metal、性能、温升、恢复。发布验证 bundle/version/build/source commit/签名/Apple processing/本人组/实际安装分别记录。

Unity .meta 入库，Library/Temp/build/凭据不入库，包版本固定，首次 Editor 导入后提交真实 packages-lock。不复制旧工程，不修改其代码/签名，不复用旧 bundle ID。公开工程和 TestFlight 构建由同一 commit 追踪；用户设备原始日志不进入公开证据。

## 当前实现决策
ContentCatalog.json作为首批数据真值，由运行时和测试共享校验；暂不额外维护ScriptableObject副本，以避免两套数据漂移。ProgressService先写临时文件并flush/原子替换后才发布内存状态。损坏文件转存，未来版本不静默降级；家长区恢复保留原件。规则测试与Mac运行检验分别记录，规则20次循环不等于真机20次触控验收。
