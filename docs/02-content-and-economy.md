# 内容、装备和数值 v0.1

## 世界与生态
| 阶段/海域 | 水层与景观 | 可钓鱼候选 |
|---|---|---|
| P0/P1 珊瑚海湾 | 码头、沙地、局部礁石、清澈浅水 | 蓝鳍鲹、黄鳍鲷、赤点石斑鱼、红笛鲷 |
| P1 近海航道 | 小船、开阔水面、鱼群剪影 | 大西洋鲭、鲣、鬼头刀、黄鳍金枪鱼 |
| P1 陆坡海域 | 安全研究船、较暗蓝水、岩壁 | 大眼金枪鱼、金目鲷、太平洋鳕、大西洋庸鲽 |
| P2 深海研究站 | 探照灯、海雪、发光观察生物 | 新增鱼种需生态和美术双重审查 |

候选鱼的地理分布不意味着在现实同一海湾共存。世界设定为多个研究海域，物种页在素材制作前核对学名、分布、水深与保护状态，未经核对不能写成科普事实。海龟、海豚、鲸鲨作为 P1 仅观察候选，不可上钩。P0 从候选中做 3 个轮廓占位，不能算完成物种模型。

## 钓具与成长
6 竿：入门轻竿、礁区轻竿、近海中竿、船钓中重竿、远投竿、深水竿。4 轮：入门纺车、轻型纺车、船钓轮、深水电动辅助轮。4 线：轻/中/重/深水等级，强度递增。6 饵：软虫、鱼形软饵、亮片、米诺、铁板、深水发光拟饵。所有模型须包含竿梢、导环、握把、轮座、线杯、摇柄及连接关系；持竿、弯曲、摇轮、抛投和收鱼动画配套。

竿决定弹性和可控鱼体量，轮决定收线速度与泄力，线决定张力上限，饵决定目标鱼偏好与水层。不是简单所有属性线性增加；每套都可捕获对应鱼种。初始套装无限使用，无消耗性活饵和修理费用。

进度阈值提案：0/300/900/1800 研究积分解锁轻竿/礁区/近海/陆坡。首个海域不锁死教学；用户可重复常见鱼推进。不靠随机稀有鱼阻断主线。

## 积分（待平衡）
捕获基础分 = round(物种基础分 × clamp(重量/物种参考重量, 0.5, 1.8))。
本次总分 = 基础分 + 首次发现 100 + 技巧奖 0…30 + 放流 20。同一 catchId 只能结算一次；放流是记录动作，不重复加分。逃脱保留已经完成的独立任务进度、不扣已有积分。研究积分只增不减；装备由进度解锁，不用积分购买，避免儿童花错资源。

每条鱼用稳定 fishId；尺寸为有界分布，稀有度影响遇见权重而非现实保护等级。随机种子可注入测试，教程固定。概率与最终解锁时间先跑 1000 次模拟，再用人类试玩调整，不暗中按付费行为调概率。

## 数据合同
FishDefinition: id, displayNameKey, scientificName, habitatIds, depthRange, weightRange, basePoints, baitAffinities, stamina, burstStrength, modelId, conservationReview。
GearDefinition: id, category, unlockPoints, compatibleIds, stiffness, drag, lineStrength, retrieveSpeed, modelId。
HabitatDefinition: id, depthRange, fishPool, lightingProfile, currentProfile, unlockPoints。
CatchRecord: catchId, fishId, weight, length, mode, skillPoints, released, awardedPoints。
SaveEnvelope: schemaVersion, totalPoints, catches, unlocks, tasks, settings；派生解锁可重新计算。

生产数据用 ScriptableObject 导出可验证 JSON，所有 ID 唯一且不可复用删除物种 ID；资源索引缺失必须清楚报错并阻止正式构建。
