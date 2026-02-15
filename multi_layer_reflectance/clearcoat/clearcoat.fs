// Note: Code is not fully optimized, dot products might be calculated more than once for a single pass across diffusion and speular calc
// This is for learning purpose and thus done in this manner

#version 330

// Inputs from the Vertex Shader
in vec3 fragNormal;
in vec3 fragPosition;
in vec3 fragTangent;
in vec3 fragBitangent; 

// Uniforms (set from cook_torrance.c)
uniform vec3 lightPos;
uniform vec3 lightColor;
uniform vec3 objectColor;
uniform vec3 viewPos;

uniform float roughnessValue;
uniform float metallicValue;
uniform float iorValue;
uniform float alphaValue;

uniform float clearcoatWeightValue;
uniform float clearcoatRoughnessValue;
uniform float clearcoatIorValue;
uniform vec3 clearcoatTint;

// Output color to the screen
out vec4 finalColor;

// Define PI
const float PI = 3.14159265359;

// D: Normal Distribution Functions (NDF) - FIXED TO GGX ONLY
float Distribution(float roughness, vec3 N, vec3 H)
{
    // D_GGX = alpha^2 / (pi * ((alpha^2-1) * (NdotH^2) + 1)^2)

    float alpha2 = max(roughness * roughness, 0.0001); // Prevent division by zero
        
    float NdotH = max(dot(N, H), 0.0001); // Prevent division by zero

    float NdotH2 = NdotH * NdotH;

    float denomPart = ((alpha2 - 1.0) * NdotH2 + 1.0);

    float denom = PI * denomPart * denomPart;

    return alpha2 / denom;
}

// G: Geometry Shadowing Functions (GSF) - ONLY SMITH-GGX
float Geometry(float roughness, vec3 N, vec3 L, vec3 V)
{
    // G_smith_GGX = (Chi(NdotL) * Chi(NdotV)) / (1 + Lambda(NdotL) + Lambda(NdotV))

    // Fast approximation 
    // Lambda(X) = (-1 + √(1 + (alpha^2 * tan(θ_X)))) / 2
    // a = 1 / (alpha * tan(θ))
        
    // Chi is the Heaviside function -> simplified to max of num or 0 in rendering

    float alpha = max(roughness, 0.0001); // Prevent division by 0
    float alpha2 = alpha * alpha;

    float NdotL = max(dot(N, L), 0.0001);
    float NdotV = max(dot(N, V), 0.0001);

    // ================= View term =================
    float NdotV2 = NdotV * NdotV;
    float tanThetaV2 = (1.0 - NdotV2) / NdotV2;

    // GGX Lambda for view
    float lambdaV = (-1.0 + sqrt(1.0 + alpha2 * tanThetaV2)) * 0.5;

    // ================= Light term =================
    float NdotL2 = NdotL * NdotL;
    float tanThetaL2 = (1.0 - NdotL2) / NdotL2;

    // GGX Lambda for light
    float lambdaL = (-1.0 + sqrt(1.0 + alpha2 * tanThetaL2)) * 0.5;

    // ================= Height-correlated Smith =================
    return (NdotV * NdotL) / (1.0 + lambdaV + lambdaL);
}

// F: Fresnel Functions (FF) - ONLY SCHLICK
vec3 Fresnel(float metallic, float ior, vec3 V, vec3 H)
{
    // F_schlick = F0 + (1 - F0) * (1 - VdotH)^5

    // Base reflectivity for dielectrics (non-metals)
    float reflectivity = pow((1.0 - ior) / (1.0 + ior), 2.0);
        
    // Metals use the albedo color as F0, lerp based on metallic value
    vec3 F0 = mix(vec3(reflectivity), objectColor, metallic);

    float VdotH = max(dot(V, H), 0.0);

    return F0 + (1.0 - F0) * pow(clamp(1.0 - VdotH, 0.0, 1.0), 5.0);
}

