#version 330

// Inputs from the Vertex Shader
in vec3 fragNormal;
in vec3 fragPosition;

// Uniforms (set from lambert.c)
uniform vec3 lightPos;
uniform vec3 lightColor;
uniform vec3 objectColor;

// Output color to the screen
out vec4 finalColor;

// Define PI
const float PI = 3.14159265359;

void main()
{
    // Setup vectors
    vec3 N = normalize(fragNormal);              // Normal Dir
    vec3 L = normalize(lightPos - fragPosition); // Light Dir

    // Calculate the raw dot product first
    float rawNdotL = dot(N, L);

    // Clamping to a tiny epsilon (0.0001) prevents the denominator from ever reaching a 0/0 situation.
    float NdotL = max(rawNdotL, 0.0001);

    // ==================== Ambient Term (Simple) ====================

    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * lightColor * objectColor;

    // ==================== Diffuse Term (Lambert) ====================

    // The Lambertian formula (Energy conserving term = 1/pi)
    vec3 diffuse = (objectColor / PI) * lightColor * NdotL;
    
    // ==================== Combine ====================

    vec3 result = ambient + diffuse;

    // In PBR, we usually need much brighter lights (Intensity > 1.0).
    // For this demo, let's multiply the final result by an "Exposure" factor.
    // Allows us to see it clearly on our screen.
    float exposure = 3.0; 
    
    finalColor = vec4(result * exposure, 1.0);
}