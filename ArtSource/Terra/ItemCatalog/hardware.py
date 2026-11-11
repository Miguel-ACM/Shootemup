from mesh import Model
import math

def model(p='596e75', a='efaa42'):
    return Model(p, a)

def box(m, c, s, t=0, b=0.025, a=0):
    m.box(c, s, t, bevel=b, angle=a)

def tube(m, x, y, r=0.1, h=0.4, t=6):
    m.lathe([(-h / 2, r * 0.82), (-h / 2 + 0.04, r), (h / 2 - 0.04, r), (h / 2, r * 0.82)], t, center=(x, y, 0), sides=8)
    m.ring((x, y + h / 2 + 0.006, -0.002), r * 0.84, r * 0.51, 0.025, 4, sides=8, axis='y')

def gunbody(m):
    box(m, (0, -0.2, 0), (0.68, 0.35, 0.34), 0, 0.06)
    box(m, (0, -0.2, -0.19), (0.25, 0.17, 0.04), 4)
    box(m, (0, -0.2, -0.22), (0.14, 0.09, 0.03), 7)

def BaseWeaponItem():
    m = model('53686d', 'e99935')
    box(m, (0, -0.22, 0.02), (0.65, 0.36, 0.33), 0, 0.055)
    box(m, (0, -0.22, -0.17), (0.51, 0.29, 0.035), 2, 0.035)
    box(m, (0, -0.2, -0.195), (0.36, 0.21, 0.025), 0, 0.025)
    box(m, (0, -0.2, -0.214), (0.105, 0.14, 0.016), 7, 0.008)
    box(m, (0, -0.405, -0.08), (0.27, 0.075, 0.22), 6, 0.015)
    for x in [-0.22, 0.22]:
        tube(m, x, 0.15, 0.074, 0.46, 4)
        for y in [-0.02, 0.15, 0.3]:
            m.lathe([(-0.035, 0.085), (0.035, 0.085)], 6, center=(x, y, 0), sides=8)
        m.lathe([(-0.055, 0.095), (0.055, 0.095)], 6, center=(x, 0.38, 0), sides=8)
        m.ring((x, 0.44, 0), 0.101, 0.052, 0.025, 4, sides=8, axis='y')
        box(m, (x * 1.75, -0.22, 0), (0.16, 0.3, 0.27), 3, 0.025)
        box(m, (x * 1.75, -0.22, -0.15), (0.1, 0.19, 0.025), 0, 0.01)
    return m

def ScatterCannonItem():
    m = model('4f6064', 'e8a63a')
    m.lathe([(-0.31, 0.19), (-0.25, 0.25), (0.02, 0.25), (0.08, 0.19)], 0, axis='z')
    m.ring((0, -0.1, -0.28), 0.22, 0.12, 0.04, 8)
    for i in range(-2, 3):
        x = i * 0.115
        m.strut((x * 0.5, 0.08, 0), (x, 0.49, 0), 0.064, 8)
        m.ring((x, 0.49, 0), 0.065, 0.039, 0.025, 4, axis='y', sides=8)
    return m

def WindUpCannonItem():
    m = model('88499e', 'e4a942')
    box(m, (0, -0.27, 0), (0.48, 0.35, 0.4), 0, 0.06)
    box(m, (0, -0.27, -0.22), (0.32, 0.22, 0.03), 1)
    for y in [-0.05, 0.08, 0.21]:
        m.ring((0, y, 0), 0.22, 0.145, 0.045, 8, axis='y')
    tube(m, 0, 0.39, 0.22, 0.22, 0)
    m.strut((-0.24, -0.25, 0), (-0.43, -0.25, 0), 0.04, 8)
    m.ring((-0.46, -0.25, 0), 0.09, 0.035, 0.04, 8)
    return m

def BeamEmitterItem():
    m = model('258993', '8ce9e6')
    gunbody(m)
    for x in [-0.27, 0.27]:
        m.prism([(-0.1, -0.18), (0.1, -0.18), (0.08, 0.42), (-0.07, 0.5)], 0.22, 0, center=(x, 0, 0))
        box(m, (x, 0.18, -0.13), (0.075, 0.38, 0.03), 7)
    tube(m, 0, 0.12, 0.075, 0.5, 7)
    m.ring((0, -0.19, -0.25), 0.13, 0.07, 0.04, 6)
    return m

