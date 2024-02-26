// This code defines interfaces and structs for gradient noise generation.

// Importing Unity.Mathematics namespace for math functions.
using Unity.Mathematics;

// Importing static math functions for easier access.
using static Unity.Mathematics.math;

// Declaring a partial class Noise.
public static partial class Noise {
    // Declaring an interface for gradient noise.
    public interface IGradient {
        float4 Evaluate(SmallXXHash4 hash, float4 x); // Evaluates gradient noise at a single point.
        float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y); // Evaluates gradient noise at a 2D point.
        float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z); // Evaluates gradient noise at a 3D point.
        float4 EvaluateAfterInterpolation(float4 value); // Applies interpolation after noise evaluation.
    }

    // Implementation of value noise.
    public struct Value : IGradient {
        // Evaluates value noise at a single point.
        public float4 Evaluate(SmallXXHash4 hash, float4 x) => hash.Floats01A * 2f - 1f;

        // Evaluates value noise at a 2D point.
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y) => hash.Floats01A * 2f - 1f;

        // Evaluates value noise at a 3D point.
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z) => hash.Floats01A * 2f - 1f;

        // Applies no interpolation after noise evaluation.
        public float4 EvaluateAfterInterpolation(float4 value) => value;
    }

    // Implementation of Perlin noise.
    public struct Perlin : IGradient {
        // Evaluates Perlin noise at a single point.
        public float4 Evaluate(SmallXXHash4 hash, float4 x) =>
            (1f + hash.Floats01A) * select(-x, x, ((uint4)hash & 1 << 8) == 0);

        // Evaluates Perlin noise at a 2D point.
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y) {
            float4 gx = hash.Floats01A * 2f - 1f;
            float4 gy = 0.5f - abs(gx);
            gx -= floor(gx + 0.5f);
            return (gx * x + gy * y) * (2f / 0.53528f);
        }

        // Evaluates Perlin noise at a 3D point.
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z) {
            float4 gx = hash.Floats01A * 2f - 1f, gy = hash.Floats01D * 2f - 1f;
            float4 gz = 1f - abs(gx) - abs(gy);
            float4 offset = max(-gz, 0f);
            gx += select(-offset, offset, gx < 0f);
            gy += select(-offset, offset, gy < 0f);
            return (gx * x + gy * y + gz * z) * (1f / 0.56290f);
        }

        // Applies no interpolation after noise evaluation.
        public float4 EvaluateAfterInterpolation(float4 value) => value;
    }

    // Implementation of turbulence noise.
    public struct Turbulence<G> : IGradient where G : struct, IGradient {
        // Evaluates turbulence noise at a single point.
        public float4 Evaluate(SmallXXHash4 hash, float4 x) =>
            default(G).Evaluate(hash, x);

        // Evaluates turbulence noise at a 2D point.
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y) =>
            default(G).Evaluate(hash, x, y);

        // Evaluates turbulence noise at a 3D point.
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z) =>
            default(G).Evaluate(hash, x, y, z);

        // Applies absolute value to the result of interpolation.
        public float4 EvaluateAfterInterpolation(float4 value) =>
            abs(default(G).EvaluateAfterInterpolation(value));
    }
}
