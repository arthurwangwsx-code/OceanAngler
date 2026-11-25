"""Known source rig versions; reject arbitrary added or missing bones."""
BASE_BONES={'root','head','spine1','spine2','spine3','peduncle','tail','jaw','pectoralL','pectoralR','pelvicL','pelvicR'}
FLEX_BONES={'pectoralLMid','pectoralLTip','pectoralRMid','pectoralRTip'}


def known_rig(rig):
    recipe=rig.get('pectoralFlexRecipe')
    if recipe is None:return {b.name for b in rig.data.bones}==BASE_BONES
    return recipe in {'paired-distal-curl-v1','paired-distal-curl-v2','paired-distal-curl-v3'} and {b.name for b in rig.data.bones}==BASE_BONES|FLEX_BONES
