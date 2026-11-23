# App Store Connect / 本人 TestFlight

内部代号 OceanAngler；独立 bundle com.arthur.OceanAngler，App ID 6838308856，版本0.1.0，从 build1开始，禁止借用其他游戏的 App ID。唯一发行配置在 publication/release.json。

1. tools/asc.py status 使用本机受保护环境中的 ASC_KEY_ID/ASC_ISSUER_ID/ASC_KEY_PATH 查询精确 bundle。凭据只进内存，日志不打印 JWT 或密钥。
2. 注册独立 bundle，创建 iOS app 记录（名称/SKU/语言/bundle），记录 numeric app ID；[Apple 官方 Apps API说明](https://developer.apple.com/documentation/appstoreconnectapi/apps)明确新App须在网站创建，因此走 App Store Connect UI，不伪造 app ID。
3. Unity IL2CPP 导出到新 build 目录，固定版本/构建号/源码SHA；构建前测试通过且源码干净。使用已核实 team、已有分发证书和本App独立App Store描述文件进行手动签名；不重建或撤销已有证书。
4. archive → 签名/entitlements/bundle/version/build校验 → export；ExportOptions 使用 method=app-store-connect 和 testFlightInternalTestingOnly=true，确认 internal-only 后上传。上传结果未知先查状态，禁止盲目重复提交。
5. Apple processingState=VALID 后查询 buildBetaDetail，补充 beta notes/出口合规。只建立本人内部组，核对 ACCOUNT_HOLDER 及用户明确指定的测试账号，审计现有组与直接测试者。不创建外部邀请、公开链接或新增非本人测试者。
6. 记录 Apple app/build ID、版本、源码 SHA、上传时间、处理状态、本人组；设备 TestFlight 实际安装启动另记，不能由 API 状态推定。

Apple的[分发说明](https://developer.apple.com/documentation/xcode/distributing-your-app-for-beta-testing-and-releases/)将TestFlight Internal Only限制为团队内部测试；本项目还需逐组核查成员，进一步限制为本人。组的isInternalGroup或导出选项本身不能证明成员只有一人。

测试身份与公开支持邮箱分开配置。publication/release.json的internalTesterEmails只接受用户明确指定的本人测试账号；不从supportEmail自动推断。工具核查其已是ASC中具有本App访问权的合格内部用户，缺失时停止，不擅自新增账号或扩大角色/应用权限。本人使用多个账号时保留既有持有人账号并添加明确指定账号，按授权账号集合审计，而非强制只有一条邮箱记录。2026-11-23用户明确指定arthur.wang.wsx@gmail.com用于测试，已核实其为现有可访问本App的ASC账号。内部测试条件见[Apple官方说明](https://developer.apple.com/help/app-store-connect/test-a-beta-version/add-internal-testers/)。

Test notes应明确：早期原型，测试抛竿/张力/收鱼/积分/存档/中断；3D资产尚非最终拟真质量。支持/反馈/隐私邮箱统一 arthur.wang.wsx@gmail.com。

公开上架不在本次范围；正式隐私政策、分级、截图和Kids Category等到实际功能/发行主体明确后完成。本人内部测试也必须处理签名与合规，不能绕过Apple权限或登录。

## 当前应用记录
2026-11-23 已通过网页创建 OceanAngler，App ID为6838308856，bundle为com.arthur.OceanAngler（Apple resource ID 6A73JBG8G4），主要语言简体中文，SKU为OceanAngler-iOS。已用API读回匹配。此前网页连接阻塞已解除。本App描述文件已准备并复用现有分发私钥，未创建/撤销证书。归档、上传、Apple处理、内部组和安装状态须分别查验，不从注册成功推定。

工具配置须显式提供ASC_KEY_ID、ASC_ISSUER_ID、ASC_KEY_PATH，或将ASC_ENV_FILE指向自己本机已有的受保护配置。工具不自动读取其他项目的凭据，不将配置或密钥提交仓库。

## 当前项目执行入口
`tools/beta_signing.py --apply`准备本App描述文件；`tools/beta.py native --apply`冻结已提交源码与Unity导出，再依次archive/export/upload/status/notes/self。每步均核对版本与App，写操作需要--apply。使用publication/release.json的唯一版本/构建号，未知上传必须先查Apple实际状态，不重复上传。签名、归档和IPA校验记录在.local/beta，公开回执另行脱敏。

每次原生导出必须使用新目录；首次iOS切换可能自动补写Unity平台设置，冻结检查会拒绝这类源码变化。审阅并提交真实配置后重新导出，不给已变化源码的产物补造冻结回执。归档使用冻结源码SHA、源码摘要与原生目录摘要；导出后检查IPA中的App标识和版本。上传前先核对同版本、同构建的Apple记录，并审计已有组。ATTEMPTED日志不是上传失败的证明，网络中断时先查Apple处理状态和私有传输日志。

原始签名日志、描述文件、IPA、设备标识及截图仅存本机忽略目录，不进入公开仓库。设备验收必须说明安装来自TestFlight还是开发侧载；仅发现设备或成功启动同bundle不能证明TestFlight安装链路。
