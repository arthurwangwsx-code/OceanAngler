# 完整成长流程验证

2026-11-25当前完整成长复核：45b04aa/8f8d883b同一现有模拟器包在手机/平板各自全新隔离存档中，正常26/27轮收齐12鱼种、完成18任务、逐件装备20件钓具及3观察，4482/4710分；53次同帧重复放流无重复奖励。逐轮任务/积分独立重算、76PNG解码、新进程页面及文件一致、105份已有存档相关文件字节保留通过。平板一次2.270秒核对/捕获重叠保留排除，采用随后串行复核。仅功能内容PASS；非英雄鱼仍有样板形体，首套用户/科学/儿童、完整拟真/真实触控/双真机及完整产品仍未验。游戏源码和模型未改，未新编译或更新个人手机/TestFlight，apply未完成。见[当前双端结果](../reports/content-current.md)。

本入口补足“连续20轮”与“收齐内容”之间的区别。目标为REQ-01/02/03/07：通过正常GameUI/EventSystem，随机收齐12种可钓鱼、完成18个任务、自然解锁并逐一装备20件钓具，再核对重启恢复。它是程序自动控线的功能验证，不替代儿童试玩、系统触摸、美术或双真机20分钟。

## 真实路径与界限

`RuntimeSmoke.RunJourney20(output, capture, quit, true)`及原生参数`--ocean-content-journey <截图目录>`共用完整模式。每次必须使用显式、全新、隔离的`--ocean-save-root`，拒绝覆盖已有回执。旧`--ocean-journey20`保留固定20轮语义。

完整模式仍调用正常抛竿、提竿、收线、放流按钮；不直接推进FishingSession、不注入积分/鱼获、不替换随机种子或指定目标鱼。自动策略在已解锁海域中优先选择尚未发现鱼种较多的海域，装备已解锁的较强钓具，张力达到0.65时松手。前10轮辅助，第11轮在设置中切换标准模式。重复鱼照常记录，不伪装成新物种。

每轮通过正常存档读取校验放流数、唯一鱼获ID和积分总账；放流同帧再点一次，核对不会重复奖励。只有完成至少20轮、全部18任务及全部20件装备自然解锁后才停止钓鱼。上限96轮、总时限45分钟；达到上限仍缺内容时记录FAIL，不能放宽为20轮PASS。该上限用于限定一次自动验证，不证明任意随机序列都能在96轮内完成。

最后逐件通过可见、射线命中检查的按钮装备20件钓具，确认积分不扣除；逐一打开12种鱼的图鉴，核对已发现记录及学名；核对18张任务卡的目标/完成进度并滚动截图，随后记录三种保护观察。模型与自然知识另行验收，不因页面可打开就判定科学/美术合格。

## 运行入口

所有Unity作业串行；通过当前iSpec apply attempt执行验证并保留失败。单独的完整无画面PlayMode用例为：

```sh
ispec changes measure --change product-technical-foundation --stage apply --category test -- python3 tools/game.py tests --test-platform PlayMode --test-filter '^OceanAngler.Tests.ContinuousJourneyTests.AllEighteenTasksAndTwelveRandomSpeciesSurviveSceneRestart$' --label contentReview1
```

每次使用新label。测试会保留本次前后存档，结束后恢复同一测试根的原文件；真实个人存档不参与。无画面结果不证明文字实际渲染与可读性。

原生构建仍使用`tools/game.py simulator --label <新标识>`和实际Xcode ARM64 Simulator编译。冻结源码、Unity Data和App摘要后，只在本项目拥有的QA模拟器运行新包；新进程以同一隔离存档根恢复，核对鱼获、积分、18任务、装备和观察记录。设备标识、容器路径、原始日志和完整回执只留`.local`；公开报告仅包含自有App画面及脱敏摘要。

## 验收状态

当前运行见[本次同包双端证据](../reports/content-current.md)；早期完整成长见[历史证据](../reports/complete-content-journey.md)，二者保留各自冻结范围。固定20轮历史、当前完整模式、无画面、原生图像与物理设备必须分别报告。iSpec完整apply及最终产品出口仍需其他任务完成。
