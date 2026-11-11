from mesh import Model
import math

def model(a='426d78', b='e9aa45'):
    return Model(a, b)

def poly(m, pts, depth=0.18, tile=0, z=0):
    m.prism(pts, depth, tile, center=(0, 0, z))

def bar(m, a, b, r=0.035, t=6, **kw):
    m.strut(a, b, r, t, **kw)

def dot(m, x, y, z=-0.2, r=0.04, t=8):
    m.gem((x, y, z), (r, r, r * 0.5), t, sides=6)

def bottle(m, body=0):
    m.lathe([(-0.55, 0.25), (-0.48, 0.31), (0.15, 0.31), (0.34, 0.2), (0.43, 0.15)], body, sides=8)
    m.lathe([(0.43, 0.15), (0.52, 0.17), (0.57, 0.16)], 8, sides=8)

def label(m, color=5):
    m.box((0, -0.05, -0.305), (0.43, 0.29, 0.025), color, bevel=0.025)

def shield(m, t=0):
    poly(m, [(-0.48, 0.45), (0.48, 0.45), (0.42, -0.24), (0, -0.58), (-0.42, -0.24)], 0.17, t)

def bolt(m, x, y):
    dot(m, x, y, -0.17, 0.034, 8)

def arrows(m, x=0, y=0, t=8):
    poly(m, [(x - 0.16, y - 0.12), (x + 0.02, y - 0.12), (x + 0.02, y - 0.22), (x + 0.22, y), (x + 0.02, y + 0.22), (x + 0.02, y + 0.12), (x - 0.16, y + 0.12)], 0.04, t, z=-0.23)

def energy():
    m = model('177987', 'f9bd47')
    m.lathe([(-0.5, 0.28), (-0.43, 0.4), (-0.3, 0.4), (-0.26, 0.3), (0.26, 0.3), (0.3, 0.4), (0.43, 0.4), (0.5, 0.28)], 0, sides=8)
    m.gem((0, 0, -0.29), (0.25, 0.29, 0.1), 7)
    m.ring((0, 0, -0.36), 0.28, 0.22, 0.04, 6, sides=10)
    for x in [-0.34, 0.34]:
        m.box((x, 0, -0.03), (0.12, 0.56, 0.2), 6, bevel=0.03)
    return m

def ball():
    m = model('265cb2', 'f0bf55')
    m.gem((0, 0, 0), (0.48, 0.48, 0.38), 0, sides=10)
    m.ring((0, 0, -0.37), 0.27, 0.22, 0.04, 5, sides=10)
    m.box((0, 0, -0.4), (0.09, 0.34, 0.04), 5)
    m.box((0, 0, -0.4), (0.34, 0.09, 0.04), 5)
    for x in [-0.3, 0.3]:
        dot(m, x, 0.2, -0.3, 0.045, 3)
    return m

def poison():
    m = model('557746', 'ead594')
    bottle(m)
    label(m, 5)
    m.gem((0, 0, -0.34), (0.11, 0.13, 0.05), 4)
    dot(m, -0.045, 0.025, -0.39, 0.023, 5)
    dot(m, 0.045, 0.025, -0.39, 0.023, 5)
    bar(m, (-0.16, -0.13, -0.35), (0.16, -0.29, -0.35), 0.025, 5)
    bar(m, (0.16, -0.13, -0.35), (-0.16, -0.29, -0.35), 0.025, 5)
    return m

def gasoline():
    m = model('bd431d', 'f8bd42')
    m.box((0, -0.05, 0), (0.63, 0.72, 0.28), 0, bevel=0.09)
    m.box((-0.16, 0.36, 0), (0.18, 0.16, 0.23), 4)
    m.box((0.2, 0.34, 0), (0.22, 0.17, 0.18), 6, bevel=0.02)
    m.box((0, -0.05, -0.16), (0.45, 0.45, 0.04), 2, bevel=0.04)
    poly(m, [(-0.08, -0.24), (0.09, -0.1), (0.02, -0.05), (0.13, 0.16), (-0.05, 0.03), (-0.01, 0), (-0.14, -0.1)], 0.04, 3, z=-0.2)
    return m

def glasses():
    m = model('d39e36', '5bbac6')
    for x in [-0.27, 0.27]:
        m.ring((x, 0, 0), 0.25, 0.19, 0.12, 8, sides=10)
        m.lathe([(-0.01, 0.18), (0.01, 0.18)], 9, center=(x, 0, -0.05), sides=10, axis='z')
        bar(m, (x - 0.1, 0.09, -0.08), (x + 0.02, 0.18, -0.08), 0.014, 5)
    bar(m, (-0.07, 0.03, 0), (0.07, 0.03, 0), 0.035, 8)
    bar(m, (-0.5, 0.09, 0), (-0.58, 0.24, 0.3), 0.035, 8)
    bar(m, (0.5, 0.09, 0), (0.58, 0.24, 0.3), 0.035, 8)
    return m