def FlameProjectorItem():
    m = model('b33b19', 'f5a73c')
    tube(m, -0.21, -0.06, 0.18, 0.65, 0)
    box(m, (0.15, -0.35, 0), (0.55, 0.12, 0.25), 6)
    m.lathe([(-0.3, 0.07), (0.05, 0.07), (0.12, 0.15), (0.39, 0.23), (0.44, 0.23)], 3, center=(0.23, 0, 0))
    m.ring((0.23, 0.45, 0), 0.22, 0.15, 0.035, 4, axis='y')
    box(m, (-0.21, -0.1, -0.19), (0.1, 0.3, 0.03), 8)
    return m

def BackdraftItem():
    m = model('ad4d21', 'f6aa45')
    box(m, (0, -0.28, 0), (0.75, 0.17, 0.3), 0, 0.04)
    for x in [-0.23, 0.23]:
        tube(m, x, 0.13, 0.14, 0.57, 0)
        m.ring((x, 0.08, 0), 0.15, 0.12, 0.04, 8, axis='y')
        m.ring((x, 0.3, 0), 0.15, 0.12, 0.04, 8, axis='y')
    m.prism([(-0.38, -0.38), (0.38, -0.38), (0.28, -0.49), (-0.28, -0.49)], 0.3, 3)
    return m

def DrillItem():
    m = model('718888', 'eeb04d')
    tube(m, 0, -0.28, 0.18, 0.28, 0)
    m.lathe([(-0.15, 0.27), (-0.08, 0.28), (0.03, 0.24), (0.16, 0.18), (0.29, 0.11), (0.48, 0)], 6, sides=10)
    for i in range(10):
        a = i * 0.6
        b = (i + 1) * 0.6
        y = -0.1 + i * 0.055
        r = 0.26 - i * 0.023
        rr = r - 0.023
        m.strut((r * math.cos(a), y, r * math.sin(a)), (rr * math.cos(b), y + 0.055, rr * math.sin(b)), 0.025, 3)
    return m

def DepthChargeItem():
    m = model('476783', 'edaa35')
    for x in [-0.32, 0.32]:
        box(m, (x, 0, 0), (0.1, 0.9, 0.2), 0)
        box(m, (x, -0.45, 0), (0.22, 0.09, 0.3), 6)
    box(m, (0, 0.42, 0), (0.76, 0.1, 0.25), 6)
    for y in [-0.25, 0, 0.25]:
        tube(m, 0, y, 0.19, 0.19, 0)
        m.ring((0, y + 0.09, 0), 0.195, 0.15, 0.025, 8, axis='y')
    return m

def SwarmRackItem():
    m = model('60902d', 'ef8333')
    box(m, (0, 0, 0), (0.67, 0.72, 0.35), 0, 0.065)
    for x in [-0.19, 0.19]:
        for y in [-0.23, 0, 0.23]:
            m.ring((x, y, -0.2), 0.105, 0.075, 0.045, 6, sides=6)
            m.gem((x, y, -0.24), (0.06, 0.06, 0.045), 3, sides=6)
    return m

def DiscThrowerItem():
    m = model('3ca6a0', 'e4aa50')
    box(m, (0, -0.36, 0), (0.58, 0.17, 0.23), 0)
    for i in range(10):
        a = 2 * math.pi * i / 10
        m.prism([(0.23 * math.cos(a - 0.25), 0.23 * math.sin(a - 0.25)), (0.42 * math.cos(a), 0.42 * math.sin(a)), (0.23 * math.cos(a + 0.25), 0.23 * math.sin(a + 0.25))], 0.1, 1)
    m.ring((0, 0, -0.08), 0.24, 0.13, 0.08, 0)
    m.ring((0, 0, -0.14), 0.12, 0.06, 0.035, 8)
    return m

def BurstLauncherItem():
    m = model('b55224', 'ed9d3a')
    box(m, (0, -0.28, 0), (0.51, 0.3, 0.38), 0, 0.06)
    m.lathe([(-0.08, 0.12), (0.05, 0.15), (0.38, 0.25), (0.46, 0.25)], 3)
    m.ring((0, 0.46, 0), 0.25, 0.17, 0.05, 4, axis='y')
    m.ring((0, -0.24, -0.22), 0.21, 0.11, 0.05, 6)
    return m

