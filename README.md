# Echoing Velocity

[![Engine](https://img.shields.io/badge/Made%20with-Unity-black?style=flat-square&logo=unity)](https://unity.com/)
[![Language](https://img.shields.io/badge/Language-C%23-blue?style=flat-square&logo=c-sharp)](#)
[![Playable Build](https://img.shields.io/badge/Playable_Build-itch.io-FA5C5C?style=flat-square&logo=itchdotio)](YOUR_ITCH_LINK)

**Echoing Velocity** is a high-speed, first-person sci-fi platformer built from scratch during an intense 24-hour college game jam. The project centers around advanced player momentum manipulation, gravity inversion, and complex temporal state-rewinding architecture.

## 🧠 System Architecture & Mechanics

### 1. Temporal State Buffer (Time Rewind)
To achieve the rewind mechanic without memory leaks or heavy performance overhead, I implemented a circular data buffer. 
*   **Implementation:** A coroutine samples the player's `Transform` (Position, Rotation) and `Rigidbody` velocity at fixed intervals, storing them in a `Struct` array. 
*   **Execution:** Upon triggering the rewind state, the physics engine is temporarily suspended while the controller interpolates backward through the cached array, restoring the player's momentum precisely at the exit frame.

### 2. Custom Momentum-Based Kinematics
Standard Unity rigidbodies were insufficient for the required snappy platforming feel.
*   Engineered a custom physics handler that processes input vectors, applies friction curves based on grounded states, and calculates "Echo Dash" bursts utilizing dot products to determine maximum velocity thresholds.

### 3. Dynamic Gravity Manipulation
*   Implemented trigger zones that broadcast rotation matrices to the player controller, interpolating the camera and gravity vectors seamlessly by 90 to 180 degrees to traverse walls and ceilings.

## 📂 Key Scripts to Review
*   `PlayerMovementController.cs` - Core kinematic calculations and input handling.
*   `TimeRewindManager.cs` - The circular buffer logic and state restoration.
*   `GravityZone.cs` - Matrix rotation and local gravity overrides.

## ⚙️ Installation & Setup
1. Clone the repository.
2. Open the project in **Unity 2022.x** or newer.
3. Ensure text mesh pro dependencies are imported. Press Play.
