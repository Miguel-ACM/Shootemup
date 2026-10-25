#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyEnemyFlight
{
    const string Folder = "ArtSource/CombatReview/EnemyFlight";
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        float speed = GameRules.enemySpeed;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameRules.enemySpeed = 1;
            VerifyMovementPatterns.Run();
            Vector3 baseline = Trial(60);
            Check(Vector3.Distance(baseline, Trial(30)) < .12f, "Tracker differs at 30 FPS");
            Check(Vector3.Distance(baseline, Trial(144)) < .12f, "Tracker differs at 144 FPS");
            Render();
            File.WriteAllText(Folder + "/Verification.txt", "PASS: movement regression suite; tracker acceleration, braking, reversal, aim lock and 30/60/144 FPS agreement; real fleet motion captures.\n");
        }
        finally { GameRules.enemySpeed = speed; }
    }

    static Vector3 Trial(int fps)
    {
        var player = new GameObject("Flight test target"); player.tag = "Player";
        var go = new GameObject("Flight test tracker");
        go.transform.position = new Vector3(0, Camera.UpLimit - 2, 7);
        var mover = go.AddComponent<TopTracker>();
        Vector2 previous = Vector2.zero;
        bool moved = false, braked = false, fired = false;
        try
        {
            for (int i = 0; i < fps * 8; i++)
            {
                player.transform.position = new Vector3(i < fps ? 4 : -4, -6, 0);
                mover.Step(1f / fps);
                var velocity = mover.Velocity;
                float gameDt = PatternMover.FlightTempo * LevelRun.EnemyTempoScale / fps;
                Check((velocity - previous).magnitude <= 14.01f * gameDt, "Tracker snapped its velocity");
                if (velocity.magnitude > 1) moved = true;
                if (moved && velocity.magnitude < .05f) braked = true;
                if (mover.CanFire)
                {
                    fired = true;
                    Check(velocity.magnitude < .01f, "Tracker fires before stopping");
                    Check(mover.LockedAim.HasValue, "Tracker lost committed aim");
                }
                previous = velocity;
            }
            Check(moved && braked && fired, "Tracker failed a movement phase");
            Vector3 held = go.transform.position;
            mover.SetMove(false); mover.Step(1);
            Check(go.transform.position == held, "Tracker moves while paused");
            return held;
        }
        finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(player); }
    }

    static void Render()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(960, 720, 24);
        var pixels = new Texture2D(960, 720, TextureFormat.RGB24, false);
        var player = new GameObject("Flight review target"); player.tag = "Player";
        var roots = new GameObject[3];
        try
        {
            var cameraGo = new GameObject("Flight review camera"); SceneManager.MoveGameObjectToScene(cameraGo, scene);
            var camera = cameraGo.AddComponent<UnityEngine.Camera>(); camera.scene = scene;
            camera.transform.position = new Vector3(0, 0, -20); camera.orthographic = true; camera.orthographicSize = 6;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .035f, .05f);
            camera.targetTexture = target;
            var movers = new PatternMover[3]; var models = new Transform[3];
            var attitudes = new[] { new EnemyFlightAttitude(), new EnemyFlightAttitude(), new EnemyFlightAttitude() };
            var kinds = new[] { 12, 14, 0 }; var scales = new[] { .45f, .8f, .48f };
            for (int i = 0; i < 3; i++)
            {
                var type = i == 0 ? typeof(PursuitFighter) : i == 1 ? typeof(CommandGunship) : typeof(ScoutInterceptor);
                var enemy = DistrictEnemy.Spawn(type, new Vector3(0, Camera.UpLimit - 2, 0));
                roots[i] = enemy.gameObject;
                if (i == 2)
                {
                    var old = roots[i].GetComponent<PatternMover>(); UnityEngine.Object.DestroyImmediate(old);
                    var flight = roots[i].AddComponent<FormationFlight>(); flight.phrase = FlightPhrase.Ribbon; flight.duration = 9;
                    movers[i] = flight;
                }
                else movers[i] = roots[i].GetComponent<TopTracker>();
                var model = new GameObject("Rendered flight " + kinds[i]); SceneManager.MoveGameObjectToScene(model, scene);
                model.transform.localScale = Vector3.one * scales[i];
                model.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CombatHull(kinds[i]);
                model.AddComponent<MeshRenderer>().sharedMaterial = CombatMaterials.Fleet; models[i] = model.transform;
            }
            for (int frame = 0; frame < 600; frame++)
            {
                float t = frame / 60f;
                player.transform.position = new Vector3(t % 4 < 2 ? 3.8f : -3.8f, -6, 0);
                for (int i = 0; i < 3; i++)
                {
                    movers[i].Step(1f / 60);
                    var p = roots[i].transform.position;
                    float heading = i == 2 ? Vector2.SignedAngle(Vector2.down, ((FormationFlight)movers[i]).Tangent) : 0;
                    models[i].localRotation = attitudes[i].Step(p, heading, Mathf.InverseLerp(.45f, 1.1f, scales[i]), 1f / 60);
                    models[i].position = new Vector3(p.x, i == 0 ? 3.7f : i == 1 ? .2f : -3.6f + (p.y - 3) * .22f, 0);
                }
                if (frame % 5 != 0) continue;
                camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 960, 720), 0, 0); pixels.Apply(); RenderTexture.active = previous;
                File.WriteAllBytes(Folder + "/" + (frame / 5).ToString("D4") + ".png", pixels.EncodeToPNG());
            }
        }
        finally
        {
            foreach (var root in roots) if (root != null) UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(player); UnityEngine.Object.DestroyImmediate(pixels);
            target.Release(); UnityEngine.Object.DestroyImmediate(target); EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
#endif
