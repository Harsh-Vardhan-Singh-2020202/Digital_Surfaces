#version 330

// Inputs from the Vertex Shader
in vec3 fragNormal;
in vec3 fragPosition;

// Uniforms (set from phong.c)
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
    // NOTE: We will use the Burley Diffuse Model combined with a Phong Specular Model

    // Setup vectors
    vec3 N = normalize(fragNormal);                 // Normal Vector
    vec3 L = normalize(lightPos - fragPosition);    // Light Vector
    vec3 V = normalize(viewPos - fragPosition);     // View Vector
    vec3 H = normalize(L + V);                      // Half Vector
    vec3 R = reflect(-L, N);                        // Reflection Vector 

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
    
    // The shininess will be derived from the roughness input.
    // Map roughness to shininess exponent
    // High roughness (1.0) -> low shininess (2.0)
    // Low roughness (0.0) -> high shininess (256.0)
    float shininess = pow(2.0, (1.0 - roughnessValue) * 8.0); 

    // ==================== Ambient Term (Simple) ====================
    
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * lightColor * objectColor;

    // ==================== Diffuse Term (Lambert) ====================

    // The Lambertian formula (Energy conserving term = 1/pi)
    vec3 diffuse = (objectColor / PI) * lightColor * NdotL;
    
    // ==================== Specular Term (Phong) ====================
    
    // Calculate specular intensity
    float specDot = max(dot(R, V), 0.0);
    float specPower = pow(specDot, shininess);
    
    // Intensity of the highlight
    float specularStrength = 0.15; 
    
    // If light is behind the surface, no specular should be present
    vec3 specular = vec3(0.0);
    if (rawNdotL > 0.0) 
    {
        // Normalize the specular intensity so it gets dimmer as it gets wider
        // Ensures the specular highlight maintains consistent total energy regardless of the shininess/roughness value.
        float energyConservation = (shininess + 2.0) / (8.0 * PI); 
        specular = specularStrength * lightColor * specPower * energyConservation;
    }

    // ==================== Combine ====================
    
    vec3 result = ambient + diffuse + specular;

    // In PBR, we usually need much brighter lights (Intensity > 1.0).
    // For this demo, let's multiply the final result by an "Exposure" factor 
    // just to see it clearly on our screen.
    float exposure = 3.0; 
    
    finalColor = vec4(result * exposure, 1.0);
}