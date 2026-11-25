# OceanAngler 协作规则
本目录为唯一 canonical 项目。内部代号 OceanAngler，中文暂名海洋小钓手。
用户会话指令优先。master 是唯一长期基线；所有 feature/fix/chore/spike/integration 分支从 master 建立。
禁止 worktree、复制 checkout 和多 Agent 同时写入；主 Agent 独占分支、iSpec、代码、设备、提交和验证。
修改前读取本文件及相关 ispec，运行 git status --short、git branch --show-current、git worktree list，检查 master 和 WIP。
禁止 reset --hard、clean -fdx、stash 丢弃工作。通过检查和审查后受控 fast-forward。
优先本机 canonical iSpec 技能与 CLI；需求、设计、任务、验收在 ispec 中保留。计划不等于实现。
实现 PLANNED/IN_PROGRESS/IMPLEMENTED；验收 NOT_RUN/PASS/PARTIAL/FAIL/BLOCKED。逻辑、Editor、模拟器、真机、美术和分发分别验收。
Unity 作业串行；保留 .meta，不伪造 Editor 导入和 package lock。模型未达到美术标准前标明 prototype。
用户授权本项目后续 GitHub 开源和本人 TestFlight；只公开本项目经许可审查的代码/自有资产，不上传旧项目源码、凭据、付费模型、私人媒体或设备日志。
TestFlight 仅核实的本人内部组，不创建公开邀请、不启用外部测试、不提交 App Store 正式审核。
公开支持、反馈、隐私请求统一 arthur.wang.wsx@gmail.com；邮箱不证明法律发行主体。

## 本机构建空间治理
完整构建、测试、导出、安装、取证流水线必须通过 `python3 tools/storage_gate.py --expected-gib N -- COMMAND ARG`，保守申报峰值增长；共享CLI为 `/Users/ai/project/_workspace/tools/build_storage.py`，异机由 PERSONAL_BUILD_STORAGE_TOOL 指定。单个Unity/Xcode子进程适配不能替代整个流水线的外层锁。
每日实测空闲目标超过200 GB；120 GiB硬底线不变，预算须扣除所有活跃预留。拒绝或Busy不能通过降低申报、底线或绕过入口重试。未知工程不自动成为清理范围。
复用稳定SDK/配置缓存；保留最新两份完整原生导出/发布版本，历史冻结内容须先核验归档再收尾。保留源码、WIP、签名、个人媒体、失败和被引用证据，禁止无审查删除build、Library或导出。收据记录来源、退出状态与证据位置。共享合同 `/Users/ai/project/_workspace/docs/build-storage-lifecycle.md` 是本机治理事实源；不能声称本适配器拦截所有直接工具调用。
