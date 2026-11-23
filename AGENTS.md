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