def shrink():
    m = model('648e48', 'd4ba6d')
    bottle(m)
    label(m)
    poly(m, [(-0.15, 0.04), (0.15, 0.04), (0, -0.16)], 0.03, 2, z=-0.34)
    m.box((0, 0.53, 0), (0.34, 0.12, 0.32), 8, bevel=0.03)
    return m

def lighter():
    m = model('dc531e', 'f9bd40')
    m.box((0, -0.14, 0), (0.5, 0.7, 0.23), 0, bevel=0.08)
    m.box((0, 0.27, 0), (0.5, 0.18, 0.24), 6, bevel=0.03)
    m.box((-0.11, 0.39, 0), (0.2, 0.09, 0.19), 4, bevel=0.02)
    m.ring((-0.12, 0.38, -0.12), 0.09, 0.04, 0.04, 6, sides=8)
    m.gem((0.12, 0.52, 0), (0.11, 0.22, 0.06), 3)
    return m

def returning():
    m = model('d76220', 'f5be53')
    poly(m, [(-0.49, -0.42), (0.34, -0.42), (0.55, 0), (0.34, 0.42), (-0.49, 0.42), (-0.33, 0.26), (0.23, 0.26), (0.36, 0), (0.23, -0.26), (-0.33, -0.26)], 0.18, 0)
    for y in [-0.34, 0.34]:
        bar(m, (-0.33, y, -0.12), (0.25, y, -0.12), 0.025, 8)
    for y in [-0.35, 0.35]:
        bolt(m, -0.38, y)
    return m

def drone():
    m = model('9e3c92', '62d8e0')
    m.gem((0, 0, 0), (0.29, 0.3, 0.22), 0)
    m.ring((0, 0, -0.22), 0.19, 0.13, 0.06, 6, sides=8)
    m.gem((0, 0, -0.26), (0.12, 0.12, 0.04), 7)
    for s in [-1, 1]:
        bar(m, (s * 0.2, 0.03, 0), (s * 0.48, 0.03, 0), 0.07, 2)
        m.gem((s * 0.48, 0.03, 0), (0.17, 0.17, 0.13), 0)
        dot(m, s * 0.48, 0.03, -0.13, 0.08, 7)
    m.box((0, 0.29, 0), (0.17, 0.1, 0.16), 3, bevel=0.02)
    return m

def counter():
    m = model('286799', 'f2bb42')
    shield(m)
    poly(m, [(-0.32, 0.3), (0.32, 0.3), (0.28, -0.16), (0, -0.4), (-0.28, -0.16)], 0.05, 1, z=-0.12)
    bar(m, (0, -0.3, -0.18), (0, 0.28, -0.18), 0.06, 8)
    m.lathe([(-0.05, 0.09), (0.08, 0.09), (0.13, 0.05)], 3, center=(0, 0.22, -0.19), axis='y')
    m.ring((0, 0, -0.19), 0.18, 0.13, 0.03, 8, sides=10)
    return m

def capacitor():
    m = model('225c9e', 'f1b845')
    for x in [-0.29, 0.29]:
        m.lathe([(-0.47, 0.14), (-0.4, 0.17), (0.37, 0.17), (0.44, 0.14)], 0, center=(x, 0, 0), sides=8)
        m.lathe([(0.44, 0.12), (0.5, 0.12)], 8, center=(x, 0, 0))
    m.box((0, -0.43, 0), (0.76, 0.1, 0.28), 6, bevel=0.03)
    m.box((0, 0.4, 0), (0.76, 0.1, 0.28), 6, bevel=0.03)
    m.ring((0, 0, -0.18), 0.18, 0.14, 0.05, 8, sides=10)
    bar(m, (0, 0, -0.22), (0, 0.11, -0.22), 0.02, 4)
    bar(m, (0, 0, -0.22), (0.08, -0.04, -0.22), 0.02, 4)
    return m

def fuse():
    m = model('c54537', 'ebbd54')
    m.gem((0, 0, 0), (0.35, 0.38, 0.25), 0)
    m.ring((0, 0, -0.24), 0.2, 0.15, 0.05, 8, sides=10)
    m.gem((0, 0, -0.28), (0.13, 0.13, 0.05), 7)
    for s in [-1, 1]:
        bar(m, (s * 0.26, 0.12, 0), (s * 0.43, 0.42, 0), 0.035, 8)
        dot(m, s * 0.43, 0.42, 0, 0.07, 8)
    m.lathe([(-0.48, 0.13), (-0.37, 0.15)], 6)
    return m