def NeedleCannonItem():
    m = model('70448c', '82dbe1')
    box(m, (0, -0.39, 0), (0.58, 0.09, 0.27), 0, 0.02)
    m.lathe([(-0.31, 0.11), (-0.26, 0.18), (0.04, 0.18), (0.13, 0.13)], 0, sides=8)
    m.lathe([(-0.03, 0.11), (0.03, 0.17)], 6, center=(0, 0.16, 0), sides=8)
    m.lathe([(-0.04, 0.09), (0.03, 0.1)], 4, center=(0, 0.23, 0), sides=8)
    m.lathe([(0.25, 0.045), (0.34, 0.04), (0.53, 0)], 6, sides=8)
    for x in [-0.16, 0.16]:
        box(m, (x, -0.13, -0.08), (0.055, 0.38, 0.08), 6, 0.01)
        box(m, (x, -0.13, -0.13), (0.022, 0.29, 0.018), 7, 0.005)
    for y in [-0.28, 0.01]:
        m.ring((0, y, 0), 0.19, 0.155, 0.035, 3, sides=8, axis='y')
    box(m, (0, -0.39, -0.16), (0.19, 0.055, 0.035), 3, 0.008)
    return m

def RailDriverItem():
    m = model('58508e', '86d7eb')
    box(m, (0, -0.39, 0), (0.58, 0.17, 0.35), 0)
    for x in [-0.18, 0.18]:
        box(m, (x, 0.04, 0), (0.13, 0.8, 0.2), 6)
        box(m, (x, 0.055, -0.12), (0.045, 0.65, 0.035), 7)
    for y in [-0.2, 0.1, 0.35]:
        box(m, (0, y, -0.16), (0.55, 0.07, 0.12), 1)
    return m

def ElectricityItem():
    m = model('e5b938', 'fff29d')
    m.prism([(0.05, 0.49), (-0.28, -0.02), (-0.04, -0.02), (-0.17, -0.49), (0.3, 0.1), (0.07, 0.1)], 0.18, 3, side=8)
    return m

def drone(laser=False):
    if laser:
        m = model('69598e', '8ce7ee')
        m.prism([(0, 0.46), (-0.18, 0.29), (-0.22, -0.18), (0, -0.37), (0.22, -0.18), (0.18, 0.29)], 0.27, 0)
        for side in [-1, 1]:
            m.prism([(side * 0.17, 0.17), (side * 0.43, -0.04), (side * 0.4, -0.3), (side * 0.18, -0.23)], 0.16, 2)
            m.gem((side * 0.32, -0.2, -0.11), (0.075, 0.075, 0.045), 7, sides=8)
        box(m, (0, -0.07, -0.16), (0.15, 0.34, 0.03), 6, 0.015)
        box(m, (0, -0.07, -0.183), (0.065, 0.23, 0.018), 7, 0.005)
        m.ring((0, 0.21, -0.17), 0.12, 0.075, 0.035, 6, sides=8)
        m.gem((0, 0.21, -0.2), (0.065, 0.065, 0.025), 7, sides=8)
        tube(m, 0, 0.39, 0.085, 0.17, 6)
    else:
        m = model('466c71', 'e5b152')
        m.prism([(0, 0.38), (-0.22, 0.22), (-0.26, -0.25), (0, -0.39), (0.26, -0.25), (0.22, 0.22)], 0.28, 0)
        for side in [-1, 1]:
            m.prism([(side * 0.19, -0.02), (side * 0.45, -0.18), (side * 0.42, -0.37), (side * 0.18, -0.31)], 0.18, 2)
            tube(m, side * 0.18, 0.31, 0.055, 0.28, 8)
            box(m, (side * 0.27, -0.22, -0.13), (0.15, 0.12, 0.08), 6, 0.02)
        m.ring((0, -0.09, -0.17), 0.14, 0.09, 0.04, 4, sides=8)
        m.gem((0, -0.09, -0.2), (0.075, 0.075, 0.025), 7, sides=8)
        box(m, (0, 0.18, -0.16), (0.13, 0.13, 0.025), 1, 0.015)
    return m

def BulletDroneItem():
    return drone()

def LaserDroneItem():
    return drone(True)

