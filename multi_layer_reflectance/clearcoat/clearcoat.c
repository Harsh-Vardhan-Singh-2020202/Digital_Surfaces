/*
-> Press F5 to run
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
    Image img = LoadImage("resources/sky1_2k_panaroma.jpg");
    Texture2D panorama = LoadTextureFromImage(img);
    UnloadImage(img);

    // Create skybox cube mesh
    Mesh cube = GenMeshCube(100.0f, 100.0f, 100.0f);
    Model skybox = LoadModelFromMesh(cube);

    // Load skybox shader and set panorama texture
    skybox.materials[0].shader = LoadShader("resources/skybox.vs", "resources/skybox.fs");
    
    // The shader converts the panorama to a skybox view
    skybox.materials[0].maps[MATERIAL_MAP_DIFFUSE].texture = panorama;
    
    // Generate a torus mesh
    Mesh mesh = GenMeshTorus(0.4f, 1.0f, 48, 96);
    Model torus = LoadModelFromMesh(mesh);

    // Generate tangents
    GenMeshTangents(&mesh);

    // Load and assign the shaders
    Shader shader = LoadShader("multi_layer_reflectance/clearcoat/clearcoat.vs", "multi_layer_reflectance/clearcoat/clearcoat.fs");
    torus.materials[0].shader = shader;

    // Assign the uniforms
    int lightPosLoc    = GetShaderLocation(shader, "lightPos");
    int lightColorLoc  = GetShaderLocation(shader, "lightColor");
    int objectColorLoc = GetShaderLocation(shader, "objectColor");
    int viewPosLoc     = GetShaderLocation(shader, "viewPos");
    int roughnessValueLoc = GetShaderLocation(shader, "roughnessValue");
    int metallicValueLoc  = GetShaderLocation(shader, "metallicValue");
    int iorValueLoc = GetShaderLocation(shader, "iorValue");
    int alphaLoc       = GetShaderLocation(shader, "alphaValue");
    int clearcoatWeightValueLoc = GetShaderLocation(shader, "clearcoatWeightValue");
    int clearcoatRoughnessValueLoc = GetShaderLocation(shader, "clearcoatRoughnessValue");
    int clearcoatIorValueLoc = GetShaderLocation(shader, "clearcoatIorValue");
    int clearcoatTintLoc = GetShaderLocation(shader, "clearcoatTint");

    int envLoc = GetShaderLocation(skybox.materials[0].shader, "environmentMap");
    
    // Set static uniform values
    Vector3 lightPos = { 5.0f, 5.0f, 5.0f };
    Vector3 lightColor = { 1.0f, 1.0f, 1.0f };
    Vector3 objectColor = { 0.5f, 0.0f, 0.0f };
    Vector3 clearcoatTint = { 1.0f, 1.0f, 1.0f };

    // Update shader uniform values

    SetShaderValue(shader, lightPosLoc, &lightPos, SHADER_UNIFORM_VEC3);                        // Light position
    SetShaderValue(shader, lightColorLoc, &lightColor, SHADER_UNIFORM_VEC3);                    // Light color
    SetShaderValue(shader, objectColorLoc, &objectColor, SHADER_UNIFORM_VEC3);                  // Object color

    float cameraPos[3] = { camera.position.x, camera.position.y, camera.position.z };
    SetShaderValue(shader, viewPosLoc, cameraPos, SHADER_UNIFORM_VEC3);                         // View position

    float roughnessSliderValue = 0.5f;
    float roughnessValue = roughnessSliderValue;
    SetShaderValue(shader, roughnessValueLoc, &roughnessValue, SHADER_UNIFORM_FLOAT);           // Roughness

    float metallicSliderValue = 0.5f;
    float metallicValue = metallicSliderValue;
    SetShaderValue(shader, metallicValueLoc, &metallicValue, SHADER_UNIFORM_FLOAT);             // Metallic

    float iorSliderValue = 1.5; // 1 = vaccume
    float iorValue = iorSliderValue;
    SetShaderValue(shader, iorValueLoc, &iorValue, SHADER_UNIFORM_FLOAT);                       // IOR

    float alphaSliderValue = 1.0f;
    float alphaValue = alphaSliderValue;
    SetShaderValue(shader, alphaLoc, &alphaValue, SHADER_UNIFORM_FLOAT);                        // Alpha - Visibility  

    float clearcoatWeightSliderValue = 0.0f;
    float clearcoatWeightValue = clearcoatWeightSliderValue;
    SetShaderValue(shader, clearcoatWeightValueLoc, &clearcoatWeightValue, SHADER_UNIFORM_FLOAT);       // Clearcoat weight

    float clearcoatRoughnessSliderValue = 0.025f;
    float clearcoatRoughnessValue = clearcoatRoughnessSliderValue;
    SetShaderValue(shader, clearcoatRoughnessValueLoc, &clearcoatRoughnessValue, SHADER_UNIFORM_FLOAT); // Clearcoat roughness

    float clearcoatIorSliderValue = 1.5; // 1 = vaccume
    float clearcoatIorValue = clearcoatIorSliderValue;
    SetShaderValue(shader, clearcoatIorValueLoc, &clearcoatIorValue, SHADER_UNIFORM_FLOAT);             // Clearcoat IOR


    SetShaderValue(shader, clearcoatTintLoc, &clearcoatTint, SHADER_UNIFORM_VEC3);                      // Clearcoat tint

    // Passing the environment map to the skybox shader
    SetShaderValueTexture(skybox.materials[0].shader, envLoc, panorama);

    // Lock the frames rate
    SetTargetFPS(60);
    
    // Main render loop
    while (!WindowShouldClose())
    {
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
        
        // Start rendering
        BeginDrawing();

        // Update roughness from slider
        roughnessValue = roughnessSliderValue;
        SetShaderValue(shader, roughnessValueLoc, &roughnessValue, SHADER_UNIFORM_FLOAT);

        // Update metallic from slider
        metallicValue = metallicSliderValue;
        SetShaderValue(shader, metallicValueLoc, &metallicValue, SHADER_UNIFORM_FLOAT);

        // Update IOR from slider
        iorValue = iorSliderValue;
        SetShaderValue(shader, iorValueLoc, &iorValue, SHADER_UNIFORM_FLOAT);

        // Update alpha from slider
        alphaValue = alphaSliderValue;
        SetShaderValue(shader, alphaLoc, &alphaValue, SHADER_UNIFORM_FLOAT);

        // Update clearcoat weight from slider
        clearcoatWeightValue = clearcoatWeightSliderValue;
        SetShaderValue(shader, clearcoatWeightValueLoc, &clearcoatWeightValue, SHADER_UNIFORM_FLOAT);

        // Update clearcoat roughness from slider
        clearcoatRoughnessValue = clearcoatRoughnessSliderValue;
        SetShaderValue(shader, clearcoatRoughnessValueLoc, &clearcoatRoughnessValue, SHADER_UNIFORM_FLOAT);

        // Update clearcoat IOR from slider
        clearcoatIorValue = clearcoatIorSliderValue;
        SetShaderValue(shader, clearcoatIorValueLoc, &clearcoatIorValue, SHADER_UNIFORM_FLOAT);
        
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
        
        // Draw the torus model at given position, scale and color
        DrawModel(torus, (Vector3){0,0,0}, 1.0f, (Color){objectColor.x * 255, objectColor.y * 255, objectColor.z * 255, 255});

        // Exit 3D mode and return to 2D rendering
        EndMode3D();

        // Add information text
        char infoText[128];
        snprintf(infoText, sizeof(infoText), "Diffuse Burley + Specular Cook-Torrance Lighting + Clearcoat Layer");
        DrawText(infoText, 10, 10, 20, BLACK);

        DrawText("Main Layer", 10, 40, 20, BLACK);

        // Draw roughness slider
        DrawText("Roughness", 10, 70, 20, BLACK);
        GuiSlider((Rectangle){ 130, 70, 200, 20 }, "", TextFormat("%.2f", roughnessSliderValue), &roughnessSliderValue, 0.0f, 1.0f);

        // Draw metallic slider
        DrawText("Metallic", 10, 100, 20, BLACK);
        GuiSlider((Rectangle){ 130, 100, 200, 20 }, "", TextFormat("%.2f", metallicSliderValue), &metallicSliderValue, 0.0f, 1.0f);

        // Draw IOR slider
        DrawText("IOR", 10, 130, 20, BLACK);
        GuiSlider((Rectangle){ 130, 130, 200, 20 }, "", TextFormat("%.2f", iorSliderValue), &iorSliderValue, 1.0f, 3.5f);

        // Draw alpha slider
        DrawText("Alpha", 10, 160, 20, BLACK);
        GuiSlider((Rectangle){ 130, 160, 200, 20 }, "", TextFormat("%.2f", alphaSliderValue), &alphaSliderValue, 0.0f, 1.0f);

        DrawText("Clearcoat Layer", 410, 40, 20, BLACK);

        // Draw Clearcoat Weight slider
        DrawText("Weight", 410, 70, 20, BLACK);
        GuiSlider((Rectangle){ 550, 70, 200, 20 }, "", TextFormat("%.2f", clearcoatWeightSliderValue), &clearcoatWeightSliderValue, 0.0f, 1.0f);

        // Draw Clearcoat Roughness slider
        DrawText("Roughness", 410, 100, 20, BLACK);
        GuiSlider((Rectangle){ 550, 100, 200, 20 }, "", TextFormat("%.3f", clearcoatRoughnessSliderValue), &clearcoatRoughnessSliderValue, 0.0f, 1.0f);

        // Draw Clearcoat IOR slider
        DrawText("IOR", 410, 130, 20, BLACK);
        GuiSlider((Rectangle){ 550, 130, 200, 20 }, "", TextFormat("%.2f", clearcoatIorSliderValue), &clearcoatIorSliderValue, 1.0f, 3.5f);
        
        // Finish the frame and present it on screen
        EndDrawing();
    }

    // Cleanup
    UnloadTexture(panorama);
    UnloadModel(skybox);
    UnloadModel(torus);
    UnloadShader(shader);
    CloseWindow();

    return 0;
}