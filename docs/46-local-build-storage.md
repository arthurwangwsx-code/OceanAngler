# 本机构建与缓存复用

按用户2026-11-25规定，完整测试、导出、原生编译、安装和取证由一个共享租约包裹。先用共享工具的`status`核对十进制GB、GiB和扣除全部活跃预留后的buildHeadroomGiB；每日空闲目标超过200 GB，120 GiB硬底线保持。

```sh
python3 /Users/ai/project/_workspace/tools/build_storage.py status
python3 tools/storage_gate.py --expected-gib 12 -- python3 /path/to/complete-simulator-pipeline.py
```

12 GiB是当前复用缓存、保留上一份完整生成工程和App、双端隔离取证的模拟器流水线预算示例，不是所有任务的通用预算。首次编译、设备发行及导出签名应按自身峰值保守申报。工具缺失时配置PERSONAL_BUILD_STORAGE_TOOL，不能绕开准入。拒绝或Busy后保留现场与回执，不缩小估算或降低底线强行启动。

`storage_gate.py`委托共享治理器，不复制其空间规则。内部启动器核对真实租约、runId和工程锁，再为托管scratch创建短路径符号链接，避免Unity IL2CPP Unix socket路径长度限制；子进程继续使用同一空间预留，退出后移除自己的短链接，真实scratch由共享治理器收尾。正常、失败和取消的原始日志/截图/测量回执仍保留。这个入口不能拦截一切直接编译器调用；game.py和beta.py的Unity/Xcode子命令已接入，但单个子命令不能替代完整任务外层门禁。

当前模拟器沿用`build/ios-simulator-developermode20261125b`和`build/simulator-derived-developermode20261125bnativea`作为同SDK/架构/配置的稳定导出和增量缓存，不因每次尝试新建一套。历史命名不意味着其中永远保留旧版本，需以每轮source/app/export摘要及保留位置映射为准。

已有导出默认拒绝覆盖。仅模拟器显式传入`--append --retained-export PATH`时，先逐项核对上一份完整生成工程的文件字节、模式及符号链接，再用Unity的[AcceptExternalModificationsToPlayer](https://docs.unity3d.com/jp/current/ScriptReference/BuildOptions.AcceptExternalModificationsToPlayer.html)增量更新已有Xcode工程。上一App也应完整保留、核验并记录旧路径的映射。它们是生成产物，不是复制源码checkout；本地留存也不是独立备份。设备发行继续使用固定`build/DerivedData-beta`，冻结导出、归档和IPA按版本留存，上传结果未知必须先回查Apple，不重复提交。

共享合同：`/Users/ai/project/_workspace/docs/build-storage-lifecycle.md`。保留最新两份完整原生导出/发行版本；历史冻结导出与符号按核验后的本地留存策略处理，保护源码、WIP、签名、个人媒体、失败/引用证据及活跃作业。不因空间不足自动扩大清理范围或删除整棵build/Library。

本次Unity 6000.3.29f1追加导出后，在GameAssembly的buildPhases数组重复引用了同一个ShellScript GUID，Xcode以Unexpected duplicate tasks退出65；原始导出、失败日志与测量均保留。后续仅在生成的模拟器工程中删除这一个完全相同的重复引用，保存前后项目摘要与变体说明，再用同一缓存编译。没有删除不同脚本或修改游戏逻辑；Unity导出通过也不等于原生编译通过。未来追加导出应先核对实际项目差异，不能自动清空工程或用新缓存掩盖问题。
