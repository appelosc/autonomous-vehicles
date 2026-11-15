# Autonomous Vehicles – Rover and drone docking (Unity ML-Agents)

Course project where a rover is trained with reinforcement learning to drive to a meeting point on generated terrain, stop there and wait for a drone to land on it. The rover is controlled by a trained model. The drone is flown manually.

Built with Unity 6 (6000.5.8f1), ML-Agents 4.1.0 and C#.

## What the rover learns

Each episode, the rover and the meeting point are placed at random free positions on the terrain. The rover has to reach the meeting point and stand still there. Obstacles (rocks and buildings) are also moved to random positions each episode, and each rock is only included with a set probability, so the number of obstacles varies.

The final model (`Assets/Models/Rover-final.onnx`) succeeds in about 85–90% of episodes.

### Observations (8 values)

- Distance to the meeting point (normalized)
- The rover's local velocity, forward and sideways (normalized)
- Direction to the meeting point in the rover's local space (x and z)
- Rotation speed around the vertical axis (normalized)
- The rover's tilt (sideways and forward)

### Actions (2 discrete branches)

- Movement: stand still, forward or reverse
- Steering: straight, left or right

### Rewards

| Event | Reward |
| --- | --- |
| Getting closer to the meeting point | Positive, proportional to the distance gained |
| Each step | Small time penalty (`-1 / MaxStep`) |
| Standing still inside the stop radius for the hold time | `+2`, episode ends (success) |
| Hitting a building or rock | `-1`, episode ends |
| Rolling over or falling below the terrain | `-1`, episode ends |
| No progress towards the meeting point for a set time (10 seconds by default) | `-1`, episode ends (stuck) |

Successes, crashes and stuck episodes are also recorded as custom statistics (`Rover/Success`, `Rover/Crash`, `Rover/Stuck`).

## Project structure

| Path | Content |
| --- | --- |
| `Assets/Scripts/RoverAgent.cs` | The ML-Agents agent: observations, actions, rewards and wheel physics |
| `Assets/Scripts/EnviromentController.cs` | Resets each episode: random rover, meeting point and obstacle positions |
| `Assets/Scripts/TerrainGenerator.cs` | Generates the terrain height map with Perlin noise and places rocks |
| `Assets/Scripts/DroneController.cs` | Manual drone flight (thrust, tilt and yaw) |
| `Assets/Scripts/CameraFollow.cs` | Camera that follows a target |
| `Assets/Models/` | Trained models: `Rover-L1_V4.onnx` and `Rover-L2_V3.onnx` (earlier models) and `Rover-final.onnx` |
| `Assets/prefabs/Trainingarea.prefab` | The training area with rover, drone, meeting point and obstacles |
| `Assets/Scenes/SampleScene.unity` | The main scene |

## Development steps

The project was built in four steps, each on its own branch (`steg1` to `steg4`, "steg" is Swedish for "step"):

1. The rover drives to the middle and stops there. The scene resets when the drone lands on the rover.
2. The rover and the meeting point are placed at random, and a trained model manages to navigate between them.
3. Terrain generation.
4. Final fixes and the final model.

## Running the project

1. Open the project in Unity 6 (6000.5.8f1). The ML-Agents package is installed through the package manifest.
2. Open `Assets/Scenes/SampleScene.unity` and press Play. With a model from `Assets/Models/` assigned in the rover's Behavior Parameters, the rover drives on the trained model.
3. Fly the drone with the keyboard: `W` / `A` / `S` / `D` tilt, `Q` / `E` turn left and right, `R` increases thrust and `F` decreases it.

To drive the rover yourself, set its Behavior Type to "Heuristic Only" and use the arrow keys.

## Third-party assets

The 3D models and environment assets under `Assets/` (Polytope Studio, Mellow Fox Studios, Realistic Drone, Dnk_Dev and Pack_Pickup) are third-party asset packs and belong to their respective creators. The scripts in `Assets/Scripts` and the trained models are the work of this project.
