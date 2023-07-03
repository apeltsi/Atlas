<p align="center">
  <img width="612" height="459" src="https://solidcodegames.com/assets/Atlas-Feature-Small.png" alt="Atlas Logo">
</p>

# Atlas

## A .NET 2D Framework

Atlas is a 2D game framework with the goal of being flexible,
cross-platform, performant and developer friendly. The framework has a
syntax similar to Unity and uses C# as its primary language, making it
easy to learn for developers already familiar with Unity.

## Getting Started

[Check out the Getting Started Tutorial](https://github.com/apeltsi/Atlas/wiki/Getting-Started)

## IMPORTANT!

Atlas is still quite early in development and is very much subject to change.
There is also still a lot of polish needed before Atlas is ready to be used for bigger projects. 

## Quick Feature Rundown

- Similar coding experience to Unity
- Rendering with Vulkan or Direct3D11
- Audio With OpenAL
- Input with Mouse & Keyboard or Gamepad
- Support for HLSL or GLSL Shaders
- Multithreading
- Windows & Linux support (Tested on SteamDeck) 

## Portability

Atlas uses the .NET framework, this together with its support for the Direct3D11 and Vulkan backends makes it compatible with Linux & Windows. 
(Compatibility for MacOS and mobile devices is also planned)

## Flexibility

Being based on the .NET framework, Atlas gives you full access to all
of the tools in the .NET API. Atlas also has a very customizable
rendering pipeline, allowing you to write your own shaders,
post-processing effects, and even allowing you to send custom
instructions to the GPU.

## Size

When correctly configured, an empty C# app using Atlas will generally compile to under 18MiB.

(Tested with Atlas-Boilerplate, with `dotnet publish --configuration Release --arch x64`)

Please note that the final compile-size of an Atlas app will change as development continues.

## Tools

Any Debug build of Atlas will include the Telescope debugger. A web based interface that seamlessly connects to Atlas giving you useful insights into your app's performance.
