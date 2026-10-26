#!/usr/bin/env python3
"""Engine-independent checks against the exact JSON consumed by Unity.

These test geometry and explicit routes, not Unity compilation or gameplay.
"""
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LAYOUT = ROOT / 'Assets/DragonCourt/Resources/WhiteboxLayout.json'
DATA = json.loads(LAYOUT.read_text())
BLOCKS, RAMPS = DATA['blocks'], DATA['ramps']
ROUTES = {
    'west_chapel_to_sword': [(0,4),(0,27),(-15,28),(-30,45),(-30,65),(-30,95),(-30,104),(-30,131),(-20,133),(0,133),(0,166)],
    'east_tower_to_sword': [(0,4),(0,27),(14,28),(30,40),(30,62),(30,79),(30,92),(30,104),(30,131),(20,133),(0,133),(0,166)],
    'sunken_garden': [(0,102),(0,112),(0,122),(0,133),(0,166)],
}


def bounds(b):
    return [(c-s/2, c+s/2) for c, s in zip(b['center'], b['size'])]


def floor_at(x, z):
    heights = []
    for b in BLOCKS:
        a = bounds(b)
        if b['kind'] == 'floor' and a[0][0]-1e-6 <= x <= a[0][1]+1e-6 and a[2][0]-1e-6 <= z <= a[2][1]+1e-6:
            heights.append(a[1][1])
    for r in RAMPS:
        if abs(x-r['x']) <= r['width']/2 and r['z0']-1e-6 <= z <= r['z1']+1e-6:
            heights.append(r['y0']+(r['y1']-r['y0'])*(z-r['z0'])/(r['z1']-r['z0']))
    assert heights, f'No ground at {(x,z)}'
    return max(heights)


def validate():
    for b in BLOCKS:
        assert len(b['center']) == len(b['size']) == 3
        assert all(math.isfinite(v) for v in b['center']+b['size'])
        assert all(v > 0 for v in b['size'])
    for r in RAMPS:
        assert r['z1'] > r['z0'] and r['width'] > .6
        angle = math.degrees(math.atan2(abs(r['y1']-r['y0']), r['z1']-r['z0']))
        assert angle < 48, (r['name'], angle)
    for name, points in ROUTES.items():
        previous = None
        samples = 0
        for a,b in zip(points,points[1:]):
            distance = math.dist(a,b)
            n = max(1,math.ceil(distance/.2))
            for i in range(n+1):
                x = a[0]+(b[0]-a[0])*i/n
                z = a[1]+(b[1]-a[1])*i/n
                y = floor_at(x,z)
                if previous is not None:
                    assert abs(y-previous) < .3, f'{name}: discontinuity at {(x,y,z)}'
                previous = y
                for block in BLOCKS:
                    k = bounds(block)
                    intersects = (k[0][0]-.3 < x < k[0][1]+.3 and
                                  k[2][0]-.3 < z < k[2][1]+.3 and
                                  k[1][0] < y+1.8 and k[1][1] > y+.3)
                    assert not intersects, f'{name}: body blocked by {block["name"]} at {(x,y,z)}'
                samples += 1
        print(f'PASS {name}: {samples} supported, unobstructed standing-height samples')
    for name,x,z in [('start',0,4),('outer checkpoint',0,20),('west checkpoint',-30,103),('east checkpoint',30,103),('final checkpoint',0,138),('sword',0,166)]:
        assert abs(floor_at(x,z)) < .01, name
    # Safety is based on physical roof coverage, not floor colour.
    for x,z in [(0,20),(-30,103),(30,103),(0,138)]:
        assert any(b['kind']=='roof' and bounds(b)[0][0] <= x <= bounds(b)[0][1]
                   and bounds(b)[2][0] <= z <= bounds(b)[2][1] for b in BLOCKS), f'Unroofed checkpoint {(x,z)}'
    print(f'PASS {len(BLOCKS)} boxes, {len(RAMPS)} traversable ramps; goal + 4 checkpoint floors and roofs')


