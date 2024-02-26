// This struct represents a space transformation (translation, rotation, scale) in 3D space.
// It provides a Matrix property to retrieve a transformation matrix based on the space parameters.

using Unity.Mathematics; // Importing Unity's mathematics library.

// Serializable attribute allows the struct to be serialized and shown in the Unity Inspector.
[System.Serializable]
public struct SpaceTRS {
    // Translation, rotation, and scale vectors.
    public float3 translation, rotation, scale;

    // Property to get the transformation matrix.
    public float3x4 Matrix {
        get {
            // Create a transformation matrix using Unity's TRS method.
            float4x4 m = float4x4.TRS(
                translation,
                // Convert rotation angles to radians and create a quaternion rotation.
                quaternion.EulerZXY(math.radians(rotation)),
                scale
            );
            // Convert the matrix to a float3x4 representation and return.
            return math.float3x4(m.c0.xyz, m.c1.xyz, m.c2.xyz, m.c3.xyz);
        }
    }
}
