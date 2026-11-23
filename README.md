# OceanAngler · 海洋小钓手

[GitHub 开源仓库](https://github.com/arthurwangwsx-code/OceanAngler) · [完整验收总账](reports/acceptance-ledger.md)

儿童与家庭向的 3D 海洋钓鱼探索游戏。真实比例鱼类、漂亮水下世界、易懂钓具、积分成长、图鉴与放流。正式名称待商标检查。

**当前已具备可试玩的钓鱼/进度闭环，21项规则与存档测试、Mac运行及手机/平板逻辑点布局检查通过。0.1.0 (1)已通过Apple处理并加入仅本人的内部TestFlight组，[发布回执](reports/testflight-0.1.0-1.json)与实际安装分别记录。模型与环境仍是prototype，真机与完整产品验收未完成。** 首发 iPhone/iPad，Unity 6000.3.29f1 + URP 17.3.0 + C# + Blender；离线单人、无广告、无内购、无聊天。

| 阅读目的 | 文件 |
|---|---|
| 完整产品、玩法、范围与交互 | [产品方案](docs/01-product.md) |
| 鱼类、装备、海域与积分 | [内容与数值](docs/02-content-and-economy.md) |
| 漂亮拟真的模型如何制作和验收 | [美术与资产标准](docs/03-art-pipeline.md) |
| 引擎、架构、AI、物理、存档与性能 | [技术方案](docs/04-technology.md) |
| 联网研究及旧项目参考 | [参考研究](docs/05-research.md) |
| 分阶段任务、投入、风险与质量 | [路线及验收](docs/06-delivery.md) |
| GitHub 开源流程 | [开源流程](docs/07-open-source.md) |
| App Store Connect 与本人 TestFlight | [发布流程](docs/08-testflight.md) |
| 当前真实结果 | [完整总账](reports/acceptance-ledger.md)、[移动布局与发行](reports/mobile-layout.md)、[逻辑与Mac运行](reports/progression-validation.md) |
| 首条独立源模型 | [蓝鳍鲹候选及验收边界](reports/bluefin-art.md)、[源资产重建](art/source/bluefin/README.md) |
| 第一套完整钓组候选 | [纺车钓组与验证边界](reports/spinning-rig-art.md)、[源资产重建](art/source/spinning-rig/README.md) |
| 第一处独立海域候选 | [珊瑚海湾与验证边界](reports/coral-bay-art.md)、[海湾源资产重建](art/source/coral-bay/README.md) |

需求合同在 ispec/specs，首批变更在 ispec/changes/product-technical-foundation；planning/backlog.csv 管理任务。仅此目录可写，不修改赛车和枪战项目。

```sh
python3 tools/validate.py
python3 tools/game.py prepare
python3 tools/game.py tests
python3 tools/game.py ios --build 1
ASC_ENV_FILE=/path/to/private/asc.env python3 tools/asc.py status
```

代码采用 MIT；资产逐项登记，未取得可再分发许可的模型不得进入公开仓库。后续开源不包含 Unity 引擎。支持与隐私请求：arthur.wang.wsx@gmail.com。

![蓝鳍鲹源模型工作室预览：候选，尚非最终美术或游戏场景](art/previews/bluefin/three-quarter.png)

[360度源模型转台](art/previews/bluefin/turntable.mp4)。首条蓝鳍鲹已接入本地开发版；第一套完整纺车钓组在开发分支接入，新增[工作室近景](art/previews/spinning-rig/reel.png)及[卷线器转台](art/previews/spinning-rig/turntable.mp4)，其源文件与5项无画面机构检查通过，新界面及画面待桌面解锁后核验。现有TestFlight 0.1.0 (1)仍对应此前冻结源码，不含这两项模型。其余鱼种、钓具和海域仍待成品制作与验收。

integration/art-candidates已串行集成蓝鳍鲹、完整纺车钓组和珊瑚海湾，25项Editor及6项无画面PlayMode检查通过，涵盖真实场景内的海湾观察、暂停返回、钓组试摇、恢复抛竿、战斗中导航停止收线及实际机构/蒙皮；Mac原生包编译通过。[集成记录](reports/integrated-art-candidates.md)与源图另行保留。新界面画面、触控与真机仍待验，当前TestFlight 0.1.0 (1)不含这些资产。

Mac验证：`python3 tools/game.py mac`，再运行`python3 tools/runtime_smoke.py --label unique-run`。字体重建见tools/requirements.txt；ASC发行工具另需PyJWT与cryptography，并显式配置本机凭据。
