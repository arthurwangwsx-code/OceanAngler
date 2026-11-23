## ADDED Requirements

### Requirement: 可追踪产品与技术基线
系统 SHALL 保存完整玩法、内容、3D资产、架构、数值、测试和阶段方案，并区分计划与实现。

#### Scenario: 审阅首批方案
- **WHEN** 打开项目README
- **THEN** 可找到产品、技术、美术、任务和验收入口，12项产品需求均关联任务。

### Requirement: 独立且可验证的工程
工程 MUST 固定Unity/URP版本并提供串行导入、规则测试和iOS导出入口，测试失败不得标为运行通过。

#### Scenario: 本机服务不可用
- **WHEN** Unity导入或授权服务失败
- **THEN** 保留私有日志并记录PARTIAL/BLOCKED，不伪造包锁、测试或可安装包。

### Requirement: 受许可约束的开源
工程 SHALL 提供MIT代码许可、单独资产登记、CI与master具名分支策略。

#### Scenario: 公开资产审查
- **WHEN** 准备向GitHub公开项目
- **THEN** 凭据、私有日志、付费原始资产和旧项目源码不得进入公开提交。

### Requirement: 本人内部TestFlight
发行工具 MUST 使用本项目独立bundle和App，只有Apple处理完成及本人内部组可访问时才能声称已分发。

#### Scenario: 尚无App记录
- **WHEN** ASC精确查询只有bundle没有app
- **THEN** 标为待网站创建，不尝试借用其他App上传，不创建外部邀请。
