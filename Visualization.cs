using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;

// Abstract class for visualizations
public abstract class Visualization : MonoBehaviour {
    // Shader property IDs
    static int
        positionsId = Shader.PropertyToID("_Positions"),
        normalsId = Shader.PropertyToID("_Normals"),
        configId = Shader.PropertyToID("_Config");

    // Serialized fields
    [SerializeField]
    Mesh instanceMesh; // Mesh used for instancing

    [SerializeField]
    Material material; // Material used for rendering

    [SerializeField, Range(1, 512)]
    int resolution = 16; // Resolution of the visualization grid

    [SerializeField, Range(-0.5f, 0.5f)]
    float displacement = 0.1f; // Displacement factor

    [SerializeField, Range(0.1f, 10f)]
    float instanceScale = 2f; // Scale factor for instances

    // Native arrays for positions and normals
    NativeArray<float3x4> positions, normals;

    // Compute buffers for positions and normals
    ComputeBuffer positionsBuffer, normalsBuffer;

    // Material property block for shader properties
    MaterialPropertyBlock propertyBlock;

    // Flag for dirty state
    bool isDirty;

    // Bounds of the visualization
    Bounds bounds;

    // Enumeration for different shapes
    public enum Shape { Plane, Sphere, Torus }

    // Array of job functions for different shapes
    static Shapes.ScheduleDelegate[] shapeJobs = {
        Shapes.Job<Shapes.Plane>.ScheduleParallel,
        Shapes.Job<Shapes.Sphere>.ScheduleParallel,
        Shapes.Job<Shapes.Torus>.ScheduleParallel
    };

    [SerializeField]
    Shape shape; // Selected shape for visualization

    // Called when the object is enabled and intizialises buffers, arrays and enables the visualization
    void OnEnable() {
        //Check if the frame is dirty and needs to be updated
        isDirty = true;

        //Initialize arrays and computeBuffers with correct length and stride according to manual vectorization
        int length = resolution * resolution;
        length = length / 4 + (length & 1);
        positions = new NativeArray<float3x4>(length, Allocator.Persistent);
        normals = new NativeArray<float3x4>(length, Allocator.Persistent);
        positionsBuffer = new ComputeBuffer(length * 4, 3 * 4);
        normalsBuffer = new ComputeBuffer(length * 4, 3 * 4);

        //checks if property block is null and assigning a new property block if it is
        propertyBlock ??= new MaterialPropertyBlock();

        //Enable visualization. Functionality depends on the method in the class that inherets visualization
        EnableVisualization(length, propertyBlock);

        //Set buffers and configuration in the shader.
        propertyBlock.SetBuffer(positionsId, positionsBuffer);
        propertyBlock.SetBuffer(normalsId, normalsBuffer);
        propertyBlock.SetVector(configId, new Vector4(
            resolution, instanceScale / resolution, displacement
            ));
    }

    // Called when the object is disabled and disposes of all buffers and arrays as well as disabling the visualization
    void OnDisable() {
        positions.Dispose();
        normals.Dispose();
        positionsBuffer.Release();
        normalsBuffer.Release();
        positionsBuffer = null;
        normalsBuffer = null;
        DisableVisualization();
    }

    // Called when properties are changed in the editor. Reloads visualization.
    void OnValidate() {
        //Checks if positions buffer is null as well as if the gameobject is enabled to avoid calling OnDisable twice
        if (positionsBuffer != null && enabled) {
            OnDisable();
            OnEnable();
        }
    }

    // Called once per frame
    void Update() {
        //Checks if frame needs updating or if the gameObject transform has changed
        if (isDirty || transform.hasChanged) {
            //Sets both to false
            isDirty = false;
            transform.hasChanged = false;

            //Updates the visualization with the correct positions, resolution of vizualisation and the selected shape
            UpdateVisualization(
                positions, resolution,
                //Casts shape enumerable to int and uses as index in the array of jobs
                shapeJobs[(int)shape](
                    positions, normals, resolution, transform.localToWorldMatrix, default
                )
            );

            //Sends data to shader, reinterpreted as float3's
            positionsBuffer.SetData(positions.Reinterpret<float3>(3 * 4 * 4));
            normalsBuffer.SetData(normals.Reinterpret<float3>(3 * 4 * 4));

            //updates bounds of visualization with an aproximation of vizualisation size
            bounds = new Bounds(
                transform.position,
                float3(2f * cmax(abs(transform.lossyScale)) + displacement)
            );
        }
        //Procedually draw the mesh every frame with the appointed instance mesh, material, bounds, amount and properties
        Graphics.DrawMeshInstancedProcedural(
                instanceMesh, 0, material, bounds, resolution * resolution, propertyBlock
        );
    }

    // Method to enable visualization
    protected abstract void EnableVisualization(
      int dataLength, MaterialPropertyBlock propertyBlock
    );

    // Method to disable visualization
    protected abstract void DisableVisualization();

    // Method to update visualization
    protected abstract void UpdateVisualization(
        NativeArray<float3x4> positions, int resolution, JobHandle handle
    );
}