def splitter():
    m=model('ea8b21','ffe175')
    m.paint(12,'642c20')
    m.paint(13,'f5c45d')
    m.lathe([(-.47,.13),(-.37,.18),(-.19,.18)],12,sides=8)
    m.ring((0,-.2,-.08),.2,.13,.13,8,sides=10)
    m.gem((0,-.2,-.17),(.095,.095,.045),7)
    for side in [-1,1]:
        bar(m,(side*.04,-.12,0),(side*.31,.29,0),.105,0)
        bar(m,(side*.27,.21,0),(side*.37,.38,0),.13,8)
        bar(m,(side*.33,.32,-.02),(side*.41,.45,-.02),.075,13,end_radius=.025)
        bar(m,(side*.08,-.09,-.105),(side*.25,.18,-.105),.025,3)
    return m

def caffeine():
    m = model('f4ead4', 'cf9860')
    m.paint(12, '593623')
    m.lathe([(-.43,.28),(-.38,.34),(.26,.34),(.32,.32),(.32,.28),(.235,.272),(.235,0)],0,sides=10,bands=(2,0,0,5,2,12))
    m.ring((.34,-.03,0),.23,.15,.1,5,sides=10)
    bar(m,(-.11,.31,0),(-.15,.58,0),.02,6)
    return m

def protein():
    m = model('e5e9e0', '277ab5')
    m.paint(12, '174054')
    m.paint(13, 'ffe373')
    m.lathe([(-.46, .28), (-.42, .34), (-.35, .36), (.25, .36),
             (.31, .30), (.36, .30)], 0, sides=12, bands=(1, 0, 12, 0, 1))
    m.lathe([(.351, .355), (.46, .355), (.49, .33)], 3, sides=12)
    for y in (.375, .414, .453):
        m.ring((0, y, 0), .362, .35, .014, 12, sides=12, axis='y')
    m.box((0, -.052, -.335), (.50, .49, .062), 12, bevel=.024)

    # The dumbbell and condensed lettering are part of the tub's printed label.
    dumbbell = [(-.19, .119), (-.125, .119), (-.125, .078), (.125, .078),
                (.125, .119), (.19, .119), (.19, -.005), (.125, -.005),
                (.125, .036), (-.125, .036), (-.125, -.005), (-.19, -.005)]
    m.face(13, [(x, y, -.369) for x, y in dumbbell])
    lettering = (
        "101 101 111 101",
        "101 101 100 101",
        "101 111 110 010",
        "111 101 100 010",
        "101 101 111 010",
    )
    for row, pixels in enumerate(lettering):
        y = -.063 - row * .032
        for column, pixel in enumerate(pixels):
            if pixel == "1":
                x = (column - 7) * .028
                m.face(5, [(x - .014, y - .016, -.369), (x - .014, y + .016, -.369),
                           (x + .014, y + .016, -.369), (x + .014, y - .016, -.369)])
    return m

def hull():
    m = model('b53f53', 'ebbc87')
    for dx, dy, z in [(0.15, 0.13, 0.08), (-0.15, -0.13, -0.08)]:
        pts = [(-0.33 + dx, 0.34 + dy), (0.33 + dx, 0.34 + dy), (0.29 + dx, -0.27 + dy), (dx, -0.38 + dy), (-0.29 + dx, -0.27 + dy)]
        poly(m, pts, 0.13, 0, z)
        for x, y in [(-0.23, 0.24), (0.23, 0.24), (-0.2, -0.2), (0.2, -0.2)]:
            dot(m, x + dx, y + dy, z - 0.09, 0.03, 8)
    for y in [-0.1, 0.08]:
        bar(m, (-0.3, y - 0.1, -0.18), (-0.15, y, -0.18), 0.035, 5)
        bar(m, (-0.15, y, -0.18), (0, y - 0.1, -0.18), 0.035, 5)
    return m

def whistle():
    m = model('d6a733', 'efca64')
    m.lathe([(-0.22, 0.25), (0.15, 0.25), (0.2, 0.21)], 0, axis='z', sides=10)
    m.ring((0, 0, -0.25), 0.25, 0.2, 0.04, 8, sides=10)
    m.box((0.34, 0.02, 0), (0.4, 0.25, 0.22), 0, bevel=0.04)
    m.box((0.34, 0.03, -0.13), (0.2, 0.08, 0.03), 4)
    m.ring((0, 0.33, 0), 0.22, 0.18, 0.05, 3, sides=12)
    return m

