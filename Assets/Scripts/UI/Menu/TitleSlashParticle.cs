using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ParticleSystem))]
public sealed class TitleSlashParticle : MaskableGraphic
{
    const float Lifetime = .29f;
    readonly ParticleSystem.Particle[] particles = new ParticleSystem.Particle[1];
    ParticleSystem emitter;
    bool drawing;

    public bool HasParticle => emitter != null && emitter.particleCount > 0;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
        emitter = GetComponent<ParticleSystem>();
        emitter.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = emitter.main;
        main.playOnAwake = false;
        main.loop = true;
        main.useUnscaledTime = true;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 1;
        main.startLifetime = Lifetime;
        main.startSpeed = 0;
        main.startSize3D = true;
        main.startSizeX = 300;
        main.startSizeY = 36;
        var emission = emitter.emission;
        emission.enabled = false;
        var shape = emitter.shape;
        shape.enabled = false;

        var size = emitter.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = true;
        size.x = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, .33f, 1.67f));
        size.y = 1;
        var fade = emitter.colorOverLifetime;
        fade.enabled = true;
        fade.color = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.82f, .36f), new GradientAlphaKey(0, 1) }
        };

        // The screen-space overlay draws the particle's untextured mesh above the letters.
        GetComponent<ParticleSystemRenderer>().enabled = false;
        // Make the native emitter allocate its particle while the title is still black.
        emitter.Play(false);
        emitter.Emit(1);
        emitter.Clear(false);
    }

    public void Emit(Vector2 position, Vector2 travel)
    {
        emitter.Emit(new ParticleSystem.EmitParams { position = position, velocity = travel / Lifetime }, 1);
        drawing = true;
        SetVerticesDirty();
    }

    void LateUpdate()
    {
        if (!drawing)
            return;
        drawing = HasParticle;
        if (!drawing)
            emitter.Stop(false);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (!HasParticle || emitter.GetParticles(particles) == 0)
            return;

        var particle = particles[0];
        Vector2 centre = particle.position;
        Vector2 direction = ((Vector2)particle.velocity).normalized;
        Vector3 size = particle.GetCurrentSize3D(emitter);
        Vector2 axis = direction * (size.x * .5f);
        Vector2 width = Vector2.Perpendicular(direction) * (size.y * .5f);
        Color light = particle.GetCurrentColor(emitter);
        Color rim = new Color(.8f, .9f, 1, light.a * .36f);
        Streak(mesh, centre, axis, width, rim);
        Streak(mesh, centre, axis * .94f, width * .38f, light);
    }

    static void Streak(VertexHelper mesh, Vector2 centre, Vector2 axis, Vector2 width, Color light)
    {
        int first = mesh.currentVertCount;
        var tip = light;
        tip.a *= .15f;
        mesh.AddVert(centre - axis, tip, Vector2.zero);
        mesh.AddVert(centre - width, light, Vector2.zero);
        mesh.AddVert(centre + axis, tip, Vector2.zero);
        mesh.AddVert(centre + width, light, Vector2.zero);
        mesh.AddTriangle(first, first + 1, first + 2);
        mesh.AddTriangle(first, first + 2, first + 3);
    }
}
