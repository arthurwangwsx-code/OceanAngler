# 蓝鳍鲹英雄鱼候选

物种：蓝鳍鲹，Caranx melampygus。用于首条英雄鱼的独立美术制作与审阅；当前为候选，不能视为A1–A3通过。其余11物种尚未按此质量生产。

## 科学参考与权利边界

- [Australian Museum物种页](https://australian.museum/learn/animals/fishes/bluefin-trevally-caranx-melampygus-cuvier-1833/)：核对上半体灰至黄铜色、小蓝黑斑点、第二背鳍/臀鳍/尾鳍蓝色等识别特征。
- [Smithsonian热带太平洋鱼类数据库](https://biogeodb.stri.si.edu/sftep/en/thefishes/species/1248)：核对侧扁体形、角状头部轮廓、双背鳍、前段拱起的侧线与后段硬鳞，以及长胸鳍。鳍条数量作为建模参考，不能代替科学人工审校。

资料核对日期2026-11-23。网页照片仅用于观察，不下载、不嵌入模型、贴图或公开参考板，不主张拥有其照片版权。几何、UV、程序贴图、骨骼和关键帧由本项目tools/art_bluefin.py原创构建，采用项目MIT许可。参考板使用本项目自产正/侧/俯渲染，不使用第三方图片。

## 可重建资产

推荐入口：python3 tools/art.py build --label unique-build --no-preview，然后python3 tools/art.py validate --label unique-validation。Blender内置bpy/numpy，无外部媒体或网络依赖。入口将Python异常映射为非零退出码，并保留独立日志；制作作业和Unity共用串行锁，不能同时改写与导入FBX。

- Bluefin.blend：可编辑网格、骨骼、动作、材质、灯光和检查相机。
- game/Assets/OceanAngler/Art/Bluefin：FBX及原创baseColor/normal/roughness/fin贴图。
- build-report.json：实际顶点/三角面数、骨骼、动作及贴图摘要。
- .local/art/bluefin：本机检查图，审阅后选择公开候选图；不将生成日志混入资产。

Unity执行python3 tools/game.py bluefin，重建URP材质、动画控制器和Resources/Models/Bluefin.prefab。无需本机Blender也能使用已提交FBX构建游戏。模型坐标修正与嘴部HookPoint保存在prefab中；删除导入器自动LOD组后仅由一个LODGroup控制三个档位，防止重叠渲染和表面闪烁。

静态四角：python3 tools/art.py review --label unique-rest。咬钩姿态：增加--motion bite --frame 16。360度：增加--turntable。这些是Blender工作室检查图；游戏画面、目标设备和人的审阅须分别记录。

## 审阅重点

检查头吻与口部、鳃盖嵌合、眼眶位置、第一/第二背鳍分离、长镰状胸鳍、叉尾、鳍根和侧线。检查正/侧/俯及逆光时破面、透明排序、硬鳞穿插，动画时口/鳃/鳍与鱼线连接，LOD切换的轮廓和材质变化。三角面数、贴图和七动作存在仅代表工程清单，并不证明拟真、自然或好看。

首条英雄鱼与第一套完整钓具须有360°转台、四角截图、游戏内白天/阴影/水下近景和真机预算证据，经用户审阅后再批量制作其他物种。第一套纺车钓组现有独立源模型候选，见../spinning-rig/README.md；环境仍为旧样板，此鱼的工作不能覆盖它们的验收。
