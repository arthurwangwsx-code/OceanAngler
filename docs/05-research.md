# 研究与复用审计 · 2026-11-23

## 外部参考（设计推导，不是已集成依赖）
| 参考 | 可借鉴内容 | 本项目取舍 |
|---|---|---|
| [Fishing Planet](https://www.fishingplanet.com/) | 拟真钓具、鱼种差异、抛投与控线 | 简化参数、缩短等待，保留手感，不复制商业系统 |
| [Endless Ocean Luminous](https://www.nintendo.com/us/store/products/endless-ocean-luminous-119244/) | 水下探索、观察记录、海洋生命多样性 | 鱼类知识与图鉴，保护生物只观察 |
| [Unity 官方支持政策](https://unity.com/releases/unity-6/support) | LTS 生命周期 | 固定本机 6000.3.29f1，升级另做兼容验证 |
| [Unity 资产许可](https://unity.com/legal/as-terms) | 资产与引擎、源码许可分别处理 | 商店模型不直接进入开源仓库 |
| [Apple 内部测试](https://developer.apple.com/help/app-store-connect/test-a-beta-version/add-internal-testers/) | 内部组与内部专用构建 | 只给本人，不启用外部邀请 |
| [Apple 上传](https://developer.apple.com/help/app-store-connect/manage-builds/upload-builds) | bundle/version/build 匹配与处理状态 | 上传成功后仍等待 Apple processing 并核对可测试状态 |
| [Apple RealityKit](https://developer.apple.com/documentation/realitykit/scene-content-models-and-meshes) | 原生 mesh/model 方案 | 作为比较，不引入首版 |

## 本机只读参考
ToyRacer：/Users/ai/Documents/Codex/2026-11-22/3d-1-a-ios-b-3d。
已读 README 与工具入口：Unity 6000.3.29f1、URP、iPad 模拟器、资源与隐私验收、输入生命周期、构建产物隔离。复用流程经验，不将赛车的驾驶参数/个人设备证据移入钓鱼项目。

TacticalFPS：/Users/ai/Documents/Codex/2026-11-23/3d-1-a-cs-b-c。
已读 AGENTS、README、tooling/README、Packages/manifest 和 App Store Connect 工具源码。可参考模块隔离、每 build 的源码/原生摘要、本人内部组和签名校验；不复制 checkout、不触碰其 WIP、不使用其 app ID。ASC实现借鉴其安全边界；当前tools/asc.py已独立实现并只接受显式环境配置，不发布旧项目工具源码、密钥、测试者或发布产物。

本机核实 Xcode27.2、Unity6000.3.29f1、xcodegen/gh/ispec 存在，Apple 分发身份有效。初期gh登录问题已解除；已核实身份并按本项目授权公开OceanAngler仓库。2026-11-23移动布局与发行源码已推送master，CI通过；后续仍逐次核实目标remote和待公开范围，不把身份查询当成任意仓库的写授权。

## 资源候选原则
Blender 原创鱼为主；CC0 环境贴图可从 Poly Haven 等官方许可页逐项确认后采用；付费鱼模型只作为成本比较，当前未购买/下载/集成。[Poly Haven 官方资产许可](https://polyhaven.com/license)核实为CC0，仍需对实际采用的具体文件记录来源和哈希。没有将参考网页截图或游戏模型写入仓库。

2026-11-24已逐项核对12鱼与3观察候选的一手物种资料，来源为NOAA、FAO、澳大利亚博物馆、AFMA及中研院鱼类资料库，见[物种制作交接](10-species-production.md)。仅保留链接与自行整理的事实，未复制网页媒体。科学人工审阅及保育资料复核仍待完成；外观资料不等同于现成模型、现实海域共存或游戏平衡范围的科学认证。
