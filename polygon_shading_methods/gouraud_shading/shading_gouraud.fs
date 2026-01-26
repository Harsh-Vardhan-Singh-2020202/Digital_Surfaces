#version 330

// Inputs from the Vertex Shader
in vec3 vertColor; // The GPU has smoothly blended the vertex colors for us

// Output color to the screen
out vec4 finalColor;

void main()
{
    finalColor = vec4(vertColor, 1.0);
}