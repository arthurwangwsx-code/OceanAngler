# OceanAngler 产品合同

## Purpose
定义儿童向3D海洋钓鱼产品的长期行为与分阶段验收要求。当前实现和验收状态单独记录，不将需求当作已实现。

## Requirements

### Requirement: REQ-13 本人开发者模式
The system SHALL provide an explicit parent-area developer mode that removes point unlock requirements for all habitats, gear, fish journal models and observation content while keeping normal and test progress isolated.

#### Scenario: REQ-13 acceptance
- **WHEN** 本人确认进入开发者模式
- **THEN** 零积分可以使用全部三海域、二十件钓具、十二种图鉴模型及三观察，进度独立保存；取消不切换，关闭还原正常进度，重启保持选择，写入失败不换态，正常模式原门槛继续有效。必须提供实际测试和页面证据，不以加分或伪造鱼获代替解锁。

### Requirement: REQ-01 儿童钓鱼完整循环
The system SHALL satisfy the P0 contract: 能从抛竿完成至放流并再次抛竿，20次无卡死。

#### Scenario: REQ-01 acceptance
- **WHEN** 对应阶段执行验收
- **THEN** 必须提供以下证据：能从抛竿完成至放流并再次抛竿，20次无卡死，未运行不得写 PASS。

### Requirement: REQ-02 张力和两档辅助
The system SHALL satisfy the P0 contract: 松手降低张力，超载可逃脱，辅助与标准均可收集。

#### Scenario: REQ-02 acceptance
- **WHEN** 对应阶段执行验收
- **THEN** 必须提供以下证据：松手降低张力，超载可逃脱，辅助与标准均可收集，未运行不得写 PASS。

### Requirement: REQ-03 积分幂等结算及恢复
The system SHALL satisfy the P0 contract: 重复catchId不重复奖励，损坏存档可恢复备份。

#### Scenario: REQ-03 acceptance
- **WHEN** 对应阶段执行验收
- **THEN** 必须提供以下证据：重复catchId不重复奖励，损坏存档可恢复备份，未运行不得写 PASS。

### Requirement: REQ-04 12鱼种与观察生物
The system SHALL satisfy the P1 contract: P1有12种可钓鱼和3种仅观察生物，保护对象不可钓。

#### Scenario: REQ-04 acceptance
- **WHEN** 对应阶段执行验收
- **THEN** 必须提供以下证据：P1有12种可钓鱼和3种仅观察生物，保护对象不可钓，未运行不得写 PASS。

### Requirement: REQ-05 完整3D鱼与钓具
The system SHALL satisfy the P1 contract: 鱼和钓具满足完整资产清单、美术A1-A3。

#### Scenario: REQ-05 acceptance
- **WHEN** 对应阶段执行验收
- **THEN** 必须提供以下证据：鱼和钓具满足完整资产清单、美术A1-A3，未运行不得写 PASS。

### Requirement: REQ-06 三海域与水下效果
The system SHALL satisfy the P1 contract: 三个海域均有水面/水下可读场景。

#### Scenario: REQ-06 acceptance
- **WHEN** 对应阶段执行验收
- **THEN** 必须提供以下证据：三个海域均有水面/水下可读场景，未运行不得写 PASS。

### Requirement: REQ-07 图鉴和18任务
The system SHALL satisfy the P1 contract: 图鉴记录尺寸与首次发现，18固定任务无日期锁定。

#### Scenario: REQ-07 acceptance
- **WHEN** 对应阶段执行验收
- **THEN** 必须提供以下证据：图鉴记录尺寸与首次发现，18固定任务无日期锁定，未运行不得写 PASS。

### Requirement: REQ-08 iPhone/iPad与无障碍
The system SHALL satisfy the P1 contract: 横屏安全区、左右手、减弱动态、非纯颜色提示。

#### Scenario: REQ-08 acceptance
- **WHEN** 对应阶段执行验收
- **THEN** 必须提供以下证据：横屏安全区、左右手、减弱动态、非纯颜色提示，未运行不得写 PASS。

#### Scenario: REQ-08 gentle rest reminder
- **WHEN** 未暂停游玩累计900秒
- **THEN** 仅在准备抛竿时借用原边缘提示，暂停/首页复用原继续按钮和说明；不自动暂停、不遮挡提竿/控线/收鱼。普通导航不清除，主动确认继续才开启下一周期，不改变已有积分、记录或设置。计时边界、原生实际时间/画面与儿童体验分别验收。

### Requirement: REQ-09 隐私家长区
The system SHALL satisfy the P1 contract: 无跟踪SDK或隐私权限，清档与外链经家长区。

#### Scenario: REQ-09 acceptance
- **WHEN** 对应阶段执行验收
- **THEN** 必须提供以下证据：无跟踪SDK或隐私权限，清档与外链经家长区，未运行不得写 PASS。

### Requirement: REQ-10 开源许可与GitHub流程
The system SHALL satisfy the F0 contract: MIT代码、独立资产清单、CI与公开目录审查。

#### Scenario: REQ-10 acceptance
- **WHEN** 对应阶段执行验收
- **THEN** 必须提供以下证据：MIT代码、独立资产清单、CI与公开目录审查，未运行不得写 PASS。

### Requirement: REQ-11 本人TestFlight发布
The system SHALL satisfy the P0 contract: 独立App、内部专用构建、仅本人组，安装另记。

#### Scenario: REQ-11 acceptance
- **WHEN** 对应阶段执行验收
- **THEN** 必须提供以下证据：独立App、内部专用构建、仅本人组，安装另记，未运行不得写 PASS。

### Requirement: REQ-12 真机性能与恢复
The system SHALL satisfy the P1 contract: 目标真机20分钟性能、后台和锁屏恢复分别记录。

#### Scenario: REQ-12 acceptance
- **WHEN** 对应阶段执行验收
- **THEN** 必须提供以下证据：目标真机20分钟性能、后台和锁屏恢复分别记录，未运行不得写 PASS。
