# 首批验收记录 · 2026-11-23

| 项目 | 最终状态 | 证据与范围 |
|---|---|---|
| 产品、技术、美术、路线、开源与发布方案 | PASS | docs/01–08，本地链接及计划校验通过 |
| 需求与任务基线 | PASS | 12项需求、14任务；任务不是全部实现 |
| iSpec工件 | PASS | 四工件正式done；长期规格和变更strict校验2/2通过 |
| Unity工程导入与场景生成 | PASS | 6000.3.29f1实际导入，Bay.unity、URP、真实包锁和.meta已生成 |
| 基础规则测试 | PASS | EditMode 5/5：错过咬钩、暂停、超载、控线捕获/重复结算、非法时间 |
| Python工具与Git检查 | PASS | py_compile、validate.py、diff及分支政策 |
| 成品鱼模/钓具/水下美术 | PLANNED / NOT_RUN | 仅几何占位，资产清单无成品 |
| ASC连接及独立Bundle注册 | PASS | com.arthur.OceanAngler / 6A73JBG8G4，API读回确认 |
| Apple应用记录 | BLOCKED | apps查询为空；官方须网站创建，网页控制两次连接超时 |
| GitHub开源流程/CI | PARTIAL | 本地许可、资产规则、CI已建立；gh登录失效，尚无远程发布 |
| TestFlight上传与分发 | NOT_RUN | 尚无归档、上传或Apple处理通过的构建 |
| 模拟器、真机、安装与视觉 | NOT_RUN | 规则测试不证明视觉、触控、性能或安装 |

初期两次Unity启动遇到UPM IPC及授权服务超时，第三次恢复并完成导入和规则测试。iSpec早期start超时停止后也已成功重跑。原始日志和XML在.local，不进入公开仓库。

原型目前只有一条几何鱼、钓鱼规则和临时120积分，没有P0完整存档、三鱼种/两套钓组和正式中文触控UI。局部通过不代表完整游戏或拟真模型完成。

后续完成P0与英雄鱼/完整钓具样板；恢复Apple网页并创建独立App后再归档和本人内部TestFlight。GitHub按公开文件审查清单后续创建远程。
