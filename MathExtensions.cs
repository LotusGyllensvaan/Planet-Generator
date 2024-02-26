// Import necessary namespaces
using Unity.Mathematics; // Unity's mathematics library
using static Unity.Mathematics.math;

// Declare a static class for extension methods
public static class MathExtensions {


    // Extension method to transform vectors using a 3x4 matrix
    // Parameters:
    // trs: The 3x4 transformation matrix
    // p: The 4x3 XYZ-column matrix to transform
    // w (optional): The weight for the transformation. Default value is 1f and is changed depending on if your calculating normal vectors.
    public static float4x3 TransformVectors(
        this float3x4 trs, float4x3 p, float w = 1f
    ) => float4x3(
        // Perform the transformation element-wise and return the result
        trs.c0.x * p.c0 + trs.c1.x * p.c1 + trs.c2.x * p.c2 + trs.c3.x * w,
        trs.c0.y * p.c0 + trs.c1.y * p.c1 + trs.c2.y * p.c2 + trs.c3.y * w,
        trs.c0.z * p.c0 + trs.c1.z * p.c1 + trs.c2.z * p.c2 + trs.c3.z * w
    );

    // Extension method to extract the upper 3x4 portion of a 4x4 matrix
    // Parameters:
    // m: The 4x4 matrix
    public static float3x4 Get3x4(this float4x4 m) =>
        // Construct a new 3x4 matrix using the XYZ components of the columns of m
        float3x4(m.c0.xyz, m.c1.xyz, m.c2.xyz, m.c3.xyz);
}