#version 330

// Inputs from the Vertex Shader
// Note: Must match the "flat" keyword from the vertex shader
flat in vec3 fragNormal;
in vec3 fragPosition;

// Uniforms (set from flat.c)
uniform vec3 lightPos;
uniform vec3 lightColor;
uniform vec3 objectColor;

// Output color to the screen
out vec4 finalColor;

void main()
{
    // NOTE: The calculation of lighting will be disucssed in detail in later parts, take it as a given for now
    
    // The effective face normal
    vec3 N = normalize(fragNormal);

    // Vector from surface to light
    vec3 L = normalize(lightPos - fragPosition);

    // Dot product of N and L, we max it with 0.0 because we can not have negative light
    float NdotL = max(dot(N, L), 0.0);

    // Final color calculation using the Lambertian formula
    vec3 result = objectColor * lightColor * NdotL;
    
    finalColor = vec4(result, 1.0);
}