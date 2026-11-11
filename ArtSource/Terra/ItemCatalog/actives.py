"""Low-poly catalog studies: active items, pacts, pickups and selected passives."""
from math import sin, cos, pi
from mesh import Model

def poly(m, points, depth=0.1, tile=0, z=0):
    m.prism(points, depth, tile, center=(0, 0, z))

def bar(m, a, b, r=0.035, tile=6):
    m.strut(a, b, r, tile, sides=6)

def disc(m, x, y, r, tile=0, z=0, depth=0.1):
    m.lathe([(-depth / 2, r), (depth / 2, r)], tile, center=(x, y, z), sides=12, axis='z')

def cross(m, x=0, y=0, z=-0.16, s=0.18, tile=3):
    m.box((x, y, z), (s * 0.36, s, 0.035), tile, bevel=0.015)
    m.box((x, y, z), (s, s * 0.36, 0.035), tile, bevel=0.015)

def bolt(m, x, y, z=-0.18, s=1, tile=3):
    poly(m, [(x - 0.02 * s, y + 0.22 * s), (x - 0.16 * s, y - 0.02 * s), (x - 0.015 * s, y - 0.02 * s), (x - 0.07 * s, y - 0.22 * s), (x + 0.16 * s, y + 0.06 * s), (x + 0.02 * s, y + 0.06 * s)], 0.035, tile, z)

def star(m, x, y, r, tile=3, z=-0.1, count=5):
    pts = []
    for i in range(count * 2):
        a = pi / 2 + i * pi / count
        q = r if i % 2 == 0 else r * 0.42
        pts.append((x + q * cos(a), y + q * sin(a)))
    poly(m, pts, 0.04, tile, z)

def canister(m, x=0, y=0, scale=1, tile=0):
    m.lathe([(-0.35 * scale, 0.21 * scale), (-0.29 * scale, 0.25 * scale), (0.22 * scale, 0.25 * scale), (0.3 * scale, 0.18 * scale), (0.37 * scale, 0.18 * scale)], tile, center=(x, y, 0), sides=8)
    m.lathe([(0.3 * scale, 0.19 * scale), (0.37 * scale, 0.19 * scale)], 6, center=(x, y, 0), sides=8)

def PocketFan():
    m = Model('20a6b0', '74fff0')
    m.ring((0, 0.14, 0), 0.42, 0.34, 0.1, 6, 12)
    m.box((0, -0.37, 0), (0.15, 0.32, 0.13), 0, bevel=0.035)
    for i in range(3):
        a = i * 2 * pi / 3
        pts = []
        for x, y in [(0, 0.04), (0.12, 0.12), (0.19, 0.3), (0.08, 0.29), (-0.02, 0.12)]:
            pts.append((x * cos(a) - y * sin(a), 0.14 + x * sin(a) + y * cos(a)))
        poly(m, pts, 0.07, 3)
    disc(m, 0, 0.14, 0.075, 8, -0.08)
    return m

def BarrelRoll():
    m=Model('15889c','65f0e1')
    m.ring((0,0,.08),.43,.35,.12,0,16,start=40,sweep=280)
    m.ring((0,0,.07),.48,.455,.045,3,16,start=58,sweep=125)
    poly(m,[(.30,.30),(.49,.25),(.36,.09)],.13,3,.03)
    poly(m,[(0,.32),(-.13,.04),(-.28,-.17),(-.10,-.12),(0,-.29),(.10,-.12),(.28,-.17),(.13,.04)],.16,5,-.04)
    m.gem((0,.04,-.15),(.075,.18,.065),0,6)
    for x in [-.13,.13]:
        m.box((x,-.16,-.04),(.07,.11,.13),6,bevel=.02)
    return m

