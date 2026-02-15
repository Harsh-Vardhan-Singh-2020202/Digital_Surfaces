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
uniform float anisotropyValue;
uniform float iorValue;
uniform float alphaValue;

uniform int ndfType;
uniform int gsfType;
uniform int fresnelType;

uniform int multiScatterType;

uniform int conductorPresetType;

// Output color to the screen
out vec4 finalColor;

// Define PI
const float PI = 3.14159265359;

vec3 ACESFilm(vec3 x)
{
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return clamp((x*(a*x+b))/(x*(c*x+d)+e), 0.0, 1.0);
}

// D: Normal Distribution Functions (NDF)
float Distribution(float roughness, float anisotropy, vec3 N, vec3 L, vec3 V, vec3 H, vec3 T, vec3 B)
{
    // Beckmann
    if (ndfType == 0) 
    {
        // D_beckmann = (e^(-(tan(θ_H)^2 / alpha^2)) / (pi * alpha^2 * NdotH^4)
        // tan(θ_H)^2 = sin(θ_H)^2 / cos(θ_H)^2 = (1 - cos(θ_H)^2) / cos(θ_H)^2 = (1 - NdotH^2) / NdotH^2

        float alpha2 = max(roughness * roughness, 0.0001); // Prevent division by zero
        
        float NdotH = max(dot(N, H), 0.0001); // Prevent division by zero

        float NdotH2 = NdotH * NdotH;

        float tanThetaH2 = (1.0 - NdotH2) / NdotH2;
        
        return exp(-tanThetaH2 / alpha2) / (PI * alpha2 * NdotH2 * NdotH2);
    }

    // GGX (Trowbridge-Reitz)
    if (ndfType == 1) 
    {
        // D_GGX = alpha^2 / (pi * ((alpha^2-1) * (NdotH^2) + 1)^2)

        float alpha2 = max(roughness * roughness, 0.0001); // Prevent division by zero
        
        float NdotH = max(dot(N, H), 0.0001); // Prevent division by zero

        float NdotH2 = NdotH * NdotH;

        float denomPart = ((alpha2 - 1.0) * NdotH2 + 1.0);

        float denom = PI * denomPart * denomPart;

        return alpha2 / denom;
    }

    // GGX (Anisotropic)
    if (ndfType == 2) 
    {
        // D_GGX_Aniso = 1 / (pi * alpha_x * alpha_y * ((TdotH/alpha_x)^2 + (BdotH/alpha_y)^2 + (NdotH^2)^2)
        // Convert scalar roughness to directional roughness
        // anisotropy ranges from -1 (stretched along tangent) to +1 (stretched along bitangent)

        float aspect = sqrt(1.0 - anisotropy * 0.75); // To prevent extreme values
        float alpha_x = max(0.0001, roughness / aspect);
        float alpha_y = max(0.0001, roughness * aspect);
        
        float TdotH = dot(T, H);
        float BdotH = dot(B, H);
        float NdotH = max(dot(N, H), 0.0001); // Prevevent division by 0

        float NdotH2 = NdotH * NdotH;
        
        float denomPart = (TdotH * TdotH) / (alpha_x * alpha_x) + (BdotH * BdotH) / (alpha_y * alpha_y) + NdotH2;

        float denom = PI * alpha_x * alpha_y * denomPart * denomPart;
        
        return 1.0 / denom;
    }

    // Disabled
    return 1.0;
}

