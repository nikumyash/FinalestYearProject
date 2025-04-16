# Freeze Tag ML-Agents Project

A Unity ML-Agents implementation of a freeze tag game with two teams:

1. **Runners**: Avoid being frozen until time runs out
2. **Taggers**: Freeze all runners before time runs out

## Game Mechanics

### Runners
- Can move in all directions (forward, backward, strafe, rotate)
- Can collect wall balls to create defensive shields
- Will be frozen when touched by a tagger or hit by a freeze ball
- Can unfreeze teammates by staying near them for 3 seconds
- Win when time expires and at least one runner is unfrozen

### Taggers
- Can move in all directions (forward, backward, strafe, rotate)
- Can collect freeze balls to shoot at runners
- Win when all runners are frozen

## Environment Parameters

The game loads environment parameters from a JSON file (`Assets/Resources/environment_param.json`). These parameters include:

- `lesson`: Difficulty level
- `level_index`: Level index
- `num_foodballs`: Number of wall balls
- `num_runners`: Number of runner agents
- `num_taggers`: Number of tagger agents
- `num_freezeballs`: Number of freeze balls
- `time_limit`: Episode time limit

## Project Structure

- **GameManager**: Controls game flow, environment setup, and statistics tracking
- **UIManager**: Handles UI display (runner/tagger wins)
- **Agent Scripts**:
  - `AgentMovement`: Shared movement script for both agent types
  - `RunnerAgent`: ML-Agent implementation for runners
  - `TaggerAgent`: ML-Agent implementation for taggers
- **Item Scripts**:
  - `FreezeBall`: Collectible and projectile freeze balls
  - `WallBall`: Collectible wall balls
  - `Wall`: Defensive walls created by runners

## Experiment Data Collection

The system automatically collects the following data:

- Runner/Tagger wins
- Episode length
- Freeze/unfreeze counts
- Item collection statistics
- Object usage statistics

This data is saved to a CSV file when the experiment completes.

## Setup Instructions

1. Open the project in Unity (2020.3 or newer with ML-Agents package installed)
2. Ensure all prefabs are correctly assigned in the GameManager
3. Set up spawn points in the scene
4. Adjust environment parameters in the JSON file if needed
5. Press Play to start training or testing

## ML-Agents Configuration

The project is set up to work with ML-Agents for training. Key features:

- Automatically detects Academy for training mode
- Unlimited episodes when Academy is found (or 5 episodes in standalone mode)
- Event-driven architecture for accurate data collection
- Separate observation spaces for different agent types 