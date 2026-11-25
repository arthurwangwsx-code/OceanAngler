# 蓝鳍鲹连续眼表与头部近景

承接REQ-05、2.4及原虹膜候选。当前最大图鉴原图显示黑色瞳孔像独立纽扣、鳃盖边缘过黑。保存源近景确认虹膜与瞳孔为两个交叠球体，存在凸起的材质边缘。本轮用既有256色图中的深色区域表示瞳孔，与虹膜共享单一连续眼表；保留眼睛的中心、朝向与径向大小，仅调整前表曲率和湿润反光。移除独立瞳孔几何，不增材质槽或渲染通道。鳃缘保持现有几何、权重与动作，收敛过强的深色对比。

采用现有保存Blend局部更新，避免重新生成整条鱼及减面结果。三LOD的其他鱼体/嘴/鳍/线条几何、UV和权重，以及12骨和七动作须独立核对；保持25000近景三角面上限与递减LOD，八张其他PNG逐字保持。完整生成入口同步记录新眼表配方。导入后重建实际动画展示范围，继续检查完整形体、控件净空、缩放与暂停、原进度和最小游玩HUD。

先记录旧近景与结构红测，再检查保存网格、眼表可见面/贴合、实际PNG与不变项。源文件棚拍、导入Editor GPU与双端原生分别验收；新增高光必须来自曲面和现有灯光，不能画死在贴图上。实际前后对照不能仅靠数值/材质门槛宣布更拟真，也不能只用棚拍代替游戏画面。

物种资料继续参考[澳大利亚博物馆](https://australian.museum/learn/animals/fishes/bluefin-trevally-caranx-melampygus-cuvier-1833/)、[Museums Victoria物种页](https://fishesofaustralia.net.au/home/species/1652)和[Smithsonian物种条目](https://biogeodb.stri.si.edu/sftep/en/thefishes/species/1248)。前两处图片请求受限，未复制/绕过；眼表半径、突出量、湿润度与鳃缘色值为原创美术选择，不能称为来源实测或科学认可。图片不进入仓库。

源局部实现已接入：32项独立源检查与216条眼部首命中射线通过，非眼表17985个表面记录及三LOD其他UV/权重、12骨七动作保持；新面数23320/9450/3598。虹膜外圈36044像素、八张其他PNG与七套运行时动作及prefab逐字保持。局部导入入口避免重写既有动作与配置。

74项Editor与10项定向PlayMode通过，含3456个图鉴网格/控件净空样本；iOS目标Metal闭口/张口首绘与重绘差均为0，两侧眼部及鱼体原图已查看。棚拍和导入仅证明各自范围，完整美术仍PARTIAL，当前原生NOT_RUN。成品美术A1–A3、科学/用户、儿童、真实触控/双真机20分钟与当前来源TestFlight安装继续开放，2.4及apply不勾选。

局部重建入口为 `python3 tools/art.py build --asset bluefin --head-surface-only --label <新标签> --no-preview`，随后 `python3 tools/game.py bluefin-head` 保留既有运行时动作和prefab，再运行 `python3 tools/game.py journal-envelope` 更新实际几何数据。完整生成入口同步同一眼表配方。源文件局部核对使用 `tools/art_validate_bluefin_eye.py`，基线必须来自修改前的真实保存资产；不能用当前输出伪造原始基线。

后续按用户新要求优先进行[整体页面优化](43-immersive-pages.md)。afa8fed的原生编译被共享120GiB保护线拒绝，未安装；拒绝记录保留。旧24份完成构建已逐项ZIP/CRC/完整解压文件、权限和软链接核对后归档，当前回退App与TF8保留，未绕过门禁。
