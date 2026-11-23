# 图鉴物种详情与自适应滚动

2026-11-24。当前图鉴将12条相同占位说明替换为逐种辨识特征和自然栖息环境，并展示既有学名。中文名、学名、个人记录、辨识及环境使用同一详情组件；窄窗口和平板均按实际文字测量排列，通过滚动阅读。未发现鱼仍保持轮廓，浏览不会增加发现、鱼获或积分。

新增重置视角，恢复旋转、缩放和游动；安全区尺寸变化时重新排版。没有新增外链入口、权限或存档字段。详情说明游戏海域为想象场景，不代表物种现实共存分布。体长/重量显示个人游戏记录，不把游戏数值当成生物学最大值。

## 资料与范围

按[物种一手参考清单](../planning/species-references.json)逐页复核辨识及栖息概述，使用简短中文转述；未复制图片或插画。蓝鳍鲹依据[澳大利亚博物馆](https://australian.museum/learn/animals/fishes/bluefin-trevally-caranx-melampygus-cuvier-1833/)，黄鳍鲷和赤点石斑鱼依据[黄鳍棘鲷物种页](https://fishdb.sinica.edu.tw/taxon/382634-fishdb)和[赤点石斑鱼物种页](https://fishdb.sinica.edu.tw/taxon/382526-fishdb)，金目鲷依据[AFMA物种页](https://www.afma.gov.au/species/alfonsino)。

其余依据NOAA的[红笛鲷](https://www.fisheries.noaa.gov/species/red-snapper)、[大西洋鲭](https://www.fisheries.noaa.gov/species/atlantic-mackerel)、[鲣](https://www.fisheries.noaa.gov/species/pacific-skipjack-tuna)、[鬼头刀](https://www.fisheries.noaa.gov/species/pacific-mahimahi)、[黄鳍金枪鱼](https://www.fisheries.noaa.gov/species/pacific-yellowfin-tuna)、[大眼金枪鱼](https://www.fisheries.noaa.gov/species/atlantic-bigeye-tuna)、[太平洋鳕](https://www.fisheries.noaa.gov/species/pacific-cod)和[大西洋庸鲽](https://www.fisheries.noaa.gov/species/atlantic-halibut)。大眼金枪鱼太平洋页本轮重复读取超时，改读同物种大西洋页核对形态及跨洋分布，不混用种群评估。

科学人工审核、保育/法律结论和模型准确性仍未通过；这些参考不能证明当前样板模型符合辨识描述。

## 检查与未完成项

真实场景PlayMode通过图鉴卡片选择12物种，各自调整为240、310、500点宽、230点高，核对学名、辨识/环境、记录状态、正文高度及字体生成字符。每例将ScrollRect滚动到底，确认最后段落位于viewport内；旋转、放大、静止后重置，验证查看状态和积分/记录不变。36组检查通过，范围是无画面的场景/排版，不是36台设备或实际触控。

最终源码身份、Editor/PlayMode与Mac编译、字体和公开审查见[脱敏记录](journal-species.json)。原生smoke增加最长学名详情、滚动后环境两张截屏检查，含倒影图的预期总数从14改为16；新路径尚未运行。本轮CUA明确返回Mac锁定，未在锁定状态重复启动先前超时的原生流程。

当前游戏画面、iPhone/iPad触控及20分钟性能、科学人工/家长儿童可读性未验。12成品模型、仅观察生物模型/路径/交互、其他海域和A1–A3仍未完成。TestFlight 0.1.0 (2)冻结2971b11，不含本轮变化。
