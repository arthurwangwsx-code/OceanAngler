# 入门纺车轮钓组源资产

当前是第一套完整结构的原创候选，尚未通过最终美术和真机验收。它替换旧的竿/轮几何样板，不代表20件钓具已经分别制作成品模型。

可编辑源文件为SpinningRig.blend，单位米，竿长2.35米。原始几何、UV、碳纤维/软木颜色与法线贴图由tools/art_spinning_rig.py生成；没有复制品牌外形、照片、商业模型或旧项目资产。代码和原创资产使用项目MIT许可，摘要登记于art/asset-manifest.json。

结构包含连续握把芯、软木握把、螺纹轮座、渐细竿身、九个带陶瓷圈/支架/绑线的导环；卷线器包含机壳、支脚、摇柄、转子、可翻线挡、滚线轮、绕线线杯和卸力旋钮。拟饵包含实心米诺、眼、舌板、连接环及两个无倒刺单钩。竿身11根骨骼（固定Butt和10段RodBone），七个功能组件各有三档LOD。

运行时摇柄驱动转子，线杯沿竿轴往复，张力驱动竿身弯曲，鱼线经过随骨骼移动的九个导环。5.2的传动比是本游戏候选参数，不对应某款商品。高张力松线时允许线杯卸力转动。减弱动态和暂停分别控制演示运动。StudioLine只用于Blender展示，游戏使用独立LineRenderer，不随FBX重复导入。

重建和验证须串行执行：

```sh
python3 tools/art.py build --asset spinning-rig --label rig-build-new --no-preview
python3 tools/art.py validate --asset spinning-rig --label rig-validate-new
python3 tools/game.py spinning-rig
python3 tools/game.py tests --test-platform PlayMode --label rigtestsnew
python3 tools/art.py review --asset spinning-rig --label rig-review-new
python3 tools/art.py review --asset spinning-rig --label rig-turntable-new --turntable
```

每次使用新label保留证据。实际执行时用项目iSpec measure包裹检查命令。build-report.json保存实际面数；独立重开源文件的工程验证、Unity导入和运行检查各自记录。工作室渲染在art/previews/spinning-rig，不能替代游戏内画面。

尚缺游戏内白天、阴影和水下近景、完整交互/LOD观感、真实手机和平板预算、用户审阅，以及其余钓具差异化模型。机壳和部分小五金的表面、细部比例仍需美术精修。第一人称手部、抛投完整手势和拟饵水下动作尚未制作。本候选不能据工程通过把REQ-05或A1–A3改为PASS。
