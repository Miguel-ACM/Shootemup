import os, re, subprocess, sys, uuid

# One-off regrouping of Assets/Scripts and Assets/Editor by subject instead of by work phase.
# Run from the repository root: python Tools/regroup_scripts.py [--dry-run]
# Files move with their .meta, so Unity keeps every GUID and scene/prefab reference.
# Every script must match a rule; the run stops and lists any file it cannot place.
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DRY = "--dry-run" in sys.argv
S = "Assets/Scripts/"

def names(text):
    return text.split()

EXPLICIT = {}
def put(folder, *files):
    for f in files:
        EXPLICIT[f] = folder

# Core: global rules, camera limits, shared helpers.
put("Core", "GameRules", "Camera", "Utils", "CircleArray", "LineDrawer", "PersistentObject")

# Player ship and its loadout.
put("Player", "Shipcontroller", "PSXShipVisual", "ShipLoadout", "CombatUpgrades", "RunCoins")
put("Player/Weapons", "Weapons", "ExpansionWeapon")
put("Player/Projectiles", *names("ABullet Bullet ElectricityBullet PlayerRound PlayerShotPalette PlayerProjectileArt "
                                  "PlayerDiscArt PlayerFlameArt PlayerMissileArt PlayerNeedleArt RubberReturn"))
put("Companions", "SupportDrone", "TrailDrone")

# Items: shared behaviour and the visual effects items leave on enemies or the world.
put("Items/Effects", *names("HotSauceTrail StickyGum StaticCharge BurningHull PoisonedHull FrozenHull HullAfflictionVfx "
                             "GlassesFieldCue MagnifyingLens GumSpritePop ItemRarityHalo ItemSpark SpectralDecoy"))

# Enemies: shared base classes and feedback.
put("Enemies", *names("AEnemy DistrictEnemy OceanEnemy InfernalEnemy SpaceSatellite GroundVehicle EnemyFiringCue "
                       "EnemyThrusters EnemyFlightAttitude PartHitFeedback ChargedShotCue LaserSidePorts BeamRibbon "
                       "EnemyTunableAttribute EnemyTuning SandSpine"))
put("Enemies/Original", "Biggie", "Boss", "EnemyBasic", "Waver")
put("Enemies/Bosses", "DistrictBoss", "BossDestruction")
put("Enemies/Minibosses", "DistrictMiniboss")
put("Enemies/Movers", "DriftDownPattern", "SatelliteOrbit")

# Hostile projectiles and the projectile rendering/collision pipeline.
put("Projectiles", *names("AEnemyBullet EnemyBullet ExpandingWave CombatRound HostileProjectileArt LevelProjectilePalette "
                          "ProjectileCollisionLayers ProjectileCollisionWindow ProjectilePixelContact ProjectileSprites "
                          "ProjectileSpriteTint ShortLivedRound ProjectileGlow"))

# Waves: authored parts, choreography and the orchestrator.
put("Waves", *names("ConflictGroup Wave WaveOrchestrer WavePool WaveUnit WavePart WaveComposer WaveDance WaveHandoff "
                    "WavePreparation DistrictWaves FlightCurve"))

# Run flow: level routing, decisions, shops and transitions.
put("Run", *names("LevelRun LevelChoice EncounterChoice RouteCharge GameOverState LevelReveal CloudTransition "
                  "CombatDecision DecisionOverlay DecisionShop"))

# Environments: scrolling scenery for each district.
put("Environment", *names("CandylandBackdrop CityDistrictGeometry DepthBackdrop DepthNatureMeshes DesertDustStorm "
                          "DesertGeometry InfernalCaldera LevelEnvironment OceanLife SnowLandscape SpaceBackdrop "
                          "VolcanicVent CloudSpawner ScrollingBg PSXValleyFoliage PSXValleyGeometry PSXValleyLayout "
                          "PSXValleyRiver PSXGrassField PSXIndustrialField TunnelReflectors GroundTreadMarks "
                          "SandEmergenceEffects"))

# Visuals: the PSX presentation, shared materials and procedural meshes.
put("Visuals", *names("PSXPresentation PSXObjectVisual PSXShooterVisual PSXShipEffects PSXImpactEffects PSXFireballs "
                      "PSXPolygonDebris PSXProjectileEffects PSXVisibilityRelay CombatVisuals CombatMaterials "
                      "CandyMaterials EverydayItemMaterials"))
put("Visuals/Meshes", *names("IndustrialMeshFactory CombatHullFactory HullWorkshop MinibossMeshes InfernalOceanMeshes "
                             "AbyssHunterMesh DesertWormAssets SpaceAirframes EverydayItemMeshes ExpansionItemMeshes "
                             "RequestedItemMeshes RecognizableItemFactory IcicleMesh PopcornMesh WindUpCannonMesh"))

# UI and HUD.
put("UI", "LoadoutHUD", "OctagonGraphic", "UpgradeMeters", "IndustrialHUD", "FluorescentStartup", "PSXInventoryDisplay")

# Development tools that ship with the runtime code (watcher, debug menu, probes and audits).
put("DevTools", *names("WaveWatcher WaveWatcherUI WatcherCatalog DebugRunMenu PSXShowcase HudStartupCapture "
                       "FlightSmoothnessAudit WaveVarietyAudit"))

ENEMY_BASES = {"DistrictEnemy", "OceanEnemy", "InfernalEnemy", "SpaceSatellite", "GroundVehicle"}

