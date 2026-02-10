#version 330

// Input attributes from the 3D Model (Raylib sends these automatically)
in vec3 vertexPosition;
in vec3 vertexNormal;

// Uniforms (Global variables sent by Raylib)
uniform mat4 mvp;       // Projection * View * Model
uniform mat4 matModel;  // Model Matrix (to get World Space)

// Outputs to the Pixel Shader
out vec3 fragNormal;
out vec3 fragPosition;

void main()
{
    // Calculate the final position of the vertex on the screen
    gl_Position = mvp * vec4(vertexPosition, 1.0);

    // Pass the position in world space to the pixel shader
    fragPosition = vec3(matModel * vec4(vertexPosition, 1.0));

    // Calculate World Space Normal
    mat3 normalMatrix = transpose(inverse(mat3(matModel)));
    fragNormal = normalMatrix * vertexNormal;
}