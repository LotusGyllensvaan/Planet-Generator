// Import Unity.Mathematics library
using Unity.Mathematics;

// Import static members of Unity.Mathematics.math
using static Unity.Mathematics.math;

// Partial class for Noise generation
public static partial class Noise {

    // Struct defining a lattice span in 4D
    public struct LatticeSpan4 {
        public int4 p0, p1; // Lattice points
        public float4 g0, g1; // Gradients at lattice points
        public float4 t; // Interpolation factors
    }

    // Interface for lattice
    public interface ILattice {
        LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency);
    }

    // Struct defining lattice using normal method
    public struct LatticeNormal : ILattice {
        public LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency) {
            // Scale coordinates
            coordinates *= frequency;
            // Find lattice points
            float4 points = floor(coordinates);
            LatticeSpan4 span;
            span.p0 = (int4)points;
            span.p1 = span.p0 + 1;
            span.t = coordinates - points;
            span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
            span.g0 = coordinates - span.p0;
            span.g1 = span.g0 - 1f;
            return span;
        }
    }

    // Struct defining lattice using tiling method
    public struct LatticeTiling : ILattice {
        public LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency) {
            // Scale coordinates
            coordinates *= frequency;
            // Find lattice points
            float4 points = floor(coordinates);
            LatticeSpan4 span;
            span.p0 = (int4)points;
            span.g0 = coordinates - span.p0;
            span.g1 = span.g0 - 1f;

            // Apply tiling
            span.p0 -= (int4)ceil(points / frequency) * frequency;
            span.p0 = select(span.p0, span.p0 + frequency, span.p0 < 0);
            span.p1 = span.p0 + 1;
            span.p1 = select(span.p1, 0, span.p1 == frequency);

            span.t = coordinates - points;
            span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
            return span;
        }
    }

    // Struct defining 1D lattice with gradient
    public struct Lattice1D<L, G> : INoise
        where L : struct, ILattice where G : struct, IGradient {

        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency) {
            LatticeSpan4 x = default(L).GetLatticeSpan4(positions.c0, frequency);
            var g = default(G);
            //linear interoplation in one dimension
            return g.EvaluateAfterInterpolation(lerp(
                g.Evaluate(hash.Eat(x.p0), x.g0), g.Evaluate(hash.Eat(x.p1), x.g1), x.t));
        }
    }

    // Struct defining 2D lattice with gradient
    public struct Lattice2D<L, G> : INoise
        where L : struct, ILattice where G : struct, IGradient {

        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency) {
            var l = default(L);
            LatticeSpan4
                x = l.GetLatticeSpan4(positions.c0, frequency),
                z = l.GetLatticeSpan4(positions.c2, frequency);
            SmallXXHash4 h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1);

            //Bilinear interpolation in two dimensions
            var g = default(G);
            return g.EvaluateAfterInterpolation(lerp(
                lerp(
                    g.Evaluate(h0.Eat(z.p0), x.g0, z.g0),
                    g.Evaluate(h0.Eat(z.p1), x.g0, z.g1),
                    z.t
                ),
                lerp(
                    g.Evaluate(h1.Eat(z.p0), x.g1, z.g0),
                    g.Evaluate(h1.Eat(z.p1), x.g1, z.g1),
                    z.t
                ),
                x.t
            ));

        }
    }

    // Struct defining 3D lattice with gradient
    public struct Lattice3D<L, G> : INoise
        where L : struct, ILattice where G : struct, IGradient {

        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency) {
            var l = default(L);
            LatticeSpan4
                x = l.GetLatticeSpan4(positions.c0, frequency),
                y = l.GetLatticeSpan4(positions.c1, frequency),
                z = l.GetLatticeSpan4(positions.c2, frequency);
            SmallXXHash4
                h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1),
                h00 = h0.Eat(y.p0), h01 = h0.Eat(y.p1),
                h10 = h1.Eat(y.p0), h11 = h1.Eat(y.p1);

            //Linear interpolation of two bilinear interpolations (quadralinear interpolation???)
            var g = default(G);
            return g.EvaluateAfterInterpolation(lerp(
                lerp(
                    lerp(
                        g.Evaluate(h00.Eat(z.p0), x.g0, y.g0, z.g0),
                        g.Evaluate(h00.Eat(z.p1), x.g0, y.g0, z.g1),
                        z.t
                    ),
                    lerp(
                        g.Evaluate(h01.Eat(z.p0), x.g0, y.g1, z.g0),
                        g.Evaluate(h01.Eat(z.p1), x.g0, y.g1, z.g1),
                        z.t
                    ),
                    y.t
                ),
                lerp(
                    lerp(
                        g.Evaluate(h10.Eat(z.p0), x.g1, y.g0, z.g0),
                        g.Evaluate(h10.Eat(z.p1), x.g1, y.g0, z.g1),
                        z.t
                    ),
                    lerp(
                        g.Evaluate(h11.Eat(z.p0), x.g1, y.g1, z.g0),
                        g.Evaluate(h11.Eat(z.p1), x.g1, y.g1, z.g1),
                        z.t
                    ),
                    y.t
                ),
                x.t
            ));
        }
    }
}
