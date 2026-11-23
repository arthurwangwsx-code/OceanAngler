# 珊瑚海湾源资产候选

本目录保留第一处海域的完整可编辑源文件CoralBay.blend。当前为候选，不能视为3海域全部完成，也尚未通过最终美术、科学人工复核和真机验收。

场景采用米制，水面标高4.5米。连续海床从近景沙质浅滩延伸到远景，西侧抬升为岸线；包含24片原创侵蚀岩礁、分枝/盘状/丘状三类泛化珊瑚、260丛海草、独立木板/横梁/桩柱/绳栏/螺栓构成的码头，以及带开放船壳、座板、船桨和系泊绳的小艇。珊瑚形态尚未对应具体物种，不能据此发布未经审核的物种科普。

八组静态组件分别保留LOD0/1/2，水面单独保留网格。沙、石、木和三类珊瑚分别有原创颜色、法线、粗糙度及金属度/平滑度打包贴图；另有原创焦散灯纹和软边浮游颗粒图。贴图和模型均由本项目代码生成，没有下载网页图片、模型、商标、私人媒体或旧项目资产。原创部分使用MIT许可，原件摘要登记于art/asset-manifest.json。

生态分区只参考[NOAA珊瑚礁分区说明](https://oceanservice.noaa.gov/education/tutorial_corals/media/supp_coral04b.html)的岩质与沙质底部、近岸与外礁层次，没有复制其图片，也不宣称场景是现实地点的复原。游戏中的物种分布和观察生物另行审核。

重建及检查（Unity作业和资产写入须串行，每次使用新label）：

```sh
python3 tools/art.py build --asset coral-bay --label bay-build-new --no-preview
python3 tools/art.py validate --asset coral-bay --label bay-validate-new
python3 tools/game.py coral-bay
python3 tools/game.py tests
python3 tools/art.py review --asset coral-bay --label bay-review-new
```

实际检查用项目iSpec measure包裹。build-report.json记录实际面数；独立源文件、Editor导入、源图、游戏窗口、真机分别验收。Blender灯光、折射水面和体积水只用于源资产审阅，摄影水面用透明阴影近似日光穿透，未求解完整焦散；Unity采用独立的实时近似，工作室图不能当作游戏截图。摄影远裁剪须覆盖摄影用水体，避免把远景裁出亮缝。

Unity水面使用自有BayWater着色器，接入深度、场景颜色、岸边交界、波纹和视角反光；水下使用雾、少量投射焦散和最多72个软边颗粒。深度/颜色纹理开销、透明叠加和LOD切换仍须实际设备测量。依照[URP 17.3管线配置](https://docs.unity.cn/Packages/com.unity.render-pipelines.universal@17.3/api/UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.html)启用所需纹理，不把配置存在当作效果或性能合格。

游戏仅对bay启用本候选，其余两海域继续明确保留旧样板。观察模式暂停钓鱼，返回后仍需用户点继续；水上/水下各有两个镜头。首鱼、完整纺车钓组与海湾已串行集成至integration/art-candidates，实际截图和完整验收分别记录。

实体修复后的源模型对礁石与丘状珊瑚先焊接几何接缝/极点，再生成三档LOD，保留面角UV；面朝外，丘状珊瑚底部封闭并埋入基岩。独立validator在临时副本检查每个连通实体的流形边及有向体积，不改写被验源文件。旧文件失败和修复后的实际游戏对照见reports/coral-bay-solid-geometry.md；三档封闭/朝向通过不等于成品美术或真机验收通过。