def TwerpItem():
    m = model('777e85', '55d7eb')
    box(m, (0, 0.19, 0), (0.49, 0.38, 0.32), 6, 0.06)
    box(m, (0, -0.2, 0), (0.42, 0.33, 0.27), 0, 0.04)
    for x in [-0.13, 0.13]:
        box(m, (x, 0.22, -0.18), (0.09, 0.065, 0.04), 7)
        box(m, (x, -0.44, 0), (0.1, 0.18, 0.14), 4)
    box(m, (0, -0.17, -0.16), (0.25, 0.15, 0.02), 4)
    for y in [-0.2, -0.16, -0.12]:
        box(m, (0, y, -0.18), (0.16, 0.015, 0.02), 7, 0)
    m.strut((0, 0.39, 0), (0, 0.5, 0), 0.02, 6)
    m.gem((0, 0.5, 0), (0.045, 0.045, 0.045), 7)
    return m

def moon(angry=False, three=False):
    m = model('9da3a9', 'dce1dd')
    positions = [(-0.22, 0.17, 0), (0.22, 0.14, 0.035), (0, -0.24, -0.03)] if three else [(0, 0, 0)]
    for x, y, z in positions:
        r = 0.245 if three else 0.42
        m.lathe([(-r * 0.72, 0.03), (-r * 0.63, r * 0.58), (-r * 0.32, r * 0.9), (0, r), (r * 0.32, r * 0.9), (r * 0.63, r * 0.58), (r * 0.72, 0.03)], 0, center=(x, y, z), sides=12, axis='z')
        for dx, dy, rr in [(-0.35, 0.24, 0.21), (0.34, 0.19, 0.14), (0.12, -0.35, 0.2)]:
            cx = x + dx * r
            cy = y + dy * r
            cz = z - r * 0.71
            m.lathe([(-0.012, rr * r * 0.7), (0.012, rr * r * 0.7)], 2, center=(cx, cy, cz - 0.015), sides=8, axis='z')
            m.ring((cx, cy, cz - 0.03), rr * r, rr * r * 0.65, 0.022, 1, sides=8)
        if angry:
            for side in [-1, 1]:
                box(m, (x + side * 0.16 * r / 0.42, y + 0.035, z - r * 0.79), (0.17 * r / 0.42, 0.045, 0.035), 4, 0.008, -side * 20)
                m.gem((x + side * 0.145 * r / 0.42, y - 0.025, z - r * 0.84), (0.045, 0.035, 0.02), 3, sides=6)
            box(m, (x, y - 0.19, z - r * 0.75), (0.22, 0.045, 0.035), 4, 0.005)
    return m

def AMoonItem():
    return moon()

def AngryMoonItem():
    return moon(True)

def ThreeMoonsItem():
    return moon(three=True)

def BabyYouItem():
    m = model('333a49', 'f4bd36')
    m.prism([(0, 0.43), (-0.22, 0.25), (-0.23, -0.25), (0, -0.43), (0.23, -0.25), (0.22, 0.25)], 0.2, 0)
    for s in [-1, 1]:
        m.prism([(s * 0.2, 0.25), (s * 0.41, 0.43), (s * 0.41, -0.36), (s * 0.25, -0.3)], 0.18, 4)
        box(m, (s * 0.31, 0.17, -0.12), (0.07, 0.12, 0.03), 3)
    m.gem((0, 0.02, -0.16), (0.15, 0.19, 0.07), 3)
    return m

def BallOfYarn():
    m = model('b73e6f', 'f18bab')
    m.gem((0, 0, 0), (0.34, 0.34, 0.28), 0, sides=10)
    for angle in [-50, 0, 50]:
        m.ring((0, 0, -0.12), 0.35, 0.31, 0.04, 1, 12, axis='z', start=angle, sweep=320)
    for s in [-1, 1]:
        m.strut((s * 0.16, -0.26, -0.1), (-s * 0.25, 0.42, -0.1), 0.018, 8)
        m.gem((-s * 0.25, 0.42, -0.1), (0.04, 0.04, 0.04), 3)
    return m

def PaperPlane():
    m = model('ccd7d1', '5da6de')
    m.prism([(0, 0.49), (-0.44, -0.34), (0, -0.22), (0.44, -0.34)], 0.04, 5)
    m.prism([(0, 0.49), (-0.08, -0.26), (0, -0.22)], 0.07, 6)
    box(m, (0, -0.25, -0.06), (0.05, 0.16, 0.03), 3)
    return m