def BlankBullet():
    m = Model('e3b52b', 'ffe85c')
    m.lathe([(-0.39, 0.19), (-0.33, 0.21), (0.12, 0.21), (0.24, 0.16), (0.38, 0)], 0, sides=10)
    m.lathe([(-0.4, 0.23), (-0.34, 0.23)], 8, sides=10)
    m.lathe([(0.1, 0.212), (0.14, 0.212)], 5, sides=10)
    return m

def DecoyProjector():
    m=Model('273f53','50f5ed')
    m.box((0,-.33,.02),(.62,.18,.38),0,bevel=.07)
    m.box((0,-.33,-.19),(.36,.07,.025),8,bevel=.015)
    m.lathe([(-.23,.23),(-.16,.27),(-.11,.22)],8,sides=10)
    m.lathe([(-.105,.18),(-.08,.18)],7,sides=10)
    poly(m,[(0,.38),(-.24,-.04),(0,.04),(.24,-.04)],.08,3,-.025)
    poly(m,[(0,.25),(-.085,.02),(0,.065),(.085,.02)],.025,5,-.085)
    return m

def HealthPacks():
    m = Model('d9e2d5', 'f0ad1a')
    for x, y in [(-0.16, -0.04), (0.16, 0.06)]:
        m.box((x, y, 0), (0.32, 0.51, 0.2), 0, bevel=0.05)
        m.box((x, y + 0.28, 0), (0.18, 0.07, 0.16), 6, bevel=0.02)
        cross(m, x, y, -0.12, 0.2, 3)
    return m

def Overdrive():
    m = Model('40525a', 'ff642b')
    m.box((0, -0.18, 0), (0.5, 0.44, 0.28), 0, bevel=0.07)
    m.box((0, 0.13, 0), (0.12, 0.28, 0.14), 6, bevel=0.02)
    m.box((0, 0.3, 0), (0.47, 0.14, 0.2), 3, bevel=0.05)
    for x in [-0.3, 0.3]:
        poly(m, [(x, -0.34), (x * 1.5, -0.15), (x * 1.4, 0.02), (x, -0.08)], 0.12, 3)
    disc(m, 0, -0.16, 0.11, 8, -0.17)
    return m

def GravityWell():
    m=Model('314d86','8be6ff')
    m.ring((0,0,0),.28,.20,.14,0,10)
    disc(m,0,0,.17,10,.025,.08)
    m.ring((0,0,-.09),.13,.10,.04,3,10)
    for i in range(7):
        a=2*pi*i/7
        r=.29
        ux,uy=cos(a),sin(a)
        vx,vy=-uy,ux
        tip=.48 if i%2==0 else .41
        poly(m,[(r*ux-.045*vx,r*uy-.045*vy),(tip*ux+.02*vx,tip*uy+.02*vy),((r+.04)*ux+.07*vx,(r+.04)*uy+.07*vy)],.10,3,.01)
    return m

def MirrorBurst():
    m = Model('607c9f', 'b8eff9')
    m.box((0, -0.28, 0.13), (0.41, 0.12, 0.15), 4, bevel=0.025)
    for x in [-0.27, 0.27]:
        bar(m, (0, -0.23, 0.12), (x, -0.1, 0.12), 0.045, 4)
        disc(m, x, -0.1, 0.055, 6, -0.01, 0.1)
    m.gem((0, 0.03, -0.02), (0.22, 0.36, 0.18), 0, 6)
    m.gem((-0.29, 0.04, 0.015), (0.13, 0.25, 0.13), 3, 6, angle=-17)
    m.gem((0.29, 0.04, 0.015), (0.13, 0.25, 0.13), 3, 6, angle=17)
    for x in [-0.29, 0.29]:
        m.box((x, -0.13, -0.105), (0.16, 0.06, 0.08), 6, bevel=0.015)
    m.box((0, -0.24, -0.17), (0.22, 0.07, 0.07), 6, bevel=0.02)
    return m

