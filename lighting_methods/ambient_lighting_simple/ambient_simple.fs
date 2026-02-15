#version 330

// Inputs from the Vertex Shader
in vec3 fragNormal;
in vec3 fragPosition;

// Uniforms (set from ambient_simple.c)
uniform vec3 lightColor;
uniform vec3 objectColor;

// Output color to the screen
out vec4 finalColor;

void main()
{
    // ==================== Ambient Term (Simple) ====================

    float ambientStrength = 0.5;
    vec3 ambient = ambientStrength * lightColor * objectColor;

    // ==================== Combine ====================
    
    vec3 result = ambient;

    finalColor = vec4(result, 1.0);
}