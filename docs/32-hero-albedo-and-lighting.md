# 英雄鱼体色与光照对照

对应REQ-05/2.4。实际画面的鱼体偏绿、表面仍有塑料感。本轮先检查反射贡献，再调整原创体色贴图；保持现有模型、鳍膜、骨骼、动作、UI和积分规则。

新增`python3 tools/game.py fish-lighting-review --label unique --review-target ios`：加载本项目海湾和英雄鱼，以游戏视角、近景及反面做静态对照，使用Medium、Metal、Half HDR及4×MSAA。记录首次绘制、连续参考/重复、关闭环境反射和临时128像素局部探针；后者只用于诊断。默认天空反射实际存在，不能以“没有反射”解释全部塑料感。局部探针对所选鱼体画面没有明确观感收益，未加入正式资产或运行时。

首次提交绘制曾有场景子材质绑定异常，第二轮仍有少量初始差异，两次失败保留。诊断工具显式绑定原材质属性，首次图单独留存，参考与重复图要求RGB逐像素一致；参考稳定不代表玩家冷启动资格。探针捕获会触发Editor的颜色温度设置初始化；工具仅允许这一已识别的变化并恢复调用前文件，其他变化保留并报错，避免把编辑器副作用带进游戏源码。API依据[Unity RenderProbe说明](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/reflectionprobe/renderprobe)。

体色选择保持灰银侧面、克制的灰铜背部、蓝黑斑点和蓝色第二背鳍/臀鳍/尾鳍。参考[澳大利亚博物馆](https://australian.museum/learn/animals/fishes/bluefin-trevally-caranx-melampygus-cuvier-1833/)和[Fishes of Australia](https://fishesofaustralia.net.au/home/species/1652)的识别描述；RGB是美术选择，没有从照片取样、下载或再分发照片，不是科学或色度学测量。

新增`python3 tools/art.py build --asset bluefin --maps-only --label unique --no-preview`，要求已有Blend/FBX及同名链接图像，更新图像数据和源文件，保留三档网格、UV、骨骼、动作和FBX。全量重新生成曾使两档远距减面出现不同的折叠选择，独立身份检查失败；该候选保留，恢复原几何后通过贴图入口更新。独立检查实际保存的全部三档身份及FBX、八张其他PNG字节不变，只更新体色；源文件链接的颜色像素还须与导出PNG一致。

当前局部源/Editor/双端原生定向PASS，整体美术仍IN_PROGRESS。201534b同一包双端各15帧、222/191分，30份旧QA文件字节保留；双接触表及选定六张原图已查看，见[当前原图与范围](../reports/bluefin-neutral-albedo.md)。源189条件、72薄壁、三档24664/9864/3698预算、保存网格身份与图像链接分别验收；Editor、原生模拟器及人工画面分别记录。整体塑料感、近景细线、游动自然度、用户/科学/儿童、A1–A3与双真机仍待完成，不能以单次色调改进代替完整产品目标。不批量制作其他鱼种，不更改已签名TestFlight7。