// G: Geometry Shadowing Functions (GSF)
float Geometry(float roughness, float anisotropy, vec3 N, vec3 L, vec3 V, vec3 H, vec3 T, vec3 B)
{
    // Kelemen
    if (gsfType == 0) 
    {
        // G_kelmen = (NdotL * NdotV) / (VdotH^2)

        float NdotL = max(dot(N, L), 0.0);
        float NdotV = max(dot(N, V), 0.0);
        float VdotH = max(dot(V, H), 0.0001); // Prevent division by 0

        return (NdotL * NdotV) / (VdotH * VdotH);
    }

    // Neuman
    if (gsfType == 1)
    {
        // G_neumann = (NdotL * NdotV) / max(NdotL, NdotV) = min(NdotL, NdotV)

        float NdotL = max(dot(N, L), 0.0);
        float NdotV = max(dot(N, V), 0.0);

        return min(NdotL, NdotV);
    }

    // Schlick (Disney) [Direct ligthing, IBL will be handled later]
    if (gsfType == 2)
    {
        // G_schlick = G_L * G_V
        // G_L = NdotL / (NdotL * (1-k) + k)
        // G_V = NdotV / (NdotV * (1-k) + k)
        // k_disney = alpha/2 = roughness/2

        // k for direct lighting
        float k = max(roughness * 0.5, 0.0001); // Prevent division by 0

        float NdotL = max(dot(N, L), 0.0);
        float NdotV = max(dot(N, V), 0.0);

        float G_V = NdotV / (NdotV * (1.0 - k) + k);
        float G_L = NdotL / (NdotL * (1.0 - k) + k);

        return G_V * G_L;
    }

    // Schlick (Epic) [Direct ligthing, IBL will be handled later]
    if (gsfType == 3)
    {
        // G_schlick = G_L * G_V
        // G_L = NdotL / (NdotL * (1-k) + k)
        // G_V = NdotV / (NdotV * (1-k) + k)
        // k_epic = (roughness + 1)^2 / 8

        // k for direct lighting
        float k = pow(roughness + 1, 2.0) / 8.0;

        float NdotL = max(dot(N, L), 0.0);
        float NdotV = max(dot(N, V), 0.0);

        float G_V = NdotV / (NdotV * (1.0 - k) + k);
        float G_L = NdotL / (NdotL * (1.0 - k) + k);

        return G_V * G_L;
    }
    
    // Smith-Beckmann
    if (gsfType == 4) 
    {

        // G_smith_beckmann = G_L * G_V
        // G_L = Chi(NdotL) / (1 + Lambda(NdotL))
        // G_V = Chi(NdotV) / (1 + Lambda(NdotV))
        
        // Fast approximation 
        // Lambda(X) = (1 - (1.259 * a) + (0.396 * a^2)) / ((3.535 * a) + (2.181 * a^2)) if a < 1.6, 0.0 otherwise
        // a = 1 / (alpha * tan(θ))
        
        // Chi is the Heaviside function -> simplified to max of num or 0 in rendering

        float alpha = max(roughness, 0.0001); // Prevent division by 0

        float NdotL = max(dot(N, L), 0.0);
        float NdotV = max(dot(N, V), 0.0);

        // ================= View term (G1 for V) =================
        float sinThetaV = sqrt(max(1.0 - NdotV * NdotV, 0.0));
        float tanThetaV = sinThetaV / max(NdotV, 0.0001);

        float aV = 1.0 / (alpha * tanThetaV);

        float lambdaV;
        if (aV < 1.6)
        {
            lambdaV = (1.0 - 1.259 * aV + 0.396 * aV * aV) / (3.535 * aV + 2.181 * aV * aV);
        }
        else
        {
            lambdaV = 0.0;
        }

        float G1_V = NdotV / (1.0 + lambdaV);

        // ================= Light term (G1 for L) =================
        float sinThetaL = sqrt(max(1.0 - NdotL * NdotL, 0.0));
        float tanThetaL = sinThetaL / max(NdotL, 0.0001);

        float aL = 1.0 / (alpha * tanThetaL);

        float lambdaL;
        if (aL < 1.6)
        {
            lambdaL = (1.0 - 1.259 * aL + 0.396 * aL * aL) / (3.535 * aL + 2.181 * aL * aL);
        }
        else
        {
            lambdaL = 0.0;
        }

        float G1_L = NdotL / (1.0 + lambdaL);

        // ================= Full Smith geometry =================
        return G1_V * G1_L;
    }

    // Smith-GGX
    if (gsfType == 5) 
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

    // Smith-GGX Anisotropic
    if (gsfType == 6) 
    {
        // Map scalar roughness + anisotropy to directional roughness
        float aspect = sqrt(1.0 - anisotropy * 0.75);  // To prevent extreme values
        float alpha_x = max(0.0001, roughness / aspect);
        float alpha_y = max(0.0001, roughness * aspect);

        float NdotL = max(dot(N, L), 0.0001); // Prevent division by 0
        float NdotV = max(dot(N, V), 0.0001); // Prevent division by 0
        float TdotV = dot(T, V);
        float BdotV = dot(B, V);
        float TdotL = dot(T, L);
        float BdotL = dot(B, L);

        // ================= View term =================
        float tan2ThetaV = (TdotV * TdotV) / (alpha_x * alpha_x * NdotV * NdotV) + (BdotV * BdotV) / (alpha_y * alpha_y * NdotV * NdotV);

        float lambdaV = (-1.0 + sqrt(1.0 + tan2ThetaV)) * 0.5;

        // ================= Light term =================
        float tan2ThetaL = (TdotL * TdotL) / (alpha_x * alpha_x * NdotL * NdotL) + (BdotL * BdotL) / (alpha_y * alpha_y * NdotL * NdotL);

        float lambdaL = (-1.0 + sqrt(1.0 + tan2ThetaL)) * 0.5;

        // ================= Height-correlated Smith =================
        return (NdotV * NdotL) / (1.0 + lambdaV + lambdaL);
    }

    // Disabled
    return 1.0;
}

