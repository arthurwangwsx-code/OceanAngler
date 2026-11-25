"""Original presentation proportions; not measured biological parameters."""
import math
EYE_RADIUS=.043
CORNEAL_RADIUS=.060
RIM_HEIGHT=.002
PUPIL_RATIO=.032/EYE_RADIUS
PUPIL_COLOR=(.025,.05,.06)
IRIS_ROUGHNESS=.12
GILL_COLOR=(.21,.25,.24)
RECIPE='continuous-eye-v1'
def surface_height(radius):
    return RIM_HEIGHT+math.sqrt(max(0,CORNEAL_RADIUS**2-radius**2))-math.sqrt(CORNEAL_RADIUS**2-EYE_RADIUS**2)
def pupil_cover(radius):
    t=max(0,min(1,(PUPIL_RATIO+.006-radius)/.012))
    return t*t*(3-2*t)
