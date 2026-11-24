# iOS 原生模拟器验证 · 2026-11-24

**基础QA流程PASS，普通启动未确认；完整产品PARTIAL。** 按用户要求改用模拟器继续。当前源代码在iPhone和iPad模拟器实际安装、启动、渲染并各完成一次程序输入的钓鱼UI流程，32张截图逐张审阅。未对个人手机或TestFlight进行新发布。

## 验证身份

| 项目 | 当前结果 |
|---|---|
| 源码 | base `a3415b868277d5393208680e9c40bbfa8405d85d` + 本轮改动，构建时尚未提交；内容摘要见JSON |
| 实际包 | `sim4`，Unity 6000.3.29f1 / URP17.3.0，Xcode27.2(27B52) |
| 平台 | ARM64、iphonesimulator、unsigned Development；设备族1/2 |
| 发行关系 | 配置仍为0.1.0 (5)，**不是**a6bcf8e冻结的TestFlight或Ad Hoc build 5 |
| 配置恢复 | Simulator SDK和架构在finally恢复，ProjectSettings导出前后SHA256一致 |
| 数据 | 每次程序流程使用新的自有tmp存档；未卸载、未清除既有QA或个人存档 |

完整内容摘要、App树摘要和每张原图的SHA256保留在[JSON](ios-simulator.json)。App摘要按路径排序，将每个相对路径UTF-8与文件字节依次输入SHA256，复用`tools/beta.py`。源码摘要覆盖Git索引中的game/Assets、Packages、ProjectSettings、publication/release.json与tools实际内容。此身份记录支持与后续提交内容核对，不伪称构建前已有冻结提交。

## 当前通过

| 检查 | 结果与实际范围 |
|---|---|
| Editor | 52/52，含所有启动质量档与URP采样一致的新增回归 |
| PlayMode | 10/10，无画面、隔离存档；不替代原生UI |
| Unity导出与Xcode编译 | sim4均PASS；本地无签名模拟器包 |
| iPhone 17 Pro Max / iOS27.2 | PASS，2868×1320，3像素/逻辑点，安全区832×420pt，1条记录、180积分，16帧 |
| iPad Air 11 M3 / iOS27.2 | PASS，2360×1640，2像素/逻辑点，安全区1180×790pt，1条记录、187积分，16帧 |

两台均由实际iOS Player的EventSystem派发按下/松开/点击。检查实际命中区域、文字几何与鉴赏标题、抛投→咬钩→提竿→控线→记录放流/保存，海湾水上/码头/水下/岩礁观察及返回暂停，图鉴长学名/栖息环境滚动，卷线器摇柄/转子/线杯演示、整竿视角，任务、左手设置、家长验证、清档取消与导航清除按住状态。两份回执failures均为空。随机鱼体尺寸不同产生不同积分，不据此推定随机成长节奏已完整试玩。

两台Metal读回Medium，原生采样4/URP4，HDR前后true；三种测试颜色格式均支持4采样。未启用LDR诊断。平面倒影各实际绘制3次，分辨率768×353/768×534，非零方差并按静态视角复用；离开后释放检查通过。这些是功能检查，不是完整性能或跨进程逐像素稳定性验收。

## 修复与保留的失败

首次实际iPhone画面全黑，Metal原生附件1采样/URP请求4不匹配。移动Medium等档原生AA与URP不一致，现统一六档为4，并在prepare维护该约束。后续正常显示，未通过降低画质规避问题。

iOS进程确有测试参数，而托管参数列表未提供它们。新增缓存的原生`NSProcessInfo.arguments`桥接；相同入口用于隔离存档、截图、自动流程和逻辑点参数。此后自有帧输出及两台原生UI流程通过。

| 非通过运行 | 记录 |
|---|---|
| phonecapture1 | 启动生命周期已到active，但自有帧超时；实际系统帧全黑，保留MSAA错误 |
| phonecapture2 | 启动客户端180秒超时，不能以此声称游戏通过或仅认定游戏崩溃 |
| tabletcapture1 / phonecapture3 | 各为应用容器查询30秒超时 |
| phonecapture4 | 太短的boot调用被取消后设备仍Shutdown，安装405失败；后续等待完整boot并实际读回Booted |
| phonecapture5 | 主界面实际已显示，但90秒未产生自有QA输出；托管参数问题尚未修复 |

六条非零退出与后续成功的测量ID均保留。早期首次系统迁移/设备服务耗时另记，不覆盖失败。原始服务日志、设备标识、进程和容器路径仅本地保存；早期sim1 Unity导出日志被既有入口覆盖，未声称该份原日志仍完整，导出/编译产物与测量记录保留。后续导出日志有独立本地副本。

补充无QA参数的普通启动检查`plainstart1`：已验证安装包内容一致，启动客户端180秒超时，未得到可确认的默认启动帧，iPad普通启动尚未执行；本项NOT_CONFIRMED，测量`105c0bf7-adc7-44a3-aa9d-1c6c366f398c`非零保留。未重复安装或清档。它不撤销前述已实际生成并查看的两台QA流程证据，也不能被前述QA流程替代。

只读进程复查未见两台游戏进程。后续`plainstart2`在查询既有安装包容器时即90秒超时，尚未执行新的启动操作，测量`c8e1a0b5-fde7-417c-bfd4-2c26faff3faa`非零保留。两次普通启动相关检查均不记PASS；当前设备服务需先恢复核实，不以继续安装、清档或重复启动覆盖问题。

## 实际画面及未通过项

32帧均已审阅；以下公开12张代表性自有游戏帧，其余原帧与原回执本地保留，JSON保留32张摘要。无私人照片或其他应用画面。图中仍有远岸/倒影边缘阶梯感、重复岩石和叶片、原型鱼形/不透明鳍片；功能通过不等于拟真美术合格。

| 页面 | iPhone | iPad |
|---|---|---|
| 钓鱼主界面 | [原图](../art/previews/ios-simulator/phone/01-fishing.png) | [原图](../art/previews/ios-simulator/tablet/01-fishing.png) |
| 本次收鱼 | [原图](../art/previews/ios-simulator/phone/02-catch.png) | [原图](../art/previews/ios-simulator/tablet/02-catch.png) |
| 海湾水上 | [原图](../art/previews/ios-simulator/phone/07-bay-surface.png) | [原图](../art/previews/ios-simulator/tablet/07-bay-surface.png) |
| 图鉴详情 | [原图](../art/previews/ios-simulator/phone/13-species-details.png) | [原图](../art/previews/ios-simulator/tablet/13-species-details.png) |
| 钓具演示 | [原图](../art/previews/ios-simulator/phone/04-gear.png) | [原图](../art/previews/ios-simulator/tablet/04-gear.png) |
| 设置 | [原图](../art/previews/ios-simulator/phone/10-settings.png) | [原图](../art/previews/ios-simulator/tablet/10-settings.png) |

本轮未验证新航道地形/保护观察原生交互、陆坡、其余11种鱼的完整模型、其他钓具、完整18任务实玩、实际20轮UI、系统触摸、儿童首鱼≤3分钟、科学/用户美术、音频听感/触觉、最低系统和真实iPhone+iPad20分钟。此前Mac严格静帧差异PARTIAL不因本轮功能检查关闭。独立命令receipt审计仍INCOMPLETE；apply与完整产品出口保持未完成。后续按[验收总账](acceptance-ledger.md)继续。