def ThrowableShield():
    m=Model('376f84','52d4c9')
    disc(m,0,0,.384,4,.09,.12)
    m.ring((0,0,0),.39,.32,.15,6,12)
    disc(m,0,0,.31,0,0,.12)
    m.ring((0,0,-.09),.25,.22,.03,3,12)
    m.gem((0,0,-.13),(.11,.11,.055),8,8)
    for i in range(8):
        a=2*pi*i/8
        disc(m,.355*cos(a),.355*sin(a),.022,5,-.09,.025)
    return m

def FillableFlask():
    m = Model('397650', 'f3b34d')
    m.lathe([(-0.37, 0.18), (-0.31, 0.26), (0.11, 0.26), (0.24, 0.15), (0.29, 0.12), (0.36, 0.12)], 0, sides=10)
    m.lathe([(0.29, 0.15), (0.38, 0.15)], 8, sides=8)
    m.box((0, -0.08, -0.24), (0.3, 0.25, 0.04), 5, bevel=0.04)
    cross(m, 0, -0.08, -0.27, 0.18, 3)
    return m

def BulletTime():
    m = Model('665097', '80e6ec')
    m.ring((0, 0, 0), 0.36, 0.29, 0.14, 0, 12)
    disc(m, 0, 0, 0.28, 3, 0, 0.11)
    for x in [-0.06, 0.06]:
        m.box((x, 0, -0.09), (0.05, 0.27, 0.04), 8, bevel=0.01)
    m.box((0, 0.4, 0), (0.15, 0.1, 0.13), 8, bevel=0.02)
    return m

def SpellCard():
    m = Model('9d3c43', 'f2d7a5')
    m.box((0, 0, 0), (0.5, 0.72, 0.1), 0, bevel=0.05)
    m.box((0, 0, -0.065), (0.41, 0.62, 0.03), 5, bevel=0.035)
    m.ring((0, 0.1, -0.09), 0.14, 0.1, 0.03, 3, 10)
    star(m, 0, 0.1, 0.08, 8, -0.12)
    m.box((0, -0.2, -0.09), (0.17, 0.035, 0.025), 0)
    return m

def Kusabimaru():
    m = Model('82969b', 'e8d3a1')
    poly(m, [(-0.055, -0.02), (-0.06, 0.3), (0, 0.48), (0.06, 0.3), (0.055, -0.02)], 0.055, 5)
    m.box((0, -0.07, 0), (0.31, 0.055, 0.12), 8, bevel=0.025)
    m.box((0, -0.26, 0), (0.08, 0.34, 0.08), 4, bevel=0.01)
    for y in [-0.15, -0.23, -0.31]:
        m.box((0, y, -0.05), (0.085, 0.025, 0.015), 3, angle=25)
    return m

def PolarityEngine():
    m = Model('344966', 'f2e8c8')
    m.ring((0, 0, 0.03), 0.4, 0.32, 0.18, 6, 16)
    disc(m, 0, 0, 0.315, 10, 0.01, 0.12)
    pts = [(0, 0.31), (0.12, 0.285), (0.22, 0.22), (0.285, 0.12), (0.31, 0), (0.285, -0.12), (0.22, -0.22), (0.12, -0.285), (0, -0.31), (-0.1, -0.27), (-0.15, -0.19), (-0.15, -0.12), (-0.1, -0.04), (0, 0), (0.1, 0.04), (0.15, 0.12), (0.15, 0.19), (0.1, 0.27)]
    poly(m, pts, 0.035, 5, -0.075)
    disc(m, 0, 0.155, 0.045, 10, -0.105, 0.025)
    disc(m, 0, -0.155, 0.045, 5, -0.105, 0.025)
    m.ring((0, 0, -0.105), 0.365, 0.34, 0.035, 0, 16)
    for x in [-0.4, 0.4]:
        m.box((x, 0, 0.02), (0.1, 0.18, 0.19), 0, bevel=0.025)
        m.box((x, 0, -0.085), (0.055, 0.08, 0.025), 8, bevel=0.01)
    return m

