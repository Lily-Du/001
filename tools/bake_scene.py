#!/usr/bin/env python3
"""Bake the supplied whitebox into a directly inspectable Unity text scene.

Uses built-in primitive meshes and BoxColliders, so no imported model IDs or
temporary meshes need to survive between machines. Requires PyYAML only for
this offline authoring tool; Unity does not run or depend on Python.
"""
import json
import math
from pathlib import Path
import uuid
import yaml

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'Assets/DragonCourt'
BUILTIN_MESH = '0000000000000000e000000000000000'
BUILTIN_SHADER = '0000000000000000f000000000000000'


def guid(path):
    meta = Path(str(ROOT / path) + '.meta')
    if meta.exists():
        for line in meta.read_text().splitlines():
            if line.startswith('guid: '):
                return line.split()[1]
    return uuid.uuid5(uuid.NAMESPACE_URL, 'dragon-court/' + path).hex


def ref(file_id=0, asset=None, kind=3):
    result = {'fileID': file_id}
    if asset is not None:
        result.update(guid=asset, type=kind)
    return result


def xyz(values):
    return dict(zip(('x', 'y', 'z'), values))


def quat(degrees=0):
    half = math.radians(degrees) / 2
    return dict(x=math.sin(half), y=0, z=0, w=math.cos(half))


def rgba(r, g, b, a=1):
    return dict(r=r, g=g, b=b, a=a)


def header(game_object):
    return dict(m_ObjectHideFlags=0, m_CorrespondingSourceObject=ref(),
                m_PrefabInstance=ref(), m_PrefabAsset=ref(), m_GameObject=ref(game_object))


def document(class_id, file_id, name, fields):
    body = yaml.safe_dump({name: fields}, sort_keys=False, allow_unicode=True, width=140)
    return f'--- !u!{class_id} &{file_id}\n{body}'


def write_meta(path, importer='DefaultImporter', main_id=None):
    meta = Path(str(path) + '.meta')
    if meta.exists():
        return
    relative = path.relative_to(ROOT).as_posix()
    values = {'fileFormatVersion': 2, 'guid': guid(relative)}
    if path.is_dir():
        values['folderAsset'] = 'yes'
    config = {'externalObjects': {}}
    if importer == 'MonoImporter':
        config.update(serializedVersion=2, defaultReferences=[], executionOrder=0, icon=ref())
    if main_id is not None:
        config['mainObjectFileID'] = main_id
    config.update(userData='', assetBundleName='', assetBundleVariant='')
    values[importer] = config
    meta.write_text(yaml.safe_dump(values, sort_keys=False))


