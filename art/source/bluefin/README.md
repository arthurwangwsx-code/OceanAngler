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

首条英雄鱼与第一套完整钓具须有360°转台、四角截图、游戏内白天/阴影/水下近景和真机预算证据，经用户审阅后再批量制作其他物种。第一套纺车钓组与珊瑚海湾现已串行集成至integration/art-candidates；三海域现均有独立候选，此鱼的工作不能覆盖它们的验收。

表面后续迭代将鳃盖隆起融入鱼身，移除独立刚性覆盖片；细鳃缘与鱼体使用相同纵向权重。原始材质提高粗糙度、减弱皮肤清漆及法线、降低鳍平滑度，Unity显式写入主颜色与兼容颜色字段，防止后续导入补写导致源码变化。验证器读取七动作各五个姿态的实际变形网格，检查鳃缘到身体表面最近距离；它只验证贴合，不判定生物结构或审美。新四角、咬钩姿态、转台和游戏前后对照见reports/bluefin-surface-and-ui.md，成品美术仍未通过。

当前口部又修复了刚性下唇、倾斜且方向错误的下颌轴，补充混合权重、颌后连接和朝内的口腔/喉壁/内颊。独立检查实际向下位移、横向扭转、内壁射线及背部UV接缝；鳍膜改成一段UV对应一个几何鳍条间隔，膜厚对称以减少闪点。当前源四角、口/鳍近景、咬钩及完整周期转台在art/previews/bluefin-anatomy；原bluefin预览目录是历史快照。详情见[口部与鳍条证据](../../../reports/bluefin-mouth-and-fins.md)。Unity `fish-review`只做导入资产的Editor离屏诊断，保留修复前异常首图；现按子材质索引设置颜色，首次/重复绘制均一致，不克隆或修改源材质。30项Editor、7项PlayMode和Mac编译通过；PropertyBlock不使用SRP Batcher，成本待真机测量。隔离工具不能验收玩家启动或真机。

当前鳍色/比例与表面迭代见[bluefin-palette.md](../../../reports/bluefin-palette.md)：三LOD24284/9712/3642，六源材质槽，蓝/灰铜鳍区共用1024×512纹理；斑点按局部UV尺度补偿。当前工作室/Unity图与转台在art/previews/bluefin-palette，原anatomy目录是历史快照。科学摄影仅浏览参考，没有收入资产。近景阴影通过自身投影/焦散/法线与偏移对照，最终保留1024单级联及投射/接收，使用24米/Low软过滤和偏移2/0；成本与A1–A3继续待验。转台可由review --turntable生成36帧，再用ffmpeg -framerate 12 -i turn-%03d.png -c:v libx264 -pix_fmt yuv420p -movflags +faststart turntable.mp4合成；该帧率不是运行时性能。

当前薄鳍迭代见[制作约束](../../../docs/25-hero-fin-tissue.md)：保留24664面LOD0预算，三LOD24664/9864/3699；鳍膜在各LOD独立重建闭合渐薄壁，胸鳍边界按有序轮廓修正；原RGBA膜图与法线、粗糙度、MetallicSmoothness均为本项目生成。鳍条与膜拆为七材质槽，实体保持不透明，膜采用URP Lit透明Alpha并保留高光。膜不写深度或投影，不声称真实折射或生物透射率；多角度、原生运行、美术和科学验收须分开。

当前胸鳍曲线B内收后缘，三LOD24664/9864/3698，保留原贴图；非胸鳍LOD0在保持六位精度/面朝向/重复数量、归一面顺序/循环起点后几何UV权重与材质角色一致。第一版工程通过但画面被否决，失败样本不删除；当前仍非完整美术通过，见[胸鳍回执](../../../reports/bluefin-pectoral.md)。
# 体色单独更新入口

已有源模型需要改色时，使用`python3 tools/art.py build --asset bluefin --maps-only --label unique --no-preview`保留现有网格、UV、骨骼、动作和FBX；随后重新验证源文件和导入贴图。完整build会重新执行减面，不保证不同折叠选择下逐顶点身份不变。当前体色与反射对照、首次失败及范围见[制作约束](../../../docs/32-hero-albedo-and-lighting.md)，科学、用户和完整美术仍待验收。
