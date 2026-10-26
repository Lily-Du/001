#!/usr/bin/env python3
"""Validate committed Unity scene assets/references without claiming engine execution."""
import json
import math
from pathlib import Path
import re
import yaml

ROOT = Path(__file__).resolve().parents[1]
SCENE = ROOT/'Assets/DragonCourt/Scenes/DragonCourt.unity'


def validate():
    text=SCENE.read_text()
    pieces=re.split(r'^--- !u!(\d+) &(\d+)\n',text,flags=re.M)
    docs={}
    for i in range(1,len(pieces),3):
        cid,fid,body=pieces[i:i+3]
        fid=int(fid)
        assert fid not in docs, f'Duplicate file ID: {fid}'
        parsed=yaml.safe_load(body)
        name,data=next(iter(parsed.items()))
        docs[fid]=(int(cid),name,data)
    assets={}
    for meta in (ROOT/'Assets').rglob('*.meta'):
        values=yaml.safe_load(meta.read_text())
        g=values['guid']
        assert re.fullmatch('[0-9a-f]{32}',g),meta
        assert g not in assets, f'Duplicate GUID: {g}'
        assets[g]=Path(str(meta)[:-5])
    builtin={'0000000000000000e000000000000000','0000000000000000f000000000000000'}
    refs=0
    def walk(value):
        nonlocal refs
        if isinstance(value,dict):
            if 'fileID' in value and value['fileID']:
                refs+=1
                if 'guid' in value:
                    assert value['guid'] in assets or value['guid'] in builtin, value
                else:
                    assert value['fileID'] in docs, f'Dangling local reference {value}'
            for v in value.values(): walk(v)
        elif isinstance(value,list):
            for v in value: walk(v)
    for _,_,data in docs.values(): walk(data)
    for fid,(cid,_,data) in docs.items():
        if cid==1:
            for component in data['m_Component']:
                comp=docs[component['component']['fileID']][2]
                assert comp['m_GameObject']['fileID']==fid
        if cid==4:
            for child in data['m_Children']:
                assert docs[child['fileID']][2]['m_Father']['fileID']==fid
    nodes={data['m_Name']:data for cid,_,data in docs.values() if cid==1}
    layout=json.loads((ROOT/'Assets/DragonCourt/Resources/WhiteboxLayout.json').read_text())
    # Names repeat in JSON; match each exact transform to a saved scene object.
    all_objects=[v[2] for v in docs.values() if v[0]==1]
    for b in layout['blocks']:
        matches=[]
        for obj in all_objects:
            if obj['m_Name']!=b['name']: continue
            tr=docs[obj['m_Component'][0]['component']['fileID']][2]
            if list(tr['m_LocalPosition'].values())==b['center'] and list(tr['m_LocalScale'].values())==b['size']:
                matches.append(obj)
        assert matches, f'Layout block missing from scene: {b}'
        assert any(any(docs[c['component']['fileID']][0]==65 for c in obj['m_Component']) for obj in matches)
    for r in layout['ramps']:
        node=nodes[r['name']]
        tr=docs[node['m_Component'][0]['component']['fileID']][2]
        x,y,z=tr['m_LocalPosition'].values()
        sx,sy,sz=tr['m_LocalScale'].values()
        angle=2*math.atan2(tr['m_LocalRotation']['x'],tr['m_LocalRotation']['w'])
        for sign,expected_y,expected_z in [(-1,r['y0'],r['z0']),(1,r['y1'],r['z1'])]:
            top_y=y+math.cos(angle)*sy/2-math.sin(angle)*sign*sz/2
            top_z=z+math.sin(angle)*sy/2+math.cos(angle)*sign*sz/2
            assert abs(top_y-expected_y)<1e-6 and abs(top_z-expected_z)<1e-6,r['name']
        assert abs(x-r['x'])<1e-6 and sx==r['width']
    scripts={assets[d['m_Script']['guid']].stem:(fid,d) for fid,(cid,_,d) in docs.items() if cid==114}
    assert set(scripts)=={'CourtPlayer','CourtDragon','CourtFlow'},scripts.keys()
    pid,p=scripts['CourtPlayer']; did,d=scripts['CourtDragon']; fid,f=scripts['CourtFlow']
    assert f['player']['fileID']==pid and d['player']['fileID']==pid
    assert f['dragon']['fileID']==did and d['flow']['fileID']==fid
    assert docs[p['view']['fileID']][0]==20
    for key in ['body']: assert docs[p[key]['fileID']][0]==4
    for key in ['head','leftWing','rightWing']: assert docs[d[key]['fileID']][0]==4
    for key in ['sword','bell','gate']: assert docs[f[key]['fileID']][0]==4
    for g,path in assets.items():
        assert path.exists(),f'Orphan .meta: {path}'
        if path.suffix=='.mat':
            # Material asset reference is the Standard shader, not an SRP-only shader.
            raw=path.read_text();raw=re.sub(r'^%.*\n','',raw,flags=re.M)
            raw=re.sub(r'^--- !u!\d+ &\d+\n','',raw,flags=re.M)
            walk(yaml.safe_load(raw))
    settings=(ROOT/'ProjectSettings/ProjectSettings.asset').read_text()
    assert 'activeInputHandler: 0' in settings
    assert (ROOT/'ProjectSettings/ProjectVersion.txt').read_text().strip()=='m_EditorVersion: 6000.6.6f1'
    build=(ROOT/'ProjectSettings/EditorBuildSettings.asset').read_text()
    assert 'path: Assets/DragonCourt/Scenes/DragonCourt.unity' in build
    print(f'PASS scene serialization: {len(all_objects)} saved GameObjects, {refs} valid references')
    print(f'PASS all {len(layout["blocks"])} layout blocks and {len(layout["ramps"])} ramp surfaces match the saved scene')
    print('PASS player/camera/dragon/flow/interaction references, material GUIDs, build scene and legacy input settings')
    print('NOT RUN: Unity 6000.6.6f1 import, compilation, rendering, physics or playthrough')


if __name__=='__main__': validate()
