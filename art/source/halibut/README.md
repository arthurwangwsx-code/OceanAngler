# 大西洋庸鲽解剖候选

物种：Hippoglossus hippoglossus。当前为独立可审阅候选，未通过用户、科学审校或 A1–A3 美术验收；不代表其余物种已完成，也不作为首套风格获批后的批量制作。

## 科学依据与权利

- [NOAA 大西洋庸鲽](https://www.fisheries.noaa.gov/species/atlantic-halibut)：右眼型扁鱼、大口、弯曲侧线与内凹尾缘。
- [加拿大 DFO 物种资料](https://www.dfo-mpo.gc.ca/species-especes/profiles-profils/atl-halibut-fletan-atl-eng.html)：成体通常双眼位于右侧；右侧朝上，左侧盲侧朝海底。眼侧深绿褐至近黑，盲侧较浅；大口与内凹尾缘作为形体参考。

2026-11-25 核对。仅参考文字形态事实，不下载、嵌入或再分发第三方照片或绘图。几何、UV、程序贴图、骨骼和动作由 tools/art_halibut.py 原创生成，采用本项目 MIT 许可。自产渲染不等于第三方物种照片或科学认可的参考图。

## 可重建产物

Blender 5.3：`python3 tools/art.py build --asset halibut --label unique-build --no-preview`。该命令只改写本物种源文件与导出，不重建英雄鱼、钓组或海域。

`python3 tools/art.py validate --asset halibut --label unique-validation` 读取真实保存的 blend，核查三 LOD、UV、权重、双眼位置、扁平比例、凹尾及七动作的 35 个实际变形姿态。

`python3 tools/art.py review --asset halibut --label unique-review` 输出眼侧、盲侧、斜视、薄边、正面、眼睛和嘴近景。加 `--turntable` 输出 36 帧转台；加 `--motion bite --frame 16` 检查开口。

Halibut.blend 是可编辑源，FBX 与 1K 身体 / 512 鳍 / 256 虹膜原创贴图在 game/Assets/OceanAngler/Art/Halibut。源坐标 X 向尾、Y 宽、Z 正向眼侧；Unity 导入需要核对转换后的眼侧朝上和钩点。

Unity 导入入口为 `python3 tools/game.py halibut`；隔离 GPU 图入口为 `python3 tools/game.py halibut-review --label uniqueLabel`。三个网格共享五个材质槽与七个派生动画资产。当前隔离 prefab 尚未接入实际遭遇或图鉴的选鱼工厂。

## 尚待验收

当前刚性胸鳍、边鳍摆动、细节与自然色泽仍需实际画面审阅；普通减面可能损失鳍边及眼部轮廓。Unity 开口近景已发现口缘/牙根连接未完成，牙齿呈脱离感，该项视觉 FAIL；鳃盖与眼部也需细化。源网格通过不代表 Unity、Player、双端触控、真实设备性能、儿童或完整美术通过。不能把解剖修正当作成品拟真完成。
