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

2026-11-24：0.1.0 (2)冻结于2971b11，已通过签名/身份校验、内部专用导出、上传与Apple VALID处理，并核实本人组中的构建分配。指定邮箱arthur.wang.wsx@gmail.com已在该组内，邀请为INVITED，尚未验证实际安装。详见[本次脱敏回执](../reports/testflight-0.1.0-2.json)及[设备复核步骤](09-current-beta-review.md)。候选包最低iOS/iPadOS 17，首鱼/首钓组/首海湾仍待美术与设备验收；旧build1的冻结产物及历史回执保留。

2026-11-24发行更新：0.1.0 (3)冻结05c8cc7，30项Editor、7项无画面PlayMode和11项发布工具检查通过；Unity iOS冻结导出、Xcode签名/身份校验、内部专用IPA、上传及Apple VALID/INTERNAL_ONLY通过，已分配本人授权两账号组，测试说明与反馈邮箱读回一致。指定邮箱仍INVITED，安装/运行NOT_RUN。该包包含build2之后的累计修复；科学/用户美术、当前实际画面、触控、听感、20分钟性能及完整产品仍未通过。见[build 3回执](../reports/testflight-0.1.0-3.json)及[设备复核](12-beta3-review.md)。独立receipt审计仍INCOMPLETE，3.4、4.1–4.3及apply未完成。

2026-11-24发行更新：0.1.0 (4)冻结ee7ba50，41项Editor、8项无画面PlayMode和11项发布工具检查通过，冻结提交CI通过；Unity iOS导出、签名/身份校验、内部专用IPA、归档与IPA静态元数据检查、上传和Apple VALID/INTERNAL_ONLY通过。本人授权两账号组已核实，含arthur.wang.wsx@gmail.com，指定邮箱邀请当前为INVITED，测试说明/反馈邮箱读回一致。包含build 3之后的三条环境鱼、阶段抛投/接近/收鱼、共享遭遇选择与重复抛竿保护，以及蓝鳍鲹鳍色/比例/阴影改进；积分/概率未变。本轮没有新原生玩家窗口或真机证据，实际安装、触控、20分钟性能/恢复、儿童/科学/美术A1–A3及完整产品仍未通过，其他模型/海域及观察交互仍缺。独立receipt审计INCOMPLETE，2.4、3.4、4.1–4.3及apply保持未完成。 见[build 4回执](../reports/testflight-0.1.0-4.json)。

## 用户要求的当前iPhone安装

2026-11-24用户要求有阶段性可运行进展时安装到当前iPhone。已用冻结build 4归档生成仅当前已注册iPhone可用的本地release-testing（Ad Hoc）包，沿用本项目现有分发私钥，未创建/撤销证书；签名/身份/设备范围及Unity Data一致性检查后，devicectl安装、读回、启动和后续运行进程读回通过。它是本地安装来源，不能替代TestFlight设备安装证据，详见[本地回执](../reports/iphone-local-0.1.0-4.md)。当前主界面视觉/真实触控仍待确认。

单设备描述文件须绑定本App、已有证书和明确目标设备；写请求未知先回查，禁止盲目重发。依据[Apple Profiles说明](https://developer.apple.com/documentation/appstoreconnectapi/profiles/)与[Profile创建属性](https://developer.apple.com/documentation/appstoreconnectapi/profilecreaterequest/data-data.dictionary/attributes-data.dictionary)。原始设备标识、profile、IPA及设备日志只存本机忽略目录。使用devicectl启动时，设备及输出等选项放在bundle参数之前，避免作为应用参数被捕获。出现锁屏/信任/OS授权阻碍应让用户完成，不绕过权限；不卸载应用来解决更新或签名问题，以保护进度。

2026-11-24 build 5交付：0.1.0 (5)冻结a6bcf8e，50项Editor、10项无画面PlayMode、11项发布工具、源码CI、iOS导出/签名归档、内部专用IPA/静态元数据/上传及Apple VALID/INTERNAL_ONLY/IN_BETA_TESTING通过，本人授权两账号组和说明已读回，指定邮箱仍INVITED。见[发行回执](../reports/testflight-0.1.0-5.json)。同源码已用复用的单设备profile本地更新当前iPhone 17 Pro Max / iOS 27.3，从4到5未卸载/清档，安装构建号/启动和随后同进程查询通过，见[本地回执](../reports/iphone-local-0.1.0-5.md)。这不证明TestFlight来源安装、主界面/触控、个人进度或20分钟资格；首次成功观察记录升级存档后旧4不能读取，继续使用新版，不清档降级。保护观察/近海候选已纳入，但完整模型/海域、美术A1–A3、用户/科学/儿童和双设备仍未完成；独立receipt审计INCOMPLETE，3.4及完整产品出口保持未完成。

2026-11-25 build 6交付：0.1.0 (6)冻结648b4ce，完整60项Editor、20项无画面PlayMode及12项发布保护通过；两组长流程分别34轮完整成长/20轮连续钓鱼，场景重建恢复另验。源码CI、iOS导出、签名归档、内部专用IPA与静态元数据、上传及Apple VALID/INTERNAL_ONLY/IN_BETA_TESTING通过，本人授权两账号组和说明已读回，指定邮箱状态INVITED。新增沉浸页面/底部收鱼、陆坡环境候选、鱼体眼口/薄鳍及钓组滚轮/绕线/主轴；积分和随机概率未改。未更新个人手机，本版TestFlight来源安装/实际触控/真实双设备20分钟未验，其他完整模型/科学/用户/儿童/美术A1–A3仍未完成。首次入口解析失败及直接执行红/绿修复保留；历史独立记录审计INCOMPLETE，apply及完整产品保持未完成。见[build 6回执](../reports/testflight-0.1.0-6.json)及[设备复核](27-beta6-review.md)。

2026-11-25 build 7交付：0.1.0 (7)冻结653d3a7，60项Editor、21项无画面PlayMode及12项发布保护通过；完整成长20轮覆盖12鱼/20装备/18任务，场景重建恢复3915分/20条/3观察，另一组连续20轮恢复3167分/20条/3观察。iOS原生冻结、签名/身份归档、内部专用IPA与静态元数据、单次上传和Apple VALID/INTERNAL_ONLY/IN_BETA_TESTING通过，本人授权两账号组和测试说明已读回，指定邮箱状态INVITED。900秒休息提醒通过原边缘提示及暂停/首页继续按钮确认，没有新增常驻HUD；保留沉浸式分层页面、左右手和图形操作，积分/随机概率/存档格式未变。本轮未更新个人手机，TestFlight来源安装、真实触控/双设备20分钟、拟真美术/其他完整模型、用户/科学/儿童验收仍未完成；历史独立记录审计仍INCOMPLETE，完整产品及apply保持未完成。见[build 7回执](../reports/testflight-0.1.0-7.json)和[设备复核](29-beta7-review.md)。
