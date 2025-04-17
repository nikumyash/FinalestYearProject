# ML-Agents Model Configuration

This document explains how to set up and use ML-Agents models in the Freeze Tag game.

## Model Organization

1. Create the following folders in your Unity project:
   - `Assets/Resources/Models/Runner`
   - `Assets/Resources/Models/Tagger`

2. Place your trained .onnx model files in these folders:
   - Runner agent models go in `Assets/Resources/Models/Runner`
   - Tagger agent models go in `Assets/Resources/Models/Tagger`

## Using Models in the Game

1. From the main menu, you can select which models to use for runners and taggers:
   - In the Runner Model field, enter the name of the .onnx file (without the .onnx extension)
   - In the Tagger Model field, enter the name of the .onnx file (without the .onnx extension)
   - Leave these fields empty to use default heuristic/manual behavior

2. Examples:
   - If you have a model file called `RunnerAgentV1.onnx` in the Runner folder, enter `RunnerAgentV1`
   - If you have a model file called `TaggerSmartBehavior.onnx` in the Tagger folder, enter `TaggerSmartBehavior`

## How Model Loading Works

When you start a game:

1. The MainMenuManager saves your model selections to PlayerPrefs
2. The GameManager loads these models from the Resources folders
3. When agents are spawned, the models are applied to their BehaviorParameters component's Model field
4. The BehaviorName remains unchanged - only the Model is updated

If a model file doesn't exist or the field is left empty, the agent will use default behavior based on its existing BehaviorParameters configuration. 