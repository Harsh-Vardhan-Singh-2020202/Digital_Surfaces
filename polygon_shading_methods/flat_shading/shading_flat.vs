#version 330

// Input attributes from the 3D model (Raylib sends these automatically)
in vec3 vertexPosition;
in vec3 vertexNormal;

// Uniforms (Global variables sent by Raylib)
uniform mat4 mvp;       // Model-View-Projection Matrix
uniform mat4 matModel;  // Model Matrix

// Outputs to the Pixel Shader
// "flat" tells the GPU not to interpolate this value!
flat out vec3 fragNormal;
out vec3 fragPosition;

void main()
{
    // Calculate the final position of the vertex on the screen
    gl_Position = mvp * vec4(vertexPosition, 1.0);

    // Pass the position in world space to the pixel shader
    fragPosition = vec3(matModel * vec4(vertexPosition, 1.0));

    // Pass the normal
    /*
    Because we used "flat out", the rasterizer will pick the normal of
    the "Provoking Vertex" (the first one) and give it to every
    pixel in the triangle.
    */
    mat3 normalMatrix = transpose(inverse(mat3(matModel)));
    fragNormal = normalMatrix * vertexNormal;
}