def triggers():
    m = model('b7652e', 'edc46a')
    for s in [-1, 1]:
        m.box((s * 0.27, 0.28, 0), (0.35, 0.2, 0.22), 8, bevel=0.04)
        m.box((s * 0.3, -0.13, 0), (0.17, 0.65, 0.2), 0, bevel=0.03, angle=s * -12)
        m.ring((s * 0.17, 0.02, -0.15), 0.15, 0.1, 0.05, 6, sides=8, start=180 if s < 0 else 0, sweep=180)
    bar(m, (-0.13, 0.2, 0), (0.13, 0.2, 0), 0.045, 3)
    dot(m, 0, 0.2, -0.15, 0.06, 3)
    return m

def emp():
    m = model('188c91', 'f1ae4a')
    m.ring((0, 0, 0), 0.35, 0.23, 0.18, 0, sides=10)
    m.gem((0, 0, -0.12), (0.19, 0.19, 0.09), 7)
    for i in range(8):
        a = 2 * math.pi * i / 8
        bar(m, (0.3 * math.cos(a), 0.3 * math.sin(a), 0), (0.49 * math.cos(a), 0.49 * math.sin(a), 0), 0.065, 0)
        dot(m, 0.47 * math.cos(a), 0.47 * math.sin(a), -0.06, 0.035, 3)
    return m

def reactor():
    m = model('b63f23', 'e6ad46')
    for x in [-0.22, 0.22]:
        m.lathe([(-0.4, 0.17), (-0.32, 0.21), (0.25, 0.21), (0.34, 0.17)], 0, center=(x, 0, 0))
        m.lathe([(0.34, 0.13), (0.4, 0.13)], 8, center=(x, 0, 0))
    for y in [-0.27, 0.2]:
        m.box((0, y, -0.22), (0.85, 0.09, 0.08), 4, bevel=0.02)
    bar(m, (-0.22, 0.41, 0), (0.22, 0.41, 0), 0.035, 8)
    bar(m, (0.22, 0.41, 0), (0.31, 0.53, 0), 0.035, 8)
    dot(m, 0.31, 0.53, 0, 0.055, 3)
    return m

def battery():
    m = model('8d9d35', 'ed9d3c')
    m.lathe([(-0.25, 0.23), (-0.18, 0.26), (0.17, 0.26), (0.23, 0.23)], 0, axis='x')
    m.lathe([(-0.27, 0.15), (-0.21, 0.15)], 6, axis='x')
    m.lathe([(0.22, 0.15), (0.3, 0.15)], 6, axis='x')
    m.lathe([(-0.5, 0.1), (0.3, 0.1), (0.52, 0)], 3, sides=8)
    poly(m, [(-0.1, -0.4), (-0.24, -0.54), (-0.09, -0.52)], 0.08, 0)
    poly(m, [(0.1, -0.4), (0.24, -0.54), (0.09, -0.52)], 0.08, 0)
    return m

def arm():
    m = model('d19a74', '454a50')
    bar(m, (-0.43, 0.32, 0), (-0.14, -0.18, 0), 0.11, 0)
    m.gem((-0.14, -0.18, 0), (0.13, 0.13, 0.12), 1)
    bar(m, (-0.14, -0.18, 0), (0.23, 0.13, 0), 0.1, 0)
    m.box((0.29, 0.2, 0), (0.2, 0.24, 0.13), 0, bevel=0.04, angle=-40)
    for i in range(4):
        bar(m, (0.25 + i * 0.06, 0.26 + i * 0.04, 0), (0.29 + i * 0.06, 0.39 + i * 0.04, 0), 0.027, 1)
    m.box((-0.42, 0.33, 0), (0.25, 0.2, 0.24), 3, bevel=0.03, angle=35)
    return m

def energyshield():
    m = model('25638d', '84dcea')
    shield(m)
    poly(m, [(-0.34, 0.32), (0.34, 0.32), (0.29, -0.18), (0, -0.43), (-0.29, -0.18)], 0.06, 7, z=-0.12)
    m.gem((0, 0, -0.19), (0.22, 0.27, 0.06), 9)
    for x, y in [(-0.38, 0.34), (0.38, 0.34), (-0.3, -0.2), (0.3, -0.2), (0, -0.48)]:
        dot(m, x, y, -0.14, 0.04, 5)
    return m

