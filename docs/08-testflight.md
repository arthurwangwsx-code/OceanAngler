# App Store Connect / 本人 TestFlight

内部代号 OceanAngler；独立 bundle 候选 com.arthur.OceanAngler，版本0.1.0，从 build1开始，禁止借用其他游戏的 App ID。

1. tools/asc.py status 使用本机受保护环境中的 ASC_KEY_ID/ASC_ISSUER_ID/ASC_KEY_PATH 查询精确 bundle。凭据只进内存，日志不打印 JWT 或密钥。
2. 注册独立 bundle，创建 iOS app 记录（名称/SKU/语言/bundle），记录 numeric app ID；[Apple 官方 Apps API说明](https://developer.apple.com/documentation/appstoreconnectapi/apps)明确新App须在网站创建，因此走 App Store Connect UI，不伪造 app ID。
3. Unity IL2CPP 导出到新 build 目录，固定版本/构建号/源码SHA；构建前测试通过且源码干净。Xcode 自动签名只选择已核实 team；不重建或撤销已有证书。
4. archive → 签名/entitlements/bundle/version/build校验 → export；ExportOptions 使用 method=app-store-connect 和 testFlightInternalTestingOnly=true，确认 internal-only 后上传。上传结果未知先查状态，禁止盲目重复提交。
5. Apple processingState=VALID 后查询 buildBetaDetail，补充 beta notes/出口合规。只建立本人内部组，核对 ACCOUNT_HOLDER 对应身份，审计现有组与直接测试者。不创建外部邀请、公开链接或新增非本人测试者。
6. 记录 Apple app/build ID、版本、源码 SHA、上传时间、处理状态、本人组；设备 TestFlight 实际安装启动另记，不能由 API 状态推定。

Test notes应明确：早期原型，测试抛竿/张力/收鱼/积分/存档/中断；3D资产尚非最终拟真质量。支持/反馈/隐私邮箱统一 arthur.wang.wsx@gmail.com。

公开上架不在本次范围；正式隐私政策、分级、截图和Kids Category等到实际功能/发行主体明确后完成。本人内部测试也必须处理签名与合规，不能绕过Apple权限或登录。

本机已查到 Apple Distribution: Arthur Wang 的签名身份；身份可用不证明本项目 profile 或 App 记录已完成。真实连接与构建结果见 reports/foundation.md。

本轮已注册 bundle com.arthur.OceanAngler（Apple resource ID 6A73JBG8G4），API可用但apps查询为空。网页控制两次连接超时，因此尚无App记录及可上传目标。需恢复网页连接或用户在网站新建同bundle的iOS记录后，tools/asc.py status核查numeric ID再继续。建议字段：Name=OceanAngler、Primary Language=Simplified Chinese、SKU=OceanAngler-iOS、平台iOS。

## 当前应用记录
2026-11-23 已通过网页创建 OceanAngler，App ID为6838308856，bundle为com.arthur.OceanAngler，主要语言简体中文，SKU为OceanAngler-iOS。已用API读回匹配。前述网页连接阻塞已解除；尚未归档上传，后续只操作此App。

工具配置须显式提供ASC_KEY_ID、ASC_ISSUER_ID、ASC_KEY_PATH，或将ASC_ENV_FILE指向自己本机已有的受保护配置。工具不自动读取其他项目的凭据，不将配置或密钥提交仓库。
