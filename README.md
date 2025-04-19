# Freeze Tag Game Project

## Project Overview
The Freeze Tag Game is a Unity-based simulation project that uses machine learning to train agents in a freeze tag environment. The game involves two types of agents: Runners and Taggers. Runners aim to avoid being frozen by Taggers, while Taggers attempt to freeze all Runners. The project leverages Unity's ML-Agents toolkit to implement reinforcement learning algorithms, allowing agents to learn and improve their strategies over time.

## Setup Instructions
To set up this project from the Git repository, follow these steps:

1. **Clone the Repository**: Clone the project repository to your local machine using the following command:
   ```bash
   git clone <repository-url>
   ```

2. **Open in Unity**: Launch Unity Hub and open the cloned project folder. Ensure you have the correct version of Unity installed as specified in the project settings.

3. **Install ML-Agents**: If not already included, install the ML-Agents package via Unity's Package Manager. This project requires ML-Agents version 2.0 or later.

4. **Configure Environment**: Adjust the environment settings in the `Assets/config/environment_config.yaml` file to suit your training needs.

5. **Run the Simulation**: Press the Play button in the Unity Editor to start the simulation. The agents will begin training based on the configured parameters.

## Rewards
The project uses a reward system to guide the learning process of the agents. Here is a list of rewards used for both Runners and Taggers:

### Runners
- **Survival Reward**: +0.001 per timestep for staying unfrozen.
- **Unfreeze Teammate Reward**: +0.005 per timestep while unfreezing a teammate, +1.5 for successfully unfreezing.
- **Wall Creation Reward**: +0.1 for creating a wall.
- **Frozen Penalty**: -1.0 for getting frozen.

### Taggers
- **Freeze Runner Reward**: +1.0 for freezing a runner by direct contact.
- **Freeze Ball Hit Reward**: +1.5 for freezing a runner with a freeze ball.
- **Collect Freeze Ball Reward**: +0.2 for collecting a freeze ball.

## Metrics
The project tracks various metrics to evaluate the performance of the agents and the overall simulation. These metrics include:

- **Episode Length**: Duration of each episode.
- **Runners Win Count**: Number of episodes won by Runners.
- **Taggers Win Count**: Number of episodes won by Taggers.
- **Total Freezes and Unfreezes**: Count of freeze and unfreeze events.
- **Freeze Balls and Wall Balls Collected**: Number of items collected by agents.
- **Total Time Spent Freezing**: Cumulative time agents spend in a frozen state.
- **Fastest and Average Unfreeze Time**: Metrics for evaluating unfreeze efficiency.
- **Wall Hits**: Count of wall hits by Taggers and Freeze Balls.

These metrics are exported to a CSV file after every 5 episodes and at the end of the simulation, providing a comprehensive overview of the training progress and agent performance.

---

This README provides a high-level overview of the Freeze Tag Game project, including setup instructions, reward details, and metrics information. For more detailed information on the code and implementation, please refer to the source files and comments within the project. 