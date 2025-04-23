# Freeze Tag Game: Agent Learning and Performance

The Freeze Tag Game is a Unity-based simulation project that implements reinforcement learning to train intelligent agents in a freeze tag environment. This project leverages Unity's ML-Agents toolkit to enable agents to learn and improve their strategies through experience.

## Game Overview

The game consists of two competing agent types:
- **Runners**: Agents that try to avoid being frozen by taggers and can unfreeze their teammates.
- **Taggers**: Agents that attempt to freeze all runners within the time limit.

## Gameplay Mechanics

- **Core Loop**: Taggers try to freeze all runners before time runs out, while runners try to survive.
- **Victory Conditions**:
  - Runners win if at least one runner remains unfrozen when time expires.
  - Taggers win if they freeze all runners before time expires.
- **Agent Abilities**:
  - **Runners**:
    - Move freely in the environment.
    - Create walls to block taggers and freeze balls.
    - Unfreeze teammates (takes 3 seconds of standing nearby).
    - Collect wall balls to create barriers.
  - **Taggers**:
    - Move freely in the environment.
    - Directly freeze runners on contact (if they have freeze balls).
    - Shoot freeze balls at runners.
    - Collect freeze balls for ammunition.
- **Items**:
  - **Wall Balls**: Allow runners to create temporary barriers.
  - **Freeze Balls**: Used by taggers to freeze runners, either through direct contact or by shooting them.

## Technical Implementation

- **ML-Agents**: Uses Unity's machine learning framework to implement reinforcement learning algorithms.
- **PPO Algorithm**: Both agent types use Proximal Policy Optimization for training.
- **Curriculum Learning**: Multiple difficulty lessons with progression based on agent performance.
- **Reward System**: Comprehensive reward/punishment system to guide learning.
- **Metrics Tracking**: The system collects comprehensive performance metrics.
- **Python Training Detection**: Automatically detects training vs inference mode using communicator status.

## Reward and Punishment Table for Runner and Tagger Agents

### Runner Agent Rewards
| Reward Type | Action/Condition | Value |
|-------------|------------------|-------|
| Survival | Per timestep while not frozen | +0.001 |
| Unfreezing Teammate | Per timestep while unfreezing | +0.005 |
| Unfreezing Teammate | Successfully unfreezing a teammate | +1.5 |
| Wall Creation | Creating a wall | +0.1 |
| Wall Blocking Tagger | When a wall blocks a tagger's path | +1.0 |
| Wall Blocking Freeze Ball | When a wall blocks a freeze ball | +0.8 |
| Team Victory | When runners win the episode | +3.0 |

### Runner Agent Punishments
| Punishment Type | Action/Condition | Value |
|-----------------|------------------|-------|
| Getting Frozen | When tagged or hit by freeze ball | -1.0 |
| Team Loss | When taggers win the episode | -2.0 |

### Tagger Agent Rewards
| Reward Type | Action/Condition | Value |
|-------------|------------------|-------|
| Freezing Runner (Direct) | Freezing runner by direct contact | +1.0 |
| Freezing Runner (Freeze Ball) | Freezing runner with a freeze ball | +1.5 |
| Shooting Freeze Ball | Using/shooting a freeze ball (regardless of hit) | +0.1 |
| Team Victory | When taggers win the episode | +3.0 |

### Tagger Agent Punishments
| Punishment Type | Action/Condition | Value |
|-----------------|------------------|-------|
| Team Loss | When runners win the episode | -2.0 |

## Curriculum Design

The game uses a progressive curriculum with lessons that increase in difficulty. Progression between lessons is based on both runner and tagger performance, ensuring balanced agent development.

### Lesson Progression Requirements

| Lesson | Runner Threshold (Reference) | Tagger Reward Threshold | Expected Performance |
|--------|------------------------|------------------------|-------------|
| Lesson1_Introduction | 0.8 | 0.3 | Basic tagger shooting skills (3-5 shots), minimal freezes |
| Lesson2_Basic | 1.5 | 0.7 | 5-7 shots, at least 1 freeze per episode |
| Lesson3_Intermediate | 2.5 | 1.2 | Consistent freezing (1-2 per episode), strategic shooting |
| Lesson4_Advanced | 3.5 | 1.8 | Multiple freezes per episode, coordinated team play beginning |
| Lesson5_Expert | 4.5 | 2.5 | Coordinated team tactics, occasional team victories |
| Lesson6_Master | - | - | Master level with maximum difficulty |

