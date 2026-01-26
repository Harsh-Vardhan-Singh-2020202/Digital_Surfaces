#version 330

// Input attributes from the 3D model (Raylib sends these automatically)
in vec3 vertexPosition;

// Uniforms (Global variables sent by Raylib)
uniform mat4 matProjection;
uniform mat4 matView;

// Outputs to the Pixel Shader
out vec3 fragPosition;

void main()
{
    fragPosition = vertexPosition;
    
    // Remove translation from view matrix
    mat4 rotView = mat4(mat3(matView));
    vec4 clipPos = matProjection * rotView * vec4(vertexPosition, 1.0);
    
    // Set z = w so depth is always 1.0 (furthest)
    gl_Position = clipPos.xyww;
}