// F: Fresnel Functions (FF)
vec3 Fresnel(float metallic, float ior, vec3 N, vec3 L, vec3 V, vec3 H, vec3 T, vec3 B)
{
    // Schlick approximation
    if (fresnelType == 0)
    {
        // F_schlick = F0 + (1 - F0) * (1 - VdotH)^5

        // Base reflectivity for dielectrics (non-metals)
        float reflectivity = pow((1.0 - ior) / (1.0 + ior), 2.0);
        
        // Metals use the albedo color as F0, lerp based on metallic value
        vec3 F0 = mix(vec3(reflectivity), objectColor, metallic);

        float VdotH = max(dot(V, H), 0.0);

        return F0 + (1.0 - F0) * pow(clamp(1.0 - VdotH, 0.0, 1.0), 5.0);
    }

    // Full fresnel formula (Dielectrics)
    if (fresnelType == 1)
    {
        // F_dielectric =
        // 1/2 * [ ((cosθ - η cosθ_t)/(cosθ + η cosθ_t))^2
        //       + ((η cosθ - cosθ_t)/(η cosθ + cosθ_t))^2 ]
        //
        // cosθ   = V·H
        // cosθ_t = sqrt(1 - (1/η^2)(1 - cos^2θ))

        float etaI = 1.0;   // air
        float etaT = ior;

        float cosTheta = clamp(dot(V, H), -1.0, 1.0);

        // Handle entering / exiting
        if (cosTheta < 0.0)
        {
            float temp = etaI;
            etaI = etaT;
            etaT = temp;
            cosTheta = abs(cosTheta);
        }

        float eta = etaI / etaT;
        float sinThetaT2 = eta * eta * (1.0 - cosTheta * cosTheta);

        // Total internal reflection
        if (sinThetaT2 >= 1.0)
            return vec3(1.0);

        float cosThetaT = sqrt(1.0 - sinThetaT2);

        float Rs = (etaT * cosTheta - etaI * cosThetaT) /
                   (etaT * cosTheta + etaI * cosThetaT);

        float Rp = (etaI * cosTheta - etaT * cosThetaT) /
                   (etaI * cosTheta + etaT * cosThetaT);

        float F = 0.5 * (Rs * Rs + Rp * Rp);

        return vec3(F);
    }

    // Full fresnel formula (Conductors)
    if (fresnelType == 2)
    {
        // F_conductor = ((η^2 + κ^2 - 2η cosθ + cos^2θ) / (η^2 + κ^2 + 2η cosθ + cos^2θ))
        //
        // For conductors, we need both η (refractive index) and κ (extinction coefficient)
        // These are wavelength-dependent, giving metals their characteristic colors
        
        float cosTheta = clamp(dot(V, H), 0.0, 1.0);
        float cosTheta2 = cosTheta * cosTheta;
        
        // Use objectColor to derive approximate η and κ for the metal
        // This is a simplified artistic approach - real metals have complex wavelength-dependent values
        // We'll use the object color as a tint and IOR as the base reflectivity strength
        
        // Get values of eta and kappa from presets
        vec3 eta;
        vec3 kappa;

        if (conductorPresetType == 0)       // Gold
        {
            eta   = vec3(0.17, 0.35, 1.50);
            kappa = vec3(3.10, 2.70, 1.90);
        }
        else if (conductorPresetType == 1)  // Copper
        {
            eta   = vec3(0.20, 1.10, 1.30);
            kappa = vec3(3.90, 2.60, 2.30);
        }
        else if (conductorPresetType == 2)  // Aluminum
        {
            eta   = vec3(1.44, 0.96, 0.61);
            kappa = vec3(7.30, 6.50, 5.40);
        }
        else if (conductorPresetType == 3)  // Silver
        {
            eta   = vec3(0.14, 0.16, 0.13);
            kappa = vec3(4.10, 3.10, 2.30);
        }
        else if (conductorPresetType == 4)  // Iron
        {
            eta   = vec3(2.90);
            kappa = vec3(3.30);
        }

        vec3 eta2 = eta * eta;
        vec3 kappa2 = kappa * kappa;
        
        vec3 eta2_kappa2 = eta2 + kappa2;
        vec3 eta_cosTheta = 2.0 * eta * cosTheta;
        
        vec3 numerator = eta2_kappa2 - eta_cosTheta + cosTheta2;
        vec3 denominator = eta2_kappa2 + eta_cosTheta + cosTheta2;
        
        vec3 F = numerator / max(denominator, vec3(0.0001));
        
        return F;
    }
        
    // Disabled
    return vec3(1.0);
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

    // Get the anisotropy
    float anisotropy = anisotropyValue;

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
    
    float D = Distribution(roughness, anisotropy, N, L, V, H, T, B);
    float G = Geometry(roughness, anisotropy, N, L, V, H, T, B);
    vec3  F = Fresnel(metallic, ior, N, L, V, H, T, B);

    // The Cook-Torrance Fraction
    vec3 numerator = D * G * F;
    float denominator = 4.0 * NdotL * NdotV;

    vec3 specular = numerator / max(denominator, 0.0001) * NdotL * lightColor;
    
    // ==================== Multiscatter Energy Compensation ====================
    
    // Accurate
    if (multiScatterType == 0)
    {
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
    }

    // Approximate
    if (multiScatterType == 1)
    {
        float E_ss = 1.0 - 0.28 * roughness * roughness;
        float E_ms = 1.0 - E_ss;

        // Average Fresnel
        float reflectivity = pow((1.0 - ior) / (1.0 + ior), 2.0);
        vec3 F0 = mix(vec3(reflectivity), objectColor, metallic);
        
        vec3 F_avg = F0 + (1.0 - F0)/21;

        // Not multiplying by NdotL because this is light that alreaedy failed the geomerty term
        vec3 multiScatter = E_ms * F_avg * lightColor;

        specular += multiScatter;
    }

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

    // In PBR, we usually need much brighter lights (Intensity > 1.0).
    // For this demo, let's multiply the final result by an "Exposure" factor 
    // just to see it clearly on our screen.
    float exposure = 3.0; 
    result *= exposure; 

    // TONE MAPPING (squash infinity to 1.0)
    //result = ACESFilm(result);

    // GAMMA CORRECTION (fix for human eyes)
    //result = pow(result, vec3(1.0 / 2.2));

    finalColor = vec4(result, alpha);
}