def RocketBouquet():
    m=Model('bd4a40','f1c85c')
    m.paint(12,'459fb6')
    m.paint(13,'e47b35')
    m.paint(14,'9b6caa')
    m.paint(15,'e65c6c')
    for (x,y,t) in [(-.23,.03,13),(0,.12,12),(.23,.03,14),(-.12,-.06,15),(.12,-.06,3)]:
        m.lathe([(-.20,.045),(-.16,.065),(.11,.065),(.17,.052),(.205,.025),(.215,0)],t,center=(x,y,0),sides=8)
        m.lathe([(.14,.067),(.18,.055),(.215,.01)],8,center=(x,y,0),sides=8)
        poly(m,[(x-.05,y-.12),(x-.10,y-.22),(x-.035,y-.19)],.055,4)
        poly(m,[(x+.05,y-.12),(x+.10,y-.22),(x+.035,y-.19)],.055,4)
    m.box((0,-.24,-.08),(.62,.09,.18),8,bevel=.02)
    return m

def Redline():
    m=Model('9d3033','ffe08a')
    m.ring((0,.04,0),.37,.27,.17,10,16)
    m.ring((0,.04,-.09),.35,.29,.04,0,16)
    disc(m,0,.04,.275,5,0,.12)
    for i in range(9):
        a=pi*(.12+i*.095)
        bar(m,(.21*cos(a),.04+.21*sin(a),-.08),(.255*cos(a),.04+.255*sin(a),-.08),.014,8)
    bar(m,(0,.04,-.12),(.18,.20,-.12),.023,0)
    disc(m,0,.04,.045,10,-.15,.03)
    m.box((0,-.36,0),(.17,.14,.15),8)
    return m

def NailBomb():
    m = Model('66704b', 'f4b45c')
    m.box((0, 0, 0), (0.42, 0.46, 0.3), 0, bevel=0.07)
    m.box((0, 0.26, 0), (0.46, 0.08, 0.32), 10, bevel=0.025)
    bar(m, (0.02, 0.29, 0), (0.12, 0.4, 0), 0.035, 6)
    for y in [-0.13, 0.13]:
        for s in [-1, 1]:
            m.strut((s * 0.2, y, 0), (s * 0.4, y, 0), 0.025, 6, end_radius=0.008)
    disc(m, 0, 0, 0.09, 8, -0.17, 0.04)
    return m

def CanOpener():
    m = Model('d26537', 'dbe1d4')
    m.box((-0.06, -0.17, 0), (0.37, 0.42, 0.24), 0, bevel=0.06)
    m.box((0.2, -0.19, 0), (0.16, 0.11, 0.22), 10)
    m.box((0.29, -0.24, 0), (0.09, 0.22, 0.24), 6)
    bar(m, (-0.06, 0.05, 0), (-0.06, 0.42, 0), 0.055, 6)
    for y in [0.1, 0.2, 0.3]:
        m.box((-0.06, y, -0.02), (0.22, 0.035, 0.09), 3, angle=25)
    return m

def FirecrackerBelt():
    m = Model('a83e31', 'e9bb59')
    bar(m, (-0.36, -0.18, 0.08), (0, -0.29, 0.08), 0.055, 4)
    bar(m, (0, -0.29, 0.08), (0.36, -0.18, 0.08), 0.055, 4)
    for i in range(5):
        x = (i - 2) * 0.15
        y = -0.06 + abs(i - 2) * 0.07
        m.lathe([(-0.22, 0.055), (0.16, 0.055), (0.21, 0.04)], 0, center=(x, y, 0), sides=6)
        m.lathe([(0.1, 0.062), (0.14, 0.062)], 8, center=(x, y, 0), sides=6)
        m.lathe([(-0.17, 0.063), (-0.12, 0.063)], 8, center=(x, y, 0), sides=6)
        bar(m, (x, y + 0.21, 0), (x + 0.025, y + 0.28, 0), 0.012, 11)
        bar(m, (x, y - 0.15, 0.04), (x, -0.29 + abs(i - 2) * 0.055, 0.08), 0.026, 8)
    return m