void main()
{
    // NOTE: We will use the Burley Diffuse Model combined with a Cook-Torrance Specular Model

    // Setup Vectors
    vec3 N = normalize(fragNormal);                 // Normal Vector
    vec3 L = normalize(lightPos - fragPosition);    // Light Vector
    vec3 V = normalize(viewPos - fragPosition);     // View Vector
    vec3 H = normalize(L + V);                      // Half Vector
    vec3 T = normalize(fragTangent);                // Tangent Vector
    vec3 B = normalize(fragBitangent);              // Bitangent Vector
    
    // Get shared PBR roughness (Sigma)
    float roughness = roughnessValue;

    // Get the PBR metalic (F0)
    float metallic = metallicValue;

    // Get the IOR
    float ior = iorValue;

    // Get the alpha value (not used in this shader, but could be for transparency)
    float alpha = alphaValue;
    
    // ==================== Ambient Term (Simple) ====================
    
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * lightColor * objectColor;

    // ==================== Diffuse Term (Burley) ====================
    
    // Get the dot products
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float LdotH = max(dot(L, H), 0.0);

    // The "Energy Bias" (FD90)
    // Determines if the edges get darker (smooth) or brighter (rough)
    float FD90 = 0.5 + 2.0 * roughness * (LdotH * LdotH);

    // Schlick fresnel for light and view
    float viewScatter  = 1.0 + (FD90 - 1.0) * pow(1.0 - NdotV, 5.0);
    float lightScatter = 1.0 + (FD90 - 1.0) * pow(1.0 - NdotL, 5.0);

    // Final diffuse calculation
    vec3 diffuse = (objectColor / PI) * lightColor * NdotL * viewScatter * lightScatter;

    // ==================== Specular Term (Cook-Torrance) ====================
    
    float D = Distribution(roughness, N, H);
    float G = Geometry(roughness, N, L, V);
    vec3  F = Fresnel(metallic, ior, V, H);

    // The Cook-Torrance Fraction
    vec3 numerator = D * G * F;
    float denominator = 4.0 * NdotL * NdotV;

    vec3 specular = numerator / max(denominator, 0.0001) * NdotL * lightColor;
    
    // ==================== Multiscatter Energy Compensation ====================
    
    float r2 = roughness * roughness;
        
    // Directional albedo (Probability of escape from V and L)
    float EssV = 1.0 - (0.15 * r2) / (1.0 + 2.0 * NdotV * (1.0 - r2));
    float EssL = 1.0 - (0.15 * r2) / (1.0 + 2.0 * NdotL * (1.0 - r2));

    // Hemispherical average albedo (Eavg)
    // This is a common analytic fit for the average of the Ess LUT
    float Eavg = 1.0 - 0.15 * (roughness * roughness); 
    float EmsV = 1.0 - EssV;
    float EmsL = 1.0 - EssL;

    // average fresnel (Energy return factor)
    float reflectivity = pow((1.0 - ior) / (1.0 + ior), 2.0);
    vec3 F0 = mix(vec3(reflectivity), objectColor, metallic);
    vec3 Favg = F0 + (vec3(1.0) - F0) / 21.0; 

    // The energy return engine (The "Infinite Bounce" formula)
    vec3 energyTerm = (Favg * Eavg) / (vec3(1.0) - Favg * (1.0 - Eavg));

    // The multi-scatter Lobe (fms)
    vec3 f_ms = (vec3(EmsV * EmsL) / (PI * max(1.0 - Eavg, 0.001))) * energyTerm;
               
    // Add to existing specular (Direct lighting logic) - No NdotL here
    specular += f_ms * lightColor;

    // ==================== Clearcoat ====================

    // Clearcoat parameters
    float clearcoatWeight = clearcoatWeightValue;
    float clearcoatRoughness = clearcoatRoughnessValue;
    float clearcoatIor = clearcoatIorValue;

    // Calculate clearcoat Fresnel (F0 from IOR)
    // F0 = ((n1 - n2) / (n1 + n2))^2, assuming air (n1=1.0)
    // Not passed through the metallic workflow, always dielectric
    float clearcoatF0 = pow((1.0 - clearcoatIor) / (1.0 + clearcoatIor), 2.0);

    // Clearcoat Fresnel using Schlick approximation
    float VdotH_clearcoat = max(dot(V, H), 0.0);
    float clearcoatFresnel = clearcoatF0 + (1.0 - clearcoatF0) * pow(1.0 - VdotH_clearcoat, 5.0);

    // Clearcoat specular BRDF (Cook-Torrance with its own roughness)
    float D_clearcoat = Distribution(clearcoatRoughness, N, H);
    float G_clearcoat = Geometry(clearcoatRoughness, N, L, V);
    float F_clearcoat = clearcoatFresnel;

    // Clearcoat specular calculation
    float  clearcoatNumerator = D_clearcoat * G_clearcoat * clearcoatFresnel;
    float clearcoatDenominator = 4.0 * NdotL * NdotV;
    vec3 clearcoatSpecular = vec3(clearcoatNumerator / max(clearcoatDenominator, 0.0001)) * NdotL * lightColor;

    // Apply clearcoat weight
    clearcoatSpecular *= clearcoatWeight;

    // Attenuation: Light passes through tinted clearcoat twice (in and out)
    vec3 tintAttenuation = clearcoatTint * clearcoatTint;

    // Energy conservation: What the clearcoat reflects, the base doesn't see
    float energyLoss = clearcoatFresnel * clearcoatWeight;

    // Attenuate base layer by both tint absorption and clearcoat reflection
    vec3 baseAttenuation = (1.0 - energyLoss) * tintAttenuation;

    // Apply attenuation to base diffuse and specular
    diffuse *= baseAttenuation;
    specular *= baseAttenuation;
    ambient *= tintAttenuation; // Ambient also passes through tint twice

    // ==================== Combine ====================

    // Energy conservation
    // F tells us how much light reflects (specular)
    // Whatever doesn't reflect goes into diffuse but metals don't have diffuse at all

    // kS explanation:
    // kS is simply the Specular reflection ratio which we already have as F

    // kD explanation:
    // - (1.0 - kS): Energy not reflected goes to diffuse (conservation)
    // - * (1.0 - metallic): Metals have NO diffuse, so when metallic=1, kD=0

    vec3 kS = F;                                // Specular contribution
    vec3 kD = (1.0 - kS) * (1.0 - metallic);    // Diffuse contribution

    // Combine with energy conservation
    vec3 result = ambient + kD * diffuse + specular;

    // Add clearcoat specular on top (it sits above everything)
    result += clearcoatSpecular;

    // In PBR, we usually need much brighter lights (Intensity > 1.0).
    // For this demo, let's multiply the final result by an "Exposure" factor 
    // just to see it clearly on our screen.
    float exposure = 3.0; 
    
    finalColor = vec4(result * exposure, alpha);
}