def rule(rel, text):
    name = os.path.splitext(os.path.basename(rel))[0]
    if rel.startswith("Assets/Editor/"):
        if name.startswith("Verify"):
            return "Assets/Editor/Checks"
        if name.startswith(("Review", "Render", "Capture", "Profile")) or name == "TunnelProfileExport":
            return "Assets/Editor/Reviews"
        if name.startswith("Install"):
            return "Assets/Editor/Installers"
        return "Assets/Editor/Tools"
    if not rel.startswith(S):
        return None
    sub = rel[len(S):]
    if name in EXPLICIT:
        return S + EXPLICIT[name]
    if name.endswith("Probe"):
        return S + "DevTools/Probes"
    if name.endswith("Airframe") or name.endswith("Airframes"):
        return S + "Visuals/Meshes/Airframes"
    if name.endswith("WaveParts"):
        return S + "Waves/Parts"
    if name.endswith("Choreography"):
        return S + "Waves/Choreography"
    for folder in ("Enemy/Movers", "Enemy/Shooters"):
        if sub.startswith(folder + "/"):
            return S + "Enemies/" + folder.split("/")[1]
    if sub.startswith(("Companions/", "Sound/", "UI/", "Weapons/")):
        top = sub.split("/")[0]
        return S + ("Player/Weapons" if top == "Weapons" else os.path.dirname(sub))
    if sub.startswith("Pickable/"):
        return S + "Pickups"
    if sub.startswith("Items/"):
        if sub.startswith("Items/Expansion/"):
            m = re.search(r"ItemType\.(passive|active|weapon|passiveWeapon|partner)", text)
            kind = m.group(1) if m else "passive"
            return S + "Items/" + {"passive": "Passive", "active": "Active", "weapon": "Weapon",
                                   "passiveWeapon": "PassiveWeapon", "partner": "Companion"}[kind]
        return S + os.path.dirname(sub)
    m = re.search(r"class\s+\w+\s*:\s*(\w+)", text)
    base = m.group(1) if m else ""
    if base == "DistrictBoss":
        return S + "Enemies/Bosses"
    if base == "DistrictMiniboss":
        return S + "Enemies/Minibosses"
    if base in ENEMY_BASES:
        return S + "Enemies/Ships"
    if base == "CombatRound":
        return S + "Projectiles/Rounds"
    return None

files = subprocess.run(["git", "ls-files", "Assets/Scripts/*.cs", "Assets/Editor/*.cs"], cwd=ROOT,
                       capture_output=True, text=True).stdout.split()
moves, unplaced = [], []
for rel in files:
    text = open(os.path.join(ROOT, rel), encoding="utf-8", errors="replace").read()
    target = rule(rel, text)
    if target is None:
        unplaced.append(rel)
        continue
    dst = target + "/" + os.path.basename(rel)
    if dst != rel:
        moves.append((rel, dst))
if unplaced:
    sys.exit("No rule for:\n  " + "\n  ".join(unplaced))
dsts = [d for _, d in moves]
dupes = {d for d in dsts if dsts.count(d) > 1}
if dupes:
    sys.exit("Two files would land on the same path:\n  " + "\n  ".join(sorted(dupes)))
for src, dst in moves:
    if DRY:
        print(src, "->", dst)
        continue
    os.makedirs(os.path.join(ROOT, os.path.dirname(dst)), exist_ok=True)
    subprocess.run(["git", "mv", src, dst], cwd=ROOT, check=True)
    if os.path.exists(os.path.join(ROOT, src + ".meta")):
        subprocess.run(["git", "mv", src + ".meta", dst + ".meta"], cwd=ROOT, check=True)
if DRY:
    print(len(moves), "files would move")
    sys.exit()

# Remove the folders the moves emptied, with their .meta, and give new folders a .meta.
def empty_dirs():
    found = []
    for base in ("Assets/Scripts", "Assets/Editor"):
        for d, _, _ in os.walk(os.path.join(ROOT, base)):
            if os.path.normpath(d) != os.path.normpath(os.path.join(ROOT, base)) and not os.listdir(d):
                found.append(d)
    return found

while True:
    empties = empty_dirs()
    if not empties:
        break
    for d in empties:
        os.rmdir(d)
        if os.path.exists(d + ".meta"):
            subprocess.run(["git", "rm", "-q", d + ".meta"], cwd=ROOT, check=True)
# A folder's .meta can outlive the folder when the folder emptied out in an earlier pass.
for base in ("Assets/Scripts", "Assets/Editor"):
    for d, _, files in os.walk(os.path.join(ROOT, base)):
        for f in files:
            if f.endswith(".meta") and not os.path.exists(os.path.join(d, f[:-5])):
                subprocess.run(["git", "rm", "-q", os.path.join(d, f)], cwd=ROOT, check=True)
FOLDER_META = ("fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
               "  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
for base in ("Assets/Scripts", "Assets/Editor"):
    for d, _, _ in os.walk(os.path.join(ROOT, base)):
        if os.path.normpath(d) != os.path.normpath(os.path.join(ROOT, base)) and not os.path.exists(d + ".meta"):
            with open(d + ".meta", "w", newline="\n") as f:
                f.write(FOLDER_META % uuid.uuid4().hex)
            subprocess.run(["git", "add", d + ".meta"], cwd=ROOT, check=True)

# Point documentation and tools at the new paths.
docs = subprocess.run(["git", "grep", "-lE", "Assets/(Scripts|Editor)/"], cwd=ROOT, capture_output=True, text=True).stdout.split()
for doc in docs:
    if doc.endswith((".cs", ".meta", ".unity", ".prefab", ".asset")) or doc == "Tools/regroup_scripts.py":
        continue
    path = os.path.join(ROOT, doc)
    text = open(path, encoding="utf-8", errors="surrogateescape").read()
    new = text.replace("Assets/Scripts/Enemy/Movers", "Assets/Scripts/Enemies/Movers")
    for src, dst in moves:
        new = new.replace(src, dst)
    if new != text:
        open(path, "w", encoding="utf-8", errors="surrogateescape", newline="").write(new)
print(len(moves), "files moved")