def CarbonPaper():
    m = Model('6554a1', '78f0ed')
    for x, y, z in [(0.07, 0.05, 0.08), (0.035, 0.025, 0), (0, 0, -0.08)]:
        m.box((x, y, z), (0.48, 0.62, 0.045), 0, bevel=0.025)
        m.box((x - 0.15, y, z - 0.03), (0.04, 0.56, 0.018), 8)
    for y in [0.12, 0.02, -0.08]:
        m.box((0.02, y, -0.12), (0.25, 0.025, 0.012), 3)
    return m

def Thunderhead():
    m = Model('48aabc', 'ffe77a')
    m.box((0, -0.33, 0), (0.5, 0.12, 0.32), 10, bevel=0.04)
    bar(m, (0, -0.26, 0), (0, 0.22, 0), 0.09, 0)
    for y in [-0.2, -0.1, 0, 0.1]:
        m.ring((0, y, 0), 0.17, 0.12, 0.045, 8, 8, axis='y')
    m.gem((0, 0.29, 0), (0.17, 0.13, 0.14), 3, 8)
    bolt(m, 0.24, 0.25, -0.1, 0.6, 5)
    return m

def DemolitionPlunger():
    m=Model('824033','f6c765')
    m.box((0,-.18,0),(.50,.39,.30),0,bevel=.055)
    m.box((0,-.18,-.16),(.39,.27,.04),4,bevel=.025)
    m.box((0,-.18,-.19),(.29,.14,.03),8,bevel=.015)
    m.box((0,-.18,-.21),(.20,.055,.02),10)
    m.box((0,.045,0),(.55,.075,.34),8,bevel=.02)
    m.lathe([(.084,.055),(.29,.055)],6,sides=8)
    m.lathe([(.08,.085),(.12,.085)],4,sides=8)
    m.box((0,.34,0),(.40,.10,.15),10,bevel=.04)
    for x in [-.17,.17]:
        m.box((x,.34,0),(.055,.105,.16),8,bevel=.018)
    disc(m,.18,-.06,.035,6,-.165,.025)
    return m

def Bookmark():
    m=Model('286a71','e37939')
    m.box((-.02,0,.04),(.51,.64,.07),0,bevel=.025)
    m.box((.015,0,-.012),(.46,.59,.045),5,bevel=.015)
    m.box((.025,0,-.052),(.48,.64,.045),0,bevel=.025)
    m.box((.025,0,-.08),(.40,.55,.018),4,bevel=.01)
    m.box((.025,0,-.093),(.37,.52,.012),0)
    poly(m,[(.06,.36),(.18,.36),(.18,-.19),(.12,-.13),(.06,-.19)],.035,3,-.12)
    return m

def SignalFlare():
    m=Model('e4482e','ffe372')
    m.lathe([(-.36,.13),(-.30,.16),(.18,.16),(.23,.135),(.28,.135)],0,sides=8)
    m.lathe([(-.373,.17),(-.30,.17)],10,sides=8)
    m.lathe([(-.18,.166),(-.10,.166)],5,sides=8)
    m.lathe([(.23,.15),(.29,.15)],8,sides=8)
    m.lathe([(.29,.12),(.36,.12),(.40,.08)],10,sides=8)
    m.box((0,.345,-.13),(.07,.10,.08),3,bevel=.015)
    bar(m,(.12,.24,.02),(.22,.24,.02),.025,6)
    m.ring((.25,.24,.02),.09,.055,.045,3,8)
    return m

def BloodPrice():
    m = Model('9b303a', 'edc573')
    poly(m, [(-0.07, -0.08), (-0.07, 0.28), (0, 0.43), (0.07, 0.28), (0.07, -0.08)], 0.08, 5)
    m.box((0, -0.11, 0), (0.3, 0.055, 0.12), 8, bevel=0.02)
    m.box((0, -0.25, 0), (0.07, 0.24, 0.08), 0)
    m.gem((0.24, -0.29, 0), (0.09, 0.15, 0.08), 3, 7)
    return m