class Scene:
    def __init__(self):
        self.next_id = 1000
        self.nodes = []
        self.components = []

    def alloc(self):
        self.next_id += 1
        return self.next_id

    def node(self, name, parent=None, pos=(0, 0, 0), scale=(1, 1, 1), angle=0, layer=0, tag='Untagged'):
        node = dict(name=name, go=self.alloc(), tr=self.alloc(), parent=parent,
                    pos=pos, scale=scale, angle=angle, layer=layer, tag=tag, children=[], components=[])
        if parent is not None:
            parent['children'].append(node)
        self.nodes.append(node)
        return node

    def component(self, node, class_id, name, fields):
        cid = self.alloc()
        data = header(node['go'])
        data.update(fields)
        node['components'].append(cid)
        self.components.append((class_id, cid, name, data))
        return cid

    def script(self, node, name, fields):
        return self.component(node, 114, 'MonoBehaviour', dict(m_Enabled=1,
            m_EditorHideFlags=0,
            m_Script=ref(11500000, guid(f'Assets/DragonCourt/Runtime/{name}.cs')),
            m_Name='', m_EditorClassIdentifier='', **fields))

    def primitive(self, name, parent, pos, scale, material, solid=True, mesh=10202, angle=0, layer=0):
        node = self.node(name, parent, pos, scale, angle, layer)
        self.component(node, 33, 'MeshFilter', {'m_Mesh': ref(mesh, BUILTIN_MESH, 0)})
        self.component(node, 23, 'MeshRenderer', dict(m_Enabled=1, m_CastShadows=1,
            m_ReceiveShadows=1, m_DynamicOccludee=1, m_StaticShadowCaster=0,
            m_MotionVectors=1, m_LightProbeUsage=1, m_ReflectionProbeUsage=1,
            m_RayTracingMode=2, m_RayTraceProcedural=0, m_RenderingLayerMask=1,
            m_RendererPriority=0, m_Materials=[ref(2100000, material)],
            m_StaticBatchInfo=dict(firstSubMesh=0, subMeshCount=0),
            m_StaticBatchRoot=ref(), m_ProbeAnchor=ref(), m_LightProbeVolumeOverride=ref(),
            m_ScaleInLightmap=1, m_ReceiveGI=1, m_PreserveUVs=0,
            m_IgnoreNormalsForChartDetection=0, m_ImportantGI=0,
            m_StitchLightmapSeams=1, m_SelectedEditorRenderState=3,
            m_MinimumChartSize=4, m_AutoUVMaxDistance=.5, m_AutoUVMaxAngle=89,
            m_LightmapParameters=ref(), m_SortingLayerID=0, m_SortingLayer=0,
            m_SortingOrder=0, m_AdditionalVertexStreams=ref()))
        if solid:
            self.component(node, 65, 'BoxCollider', dict(m_Material=ref(),
                m_IsTrigger=0, m_Enabled=1, serializedVersion=3,
                m_Size=xyz((1, 1, 1)), m_Center=xyz((0, 0, 0))))
        return node

    def save(self, path, sun):
        text = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'
        text += document(104, 1, 'RenderSettings', dict(m_ObjectHideFlags=0,
            serializedVersion=10, m_Fog=1, m_FogColor=rgba(.24,.3,.35),
            m_FogMode=3, m_FogDensity=.0025, m_LinearFogStart=0, m_LinearFogEnd=300,
            m_AmbientSkyColor=rgba(.4,.46,.52), m_AmbientEquatorColor=rgba(.4,.46,.52),
            m_AmbientGroundColor=rgba(.4,.46,.52), m_AmbientIntensity=1,
            m_AmbientMode=3, m_SubtractiveShadowColor=rgba(.42,.47,.55),
            m_SkyboxMaterial=ref(), m_HaloStrength=.5, m_FlareStrength=1,
            m_FlareFadeSpeed=3, m_HaloTexture=ref(), m_SpotCookie=ref(),
            m_DefaultReflectionMode=0, m_DefaultReflectionResolution=128,
            m_ReflectionBounces=1, m_ReflectionIntensity=1,
            m_CustomReflection=ref(), m_Sun=ref(sun), m_UseRadianceAmbientProbe=0))
        for node in self.nodes:
            text += document(1, node['go'], 'GameObject', dict(m_ObjectHideFlags=0,
                m_CorrespondingSourceObject=ref(), m_PrefabInstance=ref(), m_PrefabAsset=ref(),
                serializedVersion=6,
                m_Component=[{'component': ref(x)} for x in [node['tr']] + node['components']],
                m_Layer=node['layer'], m_Name=node['name'], m_TagString=node['tag'],
                m_Icon=ref(), m_NavMeshLayer=0, m_StaticEditorFlags=0, m_IsActive=1))
            fields = header(node['go'])
            fields.update(serializedVersion=2, m_LocalRotation=quat(node['angle']),
                m_LocalPosition=xyz(node['pos']), m_LocalScale=xyz(node['scale']),
                m_ConstrainProportionsScale=0,
                m_Children=[ref(c['tr']) for c in node['children']],
                m_Father=ref(node['parent']['tr'] if node['parent'] else 0),
                m_LocalEulerAnglesHint=xyz((node['angle'],0,0)))
            text += document(4, node['tr'], 'Transform', fields)
        for class_id, cid, name, fields in self.components:
            text += document(class_id, cid, name, fields)
        text += document(1660057539, self.alloc(), 'SceneRoots',
            {'m_ObjectHideFlags': 0, 'm_Roots': [ref(n['tr']) for n in self.nodes if n['parent'] is None]})
        path.write_text(text)


