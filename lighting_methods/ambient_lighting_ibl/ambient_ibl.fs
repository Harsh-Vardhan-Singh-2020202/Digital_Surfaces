#version 330

// Inputs from the Vertex Shader
in vec3 fragNormal;
in vec3 fragPosition;

// Uniforms (set from ambient_ibl.c)
uniform vec3 lightColor;
uniform vec3 objectColor;
uniform vec3 viewPos;

uniform sampler2D reflectionMap;

uniform float roughnessValue;
uniform float metallicValue;

// Output color to the screen
out vec4 finalColor;

// Define PI
const float PI = 3.14159265359;

// Convert 3D direction to panoramic UV coordinates
vec2 directionToSphericalUV(vec3 dir)
{
    vec3 normalized = normalize(dir);
    
    // Convert to spherical coordinates
    float u = atan(normalized.z, normalized.x);
    float v = asin(-normalized.y);
    
    // Normalize to [0, 1] range
    u = u / (2.0 * PI) + 0.5;
    v = v / PI + 0.5;
    
    return vec2(u, v);
}

void main()
{
    vec3 N = normalize(fragNormal);
    vec3 V = normalize(viewPos - fragPosition);
    vec3 R = reflect(-V, N);

    // ==================== Ambient Term (Base) ====================

    // Convert reflection direction to UV coordinates
    vec2 uv = directionToSphericalUV(R);
    // Sample the panorama texture
    vec3 reflection = texture(reflectionMap, uv).rgb;
    
    //float ambientStrength = 0.5;
    //vec3 ambient = ambientStrength * lightColor * objectColor;

    // ==================== Combine ====================
    
    vec3 result = reflection;

    finalColor = vec4(result, 1.0);
}