def Tripod():
    m = model('708447', 'c4d58c')
    box(m, (0, 0.06, 0), (0.44, 0.28, 0.3), 0, 0.05)
    tube(m, 0, 0.34, 0.095, 0.3, 6)
    for x, z in [(-0.33, 0), (0.33, 0), (0, 0.24)]:
        m.strut((0, -0.06, 0), (x, -0.42, z), 0.045, 0)
        box(m, (x, -0.43, z), (0.16, 0.06, 0.13), 4)
    box(m, (0, 0.06, -0.17), (0.19, 0.08, 0.03), 3)
    return m

def AK45Item():
    m = model('414b4a', 'ad693f')
    box(m, (-0.02, 0, 0), (0.58, 0.16, 0.16), 4)
    box(m, (-0.35, 0, 0), (0.28, 0.14, 0.14), 3)
    box(m, (0.35, 0.03, 0), (0.28, 0.06, 0.08), 6)
    box(m, (0.04, -0.16, 0), (0.11, 0.23, 0.12), 0, 0.02, 12)
    box(m, (-0.2, -0.12, 0), (0.06, 0.16, 0.08), 3)
    return m

def PaperclipBox():
    m = model('687bbb', 'b4c8ed')
    box(m, (0, -0.22, 0), (0.7, 0.38, 0.36), 0, 0.035)
    box(m, (0, -0.22, -0.2), (0.59, 0.28, 0.025), 1)
    for x in [-0.24, -0.08, 0.08, 0.24]:
        for dx in [-0.045, 0.045]:
            m.strut((x + dx, -0.04, 0), (x + dx, 0.38, 0), 0.018, 6)
        m.strut((x - 0.045, 0.38, 0), (x + 0.045, 0.38, 0), 0.018, 6)
    return m

def FireflyJar():
    m = model('77a990', 'ffcc4a')
    m.lathe([(-0.42, 0.19), (-0.38, 0.24), (-0.34, 0.24)], 0, sides=8)
    m.ring((0, -0.32, 0), 0.24, 0.2, 0.04, 6, axis='y', sides=8)
    m.ring((0, 0.27, 0), 0.23, 0.19, 0.045, 6, axis='y', sides=8)
    m.lathe([(0.28, 0.23), (0.33, 0.18), (0.4, 0.18)], 0, sides=8)
    for i in range(8):
        a = math.pi * 2 * i / 8
        x = 0.215 * math.cos(a)
        z = 0.215 * math.sin(a)
        m.strut((x, -0.32, z), (x, 0.28, z), 0.015, 6, sides=6)
    for x, y, z in [(-0.08, 0.12, -0.07), (0.1, -0.07, 0.01), (-0.05, -0.23, 0.03)]:
        m.gem((x, y, z), (0.047, 0.067, 0.04), 3, sides=7)
        m.gem((x - 0.047, y + 0.025, z), (0.045, 0.026, 0.018), 5, sides=6)
        m.gem((x + 0.047, y + 0.025, z), (0.045, 0.026, 0.018), 5, sides=6)
    return m

def SaltShaker():
    m = model('a6c3c9', 'e1e8df')
    m.lathe([(-0.4, 0.25), (-0.35, 0.28), (0.14, 0.28), (0.28, 0.2), (0.32, 0.19)], 5, sides=6)
    tube(m, 0, 0.37, 0.2, 0.12, 6)
    for x in [-0.1, 0, 0.1]:
        m.ring((x, 0.44, 0), 0.025, 0.012, 0.01, 4, axis='y', sides=6)
    box(m, (0, -0.18, -0.26), (0.3, 0.21, 0.02), 1)
    return m

def PocketMortar():
    m = model('728749', 'c9d998')
    box(m, (0, -0.42, 0), (0.72, 0.11, 0.42), 0)
    m.strut((0, -0.3, 0), (0.15, 0.31, 0), 0.16, 0)
    m.ring((0.15, 0.31, 0), 0.17, 0.105, 0.05, 6, axis='y')
    for s in [-1, 1]:
        m.strut((s * 0.25, -0.35, 0), (0.08, 0.08, 0), 0.035, 6)
    return m

def WindUpTeeth():
    m = model('9b3440', 'f1d7c4')
    m.prism([(-0.34, 0.05), (-0.3, 0.29), (0, 0.38), (0.3, 0.29), (0.34, 0.05)], 0.3, 0)
    m.prism([(-0.34, -0.08), (-0.29, -0.28), (0, -0.37), (0.29, -0.28), (0.34, -0.08)], 0.3, 0)
    for x in [-0.24, -0.12, 0, 0.12, 0.24]:
        for y in [-0.08, 0.08]:
            box(m, (x, y, -0.17), (0.105, 0.12, 0.07), 5, 0.015)
    m.strut((0.32, 0, 0), (0.45, 0, 0), 0.025, 8)
    m.ring((0.48, 0, 0), 0.09, 0.05, 0.035, 8, sides=8)
    return m

