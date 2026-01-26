#version 330

// Inputs from the Vertex Shader
in vec3 fragNormal;
in vec3 fragPosition;
in vec3 fragTangent;
in vec3 fragBitangent;

// Uniforms (set from blinn_phong.c)
uniform vec3 lightPos;
uniform vec3 lightColor;
uniform vec3 objectColor;
uniform vec3 viewPos;

uniform float metallicValue;

// Output color to the screen
out vec4 finalColor;

// Define PI
const float PI = 3.14159265359;

void main()
{
    // NOTE: We will use the Burley Diffuse Model combined with a Phong Specular Model

    // Setup vectors
    vec3 N = normalize(fragNormal);                 // Normal Vector
    vec3 L = normalize(lightPos - fragPosition);    // Light Vector
    vec3 V = normalize(viewPos - fragPosition);     // View Vector
    vec3 H = normalize(L + V);                      // Half Vector
    vec3 T = normalize(fragTangent);                // Tangent Vector
    vec3 B = normalize(fragBitangent);              // Bitangent Vector

    // Dot products 
    float rawNdotL = max(dot(N, L), 0.0);
    float rawNdotV = max(dot(N, V), 0.0);
    float rawNdotH = max(dot(N, H), 0.0);
    float rawHdotL = max(dot(H, L), 0.0);

    // Clamping to a tiny epsilon (0.0001) prevents the denominator from ever reaching a 0/0 situation.
    float NdotL = max(rawNdotL, 0.0001);
    float NdotV = max(rawNdotV, 0.0001);
    float NdotH = max(rawNdotH, 0.0001);
    float HdotL = max(rawHdotL, 0.0001);

    // Get tangent and bit tangent related dot proucts
    float HdotT = dot(H, T);
    float HdotB = dot(H, B);

    // Calculate F0 (base reflectivity)
    // For dielectrics: F0 ≈ 0.04 (4% reflection)
    // For metals: F0 = albedo color
    vec3 F0 = vec3(0.04);
    F0 = mix(F0, objectColor, metallicValue);

    // ==================== Ambient Term (Base) ====================
    
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * lightColor * objectColor;

    // ==================== Diffuse Term (Ashikkmin-Shirley) ====================
    
    // Calculate average specular reflectance (simplified)
    // For metallic workflow: rho_s ≈ F0
    vec3 rho_s = F0;
    
    // Ashikhmin-Shirley diffuse formula:
    // f_d = (28 * rho_d) / (23π) * (1 - rho_s) * (1 - (1 - N·L/2)^5) * (1 - (1 - N·V/2)^5)
    
    float term1 = 1.0 - pow(1.0 - NdotL * 0.5, 5.0);
    float term2 = 1.0 - pow(1.0 - NdotV * 0.5, 5.0);
    
    vec3 diffuse = (28.0 / (23.0 * PI)) * objectColor * (vec3(1.0) - rho_s) * term1 * term2 * lightColor * NdotL;
    
    // ==================== Combine ====================
    
    vec3 result = ambient + diffuse;

    // In PBR, we usually need much brighter lights (Intensity > 1.0).
    // For this demo, let's multiply the final result by an "Exposure" factor 
    // just to see it clearly on our screen.
    float exposure = 3.0; 
    
    finalColor = vec4(result * exposure, 1.0);
}