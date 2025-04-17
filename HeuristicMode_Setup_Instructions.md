# Heuristic Mode Dropdown Setup Instructions

This guide will help you set up the new Heuristic Mode dropdown in your Main Menu scene, allowing players to control one agent during gameplay.

## Step 1: Add the Dropdown to the Main Menu

1. Open your Main Menu scene in Unity
2. Select the Canvas in the Hierarchy panel
3. Right-click on the Canvas and select UI → Dropdown - TextMeshPro
4. Name it "HeuristicModeDropdown"
5. Position it appropriately in your UI (e.g., below the Agent Model input fields)
6. Add a label next to it saying "Human Control:" to make its purpose clear

## Step 2: Configure the Dropdown

1. Select the HeuristicModeDropdown in the Hierarchy
2. In the Inspector panel, under the TMP_Dropdown component:
   - Clear the Options list (it will be populated by script)
   - Adjust the visual appearance as desired:
     - Set colors, font size, alignment, etc.
     - Adjust the width to fit the text options
   - Enable "Raycast Target" for interaction

3. Find the Dropdown's Label component:
   - Change the text to "None" (this will be the default option)
   - Adjust font size and style as needed

## Step 3: Connect to the MainMenuManager Script

1. Select the GameObject that has the MainMenuManager script attached
2. In the Inspector, find the MainMenuManager component
3. Look for the "Heuristic Mode Dropdown" field
4. Drag the HeuristicModeDropdown from the Hierarchy into this field

## Step 4: Test the Functionality

1. Enter Play mode
2. Observe that the dropdown is initially disabled (grayed out)
3. Enter text in both the Runner Model and Tagger Model input fields
4. The dropdown should become enabled once both fields have text
5. If you clear either input field, the dropdown should become disabled again
6. When disabled, it should automatically reset to "None"

## How It Works

- The dropdown has three options:
  1. **None**: No agents will be in heuristic mode (all AI-controlled)
  2. **Runner**: One random runner agent will be controlled by keyboard input each episode
  3. **Tagger**: One random tagger agent will be controlled by keyboard input each episode

- The dropdown is only available when both runner and tagger model inputs are specified
- When a heuristic agent is enabled:
  - Each episode, a random agent of the selected type is chosen for manual control
  - The selected agent is marked with "_Heuristic" in its name for easy identification
  - You can control it using the WASD keys for movement, A/D for rotation, and Space for action

- Control keys:
  - **W**: Forward
  - **S**: Backward
  - **A**: Turn left / Strafe left (with Q)
  - **D**: Turn right / Strafe right (with E)
  - **Q**: Strafe left
  - **E**: Strafe right
  - **Space**: Action (freeze ball for tagger, wall for runner) 