def RibbonSpool():
    m = model('5995a2', 'e8843e')
    tube(m, 0, 0, 0.18, 0.7, 0)
    tube(m, 0, 0.36, 0.27, 0.08, 6)
    tube(m, 0, -0.36, 0.27, 0.08, 6)
    for y in [-0.15, -0.07, 0.01, 0.09, 0.17]:
        m.ring((0, y, 0), 0.2, 0.17, 0.05, 3, axis='y')
    m.prism([(0.16, -0.08), (0.42, -0.27), (0.38, -0.33), (0.12, -0.15)], 0.035, 3, center=(0, 0, -0.12))
    return m

def SewingKit():
    m = model('894d84', 'e9bfcf')
    box(m, (0, -0.28, 0), (0.75, 0.26, 0.4), 0, 0.03)
    box(m, (0, 0.1, 0.15), (0.75, 0.51, 0.08), 0, 0.03)
    box(m, (0, 0.1, 0.1), (0.62, 0.38, 0.025), 1)
    tube(m, -0.19, -0.08, 0.095, 0.22, 3)
    for x in [0.04, 0.18, 0.29]:
        m.strut((x, -0.18, -0.1), (x + 0.04, 0.12, -0.1), 0.012, 6)
    return m

def Popcorn():
    m = model('d54e42', 'f7e6b9')
    m.prism([(-0.3, 0.13), (0.3, 0.13), (0.22, -0.42), (-0.22, -0.42)], 0.3, 0)
    for x in [-0.18, 0, 0.18]:
        box(m, (x, -0.15, -0.17), (0.075, 0.42, 0.02), 5, 0)
    for x, y in [(-0.21, 0.2), (-0.07, 0.26), (0.09, 0.22), (0.22, 0.21), (0, 0.36)]:
        m.gem((x, y, 0), (0.14, 0.12, 0.12), 5, sides=7)
    return m

def RubberBand():
    m = model('c79e61', 'f3d397')
    m.ring((0, 0, 0), 0.37, 0.27, 0.11, 0, sides=16)
    m.ring((0.05, 0.02, -0.06), 0.31, 0.27, 0.035, 3, sides=16)
    return m

def HotSauce():
    m = model('b33325', 'edbc5a')
    m.lathe([(-0.43, 0.2), (-0.38, 0.23), (0.14, 0.23), (0.28, 0.1), (0.38, 0.1)], 0, sides=8)
    tube(m, 0, 0.4, 0.12, 0.13, 8)
    box(m, (0, -0.12, -0.22), (0.29, 0.29, 0.03), 5)
    m.prism([(0, 0.03), (-0.07, -0.1), (-0.05, -0.2), (0, -0.24), (0.07, -0.17), (0.04, -0.08)], 0.02, 3, center=(0, 0, -0.25))
    return m

def FocusTime():
    m = model('718a91', '62d6e3')
    m.lathe([(-0.22, 0.15), (-0.15, 0.28), (0.14, 0.28), (0.22, 0.15)], 0, axis='z')
    m.ring((0, 0, -0.24), 0.25, 0.18, 0.04, 6)
    for x in [-0.09, 0.09]:
        box(m, (x, 0, -0.27), (0.065, 0.24, 0.025), 7)
    return m

def BadGlasses():
    m = model('a77440', '8ccbd0')
    for x in [-0.22, 0.22]:
        m.ring((x, 0, 0), 0.2, 0.14, 0.1, 8, sides=10)
        m.gem((x, 0, 0.01), (0.14, 0.14, 0.025), 9, sides=10)
    m.strut((-0.04, 0.02, 0), (0.04, 0.02, 0), 0.025, 8)
    for s in [-1, 1]:
        m.strut((s * 0.4, 0.05, 0), (s * 0.48, 0.15, 0.22), 0.025, 8)
    return m

