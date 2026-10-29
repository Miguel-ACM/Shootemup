"""Every enemy, miniboss and boss hull for make_prompts.py."""

M = "Assets/Scripts/Visuals/Meshes"


def fn(name, where=M):
    return f"function {name} (search the C# files under {where} for `{name}(`)"


def register(add):
    # Plains
    add(0, "ScoutInterceptor", "Fields", "small agile scout interceptor, nose-aimed, the first enemy the player meets", fn("BuildScoutAirframe"))
    add(1, "PatrolGunship", "Fields", "light patrol gunship", fn("BuildPatrolAirframe"))
    add(2, "HeavyBomber", "Fields", "heavy bomber, broad wings, drops bombs that stop and burst", fn("BuildBomberAirframe"))
    add(4, "MaintenanceDrone", "Fields", "small maintenance drone", fn("BuildMaintenanceAirframe"))
    add(5, "ArmoredCarrier", "Fields", "armored carrier (ScatterCarrier family) that releases needles horizontally; keep its orange accents", fn("BuildCarrierAirframe"))
    add(39, "MeadowTank", "Fields", "tracked tank chassis driving on the valley road; ONLY the chassis and tracks, the turret is a separate part in code (keep the turret ring mount on top center). Ground vehicle: no wings, no engines", fn("BuildMeadowTank"))
    add(61, "ReedSkimmer", "Fields", "low reed-skimming hover gunship", "function in Assets/Scripts/Enemies/Ships/ReedSkimmer.cs (the mesh function at the bottom)")
    add(62, "HarrowBomber", "Fields", "harrow bomber that sows bomb lines", "function in Assets/Scripts/Enemies/Ships/HarrowBomber.cs")
    add(23, "FieldsBomberBoss", "Fields", "the Plains boss: a huge flying fortress bomber with wing spools and payload bays", fn("BuildFieldsFortressAirframe"), "boss")
    add(24, "FieldsHeavyWing", "Fields", "Plains miniboss: a heavy-wing gunship", fn("BuildHeavyWingAirframe"), "mini")
    # Snow
    add(6, "ColdWeatherFighter", "Snow", "cold-weather fighter", fn("BuildSnowFighterAirframe"))
    add(7, "SiegeGunship", "Snow", "siege gunship firing big exploding shells", fn("BuildSnowSiegeAirframe"))
    add(17, "AvalancheBomber", "Snow", "avalanche bomber that drops shells", fn("BuildAvalancheAirframe"))
    add(40, "FrostAuger", "Snow", "twin-auger ground siege crawler; ground vehicle with two big drill augers, terrain contact, no wings or engines; any turret parts in code stay separate", fn("BuildFrostAuger"))
    add(63, "IcicleWeaver", "Snow", "icicle weaver craft", "function in Assets/Scripts/Enemies/Ships/IcicleWeaver.cs")
    add(64, "HailLantern", "Snow", "hail lantern, a lantern-like hovering emitter", "function in Assets/Scripts/Enemies/Ships/HailLantern.cs")
    add(21, "SnowSiegeBoss", "Snow", "the Snow boss: a very round boss that must look like a designed spacecraft, NOT a plate covered with buttons; curved shutter petals around a circular pressure hull", fn("BuildSnowCitadelAirframe"), "boss")
    add(26, "SnowSiegeCraft", "Snow", "Snow miniboss: mortar siege craft", fn("BuildSnowMortarAirframe"), "mini")
    # City
    add(12, "PursuitFighter", "City", "fast pursuit fighter; also the City duo boss's fast ship", fn("BuildPursuitAirframe"))
    add(13, "AttackDrone", "City", "small fast attack drone", fn("BuildAttackDroneAirframe"))
    add(14, "CommandGunship", "City", "heavy command gunship; also the City duo boss's heavy ship", fn("BuildCommandAirframe"))
    add(15, "CourierDiver", "City", "courier diver, quick diving craft", fn("BuildCourierAirframe"))
    add(65, "MetroLancer", "City", "metro lancer", "function in Assets/Scripts/Enemies/Ships/MetroLancer.cs")
    add(66, "TerraceGunboat", "City", "terrace gunboat", "function in Assets/Scripts/Enemies/Ships/TerraceGunboat.cs")
    add(28, "CityCommandCarrier", "City", "City miniboss: command ark carrier", fn("BuildCommandArkAirframe"), "mini")
    # Tunnel
    add(3, "WallTurret", "Tunnel", "wall-mounted battery firing ricochets", fn("BuildWallTurretAirframe"))
    add(8, "ShieldTransport", "Tunnel", "shield transport", fn("BuildShieldAirframe"))
    add(67, "BaffleSpinner", "Tunnel", "baffle spinner", "function in Assets/Scripts/Enemies/Ships/BaffleSpinner.cs")
    add(68, "RivetMantis", "Tunnel", "rivet mantis", "function in Assets/Scripts/Enemies/Ships/RivetMantis.cs")
    add(20, "TunnelSawBoss", "Tunnel", "the Tunnel boss: a rapidly spinning saw blade machine that bounces around the arena; must read well while spinning; radially symmetric teeth; no thrusters", fn("BuildTunnelSawAirframe"), "boss")
    add(25, "TunnelDefensePlatform", "Tunnel", "Tunnel miniboss: bastion defense platform", fn("BuildTunnelBastionAirframe"), "mini")
    # Thunder
    add(9, "BeamDrone", "Thunder", "beam drone with an attached downward laser emitter", fn("BuildBeamDroneAirframe"))
    add(10, "RelayDrone", "Thunder", "relay drone", fn("BuildRelayAirframe"))
    add(11, "StormSkimmer", "Thunder", "storm skimmer", fn("BuildStormSkimmerAirframe"))
    add(18, "ArcLancer", "Thunder", "arc lancer", fn("BuildArcLancerAirframe"))
    add(69, "CapacitorKite", "Thunder", "capacitor kite", "function in Assets/Scripts/Enemies/Ships/CapacitorKite.cs")
    add(70, "StormOrgan", "Thunder", "storm organ, pipe-organ capacitor bank", "function in Assets/Scripts/Enemies/Ships/StormOrgan.cs")
    add(22, "ThunderLanceBoss", "Thunder", "the Thunder boss: a long needle lance ship with charged capacitor paddles and downward beam emitters", fn("BuildThunderNeedleAirframe"), "boss")
    add(27, "StormReactor", "Thunder", "Thunder miniboss: storm reactor", fn("BuildStormReactorAirframe"), "mini")
    # Desert
    add(30, "SandEmitter", "Desert", "buried sand eruption turret that rises from the sand and fires rings; only the part above the sand matters", fn("BuildSandEmitter"))
    add(31, "SandStriker", "Desert", "desert burst interceptor firing tight aimed bursts", fn("BuildSandStriker"))
    add(32, "SandWorm", "Desert", "head of a small articulated mechanical sandworm (body segments are separate)", fn("BuildSandWormHead"))
    add(71, "DuneMoth", "Desert", "dune moth", "function in Assets/Scripts/Enemies/Ships/DuneMoth.cs")
    add(72, "MirageObelisk", "Desert", "mirage obelisk", "function in Assets/Scripts/Enemies/Ships/MirageObelisk.cs")
    add(33, "DesertWormBoss", "Desert", "the Desert boss head: a giant white mechanical sandworm's armored jaws with a larger red head weakpoint (muted brick red); body segments are separate", fn("BuildWhiteWormHead"), "boss")
    add(56, "ScarabDredger", "Desert", "Desert miniboss: scarab dredger, a mechanical scarab beetle", "the kind == 56 branch of BuildDistrictMini in " + M + "/MinibossMeshes.cs", "mini")
    # Candyland
    add(35, "BonbonSkirmisher", "Candyland", "wrapped bonbon skirmisher", fn("BuildBonbon"), palette="candy")
    add(36, "WaferDart", "Candyland", "chocolate wafer dart, nose-aimed", fn("BuildWaferDart"), palette="candy")
    add(37, "MintSpinner", "Candyland", "peppermint spindle that spins", fn("BuildMintSpinner"), palette="candy")
    add(73, "RibbonGlider", "Candyland", "ribbon glider", "function in Assets/Scripts/Enemies/Ships/RibbonGlider.cs", palette="candy")
    add(74, "GumdropMortar", "Candyland", "gumdrop mortar", "function in Assets/Scripts/Enemies/Ships/GumdropMortar.cs", palette="candy")
    add(38, "CandyRegentBoss", "Candyland", "the Candyland boss: a very fast candy regent with launch wings", fn("BuildCandyRegent"), "boss", palette="candy")
    add(57, "TaffyDuelist", "Candyland", "Candyland miniboss: taffy duelist", "the kind == 57 branch of BuildDistrictMini in " + M + "/MinibossMeshes.cs", "mini", palette="candy")
    # Space
    add(41, "OrbitalScout", "Space", "solar-panel satellite scout", fn("BuildOrbitalScout"), palette="space")
    add(42, "LaserSatellite", "Space", "parabolic-dish laser satellite with an attached charged laser", fn("BuildLaserSatellite"), palette="space")
    add(43, "OrbitalCarrier", "Space", "four-array orbital carrier with orbiting escorts", fn("BuildOrbitalCarrier"), palette="space")
    add(44, "OrbitalMoonlet", "Space", "vertical-fin escort moonlet that orbits carriers", fn("BuildOrbitalMoonlet"), palette="space")
    add(75, "EclipseSail", "Space", "eclipse solar sail craft", "function in Assets/Scripts/Enemies/Ships/EclipseSail.cs", palette="space")
    add(76, "CometTug", "Space", "comet tug", "function in Assets/Scripts/Enemies/Ships/CometTug.cs", palette="space")
    add(45, "SpaceCitadelBoss", "Space", "the Space boss: an open-truss orbital station with a central reactor core; shield generators and turrets are separate parts in code", fn("BuildSpaceCitadel"), "boss", palette="space")
    add(60, "OrbitalWarden", "Space", "Space miniboss: orbital warden", "the kind == 60 branch of BuildDistrictMini in " + M + "/MinibossMeshes.cs", "mini", palette="space")
    # Hell
    add(46, "SkullRevenant", "Hell", "horned skull apparition interceptor", fn("BuildSkull"))
    add(47, "GhostGalleon", "Hell", "spectral broadside galleon with gun ports along both sides", fn("BuildGhostShip"))
    add(48, "CinderImp", "Hell", "cinder bat imp", fn("BuildCinderImp"))
    add(79, "AshCenser", "Hell", "ash censer", "function in Assets/Scripts/Enemies/Ships/AshCenser.cs")
    add(80, "BoneHarrier", "Hell", "bone harrier", "function in Assets/Scripts/Enemies/Ships/BoneHarrier.cs")
    add(49, "HellCrabBoss", "Hell", "the Hell boss: a giant crab body with layered carapace plates; the claws (hull 54) and eyes (hull 55) are separate parts in code, keep their sockets as recesses", fn("BuildHellCrab"), "boss")
    add(54, "HellCrabClaw", "Hell", "the crab boss's articulated pincer (large, 1.75 scale in game); a single claw part", fn("BuildHellClaw"), "mini")
    add(59, "InfernalFerryman", "Hell", "Hell miniboss: infernal ferryman", "the kind == 59 branch of BuildDistrictMini in " + M + "/MinibossMeshes.cs", "mini")
