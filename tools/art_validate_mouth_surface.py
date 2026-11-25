"""Inspect the saved mouth surface topology, independently of its authoring path."""
import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def inspect():
    result={'status':'RUNNING','scope':'Saved mouth engineering, not final art/science acceptance','lods':[]}
    for lod in range(3):
        mesh=bpy.data.objects['Bluefin_LOD'+str(lod)].data;linked={}
        for face in mesh.polygons:
            for edge in face.edge_keys:linked.setdefault(tuple(sorted(edge)),[]).append(face)
        bad=[];sampled=0
        for edge in mesh.edges:
            points=[mesh.vertices[i].co for i in edge.vertices];center=(points[0]+points[1])*.5
            if not (-.90<center.x<-.765 and center.z<-.01 and abs(center.y)>.005):continue
            faces=linked.get(tuple(sorted(edge.vertices)),[]);materials=[mesh.materials[f.material_index].name for f in faces]
            if 'Bluefin_Skin' not in materials:continue
            sampled+=1
            if len(faces)>2 or 'Bluefin_Dark' in materials:
                bad.append(dict(point=list(center),materials=materials))
        result['lods'].append(dict(lod=lod,sampledEdges=sampled,internalRoofSharingSkin=bad))
    result['status']='PASS' if all(r['sampledEdges']>10 and not r['internalRoofSharingSkin'] for r in result['lods']) else 'FAIL'
    return result
if __name__=='__main__':
    import os
    report=inspect();path=Path(os.environ['OCEAN_MOUTH_VALIDATION']);assert not path.exists();path.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
    if report['status']!='PASS':raise RuntimeError('Internal roof must not distort the external cheek normals')