def CursedCoin():
    m = Model('a96b27', 'eac764')
    disc(m, 0, 0, 0.37, 0, 0, 0.12)
    m.ring((0, 0, -0.08), 0.34, 0.29, 0.03, 8, 12)
    m.gem((0, 0.04, -0.11), (0.16, 0.19, 0.04), 5, 6)
    disc(m, -0.07, 0.07, 0.035, 10, -0.16, 0.025)
    disc(m, 0.07, 0.07, 0.035, 10, -0.16, 0.025)
    m.box((0, -0.12, -0.16), (0.17, 0.055, 0.025), 5)
    return m

def DevilImp():
    m = Model('a32e31', 'f7c765')
    m.gem((0, 0, 0), (0.27, 0.25, 0.18), 0, 8)
    for s in [-1, 1]:
        poly(m, [(s * 0.18, 0.14), (s * 0.21, 0.39), (s * 0.09, 0.24)], 0.09, 8)
        poly(m, [(s * 0.22, 0.07), (s * 0.46, 0.2), (s * 0.4, -0.1), (s * 0.24, -0.06)], 0.07, 0)
        disc(m, s * 0.1, 0.02, 0.05, 3, -0.17, 0.03)
        disc(m, s * 0.1, 0.02, 0.02, 10, -0.2, 0.02)
        poly(m, [(s * 0.1, -0.14), (s * 0.14, -0.22), (s * 0.05, -0.16)], 0.035, 5, -0.16)
    return m

def GlassHull():
    m = Model('65b9cf', 'c8f4f2')
    poly(m, [(0, 0.43), (-0.18, 0.05), (-0.37, -0.22), (-0.15, -0.18), (0, -0.35), (0.15, -0.18), (0.37, -0.22), (0.18, 0.05)], 0.1, 0)
    poly(m, [(0, 0.36), (-0.1, -0.1), (0, -0.27), (0.1, -0.1)], 0.035, 9, -0.08)
    bar(m, (0, 0.29, -0.11), (-0.04, 0.01, -0.11), 0.01, 5)
    return m

def Hunger():
    m = Model('8e2942', 'e9d7a7')
    m.ring((0, 0, 0), 0.38, 0.27, 0.17, 0, 12, start=195, sweep=150)
    m.ring((0, 0, 0), 0.38, 0.27, 0.17, 0, 12, start=15, sweep=150)
    for s in [-1, 1]:
        for x in [-0.18, 0, 0.18]:
            poly(m, [(x - 0.055, s * 0.27), (x + 0.055, s * 0.27), (x, s * 0.14)], 0.07, 5, -0.1)
    return m

def MirrorOath():
    m = Model('a46b34', 'e7c480')
    m.ring((0, 0.07, 0), 0.33, 0.26, 0.12, 8, 12)
    disc(m, 0, 0.07, 0.25, 9, 0, 0.1)
    bar(m, (-0.14, 0.22, -0.08), (0.08, -0.16, -0.08), 0.012, 5)
    m.box((0, -0.34, 0), (0.08, 0.22, 0.1), 0, bevel=0.02)
    m.gem((0, -0.47, 0), (0.06, 0.06, 0.05), 3, 6)
    return m

def LiveWire():
    m = Model('a85d28', 'f0c768')
    bar(m, (-0.34, 0.26, 0), (-0.2, 0.26, 0), 0.065, 11)
    bar(m, (0.2, -0.26, 0), (0.34, -0.26, 0), 0.065, 11)
    bar(m, (-0.2, 0.26, 0), (0.12, 0.26, 0), 0.06, 0)
    bar(m, (0.12, 0.26, 0), (-0.12, -0.26, 0), 0.06, 0)
    bar(m, (-0.12, -0.26, 0), (0.2, -0.26, 0), 0.06, 0)
    bolt(m, 0, 0, -0.12, 1.1, 3)
    return m

