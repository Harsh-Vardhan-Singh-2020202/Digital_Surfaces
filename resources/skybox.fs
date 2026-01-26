#version 330

// Inputs from the Vertex Shader
in vec3 fragPosition;

// Uniforms
uniform sampler2D texture0;  //Raylib automatically binds MATERIAL_MAP_DIFFUSE as texture0

// Output color to the screen
out vec4 finalColor;

// Define PI
const float PI = 3.14159265359;

vec2 SampleSphericalMap(vec3 v)
{
    // Normalize the direction vector
    vec3 dir = normalize(v);
    
    // Convert to spherical coordinates
    vec2 uv = vec2(atan(dir.z, dir.x), asin(-dir.y));
    
    // Normalize to [0, 1] range
    uv *= vec2(0.1591, 0.3183);  // 1/(2*PI) and 1/PI
    uv += 0.5;
    
    return uv;
}

void main()
{
    vec2 uv = SampleSphericalMap(fragPosition);
    
    // Use texture0 instead of environmentMap
    vec3 color = texture(texture0, uv).rgb;
    
    // Add tone mapping for HDR
    //color = color / (color + vec3(1.0));
    //color = pow(color, vec3(1.0/2.2));
    
    finalColor = vec4(color, 1.0);
}