def telekinesis():
    m = model('829ba8', '79dce4')
    bar(m, (-0.24, -0.5, 0), (-0.2, -0.16, 0), 0.047, 6, end_radius=0.035)
    m.lathe([(-0.51, 0.065), (-0.43, 0.065)], 4, center=(-0.24, 0, 0))
    bar(m, (-0.2, -0.16, 0), (-0.04, 0.03, 0), 0.035, 6)
    m.gem((0.08, 0.25, 0.025), (0.2, 0.29, 0.065), 6, sides=10)
    m.gem((0.08, 0.25, -0.045), (0.15, 0.235, 0.025), 9, sides=10)
    bar(m, (0.01, 0.34, -0.078), (0.09, 0.42, -0.078), 0.012, 5)
    for x, y, r in [(-0.25, 0.23, 0.055), (-0.28, 0.39, 0.04), (0.35, 0.49, 0.055)]:
        m.gem((x, y, -0.03), (r, r * 1.5, r * 0.55), 7, sides=4)
    bar(m, (-0.32, 0.03, 0), (-0.39, 0.14, 0), 0.012, 3)
    bar(m, (-0.39, 0.14, 0), (-0.4, 0.26, 0), 0.012, 3)
    return m

def warp():
    m = model('527d85', 'a06cf4')
    m.paint(12, '72e9f5')
    m.paint(13, '1b1c26')
    m.paint(14, 'f3ffff')
    for y in (-.43, .43):
        m.lathe([(y - .055, .32), (y - .035, .35), (y + .035, .35), (y + .055, .32)],
                6, sides=10, bands=(1, 0, 1))
    for x in (-.26, .26):
        bar(m, (x, -.39, 0), (x, .39, 0), .03, 6)
    m.ring((0, 0, 0), .40, .35, .045, 3, sides=12, axis='y')
    sphere = [(-.094, 0), (-.0814, .047), (-.047, .0814), (0, .094),
              (.047, .0814), (.0814, .047), (.094, 0)]
    for y, tile in ((.272, 13), (-.272, 14)):
        m.lathe(sphere, tile, center=(0, y, 0), sides=10)
    m.translucent(12, .45)
    m.gem((0, 0, 0), (.19, .29, .17), 12, sides=8)
    return m

def exploding():
    m = model('b75a28', 'f5bc4b')
    shield(m)
    m.gem((0, 0, -0.15), (0.12, 0.12, 0.06), 8)
    for i in range(8):
        a = 2 * math.pi * i / 8
        poly(m, [(0.1 * math.cos(a - 0.2), 0.1 * math.sin(a - 0.2)), (0.38 * math.cos(a), 0.38 * math.sin(a)), (0.1 * math.cos(a + 0.2), 0.1 * math.sin(a + 0.2))], 0.03, 3, z=-0.17)
    for x, y in [(-0.38, 0.35), (0.38, 0.35), (0, -0.47)]:
        dot(m, x, y, -0.14, 0.035, 8)
    return m

def health():
    m = model('e3e2d3', 'f0ad1a')
    m.box((0, 0, 0), (0.8, 0.62, 0.27), 0, bevel=0.07)
    m.box((0, 0, -0.15), (0.65, 0.48, 0.03), 5, bevel=0.025)
    m.box((0, 0, -0.18), (0.16, 0.39, 0.04), 3)
    m.box((0, 0, -0.18), (0.39, 0.16, 0.04), 3)
    bar(m, (-0.22, 0.34, 0), (-0.22, 0.46, 0), 0.04, 6)
    bar(m, (0.22, 0.34, 0), (0.22, 0.46, 0), 0.04, 6)
    bar(m, (-0.22, 0.46, 0), (0.22, 0.46, 0), 0.045, 6)
    for x in [-0.35, 0.35]:
        m.box((x, 0, -0.04), (0.08, 0.52, 0.24), 6, bevel=0.02)
    return m

def washers():
    m = model('91bacd', 'f0cd78')
    for x, y, z in [(-0.14, -0.13, 0.08), (0.14, 0.12, -0.08)]:
        m.ring((x, y, z), 0.36, 0.19, 0.1, 6, sides=10)
    return m

def spring():
    m=model('e0a83a','ffe28c')
    m.paint(12,'ffd15d')
    m.paint(13,'28252a')
    for i in range(36):
        a=i*math.pi*3.3/36
        b=(i+1)*math.pi*3.3/36
        r=.05+.42*i/36
        r2=.05+.42*(i+1)/36
        bar(m,(r*math.cos(a),r*math.sin(a),0),(r2*math.cos(b),r2*math.sin(b),0),.029,12)
    m.lathe([(-.09,.1),(.09,.1)],13,axis='z')
    m.ring((0,0,-.1),.1,.055,.025,8,sides=10)
    return m

