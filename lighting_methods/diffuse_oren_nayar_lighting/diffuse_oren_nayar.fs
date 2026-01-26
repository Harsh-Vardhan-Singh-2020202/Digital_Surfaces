#version 330

// Inputs from the Vertex Shader
in vec3 fragNormal;
in vec3 fragPosition;

// Uniforms (set from oren_nayar.c)
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
    // Setup Vectors
    vec3 N = normalize(fragNormal);
    vec3 L = normalize(lightPos - fragPosition);
    vec3 V = normalize(viewPos - fragPosition);

    // Calculate Dot Products
    float rawNdotL = max(dot(N, L), 0.0);
    float rawNdotV = max(dot(N, V), 0.0);

    // Clamping to a tiny epsilon (0.0001) prevents the denominator from ever reaching a 0/0 situation.
    float NdotL = max(rawNdotL, 0.0001);
    float NdotV = max(rawNdotV, 0.0001);

    // Get roughness (Sigma)
    // 0.0 = Smooth (Lambert), 1.0 = Very Rough (Clay/Moon)
    float roughness = roughnessValue;

    // ==================== Ambient Term (Base) ====================
    
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * lightColor * objectColor;

    // ==================== Diffuse Term (Oren-Nayar) ====================
    
    // Get sigma squared
    float sigma2 = roughness * roughness;

    // Calculate A and B (The Coefficients)
    float A = 1.0 - 0.5 * (sigma2 / (sigma2 + 0.33));
    float B = 0.45 * (sigma2 / (sigma2 + 0.09));

    // Calculate Angles (Alpha and Beta)
    // thetaL = angle between Normal and Light
    // thetaV = angle between Normal and View
    float thetaL = acos(clamp(NdotL, 0.0, 1.0));
    float thetaV = acos(clamp(NdotV, 0.0, 1.0));

    float alpha = max(thetaL, thetaV);
    float beta = min(thetaL, thetaV);

    // Project L and V onto the tangent plane
    vec3 L_proj = L - NdotL * N;
    vec3 V_proj = V - NdotV * N;

    // Get the lengths to check if projection is valid
    float L_proj_len = length(L_proj);
    float V_proj_len = length(V_proj);
    
    // Calculate Azimuthal Difference (Phi)
    float cosPhiDiff = 0.0;

    // Only calculate if both projections are non-zero
    if (L_proj_len > 0.001 && V_proj_len > 0.001)
    {
        L_proj = L_proj / L_proj_len;
        V_proj = V_proj / V_proj_len;
        cosPhiDiff = clamp(dot(L_proj, V_proj), -1.0, 1.0);
    }

    // The approximation term
    float orenNayarTerm = A + (B * max(0.0, cosPhiDiff) * sin(alpha) * tan(beta));

    // Final term (Standard Lambert * Correction Term)
    vec3 diffuse = (objectColor / PI) * lightColor * NdotL * orenNayarTerm;
    
    // ==================== Combine ====================

    vec3 result = ambient + diffuse;

    // In PBR, we usually need much brighter lights (Intensity > 1.0).
    // For this demo, let's multiply the final result by an "Exposure" factor.
    // Allows us to see it clearly on our screen.
    float exposure = 3.0; 
    
    finalColor = vec4(result * exposure, 1.0);
}