def life():
    m = Model('b83337', 'f0ad1a')
    m.box((0, -0.02, 0), (0.62, 0.45, 0.25), 0, bevel=0.07)
    m.box((0, -0.02, -0.14), (0.51, 0.35, 0.035), 5, bevel=0.035)
    cross(m, 0, -0.02, -0.17, 0.25, 3)
    m.ring((0, 0.205, 0), 0.15, 0.1, 0.1, 10, 8, start=0, sweep=180)
    for x in [-.15,.15]:
        m.box((x,.215,0),(.10,.08,.14),6,bevel=.02)
    return m

def mana():
    m = Model('315ab5', 'f2e5a3')
    m.box((0, 0, 0), (0.4, 0.62, 0.23), 0, bevel=0.045)
    m.box((0, 0.34, 0), (0.44, 0.07, 0.25), 6, bevel=0.02)
    for x in [-0.11, 0.11]:
        m.box((x, 0.4, 0), (0.075, 0.08, 0.08), 6, bevel=0.01)
    bolt(m, 0, -0.01, -0.14, 1, 5)
    return m

def money():
    m = Model('b98024', 'f2d071')
    disc(m, 0, 0, 0.35, 0, 0, 0.13)
    m.ring((0, 0, -0.08), 0.32, 0.27, 0.03, 8, 12)
    for y in [-0.12, 0, 0.12]:
        m.box((0, y, -0.105), (0.17, 0.035, 0.02), 3)
    m.box((0, 0, -0.11), (0.035, 0.34, 0.02), 3)
    return m

def RabbitsFoot():
    m = Model('dfddd0', 'e8b5a1')
    m.lathe([(-0.3, 0.14), (-0.21, 0.2), (0.18, 0.17), (0.31, 0.11)], 0, sides=8)
    for x in [-0.13, 0, 0.13]:
        m.gem((x, -0.3, -0.04), (0.08, 0.09, 0.1), 5, 6)
    m.lathe([(0.27, 0.12), (0.35, 0.12)], 8, sides=8)
    m.ring((0, 0.4, 0), 0.08, 0.05, 0.04, 6, 8)
    return m

def EarlyRise():
    m = Model('dc8438', 'f4d36b')
    m.box((0, -0.31, 0), (0.6, 0.07, 0.15), 0, bevel=0.02)
    disc(m, 0, -0.09, 0.22, 3, 0, 0.12)
    for i in range(7):
        a = pi * i / 6
        bar(m, (0.26 * cos(a), -0.09 + 0.26 * sin(a), 0), (0.36 * cos(a), -0.09 + 0.36 * sin(a), 0), 0.022, 8)
    return m

def Magnet():
    m = Model('b63338', 'd5e2e2')
    m.ring((0, -0.03, 0), 0.3, 0.18, 0.16, 0, 12, start=180, sweep=180)
    for x in [-0.24, 0.24]:
        m.box((x, 0.12, 0), (0.12, 0.31, 0.16), 0)
        m.box((x, 0.28, 0), (0.132, 0.1, 0.17), 5)
    return m

def SparePlating():
    m = Model('44855a', 'bce4ae')
    poly(m, [(-0.3, 0.3), (0.25, 0.3), (0.31, 0.2), (0.31, -0.26), (0.23, -0.32), (-0.3, -0.32)], 0.16, 0)
    poly(m, [(-0.23, 0.22), (0.2, 0.22), (0.23, 0.17), (0.23, -0.2), (-0.23, -0.2)], 0.03, 3, -0.1)
    cross(m, 0, 0, -0.13, 0.22, 5)
    for x in [-0.24, 0.24]:
        for y in [-0.23, 0.23]:
            disc(m, x, y, 0.025, 6, -0.13, 0.02)
    return m