def bearings():
    m=model('b3d5e3','ffffff')
    m.paint(12,'9fc5d5')
    m.paint(13,'344f63')
    m.ring((0,0,0),.46,.34,.16,12,sides=12)
    m.ring((0,0,-.01),.23,.14,.17,12,sides=12)
    m.ring((0,0,.025),.34,.23,.035,13,sides=12)
    for i in range(9):
        a=2*math.pi*i/9
        m.gem((.285*math.cos(a),.285*math.sin(a),-.05),(.065,.065,.065),5)
    return m

def thermos():
    m = model('18858a', 'eab656')
    m.lathe([(-0.5, 0.23), (-0.44, 0.28), (0.32, 0.28), (0.39, 0.24)], 0, sides=8)
    m.lathe([(0.39, 0.25), (0.5, 0.25)], 4, sides=8)
    m.ring((0.3, 0, 0), 0.27, 0.2, 0.09, 4, sides=8, start=270, sweep=180)
    m.box((0, -0.04, -0.27), (0.4, 0.12, 0.025), 1)
    return m

def heatsink():
    m = model('80a9c4', 'f2b75d')
    m.box((0, -0.43, 0.02), (0.91, 0.09, 0.48), 8, bevel=0.025)
    m.box((0, -0.35, 0.02), (0.86, 0.1, 0.44), 4, bevel=0.025)
    for i, x in enumerate([-0.34, -0.17, 0, 0.17, 0.34]):
        top = 0.4 if i % 2 == 0 else 0.31
        m.box((x, (-0.3 + top) / 2, 0), (0.105, top + 0.3, 0.36), 0, bevel=0.016)
        m.box((x, top + 0.009, -0.02), (0.07, 0.018, 0.3), 1, bevel=0.005)
    for x in [-0.37, 0.37]:
        m.ring((x, -0.43, -0.238), 0.035, 0.016, 0.015, 6, sides=8)
    return m

def penny():
    m = model('a85d32', 'edb575')
    m.lathe([(-0.075, 0.43), (0.075, 0.43)], 0, axis='z', sides=16)
    m.ring((0, 0, -0.085), 0.4, 0.36, 0.025, 8, sides=16)
    poly(m, [(-0.12, 0.21), (0.035, 0.25), (0.12, 0.18), (0.12, 0.09), (0.19, 0.025), (0.13, -0.015), (0.13, -0.1), (0.075, -0.17), (0.09, -0.25), (-0.13, -0.25), (-0.13, -0.12), (-0.19, -0.04), (-0.2, 0.11)], 0.035, 3, z=-0.106)
    bar(m, (-0.1, 0.15, -0.13), (-0.025, 0.19, -0.13), 0.012, 0)
    dot(m, 0.08, 0.09, -0.13, 0.014, 0)
    for a in [35, 60, 85, 110, 135, 220, 245, 270, 295, 320]:
        a = math.radians(a)
        dot(m, 0.33 * math.cos(a), 0.33 * math.sin(a), -0.11, 0.012, 8)
    return m

def whetstone():
    m = model('84a7a3', 'e7cd82')
    m.box((0, 0, 0), (0.88, 0.34, 0.31), 0, bevel=0.08, angle=12)
    m.box((0, -0.09, -0.17), (0.7, 0.09, 0.02), 2, angle=12)
    for x in [-0.22, 0, 0.22]:
        bar(m, (x - 0.02, -0.02, -0.18), (x + 0.03, 0.12, -0.18), 0.012, 4)
    return m

def oil():
    m = model('a74432', 'd67b55')
    m.lathe([(-0.11, 0.33), (0.11, 0.33)], 0, axis='z', sides=10)
    m.ring((0, 0, -0.12), 0.3, 0.24, 0.03, 2, sides=10)
    bar(m, (0.18, 0.2, 0), (0.48, 0.47, 0), 0.055, 2, end_radius=0.025)
    m.box((-0.33, 0, 0), (0.12, 0.31, 0.17), 2, bevel=0.02)
    m.ring((-0.34, 0, 0), 0.15, 0.1, 0.05, 4, sides=8, start=90, sweep=180)
    return m

def tailfin():
    m = model('d1a833', 'f2ce62')
    bar(m, (0, -0.5, 0), (0, 0.45, 0), 0.055, 4)
    dot(m, 0, 0.48, 0, 0.075, 8)
    poly(m, [(0, 0.18), (-0.34, -0.44), (-0.08, -0.37), (0, -0.46)], 0.08, 0)
    poly(m, [(0, 0.18), (0.34, -0.44), (0.08, -0.37), (0, -0.46)], 0.08, 3)
    bar(m, (-0.22, -0.35, -0.05), (-0.04, 0.02, -0.05), 0.015, 5)
    return m