def ReserveCell():
    m = model('3c7b53', '83ed94')
    box(m, (0, 0, 0), (0.58, 0.75, 0.34), 4, 0.05)
    box(m, (0, 0, -0.19), (0.43, 0.58, 0.05), 0, 0.02)
    box(m, (0, 0, -0.23), (0.32, 0.45, 0.025), 3)
    m.prism([(0.04, 0.19), (-0.09, -0.02), (0, -0.02), (-0.04, -0.19), (0.1, 0.04), (0.02, 0.04)], 0.02, 8, center=(0, 0, -0.26))
    for x in [-0.15, 0.15]:
        box(m, (x, 0.43, 0), (0.12, 0.11, 0.17), 6)
    return m

def SalvageConverter():
    m = model('b37625', 'f1c45c')
    m.prism([(-0.38, 0.25), (0.38, 0.25), (0.18, -0.13), (-0.18, -0.13)], 0.28, 0)
    box(m, (0, -0.21, 0), (0.34, 0.17, 0.28), 6)
    for x in [-0.24, 0, 0.24]:
        m.ring((x, 0.28, -0.05), 0.105, 0.07, 0.035, 8, sides=10)
        m.gem((x, 0.28, -0.08), (0.06, 0.06, 0.02), 3, sides=10)
    m.lathe([(-0.45, 0.03), (-0.3, 0.13), (-0.22, 0.13)], 3)
    return m

def FocusingLens():
    m = model('348b92', '8ce8ed')
    m.ring((0, 0.07, 0), 0.33, 0.23, 0.14, 0, sides=12)
    m.gem((0, 0.07, -0.01), (0.22, 0.22, 0.045), 9, sides=12)
    m.ring((0, 0.07, -0.09), 0.22, 0.17, 0.025, 1, sides=12)
    box(m, (0, -0.32, 0), (0.12, 0.25, 0.16), 6)
    box(m, (0, -0.45, 0), (0.31, 0.08, 0.22), 0)
    return m

def ReboundCoil():
    m = model('b6874c', 'f5d28b')
    for i in range(19):
        a = i * math.pi * 0.48
        b = (i + 1) * math.pi * 0.48
        y = -0.36 + i * 0.038
        m.strut((0.2 * math.cos(a), y, 0.2 * math.sin(a)), (0.2 * math.cos(b), y + 0.038, 0.2 * math.sin(b)), 0.027, 8)
    box(m, (0, -0.42, 0), (0.48, 0.08, 0.3), 4)
    box(m, (0, 0.42, 0), (0.48, 0.08, 0.3), 4)
    return m


BUILDERS = {
    'BaseWeaponItem': BaseWeaponItem,
    'ScatterCannonItem': ScatterCannonItem,
    'WindUpCannonItem': WindUpCannonItem,
    'BeamEmitterItem': BeamEmitterItem,
    'FlameProjectorItem': FlameProjectorItem,
    'BackdraftItem': BackdraftItem,
    'DrillItem': DrillItem,
    'DepthChargeItem': DepthChargeItem,
    'SwarmRackItem': SwarmRackItem,
    'DiscThrowerItem': DiscThrowerItem,
    'BurstLauncherItem': BurstLauncherItem,
    'NeedleCannonItem': NeedleCannonItem,
    'RailDriverItem': RailDriverItem,
    'ElectricityItem': ElectricityItem,
    'BulletDroneItem': BulletDroneItem,
    'LaserDroneItem': LaserDroneItem,
    'TwerpItem': TwerpItem,
    'AMoonItem': AMoonItem,
    'AngryMoonItem': AngryMoonItem,
    'ThreeMoonsItem': ThreeMoonsItem,
    'BabyYouItem': BabyYouItem,
    'BallOfYarn': BallOfYarn,
    'PaperPlane': PaperPlane,
    'Tripod': Tripod,
    'AK45Item': AK45Item,
    'PaperclipBox': PaperclipBox,
    'FireflyJar': FireflyJar,
    'SaltShaker': SaltShaker,
    'PocketMortar': PocketMortar,
    'WindUpTeeth': WindUpTeeth,
    'RibbonSpool': RibbonSpool,
    'SewingKit': SewingKit,
    'Popcorn': Popcorn,
    'RubberBand': RubberBand,
    'HotSauce': HotSauce,
    'FocusTime': FocusTime,
    'BadGlasses': BadGlasses,
    'ReserveCell': ReserveCell,
    'SalvageConverter': SalvageConverter,
    'FocusingLens': FocusingLens,
    'ReboundCoil': ReboundCoil,
}
