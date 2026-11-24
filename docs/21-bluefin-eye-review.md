# 英雄鱼眼部候选复核

本轮对应REQ-05/06，仍受英雄鱼及完整钓组样板审阅门约束；其余鱼种不据此批量制作。当前模型改动、失败及新模拟器证据见[眼部报告](../reports/bluefin-iris.md)。完整美术、科学与用户审阅分开记录。

所有Unity作业串行。本机iSpec非profile状态写入曾有并发竞态，因此**测量、metrics及状态修改也串行执行**；上一条测量终态未确认前，不启动下一条。历史状态已恢复，缺失启动记录的审计缺口仍保留。

使用新的label保留每次结果：

```sh
ispec changes measure --change product-technical-foundation --stage apply --category test -- python3 tools/art.py validate --label fresheyevalidation
ispec changes measure --change product-technical-foundation --stage apply --category e2e -- python3 tools/art.py review --label fresheyemacro --eyes-only
ispec changes measure --change product-technical-foundation --stage apply --category e2e -- python3 tools/art.py review --label fresheyestudio
ispec changes measure --change product-technical-foundation --stage apply --category e2e -- python3 tools/art.py review --label fresheyeturn --turntable
ispec changes measure --change product-technical-foundation --stage apply --category e2e -- python3 tools/game.py fish-review --label fresheyegpu --review-target ios
```

分别检查保存Blend、完整模型/口/鳍/双眼、36帧转台和实际导入材质，不能仅凭生成文件或退出0接受美术。`--eyes-only`限蓝鳍鲹静态review，不能与turntable组合。每次命令完成后才运行下一行。

PlayMode不带筛选运行完整套件。负向排除正则可能匹配祖先套件，最终范围必须读取XML的实际test-case清单；命令中写了排除不代表实际排除。运行后保留XML和原始日志再启动下一次测试。

新原生包须先提交并冻结游戏/工具源码与新PNG及真实Unity生成的meta，记录源摘要、SDK设置SHA和App文件摘要。构建入口和ARM64编译见[模拟器流程](19-ios-simulator-verification.md)。本轮irisa包绑定fd23c2b，不能复用旧journey2包证明眼部改动。

手动窗口检查只使用`--ocean-save-root <新的隔离目录>`和可选渲染探针，不带自动smoke/capture/20轮参数。按实际画面进行抛竿、提竿、控线及记录放流；核对真实文件后，重启新进程并显式指向同一目录。自动单轮仍可用既有`--ocean-smoke <新截图目录>`入口，须与鼠标操作分开。QA目录不得覆盖或删除。

更新安装后不要假定历史绝对数据容器路径仍有效。通过当前Bundle查询数据容器，再映射已知相对QA路径并核对原文件校验和/记录数/积分；找不到不能编造恢复结果。原始设备标识、数据路径和日志只留本地，公开限自有游戏画面及脱敏摘要。
