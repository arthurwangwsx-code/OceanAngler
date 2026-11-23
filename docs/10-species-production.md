# 物种资料与建模交接

2026-11-24。依据一手机构资料整理12种可钓候选与3种仅观察候选的学名、辨识特征、栖息概况及逐种建模要求，机器可读清单见[species-references.json](../planning/species-references.json)。参考已核对，科学人工审阅仍为PENDING；这份清单不计作15个模型或场景制作完成。

| 目录ID／候选 | 学名 | 主要制作要点与参考 |
|---|---|---|
| trevally／蓝鳍鲹 | Caranx melampygus | 蓝色第二背鳍、臀鳍与尾鳍，蓝黑小斑；[澳大利亚博物馆](https://australian.museum/learn/animals/fishes/bluefin-trevally-caranx-melampygus-cuvier-1833/) |
| snapper／黄鳍鲷 | Acanthopagrus latus | 棘鲷高体、黄鳍；内部旧ID保持兼容，不能据snapper画成笛鲷；[中研院鱼类资料库](https://fishdb.sinica.edu.tw/taxon/382634-fishdb) |
| grouper／赤点石斑鱼 | Epinephelus akaara | 粗壮大口、圆尾、橙红斑点；[中研院](https://fishdb.sinica.edu.tw/taxon/382526-fishdb) |
| redsnapper／红笛鲷 | Lutjanus campechanus | 三角形头部与犬齿；[NOAA](https://www.fisheries.noaa.gov/species/red-snapper) |
| mackerel／大西洋鲭 | Scomber scombrus | 蓝绿背部的黑色波纹与银腹；[NOAA](https://www.fisheries.noaa.gov/species/atlantic-mackerel) |
| skipjack／鲣 | Katsuwonus pelamis | 下侧、腹部的纵向暗带，与鲭的背部波纹区分；[NOAA](https://www.fisheries.noaa.gov/species/pacific-skipjack-tuna) |
| mahi／鬼头刀 | Coryphaena hippurus | 蓝绿与金银色层次，注明模型性别和成长阶段；[NOAA](https://www.fisheries.noaa.gov/species/atlantic-mahimahi/seafood) |
| yellowfin／黄鳍金枪鱼 | Thunnus albacares | 成体较长的黄色鳍，与幼体区分；[NOAA](https://www.fisheries.noaa.gov/species/pacific-yellowfin-tuna/seafood) |
| bigeye／大眼金枪鱼 | Thunnus obesus | 眼径、体高、小鳍黑边分别建模；[NOAA](https://www.fisheries.noaa.gov/species/pacific-bigeye-tuna) |
| alfonsino／金目鲷 | Beryx splendens | 橙红高体、大眼与分叉尾；[AFMA](https://www.afma.gov.au/species/alfonsino) |
| cod／太平洋鳕 | Gadus macrocephalus | 下颌须、三背鳍和两臀鳍；[NOAA](https://www.fisheries.noaa.gov/species/pacific-cod)、[FAO鳕属资料](https://www.fao.org/4/t0243e/T0243E07.pdf) |
| halibut／大西洋庸鲽 | Hippoglossus hippoglossus | 成体两眼在右侧，眼侧/盲侧分开审阅；[NOAA](https://www.fisheries.noaa.gov/species/atlantic-halibut) |
| green-turtle／绿海龟 | Chelonia mydas | 甲片布局、鳍状四肢、上浮呼吸；[NOAA](https://www.fisheries.noaa.gov/species/green-turtle) |
| bottlenose-dolphin／宽吻海豚 | Tursiops truncatus | 短粗吻部与水平尾叶；[NOAA物种页](https://www.fisheries.noaa.gov/species/common-bottlenose-dolphin)、[海豚教育资料](https://www.aoml.noaa.gov/ftp/od/library/NicodemusDolphins.pdf) |
| whale-shark／鲸鲨 | Rhincodon typus | 深色背侧浅斑纹、纵向隆脊、滤食；[澳大利亚博物馆](https://australian.museum/learn/animals/fishes/whale-shark-rhincodon-typus-smith-1828/) |

## 数据与表现边界

ContentCatalog已登记12个候选学名和独立的3个ObservationDefinition条目。观察条目不属于FishDefinition，不能进入随机可钓池；FishingSession抛竿先按可钓目录解析目标ID，未知或观察ID在改变会话前拒绝，鱼获结算同样拒绝。场景中的观察生物模型、动画、出现路径、交互与观察图鉴仍未实现；目录登记不能代替这些工作。

学名尚未进入新的物种详情界面；当前游戏里的通用占位说明仍待结合人工科学复核与移动端排版更新。保留既有12个fishId及存档版本，避免历史鱼获失去对应。观察条目在游戏中的海域归属是制作安排，不是现实地理分布。

游戏尺寸与重量边界是平衡用的采样范围。科普不得将它们写成物种真实最大值，也不将研究海域的组合当作自然共存关系。源网页照片、插画及交互模型未下载或收入开源资产，只保留链接和自行整理的事实。

## 待审阅事项

中文俗名与学名的对应、性别/成长阶段、当前保育资料和最终可钓呈现需要科学人工复核。赤点石斑鱼资料库页面的EN提示需追溯当前IUCN原评估；NOAA大西洋庸鲽资源评估针对特定种群，不作全球等级推断。相关条目单独标注reviewFlags，未自动改动鱼种或历史存档。

首条鱼与完整钓组已有四角图、转台和工作室近景，本轮已提交给用户审阅方向；尚无用户结论，A1–A3出口保持未通过。其余物种的独立源模型继续遵守[美术生产与用户审阅门槛](03-art-pipeline.md)。

目录与入口检查见[species-catalog-checks.json](../reports/species-catalog-checks.json)：27项Editor、7项无画面PlayMode及Mac编译通过；观察ID伪造抛竿与结算均被拒绝。参考结构校验只核对ID/学名/来源登记，不能证明生物学内容正确，也不覆盖观察生物的场景交互。当前TestFlight build 2尚不含这些后续变化。
