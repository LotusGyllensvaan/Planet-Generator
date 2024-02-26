// Import necessary Unity packages and libraries
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;

// Static class for generating shapes
public static class Shapes {

    // Struct representing a set of 4 points in 3D space
    public struct Point4 {
        public float4x3 positions, normals;
    }

    // Interface for defining a shape
    public interface IShape {
        Point4 GetPoint4(int i, float resolution, float invResolution);
    }

    // Struct representing a plane shape
    public struct Plane : IShape {

        // Method to generate points for a plane shape
        public Point4 GetPoint4(int i, float resolution, float invResolution) {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution);
            return new Point4 {
                positions = float4x3(uv.c0 - 0.5f, 0f, uv.c1 - 0.5f),
                normals = float4x3(0f, 1f, 0f)
            };
        }
    }

    // Struct representing a sphere shape
    public struct Sphere : IShape {

        // Method to generate points for a sphere shape
        public Point4 GetPoint4(int i, float resolution, float invResolution) {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution);

            Point4 p;
            p.positions.c0 = uv.c0 - 0.5f;
            p.positions.c1 = uv.c1 - 0.5f;
            p.positions.c2 = 0.5f - abs(p.positions.c0) - abs(p.positions.c1);
            float4 offset = max(-p.positions.c2, 0f);
            p.positions.c0 += select(-offset, offset, p.positions.c0 < 0f);
            p.positions.c1 += select(-offset, offset, p.positions.c1 < 0f);
            float4 scale = 0.5f * rsqrt(
                p.positions.c0 * p.positions.c0 +
                p.positions.c1 * p.positions.c1 +
                p.positions.c2 * p.positions.c2
            );
            p.positions.c0 *= scale;
            p.positions.c1 *= scale;
            p.positions.c2 *= scale;
            p.normals = p.positions;
            return p;
        }
    }

    // Struct representing a torus shape
    public struct Torus : IShape {

        // Method to generate points for a torus shape
        public Point4 GetPoint4(int i, float resolution, float invResolution) {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution);

            float r1 = 0.375f;
            float r2 = 0.125f;
            float4 s = r1 + r2 * cos(2f * PI * uv.c1);

            Point4 p;
            p.positions.c0 = s * sin(2f * PI * uv.c0);
            p.positions.c1 = r2 * sin(2f * PI * uv.c1);
            p.positions.c2 = s * cos(2f * PI * uv.c0);
            p.normals = p.positions;
            p.normals.c0 -= r1 * sin(2f * PI * uv.c0);
            p.normals.c2 -= r1 * cos(2f * PI * uv.c0);
            return p;
        }
    }

    // Delegate for scheduling jobs
    public delegate JobHandle ScheduleDelegate(
        NativeArray<float3x4> positions, NativeArray<float3x4> normals,
        int resolution, float4x4 trs, JobHandle dependency
    );

    // Method to convert index to UV coordinates for a shape
    public static float4x2 IndexTo4UV(int i, float resolution, float invResolution) {
        float4x2 uv;
        float4 i4 = 4f * i + float4(0, 1, 2, 3);
        uv.c1 = floor(invResolution * i4 + 0.00001f);
        uv.c0 = invResolution * (i4 - resolution * uv.c1 + 0.5f);
        uv.c1 = invResolution * (uv.c1 + 0.5f);
        return uv;
    }

    // Job struct for generating shape points in parallel
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<S> : IJobFor where S : struct, IShape {

        [WriteOnly]
        NativeArray<float3x4> positions, normals;

        public float3x4 positionTRS, normalTRS;

        public float resolution, invResolution;

        // Method to execute job for generating shape points
        public void Execute(int i) {

            Point4 p = default(S).GetPoint4(i, resolution, invResolution);

            positions[i] =
                transpose(positionTRS.TransformVectors(p.positions));
            float3x4 n =
                transpose(normalTRS.TransformVectors(p.normals, 0f));
            normals[i] = float3x4(
                    normalize(n.c0), normalize(n.c1), normalize(n.c2), normalize(n.c3)
                );
        }

        // Static method to schedule the job in parallel
        public static JobHandle ScheduleParallel(
            NativeArray<float3x4> positions, NativeArray<float3x4> normals, int resolution,
            float4x4 trs, JobHandle dependency
        ) => new Job<S> {
            positions = positions,
            normals = normals,
            resolution = resolution,
            invResolution = 1f / resolution,
            positionTRS = trs.Get3x4(),
            normalTRS = transpose(inverse(trs)).Get3x4()
        }.ScheduleParallel(positions.Length, resolution, dependency);
    }
}
