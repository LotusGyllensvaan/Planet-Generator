// Check if procedural instancing is enabled
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	// Define structured buffers for noise, positions, and normals
StructuredBuffer<float> _Noise;
StructuredBuffer<float3> _Positions, _Normals;
#endif

// Define a float4 variable for configuration
float4 _Config;

// Function to configure procedural instancing
void ConfigureProcedural() {
	// Check if procedural instancing is enabled
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	// Set the object-to-world matrix for the current instance
	unity_ObjectToWorld = 0.0;
	unity_ObjectToWorld._m03_m13_m23_m33 = float4(
		_Positions[unity_InstanceID],
		1.0
		);
	unity_ObjectToWorld._m03_m13_m23 +=
		_Config.z * _Noise[unity_InstanceID] * _Normals[unity_InstanceID];
	unity_ObjectToWorld._m00_m11_m22 = _Config.y;
#endif
}

// Function to get noise color
float3 GetNoiseColor() {
	// Check if procedural instancing is enabled
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	// Get noise value for the current instance
	float noise = _Noise[unity_InstanceID];
	// Return color based on noise value
	return noise < 0.0 ? float3(0.0, 0.0, -noise) : float3(0.0, noise, 0.0);
#else
	// Return default color if procedural instancing is not enabled
	return 1.0;
#endif
}

// Shader graph function for float precision
void ShaderGraphFunction_float(float3 In, out float3 Out, out float3 Color) {
	// Pass input position to output position
	Out = In;
	// Get noise color
	Color = GetNoiseColor();
}

// Shader graph function for half precision
void ShaderGraphFunction_half(half3 In, out half3 Out, out half3 Color) {
	// Pass input position to output position
	Out = In;
	// Get noise color
	Color = GetNoiseColor();
}
