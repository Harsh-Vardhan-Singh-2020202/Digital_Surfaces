#version 330

// Inputs from the Vertex Shader
in vec3 fragNormal;
in vec3 fragPosition;

// Uniforms (set from burley.c)
uniform vec3 lightPos;
uniform vec3 lightColor;
uniform vec3 objectColor;
uniform vec3 viewPos;
uniform float roughnessValue;

// Output color to the screen
out vec4 finalColor;

// Define PI
const float PI = 3.14159265359;

void main()
{
    // Setup vectors
    vec3 N = normalize(fragNormal);                 // Normal Vector
    vec3 L = normalize(lightPos - fragPosition);    // Light Vector
    vec3 V = normalize(viewPos - fragPosition);     // View Vector
    vec3 H = normalize(L + V);                      // Half Vector

    // Calculate dot products
    float rawNdotL = max(dot(N, L), 0.0);
    float rawNdotV = max(dot(N, V), 0.0);
    float rawLdotH = max(dot(L, H), 0.0);

    // Clamping to a tiny epsilon (0.0001) prevents the denominator from ever reaching a 0/0 situation.
    float NdotL = max(rawNdotL, 0.0001);
    float NdotV = max(rawNdotV, 0.0001);
    float LdotH = max(rawLdotH, 0.0001);
    
    // Get roughness
    // 0.0 = Smooth (Lambert), 1.0 = Very Rough (Bright periphery)
    float roughness = roughnessValue;

    // ==================== Ambient Term (Simple) ====================
    
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * lightColor * objectColor;

    // ==================== Diffuse Term (Burley) ====================

    // The "Energy Bias" (FD90)
    // Determines if the edges get darker (smooth) or brighter (rough)
    float FD90 = 0.5 + 2.0 * roughness * (LdotH * LdotH);

    // Schlick fresnel for light and view
    float viewScatter  = 1.0 + (FD90 - 1.0) * pow(1.0 - NdotV, 5.0);
    float lightScatter = 1.0 + (FD90 - 1.0) * pow(1.0 - NdotL, 5.0);

    // Final diffuse calculation
    vec3 diffuse = (objectColor / PI) * lightColor * NdotL * viewScatter * lightScatter;

    // ==================== Combine ====================

    vec3 result = ambient + diffuse;

    // In PBR, we usually need much brighter lights (Intensity > 1.0).
    // For this demo, let's multiply the final result by an "Exposure" factor.
    // Allows us to see it clearly on our screen.
    float exposure = 3.0; 
    
    finalColor = vec4(result * exposure, 1.0);
}