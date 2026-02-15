#version 330

// Input attributes from the 3D Model (Raylib sends these automatically)
in vec3 vertexPosition;
in vec3 vertexNormal;

// Uniforms (Global variables sent by Raylib)
uniform mat4 mvp;       // Model-View-Projection Matrix
uniform mat4 matModel;  // Model Matrix

// Uniforms (set from gouraud.c)
uniform vec3 lightPos;
uniform vec3 lightColor;
uniform vec3 objectColor;
uniform vec3 viewPos;

// Outputs to the Pixel Shader
out vec3 vertColor;

void main()
{
    // NOTE: The calculation of lighting will be disucssed in detail in later parts, take it as a given for now

    // Calculate the final position of the vertex on the screen
    gl_Position = mvp * vec4(vertexPosition, 1.0);

    // Pass the position in world space to the pixel shader
    vec3 fragPosition = vec3(matModel * vec4(vertexPosition, 1.0));
    
    // Calculate vectors (At the vertex!)
    mat3 normalMatrix = transpose(inverse(mat3(matModel)));
    vec3 N = normalize(normalMatrix * vertexNormal);                    // Normal
    vec3 L = normalize(lightPos - fragPosition);                        // Light Dir
    vec3 V = normalize(viewPos - fragPosition);                         // View Dir
    vec3 R = reflect(-L, N);                                            // Reflection Dir

    // Calculate the raw dot product first
    float rawNdotL = dot(N, L);

    // Clamping to a tiny epsilon (0.0001) prevents the denominator from ever reaching a 0/0 situation.
    float NdotL = max(rawNdotL, 0.0001);

    // ==================== Ambient Term (Simple) ====================

    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * lightColor * objectColor;
  	
    // ==================== Diffuse Term (Lambert) ====================
    
    vec3 diffuse = objectColor * lightColor * NdotL;
    
    // ==================== Specular Term (Phong) ==================== 
    
    // Note: This term is not to be confused with phong geometry shading method

    float specularStrength = 0.5;
    vec3 specular = vec3(0.0);
    if (rawNdotL > 0.0)
    {
        float spec = pow(max(dot(V, R), 0.0), 32);
        specular = specularStrength * spec * lightColor;
    }
    
    // ==================== Combine ====================
    
    vertColor = ambient + diffuse + specular;
}