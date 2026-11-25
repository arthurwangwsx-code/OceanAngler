# 陆坡峡谷源资产

本项目原创候选，许可 MIT。峡谷、碎石、研究潜器、测量桅杆与浮标系缆由 `tools/art_slope.py` 生成；小艇仅复用本项目自有 CoralBay 源工程，移除原码头系绳并改装，不改动原源文件。未使用外部模型、照片、付费纹理或私人媒体。

在仓库根目录依次运行，标签每次取新值；Blender 与 Unity 作业串行，运行中的 iSpec 阶段使用 `ispec changes measure` 包装验证命令：

```sh
python3 tools/art.py build --asset continental-slope --label newslopebuild
python3 tools/art.py validate --asset continental-slope --label newslopevalidate
python3 tools/art.py review --asset continental-slope --label newslopestudio
python3 tools/game.py continental-slope
python3 tools/game.py slope-review --label newslopegpu --review-target ios
```

保留独立 Blend、FBX、两组 1024 像素 PBR 和船舱剖面。Terrain、Scree、ROV、Boat、Survey 各三 LOD，水面独立。源检查读取实际保存网格，检查 UV、闭合与正体积、LOD、连续沉积权重、通道与潜器悬停空间；不以生成脚本中的预期数值代替网格测量。Unity `.meta` 必须由实际导入生成。

海平面 200 米为本地坐标。场景是艺术化深水样段，不是真实地点或完整光学、地质及生态模拟；没有复用浅海海草。Cycles 源预览使用盒式贴图与体积灯光，Unity 使用共享三轴 PBR、水面和可逆深水照明，两者分别验收。预览及 Editor 离屏图均不替代原生 Player、手机或用户验收。当前仍为样板，美术 A1–A3、科学与用户审阅未通过，详见[制作约束](../../../docs/24-continental-slope.md)。
