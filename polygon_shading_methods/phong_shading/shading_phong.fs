#version 330

// Inputs from the Vertex Shader
in vec3 fragNormal;
in vec3 fragPosition;

// Uniforms (set from phong.c)
uniform vec3 lightPos;
uniform vec3 lightColor;
uniform vec3 objectColor;
uniform vec3 viewPos;

// Output color to the screen
out vec4 finalColor;

// Define PI
const float PI = 3.14159265359;

void main()
{
    // NOTE: The calculation of lighting will be disucssed in detail in later parts, take it as a given for now

    // Setup vectors
    vec3 N = normalize(fragNormal);              // Normal Dir
    vec3 L = normalize(lightPos - fragPosition); // Light Dir
    vec3 V = normalize(viewPos - fragPosition);  // View Dir
    vec3 R = reflect(-L, N);                     // Reflection Dir

    // Calculate the raw dot product first
    float rawNdotL = dot(N, L);

    // Clamping to a tiny epsilon (0.0001) prevents the denominator from ever reaching a 0/0 situation.
    float NdotL = max(rawNdotL, 0.0001);

    // ==================== Ambient Term (Base) ====================
    
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * lightColor * objectColor;
  	
    // ==================== Diffuse Term (Lambert) ====================
    
    vec3 diffuse = objectColor * lightColor * NdotL;
    
    // ==================== Specular Term (Phong) ==================== 
    
    // Note: This term is not to be confused with phong geometry shading method
    
    float specularStrength = 0.45;
    vec3 specular = vec3(0.0);
    if (rawNdotL > 0.0)
    {
        // Note: Currently we use shininess as fixed value (16), later we will derive it from roughness
        float shininess = 16;
        float spec = pow(max(dot(V, R), 0.0), shininess); 
        float energyConservation = (shininess + 2.0) / (8.0 * PI);
        specular = specularStrength * lightColor * spec * energyConservation;
    }
    
    // ==================== Combine ====================
    
    vec3 result = ambient + diffuse + specular;

    finalColor = vec4(result, 1.0);
}