def horseshoe():
    m = model('5da873', 'dded91')
    m.ring((0, 0, 0), 0.45, 0.29, 0.17, 0, sides=10, start=40, sweep=280)
    for a in [55, 95, 135, 175, 215, 255, 295, 325]:
        r = math.radians(a)
        dot(m, 0.37 * math.cos(r), 0.37 * math.sin(r), -0.1, 0.025, 6)
    return m

def corkscrew():
    m = model('a8404b', 'd9b066')
    m.box((0, 0.38, 0), (0.77, 0.14, 0.16), 0, bevel=0.07)
    for x in [-0.33, 0.33]:
        dot(m, x, 0.38, -0.1, 0.055, 8)
    bar(m, (0, 0.3, 0), (0, 0.15, 0), 0.045, 8)
    for i in range(24):
        a = i * math.pi * 4 / 24
        b = (i + 1) * math.pi * 4 / 24
        bar(m, (0.11 * math.cos(a), 0.14 - i * 0.025, 0.11 * math.sin(a)), (0.11 * math.cos(b), 0.14 - (i + 1) * 0.025, 0.11 * math.sin(b)), 0.025, 6)
    return m

def slingshot():
    m = model('805035', 'b7c468')
    bar(m, (0, -0.48, 0), (0, -0.05, 0), 0.085, 0)
    for s in [-1, 1]:
        bar(m, (0, -0.05, 0), (s * 0.32, 0.4, 0), 0.065, 0)
        bar(m, (s * 0.32, 0.4, -0.02), (s * 0.05, 0.03, -0.08), 0.022, 3)
        m.box((s * 0.32, 0.4, 0), (0.15, 0.09, 0.16), 3, bevel=0.02)
    m.box((0, 0.03, -0.08), (0.17, 0.09, 0.08), 2, bevel=0.02)
    return m

def ink():
    m = model('243e77', 'd8cba9')
    m.lathe([(-0.48, 0.29), (-0.42, 0.32), (-0.06, 0.32), (0.02, 0.21), (0.12, 0.21)], 0, sides=8)
    m.lathe([(0.12, 0.19), (0.2, 0.19)], 4, sides=8)
    label(m, 5)
    m.box((0, -0.08, -0.34), (0.2, 0.08, 0.02), 3)
    bar(m, (0, 0.16, 0), (0.2, 0.5, 0), 0.035, 8)
    poly(m, [(0.14, 0.39), (0.27, 0.55), (0.3, 0.58), (0.25, 0.39)], 0.035, 5)
    return m

def grudge():
    m = model('8e3634', 'd7b078')
    m.box((0, 0, 0), (0.67, 0.86, 0.2), 0, bevel=0.04)
    m.box((-0.3, 0, -0.02), (0.09, 0.842, 0.23), 2)
    m.box((0, 0, -0.12), (0.49, 0.7, 0.025), 2)
    for x in [-0.16, -0.08, 0, 0.08]:
        m.box((x, 0.08, -0.15), (0.035, 0.38, 0.02), 5)
    bar(m, (-0.22, -0.04, -0.16), (0.13, 0.25, -0.16), 0.018, 5)
    m.box((0.36, -0.07, 0), (0.12, 0.15, 0.23), 8, bevel=0.02)
    return m

def marbles():
    m = model('8c6d9f', '62b5bd')
    for x, y, r, t in [(-0.25, -0.22, 0.21, 3), (0.22, -0.26, 0.18, 8), (0.04, 0.2, 0.28, 0)]:
        m.gem((x, y, 0), (r, r, r * 0.8), t, sides=10)
        m.gem((x - r * 0.23, y + r * 0.28, -r * 0.7), (r * 0.2, r * 0.13, 0.025), 5)
    return m

def hourglass():
    m=model('a9633c','f6bf46')
    m.paint(12,'eab146')
    m.paint(13,'bfe9ee')
    for y in [-.46,.46]:
        m.box((0,y,0),(.58,.1,.3),0,bevel=.04)
    for x in [-.25,.25]:
        bar(m,(x,-.4,0),(x,.4,0),.025,8)
    m.lathe([(-.37,.15),(-.32,.18),(-.16,.035)],12,sides=10)
    bar(m,(0,.04,0),(0,-.19,0),.012,12)
    m.translucent(13,.35)
    m.lathe([(-.4,.20),(-.34,.22),(-.08,.06),(0,.025),(.08,.06),(.34,.22),(.4,.20)],13,sides=10)
    return m

