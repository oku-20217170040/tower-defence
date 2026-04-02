# Tower Defence

A 2D Tower Defence game built with Unity featuring Q-Learning AI for enemy pathfinding.

## Features

- **6 Tower Types:** Cannon, Clock (slow), Ice, Laser, Machine Gun, Radar
- **Q-Learning AI:** Enemies learn from experience — they adapt their paths to avoid towers over time
- **Wave System:** Progressive enemy waves with a spawner system
- **Threat Map:** Dynamic threat assessment for enemy routing
- **Object Pooling:** Optimized bullet and floating text pooling
- **Audio System:** Background music and SFX manager
- **Full UI:** Pause menu, settings, end game screen, tower info panel, floating damage text

## Project Structure

```
Assets/
├── Scripts/
│   ├── Enemy/          # Enemy movement, health, freeze, wave logic
│   ├── Towers/         # Cannon, Clock, Ice, Laser, MachineGun, Radar towers
│   ├── Managers/       # GridManager, EnemyManager, WaveSpawner, QLearningManager
│   ├── UI/             # UIManager, EndGameManager, FloatingText, TowerInfoPanel
│   ├── Menu/           # MainMenu, PauseMenu, Settings, LevelManager
│   └── Utils/          # ObjectPool, BulletPool, MinHeap
├── Audio/              # Music and SFX assets
└── Scenes/             # Game scenes
```

## Q-Learning System

Enemies use a Q-Learning algorithm to adaptively find paths through the map:

- **Alpha (α):** Learning rate — how fast enemies adapt
- **Gamma (γ):** Discount factor — how much future rewards matter
- Enemies that survive reach the base (reward), enemies killed by towers get penalized
- Q-values are mapped back to node costs, influencing the A* pathfinder

## Requirements

- Unity 2022.x or later (2D project)
- No external dependencies

## Getting Started

1. Clone the repository
2. Open the project in Unity Hub
3. Open the main scene from `Scenes/`
4. Press Play