### Environment Parameters

| Parameter | Lesson1 | Lesson2 | Lesson3 | Lesson4 | Lesson5 | Lesson6 |
|-----------|---------|---------|---------|---------|---------|---------|
| Runners | 2 | 3 | 4 | 5 | 6 | 7 |
| Taggers | 2 | 3 | 3 | 4 | 5 | 5 |
| Wall Balls | 4 | 5 | 6 | 6 | 5 | 4 |
| Freeze Balls | 8 | 9 | 8 | 8 | 10 | 12 |
| Time Limit (sec) | 90 | 100 | 90 | 80 | 70 | 60 |
| Max Wall Balls | 1 | 2 | 2 | 2 | 2 | 1 |
| Max Freeze Balls | 3 | 3 | 3 | 3 | 3 | 4 |
| Wall Cooldown (sec) | 3.0 | 2.0 | 1.5 | 1.0 | 1.0 | 1.5 |
| Shoot Cooldown (sec) | 1.5 | 1.5 | 1.2 | 1.0 | 0.8 | 0.6 |
| Freezeball Speed | 20.0 | 25.0 | 30.0 | 35.0 | 45.0 | 50.0 |
| Wall Lifetime (sec) | 4.0 | 4.0 | 3.0 | 2.5 | 2.0 | 1.5 |
| Runner Speed Multiplier | 0.6 | 0.7 | 0.8 | 0.9 | 1.0 | 1.0 |
| Freezeball Size Multiplier | 2.0 | 1.75 | 1.5 | 1.25 | 1.0 | 1.0 |

The early levels are balanced to give taggers better chances of success, with:
- Fewer runners to chase
- More freeze balls available
- Reduced wall ball capabilities for runners
- Longer wall cooldowns for runners
- Runners moving 40% slower in lesson 1
- Freezeballs being twice as large in lesson 1

As the curriculum progresses, runners gain more advantages through:
- Increasing speed (from 60% to 100% by lesson 5)
- Smaller freezeballs (gradually reducing from 2x to normal size)
- More wall ball capabilities
- Shorter wall cooldowns

## Freeze Tag Game Metrics

These metrics are tracked throughout the simulation and exported to CSV files for analysis. The CSV export happens automatically every 5 episodes and at the end of the simulation.

| Metric | Description |
|--------|-------------|
| runnersWin | Number of episodes won by the Runner team when at least one runner remains unfrozen when time expires |
| taggersWin | Number of episodes won by the Tagger team when all runners are frozen before time expires |
| time | Total time elapsed in the current episode |
| episodeLength | The duration of each individual episode (derived from lesson time limit minus remaining time) |
| freezeballsCollected | Number of freeze balls collected by taggers during the episode |
| wallballsCollected | Number of wall balls collected by runners during the episode |
| totalFreezes | Total number of times runners have been frozen during the episode |
| totalUnfreezes | Total number of times runners have been successfully unfrozen during the episode |
| wallsUsed | Number of walls created by runners during the episode |
| freezeballsUsed | Number of freeze balls shot by taggers during the episode |
| freezeballsHit | Number of freeze balls that successfully hit and froze a runner |
| freezeByTouch | Number of times taggers froze runners by direct contact |
| frozenAgentsAtEndOfEpisode | Count of runners that were in a frozen state when the episode ended |
| totalTimeSpentFreezing | Cumulative time all runners spent in the frozen state |
| fastestUnfreeze | Shortest time taken to successfully unfreeze a runner |
| avgUnfreezeTime | Average time taken to unfreeze runners across all successful unfreezes |
| totalWallHitsToTagger | Number of times taggers collided with walls created by runners |
| totalWallHitsToFreezeBallProjectile | Number of times freeze ball projectiles hit walls |
| totalUnsuccessfulUnfreezeAttempts | Count of interrupted attempts to unfreeze teammates |
| longestSurviveFromUnfreeze | Longest time a runner survived after being unfrozen |
| shortestSurviveFromUnfreeze | Shortest time a runner survived after being unfrozen before getting frozen again | 