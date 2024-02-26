// This script defines a NoiseVisualization class that inherits from the Visualization abstract class.
// It visualizes different types of noise using compute shaders and instanced rendering.

using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

// Importing the Noise namespace to access noise-related functions.
using static Noise;

public class NoiseVisualization : Visualization {
    // Shader property ID for noise texture.
    static int noiseId = Shader.PropertyToID("_Noise");

    // Whether to use tiling for noise generation.
    [SerializeField]
    bool tiling;

    // Noise settings (default settings provided by the Settings struct).
    [SerializeField]
    Settings noiseSettings = Settings.Default;

    // Domain settings for noise generation (default scale set to 8).
    [SerializeField]
    SpaceTRS domain = new SpaceTRS {
        scale = 8f
    };

    // Native array to store the generated noise.
    NativeArray<float4> noise;

    // Compute buffer for noise data.
    ComputeBuffer noiseBuffer;

    // 2D array of delegates representing different noise generation jobs.
    static ScheduleDelegate[,] noiseJobs = {
        // Each row represents a different noise type, and each column represents a different dimensionality.
        // Jobs are scheduled to run in parallel.
        {
            Job<Lattice1D<LatticeNormal, Perlin>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Perlin>>.ScheduleParallel,
            Job<Lattice2D<LatticeNormal, Perlin>>.ScheduleParallel,
            Job<Lattice2D<LatticeTiling, Perlin>>.ScheduleParallel,
            Job<Lattice3D<LatticeNormal, Perlin>>.ScheduleParallel,
            Job<Lattice3D<LatticeTiling, Perlin>>.ScheduleParallel
        },
        {
            Job<Lattice1D<LatticeNormal, Turbulence<Perlin>>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Turbulence<Perlin>>>.ScheduleParallel,
            Job<Lattice2D<LatticeNormal, Turbulence<Perlin>>>.ScheduleParallel,
            Job<Lattice2D<LatticeTiling, Turbulence<Perlin>>>.ScheduleParallel,
            Job<Lattice3D<LatticeNormal, Turbulence<Perlin>>>.ScheduleParallel,
            Job<Lattice3D<LatticeTiling, Turbulence<Perlin>>>.ScheduleParallel
        },
        {
            Job<Lattice1D<LatticeNormal, Value>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Value>>.ScheduleParallel,
            Job<Lattice2D<LatticeNormal, Value>>.ScheduleParallel,
            Job<Lattice2D<LatticeTiling, Value>>.ScheduleParallel,
            Job<Lattice3D<LatticeNormal, Value>>.ScheduleParallel,
            Job<Lattice3D<LatticeTiling, Value>>.ScheduleParallel,
        },
        {
            Job<Lattice1D<LatticeNormal, Turbulence<Value>>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Turbulence<Value>>>.ScheduleParallel,
            Job<Lattice2D<LatticeNormal, Turbulence<Value>>>.ScheduleParallel,
            Job<Lattice2D<LatticeTiling, Turbulence<Value>>>.ScheduleParallel,
            Job<Lattice3D<LatticeNormal, Turbulence<Value>>>.ScheduleParallel,
            Job<Lattice3D<LatticeTiling, Turbulence<Value>>>.ScheduleParallel,
        }
    };

    // Array of delegates representing shape generation jobs (plane, sphere, torus).
    static Shapes.ScheduleDelegate[] shapeJobs = {
        Shapes.Job<Shapes.Plane>.ScheduleParallel,
        Shapes.Job<Shapes.Sphere>.ScheduleParallel,
        Shapes.Job<Shapes.Torus>.ScheduleParallel
    };

    // Number of dimensions for noise generation (1, 2, or 3).
    [SerializeField, Range(1, 3)]
    int dimensions = 3;

    // Enumeration representing different types of noise.
    public enum NoiseType { Perlin, PerlinTurbulence, Value, ValueTurbulence }

    // Selected type of noise for visualization.
    [SerializeField]
    NoiseType type;

    // Method called when enabling visualization.
    //dataLength = amount of data
    //propertBlock = property block to set shader
    protected override void EnableVisualization(
        int dataLength, MaterialPropertyBlock propertyBlock
    ) {
        // Allocate memory for the noise data and create a compute buffer.
        noise = new NativeArray<float4>(dataLength, Allocator.Persistent);
        noiseBuffer = new ComputeBuffer(dataLength * 4, 4);

        // Set the noise buffer in the material property block.
        propertyBlock.SetBuffer(noiseId, noiseBuffer);
    }

    // Method called when disabling visualization.
    protected override void DisableVisualization() {
        // Release allocated memory and dispose of the compute buffer.
        noise.Dispose();
        noiseBuffer.Release();
        noiseBuffer = null;
    }

    // Method called to update the visualization.
    protected override void UpdateVisualization(
        NativeArray<float3x4> positions, int resolution, JobHandle handle
    ) {
        // Select the appropriate noise generation job based on the noise type and dimensions.
        noiseJobs[(int)type, 2 * dimensions - (tiling ? 1 : 2)](
            positions, noise, noiseSettings, domain, resolution, handle
        ).Complete();

        // Set the noise data to the compute buffer.
        noiseBuffer.SetData(noise.Reinterpret<float4>(4 * 4));
    }
}