def dice():
    m = model('bf344a', 'e7ddd1')
    for x, y, z, t in [(-0.22, -0.17, 0, 0), (0.2, 0.19, 0.06, 5)]:
        m.box((x, y, z), (0.39, 0.39, 0.39), t, bevel=0.05, angle=14 if x < 0 else -14)
        col = 5 if x < 0 else 0
        for dx, dy in [(-0.1, -0.1), (0, 0), (0.1, 0.1)]:
            dot(m, x + dx, y + dy, z - 0.21, 0.035, col)
    return m

def relay():
    m = model('517ea0', 'ffab44')
    m.box((0, -0.03, 0.04), (0.77, 0.76, 0.18), 4, bevel=0.055)
    m.box((0, -0.03, -0.07), (0.66, 0.64, 0.07), 0, bevel=0.035)
    for x in [-0.28, 0.28]:
        for y in [-0.29, 0.29]:
            m.ring((x, y, -0.12), 0.032, 0.014, 0.018, 6, sides=8)
        m.box((x, 0.12, -0.15), (0.16, 0.28, 0.12), 6, bevel=0.025)
        m.box((x, 0.16, -0.225), (0.1, 0.13, 0.035), 8, bevel=0.012)
        m.box((x, 0.36, -0.1), (0.13, 0.1, 0.12), 6, bevel=0.015)
    m.ring((0, -0.22, -0.18), 0.12, 0.075, 0.09, 8, sides=10)
    m.lathe([(-0.25, 0.055), (-0.2, 0.055)], 6, center=(0, -0.22, 0), axis='z')
    bar(m, (0, -0.22, -0.26), (0.18, 0.23, -0.26), 0.043, 6)
    m.box((0.2, 0.27, -0.26), (0.17, 0.12, 0.12), 3, bevel=0.025, angle=-20)
    m.box((0, -0.36, -0.125), (0.23, 0.07, 0.025), 5, bevel=0.01)
    for x in [-0.07, 0, 0.07]:
        m.box((x, -0.36, -0.15), (0.035, 0.018, 0.012), 4)
    return m

def shrapnel_bearing():
    m=bearings()
    m.paint(14,'e3f7ff')
    for a in [0,60,120,180,240,300]:
        a=math.radians(a)
        c=math.cos(a)
        q=math.sin(a)
        poly(m,[(.39*c-.065*q,.39*q+.065*c),(.58*c,.58*q),(.39*c+.065*q,.39*q-.065*c)],.14,14,z=0)
    return m

def early_rise():
    m=model('ed8d20','ffe35e')
    m.paint(12,'fff0a0')
    m.paint(13,'7f431e')
    m.lathe([(-.09,.27),(.09,.27)],3,axis='z',sides=12)
    m.lathe([(-.115,.21),(-.09,.21)],12,axis='z',sides=12)
    for i in range(10):
        a=2*math.pi*i/10
        c=math.cos(a)
        q=math.sin(a)
        poly(m,[(.28*c-.05*q,.28*q+.05*c),(.48*c,.48*q),(.28*c+.05*q,.28*q-.05*c)],.11,3)
    m.box((0,-.4,0),(.85,.09,.22),13,bevel=.02)
    m.box((0,-.35,-.12),(.72,.045,.025),3)
    return m

BUILDERS = {
    'BallBearing': shrapnel_bearing,
    'EnergyCore': energy,
    'BouncingBall': ball,
    'Poison': poison,
    'GasolineLeak': gasoline,
    'QualityGlasses': glasses,
    'ShrinkerJuice': shrink,
    'Lighter': lighter,
    'ReturnModule': returning,
    'DroneLink': drone,
    'RecoveryCoil': counter,
    'Capacitor': capacitor,
    'ProximityFuse': fuse,
    'SplitterCore': splitter,
    'Caffeine': caffeine,
    'ProteinSupplement': protein,
    'DoubleHull': hull,
    'SquadWhistle': whistle,
    'DoubleTrouble': triggers,
    'EmpPulse': emp,
    'ChainReactor': reactor,
    'SpareBattery': battery,
    'AnotherArm': arm,
    'EnergyShield': energyshield,
    'Telekinesis': telekinesis,
    'WarpCore': warp,
    'ExplodingShield': exploding,
    'LargeHealthKit': health,
    'SteelWashers': washers,
    'ClockworkSpring': spring,
    'RollerBearings': bearings,
    'Thermos': thermos,
    'HeatSink': heatsink,
    'LuckyPenny': penny,
    'Whetstone': whetstone,
    'OilCan': oil,
    'TailFin': tailfin,
    'Horseshoe': horseshoe,
    'Corkscrew': corkscrew,
    'SlingshotBand': slingshot,
    'InkPot': ink,
    'Grudge': grudge,
    'GlassMarbles': marbles,
    'Hourglass': hourglass,
    'LoadedDice': dice,
    'RelaySwitch': relay,
}