def draw_map():
    from PIL import Image, ImageDraw, ImageFont
    image = Image.new('RGB',(1500,1600),'#14212c')
    d = ImageDraw.Draw(image)
    font_path='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
    font=ImageFont.truetype(font_path,18)
    small=ImageFont.truetype(font_path,15)
    title=ImageFont.truetype(font_path,32)
    d.text((35,28),'DRAGON COURT | UNITY WHITEBOX',font=title,fill='#f6d080')
    d.text((35,76),'Actual layout data | 1 unit = 1 metre | North is up',font=font,fill='white')
    def p(x,z): return (410+x*6.2,1450-z*7.1)
    for z in range(0,181,20):
        d.line([p(-43,z),p(43,z)],fill='#263b49')
        d.text((20,p(0,z)[1]-8),f'{z}m',font=small,fill='#92a9b9')
    for kind in ['floor','cover','roof']:
        for b in BLOCKS:
            if b['kind'] != kind or b['name'].startswith('Distant'): continue
            x,y,z=b['center'];w,h,depth=b['size']
            fill=('#51636c' if y<1 else '#967b50') if kind=='floor' else '#aeb7bb' if kind=='cover' else '#396d87'
            d.rectangle([p(x-w/2,z+depth/2),p(x+w/2,z-depth/2)],fill=fill,outline='#172831',width=1)
    for r in RAMPS:
        d.rectangle([p(r['x']-r['width']/2,r['z1']),p(r['x']+r['width']/2,r['z0'])],fill='#ba9252',outline='#f2c16e',width=2)
        d.line([p(r['x'],r['z0']),p(r['x'],r['z1'])],fill='#ffdf9b',width=2)
    colors=['#88e1ad','#f4b770','#82d5f2']
    for (name,route),color in zip(ROUTES.items(),colors):
        d.line([p(*q) for q in route],fill=color,width=3)
    for name,x,z in [('START',0,4),('SWORD',0,166),('BELL',33,79),('GATE',-30,95)]:
        px,py=p(x,z);d.ellipse((px-6,py-6,px+6,py+6),fill='#ffdf85')
        d.text((px+9,py-18),name,font=small,fill='#ffdf85')
    for label,z in [('F  Sanctuary',167),('E  Broken gallery',146),('D  Royal court',118),('C  Route choice',77),('B  Outer court',36),('A  Pilgrim path',12)]:
        d.text((720,p(0,z)[1]-12),label,font=font,fill='white')
    legend=[('Blue blocks','Solid roof / overhead cover'),('Light grey','Wall / pillar / visual occluder'),('Dark grey','Ground-level walkable surface'),('Ochre','Raised walk / sloped connector'),('Green line','West chapel route'),('Orange line','East tower route'),('Cyan line','Sunken garden alternative')]
    for i,(a,b) in enumerate(legend):
        y=280+i*56
        d.text((1050,y),a,font=font,fill='#f6d080')
        d.text((1050,y+25),b,font=small,fill='#c2ced6')
    d.text((1050,760),'ELEVATION',font=font,fill='#f6d080')
    for i,t in enumerate(['Tower: +8m','Court: 0m','Garden: -3m','Dragon: ~26-32m','Wingspan: ~42m']):
        d.text((1050,795+i*30),t,font=font,fill='white')
    d.text((1050,1040),'PLAYTEST FOCUS',font=font,fill='#f6d080')
    for i,t in enumerate(['Read the next safe space.','Choose risk or shelter.','Track the patrol overhead.','Reuse familiar cover rules.','Fly above your old route.']):
        d.text((1050,1075+i*32),t,font=small,fill='white')
    d.text((45,1535),'Map only. Unity Editor playtesting and compilation are still required.',font=font,fill='#c2ced6')
    output=ROOT/'Docs/WhiteboxMap.png'
    image.save(output)
    print('Wrote',output.relative_to(ROOT))


if __name__ == '__main__':
    import sys
    validate()
    if '--map' in sys.argv: draw_map()
