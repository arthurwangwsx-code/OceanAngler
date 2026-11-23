# 首批验收记录 · 2026-11-23

| 项目 | 结果 | 证据/限制 |
|---|---|---|
| 产品、技术、美术、路线与发布文档 | IMPLEMENTED / PASS | docs/01–08 |
| Git唯一目录及master基线 | PASS | 初始提交441065a；feature/product-technical-foundation |
| iSpec初始化 | PASS | CLI1.0.20，spec-driven；init-check ready；计划校验12需求/14任务PASS |
| 需求与任务 | IMPLEMENTED | 12需求、14工程任务；不是14项全部完成 |
| Unity工程 | PARTIAL | 固定6000.3.29f1；两次启动失败：UPM IPC30秒超时，第二次额外出现Licensing初始化超时；未导入完成 |
| 基础钓鱼逻辑 | 原型实现 / NOT_RUN | 五项Editor规则测试尚待运行 |
| 模型与水下美术 | PLANNED / NOT_RUN | 仅几何占位，资产清单无成品 |
| App Store Connect API连接 | PASS | 对com.arthur.OceanAngler精确查询成功，初始无bundle/app |
| Apple Bundle注册 | PASS | com.arthur.OceanAngler / 6A73JBG8G4，注册后API读回 |
| Apple App创建 | BLOCKED | API apps为空；官方要求网站创建，CUA连接超时 |
| Apple网页连接 | BLOCKED | CUA两次连接超时，未改变应用记录 |
| GitHub开源流程/CI | IMPLEMENTED / PARTIAL | 本地准备；gh token无效，连接器身份查询成功；远程尚未发布 |
| TestFlight | NOT_RUN | 尚无已归档/上传/Apple处理通过的构建 |
| 真机/模拟器/实际安装 | NOT_RUN | 未运行；不得从计划或API推定 |

原始日志与凭据不进入公开仓库。后续更新此文件保留明确的失败和恢复证据。

Unity失败时未运行Editor测试、模拟器、iOS导出、归档或上传；未生成伪造package lock。Python源码静态编译和计划/文档本地链接校验通过。具名分支保留未获引擎验收的工程，暂不提升master。

iSpec变更目录已由CLI创建，但工件阶段start命令长时间无返回；本轮停止该超时作业，未把工件生命周期标成完成。产品/技术正文和12项长期需求已独立落盘，阶段工件需恢复CLI后补齐。此状态不影响阅读方案，但不能声称完整iSpec工件验收通过。
