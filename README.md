# Digital Surfaces: A Deep Dive into Physically Based Reflectance

This repository contains the source code and shaders for the **Digital Surfaces** series on Medium. The project is a progressive exploration of computer graphics, moving from basic geometric interpolation to advanced physically based rendering (PBR) pipelines.

## The Series
You can follow the full deep-dive articles here:
- [Part 1: The Evolution of Digital Appearance](https://medium.com/@harvarsin/digital-surfaces-a-deep-dive-into-physically-based-reflectance-part-1-5a4191d66d32)
- Part 2: Geometry, Interpolation, and Artifacts (Coming Soon)

## 🛠 Project Structure
The code is organized by the part of the series it represents:
- `/polygon_shading_methods`: Implementations of Flat, Gouraud, and Phong shading (Part 2).
- `/ambient_lighting`: Image-Based Lighting (IBL) and environment mapping (Part 3 - Upcoming).

## 🚀 Getting Started

### Dependencies
This project uses **[Raylib](https://www.raylib.com/)**, a simple and easy-to-use library for enjoying games programming.

### Setup (Windows/MinGW)
1. Install [Raylib](https://github.com/raysan5/raylib/releases).
2. It is recommended to use the [Raylib VS Code Template](https://stackoverflow.com/questions/63791456/how-to-add-raylib-to-vs-code) for a hassle-free setup.
3. Ensure `raylib.h` and the corresponding binaries are in your compiler path.

### Building
To run the examples:
1. Open the specific folder (e.g., `/flat_shading`) in your editor.
2. Compile `main.c` linked with Raylib.
3. Ensure the `.vs` and `.fs` shader files are in the same directory as the executable or update the load path in the code.

## ⚖️ License
This project is open-source. Feel free to use the math and shader logic for your own learning and projects.
