# iOS 模拟器验证

用户要求优先使用模拟器继续验证。本流程使用本项目独立的iPhone/iPad模拟器，保留其他项目的模拟器和原始个人手机；模拟器不替代真实触控、温升、功耗或真机20分钟验收。

`tools/game.py simulator --label <新标签>` 以iOS目标调用 `BuildTools.ExportIOSSimulator`，临时选择Simulator SDK与ARM64。`finally` 恢复原SDK与模拟器架构并保存；首次实跑前后ProjectSettings文件SHA256完全相同。导出目录为 `build/ios-simulator-<标签>`，存在即拒绝覆盖。Unity官方区分[Device与Simulator SDK](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/playersettings/ios/sdkversion)及[模拟器架构](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/playersettings/ios/simulatorsdkarchitecture)。

```sh
ispec changes measure --change product-technical-foundation --stage apply --category compile -- python3 tools/game.py simulator --label freshsimlabel
```

随后用Xcode编译Unity-iPhone scheme，SDK为 `iphonesimulator`、destination为 `generic/platform=iOS Simulator`、架构ARM64，关闭签名，单独指定DerivedData路径。安装前读取实际App的Info.plist，必须核对本项目bundle、`DTPlatformName=iphonesimulator`、`CFBundleSupportedPlatforms=[iPhoneSimulator]`及设备族1/2。该本地QA包沿用开发配置版本号，不能借此当作同版本TestFlight冻结包或真实手机包；源摘要和实际App摘要单独留档。

使用 `simctl create/boot/bootstatus` 管理具名 `OceanAngler-` QA设备；不得清空、卸载或关闭其他项目的模拟器。通过 `simctl install` 安装，再以 `simctl get_app_container ... data` 获取自己的数据容器。每轮在该容器tmp下新建独立截图和存档目录，传入 `--ocean-save-root`，不使用个人进度。

初次启动先使用既有 `--ocean-capture`：应用等待真实帧完成后保存自身画面。进一步使用既有 `--ocean-smoke`：在实际iOS Player内派发EventSystem按下、松开和点击事件，检查命中区域、钓鱼/记录、海湾观察、图鉴/长卡、钓具演示、任务、设置、家长验证与清档取消，逐页读取文字与控件尺寸并保存游戏自身帧。这些是程序输入，不能称为系统触摸、儿童试玩或20次人工操作。

安装和启动必须分别核实。命令仍运行或轮询超时不等于它已失败；保留当前进程与日志，先查询实际状态，不重复安装。安装/启动后才可能声称运行通过，必须实际查看游戏输出帧及回执。主机锁定、首次系统迁移和设备服务错误分别记录，不以成功编译推定画面或交互通过。原始服务/设备日志、容器路径和个人标识只存 `.local`。

## 已发现并修复的问题

首个原生iPhone启动画面全黑，Metal报告原生附件为1采样、URP请求4采样。项目移动默认Medium等质量档的原生AA设置与URP不一致。现将所有质量档的原生采样数统一到OceanURP的4×MSAA，并加入检查实际配置的Editor回归测试。后续实际iOS Player正常渲染；HDR保持开启，没有降低抗锯齿或放宽检查阈值。

第二个问题是iOS进程实际收到测试参数，但IL2CPP的托管命令行读取没有提供这些参数，导致截图、隔离存档和自动流程未启动。新增缓存的`NSProcessInfo.arguments`桥接，截图、存档、逻辑点与自动流程共用同一读取入口。原生两设备的实际截图与流程回执均验证了该入口。默认启动没有参数时不启用这些测试功能。

仅显式Development iOS Player的`--ocean-simulator-probe <自有文件>`输出图形能力诊断。另有显式`--ocean-simulator-ldr-control`用于诊断对照；本轮通过的运行全部未使用它，读回HDR前后均为true，原生与URP采样均为4。

## 当前结果与范围

最新`sim4`为ARM64、unsigned、Development的本地模拟器包。iPhone 17 Pro Max与iPad Air 11 M3、iOS 27.2运行时分别完成一次EventSystem钓鱼、记录放流与多页UI流程，每台16张自有截图已逐张查看。52项Editor、10项无画面PlayMode通过。具体源码/二进制摘要、逐项流程、测量ID、失败和代表性原图见[模拟器验证记录](../reports/ios-simulator.md)及[结构化回执](../reports/ios-simulator.json)。

补充不带QA参数的普通启动时，首个启动客户端180秒超时；确认未见游戏进程后，下一次仅查询既有安装包的容器便90秒超时，尚未进入新的启动操作。因此普通启动NOT_CONFIRMED，未重复安装，也不把已完成的QA流程替代该项。后续应先恢复并核实当前模拟器服务，再继续普通启动与更长流程。

仍见远岸/倒影边缘阶梯感、重复岩石/叶片及原型鱼形，A1–A3不因此通过。本轮原生流程覆盖海湾；近海航道新地形、保护观察及陆坡尚未取得同等原生流程证据。程序点击不等于系统触摸，也不证明实际连续20轮UI、儿童首鱼时间、听感/触觉、最低系统、性能或真机20分钟通过。

模拟器包沿用配置0.1.0 (5)，但源码为后续开发摘要，不能当作冻结于a6bcf8e的TestFlight build 5。此次没有更新个人手机或TestFlight，没有卸载或清除个人进度。后续发行必须冻结新源码并使用新build号。
