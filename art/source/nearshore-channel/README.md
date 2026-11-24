# 近海航道源资产

本项目原创候选，许可MIT。船取自本项目自有CoralBay.blend，移除旧码头系绳并平移，原文件不改；复用的船用材质与水面效果仍来自项目自身。没有使用外部模型、照片、付费纹理或私人媒体。取得的模型/贴图与哈希见[资产登记](../../asset-manifest.json)。

需要Blender 5.3和Unity 6000.3.29f1，按顺序执行；每次使用新的证据标签，构建与Unity作业串行：

```sh
python3 tools/art.py build --asset nearshore-channel --label newchannelbuild --no-preview
python3 tools/art.py validate --asset nearshore-channel --label newchannelvalidate
python3 tools/game.py nearshore-channel
python3 tools/art.py review --asset nearshore-channel --label newchannelstudio
python3 tools/game.py channel-review --label newchannelgpu
```

从仓库根目录运行，结果和原始日志只留在.local；运行中的iSpec阶段须用`ispec changes measure`包装相应验证命令。`.blend`、FBX、四组512像素PBR纹理、船体水面剖面、Unity材质和prefab均保留；Unity `.meta`由实际导入生成，不能手写替换。

六组静态模型各三LOD，独立水面。独立验证读取保存网格，检查封闭/朝向、细叶体积、通道海床射线及三座岛各三LOD的水下基座。源预览和Editor GPU预览使用不同灯光，均非玩家窗口或手机截图。岛屿轮廓、石壁与土层衔接、重复岩石/叶片仍有明显样板感；完整拟真、美术A1–A3、生态科学、用户和设备验收未通过。见[方案与限制](../../../docs/15-nearshore-channel.md)。
