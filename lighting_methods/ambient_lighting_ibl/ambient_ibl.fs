#version 330

// Inputs from the Vertex Shader
in vec3 fragNormal;
in vec3 fragPosition;

// Uniforms
uniform vec3 lightColor;
uniform vec3 objectColor;
uniform vec3 viewPos;
uniform sampler2D reflectionMap;
uniform float reflectivityValue;

// Output color to the screen
out vec4 finalColor;

// Define PI
const float PI = 3.14159265359;

// Convert 3D direction to panoramic UV coordinates
vec2 directionToSphericalUV(vec3 dir)
{
    vec3 normalized = normalize(dir);
    float u = atan(normalized.z, normalized.x);
    float v = asin(-normalized.y);
    u = u / (2.0 * PI) + 0.5;
    v = v / PI + 0.5;
    return vec2(u, v);
}

void main()
{
    vec3 N = normalize(fragNormal);
    vec3 V = normalize(viewPos - fragPosition);
    vec3 R = reflect(-V, N);

    // ==================== Ambient Term (IBL) ====================

    // Calculate which mip level to use based on reflectivity
    // reflectivity = 0 (diffuse): Use highest mip level (most blurred)
    // reflectivity = 1 (mirror): Use mip level 0 (sharpest)
    
    float maxMipLevel = 10.0;
    float mipLevel = (1.0 - reflectivityValue) * maxMipLevel;
    
    // Sample using different mip levels for diffuse vs specular
    vec2 uvDiffuse = directionToSphericalUV(N);
    vec2 uvSpecular = directionToSphericalUV(R);

    // Smooth mipmap blending - sample adjacent mip levels and blend
    float mipFloor = floor(mipLevel);
    float mipCeil = ceil(mipLevel);
    float mipFract = fract(mipLevel);

    // Sample two adjacent mip levels for specular
    vec3 specular1 = textureLod(reflectionMap, uvSpecular, mipFloor).rgb;
    vec3 specular2 = textureLod(reflectionMap, uvSpecular, mipCeil).rgb;
    vec3 envSpecular = mix(specular1, specular2, mipFract);

    // Diffuse always uses max mip (most blurred)
    vec3 envDiffuse = textureLod(reflectionMap, uvDiffuse, maxMipLevel).rgb;
    
    // Blend between diffuse and specular based on reflectivity
    vec3 environmentContribution = mix(envDiffuse, envSpecular, reflectivityValue);
    
    // Apply object color
    vec3 ambient = environmentContribution * objectColor * lightColor;

    // ==================== Combine ====================
    vec3 result = ambient;

    finalColor = vec4(result, 1.0);
}