def Icicle():
    m = Model('61b7c8', 'd5f9fa')
    m.gem((0, 0, 0), (0.2, 0.43, 0.16), 0, 6)
    for x in [-0.2, 0.2]:
        m.gem((x, 0.05, 0.02), (0.09, 0.25, 0.09), 3, 5)
    bar(m, (-0.07, 0.24, -0.15), (-0.05, -0.14, -0.15), 0.012, 5)
    return m

def StaticSweater():
    m = Model('654b82', 'f5ce64')
    poly(m, [(-0.17, 0.3), (-0.3, 0.24), (-0.42, -0.04), (-0.29, -0.1), (-0.22, 0.02), (-0.22, -0.32), (0.22, -0.32), (0.22, 0.02), (0.29, -0.1), (0.42, -0.04), (0.3, 0.24), (0.17, 0.3)], 0.14, 0)
    m.ring((0, 0.29, -0.01), 0.13, 0.08, 0.08, 6, 8, start=180, sweep=180)
    bolt(m, 0, -0.03, -0.1, 1, 3)
    m.box((0, -0.29, -0.09), (0.4, 0.045, 0.025), 6)
    return m

def MagnifyingGlass():
    m = Model('ab7835', 'b9eef0')
    m.ring((0, 0.1, 0), 0.29, 0.23, 0.11, 8, 12)
    disc(m, 0, 0.1, 0.22, 9, 0, 0.06)
    bar(m, (0, -0.17, 0), (0, -0.45, 0), 0.055, 0)
    bar(m, (-0.12, 0.2, -0.07), (-0.04, 0.27, -0.07), 0.014, 5)
    return m

def ChewingGum():
    m = Model('bd477c', 'ffd5df')
    m.box((0, -0.07, 0), (0.56, 0.34, 0.17), 0, bevel=0.04)
    for x in [-0.25, 0.25]:
        m.box((x, -0.07, -0.02), (0.07, 0.36, 0.2), 5, bevel=0.02)
    m.box((0, -0.07, -0.1), (0.29, 0.16, 0.025), 3, bevel=0.025)
    m.gem((0.12, 0.19, -0.02), (0.15, 0.15, 0.13), 3, 8)
    return m


BUILDERS = {
    'PocketFan': PocketFan,
    'BarrelRoll': BarrelRoll,
    'BlankBullet': BlankBullet,
    'DecoyProjector': DecoyProjector,
    'HealthPacks': HealthPacks,
    'Overdrive': Overdrive,
    'GravityWell': GravityWell,
    'MirrorBurst': MirrorBurst,
    'ThrowableShield': ThrowableShield,
    'FillableFlask': FillableFlask,
    'BulletTime': BulletTime,
    'SpellCard': SpellCard,
    'Kusabimaru': Kusabimaru,
    'PolarityEngine': PolarityEngine,
    'RocketBouquet': RocketBouquet,
    'Redline': Redline,
    'NailBomb': NailBomb,
    'CanOpener': CanOpener,
    'FirecrackerBelt': FirecrackerBelt,
    'CarbonPaper': CarbonPaper,
    'Thunderhead': Thunderhead,
    'DemolitionPlunger': DemolitionPlunger,
    'Bookmark': Bookmark,
    'SignalFlare': SignalFlare,
    'BloodPrice': BloodPrice,
    'CursedCoin': CursedCoin,
    'DevilImp': DevilImp,
    'GlassHull': GlassHull,
    'Hunger': Hunger,
    'MirrorOath': MirrorOath,
    'LiveWire': LiveWire,
    'life': life,
    'mana': mana,
    'money': money,
    'RabbitsFoot': RabbitsFoot,
    'EarlyRise': EarlyRise,
    'Magnet': Magnet,
    'SparePlating': SparePlating,
    'Icicle': Icicle,
    'StaticSweater': StaticSweater,
    'MagnifyingGlass': MagnifyingGlass,
    'ChewingGum': ChewingGum,
}