def create_material(name, rgb):
    path = ASSETS / f'Materials/{name}.mat'
    path.parent.mkdir(parents=True, exist_ok=True)
    content = document(21, 2100000, 'Material', dict(m_ObjectHideFlags=0,
        m_CorrespondingSourceObject=ref(), m_PrefabInstance=ref(), m_PrefabAsset=ref(),
        m_Name=name, serializedVersion=8, m_Shader=ref(46, BUILTIN_SHADER, 0),
        m_Parent=ref(), m_ModifiedSerializedProperties=0,
        m_ValidKeywords=[], m_InvalidKeywords=[], m_LightmapFlags=4,
        m_EnableInstancingVariants=0, m_DoubleSidedGI=0, m_CustomRenderQueue=-1,
        stringTagMap={}, disabledShaderPasses=[], m_LockedProperties='',
        m_SavedProperties=dict(serializedVersion=3, m_TexEnvs=[], m_Ints=[],
            m_Floats=[{'_Glossiness': .15}, {'_Metallic': 0}],
            m_Colors=[{'_Color': rgba(*rgb)}]), m_BuildTextureStacks=[]))
    path.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n' + content)
    write_meta(path, 'NativeFormatImporter', 2100000)
    return guid(path.relative_to(ROOT).as_posix())


def bake():
    layout = json.loads((ASSETS / 'Resources/WhiteboxLayout.json').read_text())
    materials = {name: create_material(name, color) for name, color in {
        'Stone': (.65,.7,.73), 'Floor': (.32,.38,.4), 'Roof': (.24,.46,.57),
        'Gold': (.95,.7,.2), 'Dragon': (.17,.21,.25), 'Player': (.8,.23,.14)}.items()}
    scene = Scene()
    root = scene.node('DRAGON COURT - OPEN THIS ROOT TO INSPECT THE WHOLE LEVEL')
    geometry = scene.node('01 Geometry - 1 unit = 1 metre', root)
    for b in layout['blocks']:
        mat = materials[{'floor':'Floor','roof':'Roof','goal':'Gold'}.get(b['kind'],'Stone')]
        scene.primitive(b['name'], geometry, b['center'], b['size'], mat)
    for r in layout['ramps']:
        dy, dz = r['y1']-r['y0'], r['z1']-r['z0']
        angle = -math.atan2(dy,dz)
        # Offset the centre under the walking plane by half the slab thickness.
        center = (r['x'], (r['y0']+r['y1'])/2-math.cos(angle)*.25,
                  (r['z0']+r['z1'])/2-math.sin(angle)*.25)
        scene.primitive(r['name'], geometry, center,
            (r['width'],.5,math.hypot(dy,dz)), materials['Floor'], angle=math.degrees(angle))

    flow = scene.node('02 Level flow - objectives and checkpoints', root)
    player = scene.node('03 Player (1.8m) - WASD + right mouse', root, (0,.1,4), layer=2)
    scene.component(player,143,'CharacterController',dict(m_Material=ref(),m_IsTrigger=0,
        m_Enabled=1,serializedVersion=3,m_Height=1.8,m_Radius=.3,m_SlopeLimit=48,
        m_StepOffset=.3,m_SkinWidth=.04,m_MinMoveDistance=0,m_Center=xyz((0,.9,0))))
    body = scene.primitive('Knight scale reference', player, (0,.9,0),(.55,.9,.55),
                           materials['Player'],False,10208,layer=2)
    camera = scene.node('Main Camera',root,(0,9,-12),angle=18,tag='MainCamera')
    camera_id = scene.component(camera,20,'Camera',dict(m_Enabled=1,serializedVersion=2,
        m_ClearFlags=2,m_BackGroundColor=rgba(.24,.3,.35),m_projectionMatrixMode=1,
        m_GateFitMode=2,m_FOVAxisMode=0,m_NormalizedViewPortRect=dict(serializedVersion=2,x=0,y=0,width=1,height=1),
        **{'near clip plane':.1,'far clip plane':850,'field of view':66},
        orthographic=0,**{'orthographic size':5},m_Depth=-1,
        m_CullingMask=dict(serializedVersion=2,m_Bits=4294967295),m_RenderingPath=-1,
        m_TargetTexture=ref(),m_TargetDisplay=0,m_TargetEye=3,m_HDR=1,m_AllowMSAA=1,
        m_AllowDynamicResolution=0,m_ForceIntoRT=0,m_OcclusionCulling=1,
        m_StereoConvergence=10,m_StereoSeparation=.022))
    scene.component(camera,81,'AudioListener',{'m_Enabled':1})
    player_id = scene.script(player,'CourtPlayer',dict(view=ref(camera_id),body=ref(body['tr']),inputEnabled=1))
    dragon = scene.node('04 Dragon (42m wingspan)',root,(28,30,56))
    scene.primitive('Body',dragon,(0,0,0),(4,3.5,13),materials['Dragon'],False,10207)
    scene.primitive('Tail',dragon,(0,0,-12),(1.2,1.2,15),materials['Dragon'],False)
    scene.primitive('Neck',dragon,(0,.7,7),(2,2,5),materials['Dragon'],False)
    head = scene.primitive('Head - perception origin',dragon,(0,.5,10),(3,2.6,4),materials['Dragon'],False)
    wings=[]
    for side in [-1,1]:
        pivot=scene.node('Left wing pivot' if side<0 else 'Right wing pivot',dragon,(side*1.5,0,1))
        scene.primitive('Wing silhouette',pivot,(side*9.5,0,-1),(20,.35,7),materials['Dragon'],False)
        wings.append(pivot)
        scene.primitive('Eye',head,(side*.51,.1,.25),(.12,.13,.12),materials['Gold'],False,10207)
    interact = scene.node('05 Interactables - gold',root)
    sword=scene.primitive('Sword - E to restore pact',interact,(0,1.3,166),(.18,2.3,.35),materials['Gold'],False)
    scene.primitive('Sword crossguard',sword,(0,.55,0),(5,.12,1),materials['Gold'],False)
    bell=scene.primitive('Bell - E to lure',interact,(33,9,79),(1.4,.8,1.4),materials['Gold'],False,10206)
    gate=scene.primitive('West gate - E to open',interact,(-30,2,97),(13.8,4,.5),materials['Gold'])
    scene.primitive('Old pact mural - E to read',interact,(-36.5,1.5,76),(.15,2.5,3),materials['Gold'],False)
    scene.primitive('Sword light landmark',interact,(0,16,166),(.4,15,.4),materials['Gold'],False,10206)
    # Reserve both script IDs before filling their bidirectional references.
    dragon_id = scene.next_id+1
    flow_id = scene.next_id+2
    actual_dragon_id = scene.script(dragon,'CourtDragon',dict(player=ref(player_id),flow=ref(flow_id),
        head=ref(head['tr']),leftWing=ref(wings[0]['tr']),rightWing=ref(wings[1]['tr']),
        debugView=0,riding=0,patrolSeconds=56))
    actual_flow_id = scene.script(flow,'CourtFlow',dict(player=ref(player_id),dragon=ref(dragon_id),
        sword=ref(sword['tr']),bell=ref(bell['tr']),gate=ref(gate['tr'])))
    assert (actual_dragon_id,actual_flow_id)==(dragon_id,flow_id)
    sun=scene.node('06 Sun',root,angle=36)
    sun_id=scene.component(sun,108,'Light',dict(m_Enabled=1,serializedVersion=11,m_Type=1,
        m_Shape=0,m_Color=rgba(1,.96,.89),m_Intensity=1.2,m_Range=10,m_SpotAngle=30,
        m_InnerSpotAngle=21.8,m_CookieSize=10,
        m_Shadows=dict(m_Type=2,m_Resolution=-1,m_CustomResolution=-1,m_Strength=1,
            m_Bias=.05,m_NormalBias=.4,m_NearPlane=.2),
        m_Cookie=ref(),m_DrawHalo=0,m_Flare=ref(),m_RenderMode=0,
        m_CullingMask=dict(serializedVersion=2,m_Bits=4294967295),m_RenderingLayerMask=1,
        m_Lightmapping=4,m_LightShadowCasterMode=0,m_AreaSize=dict(x=1,y=1),
        m_BounceIntensity=1,m_ColorTemperature=6570,m_UseColorTemperature=0,
        m_BoundingSphereOverride=dict(x=0,y=0,z=0,w=0),m_UseBoundingSphereOverride=0,
        m_UseViewFrustumForShadowCasterCull=1,m_ShadowRadius=0,m_ShadowAngle=0))
    scene_path=ASSETS/'Scenes/DragonCourt.unity'
    scene.save(scene_path,sun_id)
    for path in sorted((ROOT/'Assets').rglob('*')):
        if path.suffix=='.meta': continue
        importer='MonoImporter' if path.suffix=='.cs' else 'NativeFormatImporter' if path.suffix=='.mat' else 'DefaultImporter'
        write_meta(path,importer,2100000 if path.suffix=='.mat' else None)
    print(f'Baked {len(scene.nodes)} GameObjects and {len(scene.components)} components into {scene_path.relative_to(ROOT)}')


if __name__=='__main__':
    bake()
