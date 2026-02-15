/*
-> Press F5 to run
-> Press F11 to preview fullscreen borderless
-> Press middle mouse button to move the camera
-> Use mouse wheel to zoom in and out
-> Press left mouse button to interact with the GUI
*/

#define RAYGUI_IMPLEMENTATION

#include <stdio.h>
#include "raylib.h"
#include "raymath.h"
#include "raygui.h"
#include "rlgl.h"

int main()
{
    // Set window dimensions
    const int screenWidth = 800;
    const int screenHeight = 800;

    // Resizable window
    SetConfigFlags(FLAG_WINDOW_RESIZABLE);
    
    // Resizable window
    SetConfigFlags(FLAG_WINDOW_RESIZABLE);

    // Initialize the window
    InitWindow(screenWidth, screenHeight, "Shading Lab");

    // Define the camera
    Camera camera = { 0 };
    camera.position = (Vector3){2.0f, 1.0f, 0.0f };
    camera.target = (Vector3){ 0.0f, 0.0f, 0.0f };
    camera.up = (Vector3){ 0.0f, 1.0f, 0.0f };
    camera.fovy = 45.0f;
    camera.projection = CAMERA_PERSPECTIVE;

    // Variables for camera orbiting
    float yaw = 0.0f;
    float pitch = 0.0f;
    float radius = 2.5f;

    // Load the panoramic environment map
    Image img = LoadImage("resources/sky2_2k.jpg");

    // Generate mipmaps on CPU before uploading to GPU
    ImageMipmaps(&img);
    printf("Generated %d mipmap levels for environment map\n", img.mipmaps);

    Texture2D panorama = LoadTextureFromImage(img);
    SetTextureWrap(panorama, TEXTURE_WRAP_REPEAT);
    SetTextureFilter(panorama, TEXTURE_FILTER_BILINEAR);
    UnloadImage(img);

    // Create skybox cube mesh
    Mesh cube = GenMeshCube(100.0f, 100.0f, 100.0f);
    Model skybox = LoadModelFromMesh(cube);

    // Load skybox shader and set cube map texture
    skybox.materials[0].shader = LoadShader("resources/skybox.vs", "resources/skybox.fs");
    
    // The shader converts the panorama to a skybox view
    skybox.materials[0].maps[MATERIAL_MAP_DIFFUSE].texture = panorama;
    
    // Generate a torus mesh
    Mesh mesh1 = GenMeshTorus(0.4f, 1.0f, 24, 48);
    Model torus = LoadModelFromMesh(mesh1);
    Mesh mesh2 = GenMeshSphere(0.4f, 48, 48);
    Model sphere = LoadModelFromMesh(mesh2);

    // Change the torus orientation
    torus.transform = MatrixRotateX(DEG2RAD * 90.0f);
    sphere.transform = MatrixRotateX(DEG2RAD * 90.0f);

    // Load and assign the shaders
    Shader shader = LoadShader("lighting_methods/ambient_lighting_ibl/ambient_ibl.vs", "lighting_methods/ambient_lighting_ibl/ambient_ibl.fs");
    torus.materials[0].shader = shader;
    sphere.materials[0].shader = shader;

    // Assign the uniforms
    int lightColorLoc  = GetShaderLocation(shader, "lightColor");
    int objectColorLoc = GetShaderLocation(shader, "objectColor");
    int viewPosLoc     = GetShaderLocation(shader, "viewPos");

    int reflectivityValueLoc = GetShaderLocation(shader, "reflectivityValue");

    int envLoc = GetShaderLocation(shader, "reflectionMap");

    // Set static uniform values
    Vector3 lightColor = { 1.0f, 1.0f, 1.0f };
    Vector3 objectColor = { 1.0f, 1.0f, 1.0f };
    //Vector3 objectColor = { 0.5f, 0.0f, 0.0f };

    // Update shader uniform values

    SetShaderValue(shader, lightColorLoc, &lightColor, SHADER_UNIFORM_VEC3);                // Light color
    SetShaderValue(shader, objectColorLoc, &objectColor, SHADER_UNIFORM_VEC3);              // Object color

    float cameraPos[3] = { camera.position.x, camera.position.y, camera.position.z };
    SetShaderValue(shader, viewPosLoc, cameraPos, SHADER_UNIFORM_VEC3);                     // View position

    float reflectivitySliderValue = 0.5f;
    float reflectivityValue = reflectivitySliderValue;
    SetShaderValue(shader, reflectivityValueLoc, &reflectivityValue, SHADER_UNIFORM_FLOAT); // Reflectivity

    // Bind environment map
    torus.materials[0].maps[MATERIAL_MAP_EMISSION].texture = panorama;
    sphere.materials[0].maps[MATERIAL_MAP_EMISSION].texture = panorama;
    shader.locs[SHADER_LOC_MAP_EMISSION] = envLoc;

    // Lock the frames rate
    SetTargetFPS(60);

    // Main render loop
    while (!WindowShouldClose())
    {
        // Fullscreen borderless
        if (IsKeyPressed(KEY_F11))
        {
            ToggleBorderlessWindowed();
        }

        // Camera orbit controls
        if (IsMouseButtonDown(MOUSE_MIDDLE_BUTTON))
        {
            Vector2 delta = GetMouseDelta();

            yaw   -= delta.x * 0.01f;
            pitch += delta.y * 0.01f;

            // Clamp pitch so camera doesn't flip
            pitch = Clamp(pitch, -PI/2 + 0.1f, PI/2 - 0.1f);
        }

        // Zoom
        float wheel = GetMouseWheelMove();
        radius -= wheel * 0.2f;
        radius = Clamp(radius, 1.0f, 10.0f);

        // Spherical to cartesian
        camera.position.x = radius * cosf(pitch) * sinf(yaw);
        camera.position.y = radius * sinf(pitch);
        camera.position.z = radius * cosf(pitch) * cosf(yaw);

        camera.target = (Vector3){ 0.0f, 0.0f, 0.0f};

        // Update camera position every frame
        float cameraPos[3] = { camera.position.x, camera.position.y, camera.position.z };
        SetShaderValue(shader, viewPosLoc, cameraPos, SHADER_UNIFORM_VEC3);

        // Rotate the torus over time
        static float angle = 0.0f;
        angle += 0.01f;
        torus.transform = MatrixMultiply(
            MatrixRotateZ(angle),
            MatrixRotateX(DEG2RAD * 90.0f)
        );

        // Rotate the sphere over time
        static float angle2 = 0.0f;
        angle2 += 0.01f;
        sphere.transform = MatrixMultiply(
            MatrixRotateZ(angle2),
            MatrixRotateX(DEG2RAD * 90.0f)
        );

        // Start rendering a new frame
        BeginDrawing();

        // Update reflectivity from slider
        reflectivityValue = reflectivitySliderValue;
        SetShaderValue(shader, reflectivityValueLoc, &reflectivityValue, SHADER_UNIFORM_FLOAT);

        // Clear the screen with an off-white background
        ClearBackground((Color){200, 200, 200, 255});

        // Switch to 3D rendering using the given camera
        BeginMode3D(camera);
        
        // Draw skybox (disable depth writing so it's always in background)
        rlDisableBackfaceCulling();
        rlDisableDepthMask();
        DrawModel(skybox, (Vector3){0, 0, 0}, 1.0f, WHITE);
        rlEnableBackfaceCulling();
        rlEnableDepthMask();

        // Draw the torus/sphere model at given position, scale and color
        //DrawModel(torus, (Vector3){0,0,0}, 1.0f, (Color){objectColor.x * 255, objectColor.y * 255, objectColor.z * 255, 255});
        DrawModel(sphere, (Vector3){0,0,0}, 1.0f, (Color){objectColor.x * 255, objectColor.y * 255, objectColor.z * 255, 255});
        
        // Exit 3D mode and return to 2D rendering
        EndMode3D();

        // Add information text
        DrawText("Ambient Lighting - IBL", 10, 10, 20, BLACK);

        // Draw reflectivity slider
        DrawText("Reflectivity", 10, 40, 20, BLACK);
        GuiSlider((Rectangle){ 130, 40, 200, 20 }, "", TextFormat("%.2f", reflectivitySliderValue), &reflectivitySliderValue, 0.0f, 1.0f);
        
        // Finish the frame and present it on screen
        EndDrawing();
    }

    // Cleanup
    UnloadTexture(panorama);
    UnloadModel(skybox);
    UnloadModel(torus);
    UnloadModel(sphere);
    UnloadShader(shader);
    CloseWindow();

    return 0;
}