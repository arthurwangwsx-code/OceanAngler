# OceanAngler · 海洋小钓手

[GitHub 开源仓库](https://github.com/arthurwangwsx-code/OceanAngler) · [完整验收总账](reports/acceptance-ledger.md)

儿童与家庭向的 3D 海洋钓鱼探索游戏。真实比例鱼类、漂亮水下世界、易懂钓具、积分成长、图鉴与放流。正式名称待商标检查。

**当前已具备可试玩的钓鱼/进度闭环。0.1.0 (4)已通过Apple处理并分配至本人授权账号的内部TestFlight组，累计纳入首鱼/首钓组/首海湾候选、图鉴/任务/音频/水面、环境鱼群、阶段钓鱼表现及鱼体/阴影改进；[发布回执](reports/testflight-0.1.0-4.json)与实际安装分别记录。美术、实际触控、真机与完整产品验收未完成。** 首发iPhone/iPad，最低iOS/iPadOS 17，Unity 6000.3.29f1 + URP 17.3.0 + C# + Blender；离线单人、无广告、无内购、无聊天。

| 阅读目的 | 文件 |
|---|---|
| 完整产品、玩法、范围与交互 | [产品方案](docs/01-product.md) |
| 鱼类、装备、海域与积分 | [内容与数值](docs/02-content-and-economy.md) |
| 漂亮拟真的模型如何制作和验收 | [美术与资产标准](docs/03-art-pipeline.md) |
| 引擎、架构、AI、物理、存档与性能 | [技术方案](docs/04-technology.md) |
| 联网研究及旧项目参考 | [参考研究](docs/05-research.md) |
| 分阶段任务、投入、风险与质量 | [路线及验收](docs/06-delivery.md) |
| GitHub 开源流程 | [开源流程](docs/07-open-source.md) |
| 保护观察后续源码与存档边界 | [交互说明](docs/14-protected-observation.md)、[A0工程证据](reports/protected-observation.md) |
| App Store Connect 与本人 TestFlight | [发布流程](docs/08-testflight.md) |
| 本次候选如何在设备上复核 | [build 4设备检查](docs/13-beta4-review.md) |
| 当前真实结果 | [完整总账](reports/acceptance-ledger.md)、[移动布局与发行](reports/mobile-layout.md)、[逻辑与Mac运行](reports/progression-validation.md) |
| 首条独立源模型 | [蓝鳍鲹候选及验收边界](reports/bluefin-art.md)、[源资产重建](art/source/bluefin/README.md) |
| 第一套完整钓组候选 | [纺车钓组与验证边界](reports/spinning-rig-art.md)、[源资产重建](art/source/spinning-rig/README.md) |
| 第二处海域与共享观察 | [近海航道、切换与验收边界](docs/15-nearshore-channel.md)、[源资产重建](art/source/nearshore-channel/README.md) |
| 第一处独立海域候选 | [珊瑚海湾与验证边界](reports/coral-bay-art.md)、[海湾源资产重建](art/source/coral-bay/README.md) |
| 物种资料和仅观察生物 | [学名、辨识与建模交接](docs/10-species-production.md) |
| 原创音频与实际画面 | [音频控制](docs/11-feedback-audio.md)、[三档原生截图与问题](reports/integrated-native-render.md) |
| 礁岩破面修复与对照 | [源网格检查和同镜头实际游戏画面](reports/coral-bay-solid-geometry.md) |
| 鱼体表面与鉴赏标题 | [鳃盖/高光迭代、标题消失回归及实际对照](reports/bluefin-surface-and-ui.md) |
| 图鉴物种详情 | [12物种学名、辨识与窄卡片滚动](reports/journal-species.md) |
| 口部、下颌与鳍条 | [真实下唇变形、内壁和导入资产离屏检查](reports/bluefin-mouth-and-fins.md) |
| 海湾水面与倒影 | [深度顺序修复、离屏GPU对照及验收边界](reports/water-rendering.md) |
| 英雄鱼材质与阴影 | [鳍色、比例、灰银/灰铜材质、转台与水下前后对照](reports/bluefin-palette.md) |
| 随机成长节奏 | [正式选择规则的1000次模拟、解锁与收集长尾](reports/encounter-balance.md) |
| 拟饵接近与收鱼表现 | [阶段路径、嘴部鱼线、几何检查与八张离屏图](reports/fishing-encounter.md) |
| 海湾环境鱼群 | [三条现有蓝鳍鲹的游动、空间检查与离屏对照](reports/bay-school.md) |
| 岸线与沙地 | [连续陆地、沙地材质及同镜头离屏对照](reports/bay-shoreline.md) |

需求合同在 ispec/specs，首批变更在 ispec/changes/product-technical-foundation；planning/backlog.csv 管理任务。仅此目录可写，不修改赛车和枪战项目。

```sh
python3 tools/validate.py
python3 tools/game.py prepare
python3 tools/game.py tests
python3 tools/game.py mac
ASC_ENV_FILE=/path/to/private/asc.env python3 tools/asc.py status
```

代码采用 MIT；资产逐项登记，未取得可再分发许可的模型不得进入公开仓库。后续开源不包含 Unity 引擎。支持与隐私请求：arthur.wang.wsx@gmail.com。

![蓝鳍鲹源模型工作室预览：候选，尚非最终美术或游戏场景](art/previews/bluefin-anatomy/three-quarter.png)

[360度源模型转台](art/previews/bluefin-anatomy/turntable.mp4)。首条蓝鳍鲹与完整纺车钓组均有独立Blend/FBX、PBR、骨骼/动作和三档LOD；另有[钓组近景](art/previews/spinning-rig/reel.png)和[卷线器转台](art/previews/spinning-rig/turntable.mp4)。这些仍是候选，A1–A3、科学/用户审阅和真机尚未通过；其余11鱼种、其他钓具、陆坡及观察生物仍需完整制作，海湾和新近海航道也尚未通过成品美术验收。

当前开发源码包含保存成功后的转向/放流与空载竿恢复、18任务指引、12候选学名和3个仅观察目录保护，以及原创水下底音/探索音乐及独立保存的开关。初始装备20次确定性模拟、逐次重载、重复记录不加分和全装备解锁已有[逻辑证据](reports/task-journey.json)，不代表儿童首鱼时间、随机遭遇耗时或实际20次UI试玩。后续源码已接入独立保护观察交互和图鉴，三种形体仍为A0样板，完整源模型与美术验收未完成。

海湾已有独立源地形、码头、小船、岩礁/珊瑚/海草、水上/水下观察、缓存倒影与深度透射。源网格朝向/封闭、鱼体鳃缘/材质及鉴赏标题修复分别保留证据；岸线从孤立沙脊改为连接陆地的缓坡，沙地波纹与颜色分离。蓝鳍鲹新增实际向下张开的下颌、口腔内颊及逐段鳍纹，修复隔离场景首次绘制的子材质颜色异常。开发分支又加入三条复用模型的环境鱼群，固定实例、局部聚合/分离，暂停和减弱动态停止；图鉴/钓具鉴赏隐藏，背景鱼不参与结算。开发分支还加入抛投、接近、控线收鱼的阶段路径，鱼线保持到收鱼并跟随动画嘴部。蓝鳍鲹又补灰银/灰铜与蓝黑斑、蓝/浅鳍色区及较收敛的鳍比例，校准近景自阴影。保护观察阶段46项Editor、9项无画面PlayMode及Mac编译通过，源文件工程检查和Editor离屏GPU图分别审阅；当前原生窗口与实际iPhone/iPad画面、触控、听感/触觉及20分钟性能仍未通过。旧版三档Mac流程各12截图属于其历史源码，不能替代新资产的验收。

Mac构建入口为`python3 tools/game.py mac`；原生检查为`python3 tools/runtime_smoke.py --label uniquerun`，使用隔离存档，须在桌面窗口可用后实际运行并核对图像/回执。Editor离屏诊断为`python3 tools/game.py water-review --label uniquereview`，不能据此宣布玩家UI或设备通过。Unity和资产写入串行，每轮用新label保留证据；字体重建见tools/requirements.txt。

TestFlight 0.1.0 (4)冻结于ee7ba50，包含三条环境鱼、阶段接近/收鱼、共享选择入口、重复抛竿保护及鱼体/阴影更新。[发行回执](reports/testflight-0.1.0-4.json)记录Apple VALID/INTERNAL_ONLY、本人两账号组和测试说明；指定邮箱邀请为INVITED，实际安装启动未验。按[build 4设备复核](docs/13-beta4-review.md)继续记录。历史[build 1](reports/testflight-0.1.0-1.json)、[build 2](reports/testflight-0.1.0-2.json)、[build 3](reports/testflight-0.1.0-3.json)产物与回执保留。新构建按[冻结源码发布流程](docs/08-testflight.md)使用新build号。ASC工具另需PyJWT与cryptography，凭据显式在本机配置，不进入公开仓库。

保护观察后续源码已接入绿海龟、宽吻海豚和鲸鲨的独立观察/图鉴/存档闭环，形体仅为A0样板。首次记录才升级payload为schema 2，旧build 4会拒绝该版本以保护记录，不能以清档解决回退。此变化尚未进入上述TestFlight 0.1.0 (4)，详见[验证与限制](reports/protected-observation.md)。

2026-11-24已按要求把冻结0.1.0 (4)以本地单设备测试包装到当前iPhone 17 Pro Max，并确认启动和运行进程；见[安装回执](reports/iphone-local-0.1.0-4.md)。它不等同于TestFlight设备安装，主界面/触控和20分钟仍未验，后续未发布源码不在该包内。

近海航道候选已接入共享水上/水下观察和按需海域切换：自有Blend/FBX/四组PBR、六组各三LOD、独立水面及复用自有系泊艇。50项Editor、10项无画面PlayMode、Mac编译和11项发布工具测试通过，12张工作室/Editor Metal原图逐张审阅。岛体轮廓、材质边界与重复岩石/叶片仍有明显样板感，不能认定拟真美术合格；当前Mac锁定，尚无本轮玩家窗口或设备画面。新候选未进入已安装的build 4。详见[近海航道证据](reports/nearshore-channel.md)。
