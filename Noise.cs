// This code defines settings for generating noise and provides interfaces and structs for noise generation using Burst.
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using System;
using Unity.Mathematics;
using UnityEngine;

// Importing static math functions for easier access.
using static Unity.Mathematics.math;

// Declaring a partial class Noise.
public static partial class Noise {
    // Serializable struct for noise generation settings.
    [Serializable]
    public struct Settings {
        public int seed; // Seed for random number generation.
        [Min(1)] public int frequency; // Frequency of noise.
        [Range(1, 6)] public int octaves; // Number of octaves for noise generation.
        [Range(2, 4)] public int lacunarity; // Lacunarity parameter for noise generation.
        [Range(0f, 1f)] public float persistence; // Persistence parameter for noise generation.
        public static Settings Default = new Settings {
            frequency = 4,
            octaves = 1,
            lacunarity = 2,
            persistence = 0.5f
        }; // Default noise generation settings.
    }

    // Interface for noise generation.
    public interface INoise {
        float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency); // Generates noise at a given position.
    }

    // Burst compiled job struct for parallel noise generation.
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<N> : IJobFor where N : struct, INoise {
        [ReadOnly] public NativeArray<float3x4> positions; // Input positions.
        [WriteOnly] public NativeArray<float4> noise; // Output noise values.
        public Settings settings; // Noise generation settings.
        public float3x4 domainTRS; // Transformation matrix for the noise domain.

        // Executes noise generation job for a single index.
        public void Execute(int i) {
            // Transform position to domain space.
            float4x3 position = domainTRS.TransformVectors(transpose(positions[i]));
            // Initialize hash with the given seed.
            var hash = SmallXXHash4.Seed(settings.seed);
            int frequency = settings.frequency;
            float amplitude = 1f, amplitudeSum = 0f;
            float4 sum = 0f;

            // Iterate through octaves for noise generation.
            for (int o = 0; o < settings.octaves; o++) {
                // Accumulate noise with varying amplitude and frequency.
                sum += amplitude * default(N).GetNoise4(position, hash + o, frequency);
                amplitudeSum += amplitude;
                frequency *= settings.lacunarity;
                amplitude *= settings.persistence;
            }

            // Normalize and store the noise value for the current index.
            noise[i] = sum / amplitudeSum;
        }

        // Schedules parallel noise generation job.
        public static JobHandle ScheduleParallel(
            NativeArray<float3x4> positions, NativeArray<float4> noise,
            Settings settings, SpaceTRS domainTRS, int resolution, JobHandle dependency
        ) => new Job<N> {
            positions = positions,
            noise = noise,
            settings = settings,
            domainTRS = domainTRS.Matrix,
        }.ScheduleParallel(positions.Length, resolution, dependency);
    }

    // Delegate type for scheduling noise generation job.
    public delegate JobHandle ScheduleDelegate(
        NativeArray<float3x4> positions, NativeArray<float4> noise,
        Settings settings, SpaceTRS domainTRS, int resolution, JobHandle dependency
    );
}
