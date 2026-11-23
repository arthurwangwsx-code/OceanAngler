# GitHub 开源与协作流程

用户已要求后续开源。本地采用 MIT 代码许可，资产单独登记；正式公开前核实远程所有者、仓库名 OceanAngler（候选）、隐私审查与模型再分发权。仓库应公开本项目可分享代码与原创/可再分发资源，不上传现有赛车/枪战源码或素材。

流程：本地检查 → 精确文件清单/秘密与许可审查 → GitHub身份与候选重名查询 → 创建目标仓库 → 设置 master 默认 → 推送已验证提交 → 验证公开仓库文件、CI、README、License、贡献说明 → 记录 URL 与 SHA。不得使用 --force，不更改其他远程。

master 唯一长期基线；named branch 从 master 创建，完成检查及审阅后 fast-forward。本地 pre-commit/check-branch-policy 防止 master 直接提交。CI pull_request/push(master) 运行计划与基础校验；Unity 需要合法 Editor/许可证的 macOS runner，不将 Unity 授权文件放仓库，不在无授权 runner 假装跑了引擎测试。

公开流程使用本项目专门目录；build、.local、密钥、签名、原始设备 ID、付费资产、私有日志全部排除。Git LFS 仅在确实大文件时启用，先核对托管预算。CONTRIBUTING 和 PR模板要求需求ID、验证及资产来源；SECURITY 提供支持邮箱。

2026-11-23 重新核查gh登录已恢复，当前用户为arthurwangwsx-code，具有repo/workflow权限。公开前执行tools/publication_audit.py检查精确跟踪文件；实际远程创建与推送以发布回执为准。

## 已发布
仓库：https://github.com/arthurwangwsx-code/OceanAngler 。默认master，首个玩法版本9e39c23已推送，基础CI运行45182671441成功。公开前144个跟踪文件审查通过。来源许可与原始OFL notice保留；原始本地测试日志、凭据、Unity缓存及构建包未上传。回执见reports/github-publication.json。不同机器上的完整Unity重建尚未另行验收。
