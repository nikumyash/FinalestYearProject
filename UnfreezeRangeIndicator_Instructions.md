# Unfreeze Range Indicator Setup Instructions

This guide will help you create and set up a glowing sphere effect that shows the unfreeze range around frozen runners.

## Step 1: Create the Unfreeze Range Indicator Prefab

1. In the Project panel, right-click and select **Create → Empty**
2. Name it "UnfreezeRangeIndicator"
3. With the empty object selected in the Hierarchy, add a sphere:
   - Right-click → 3D Object → Sphere
   - Reset its position to 0,0,0
   - Remove the Sphere Collider component (so it doesn't interfere with gameplay)

4. Create a translucent material:
   - In the Project panel, right-click → Create → Material
   - Name it "UnfreezeRangeMaterial"
   - Change the Shader to "Standard"
   - Set Rendering Mode to "Transparent"
   - Set the Albedo color to light blue (RGB: 0, 180, 255) with Alpha around 50-70
   - Assign this material to your sphere

5. Add a light effect (optional):
   - Select the UnfreezeRangeIndicator object
   - Add Component → Light → Point Light
   - Set Color to match your sphere (light blue)
   - Set Intensity to a low value (0.5-1.0)
   - Set Range to match your unfreeze range

6. Save as Prefab:
   - Drag the UnfreezeRangeIndicator from Hierarchy to Project panel
   - Delete it from your scene

## Step 2: Add the Indicator to Your Runner Prefab

1. Find your Runner agent prefab in the Project panel
2. Double-click to edit it
3. Drag the UnfreezeRangeIndicator prefab into the Runner's hierarchy
4. Position it at 0,0,0 relative to the Runner
5. **Important**: Disable it by default (uncheck the checkbox next to its name)
6. Save the Runner prefab

## Step 3: Connect the Indicator in the Inspector

1. Select your Runner prefab in the Project panel
2. In the Inspector, find the "RunnerAgent" component
3. Look for the "Unfreeze Range Indicator" field
4. Drag the UnfreezeRangeIndicator from the Runner's hierarchy into this field
5. Save the Runner prefab

## Testing

1. Enter Play mode
2. The indicator should be hidden by default
3. When a Runner gets frozen, the indicator should appear around it
4. When the Runner gets unfrozen, the indicator should disappear

## Customization

- Adjust the sphere's scale to match your desired unfreeze range
- Modify the material's transparency/color to make it more or less visible
- Add additional visual effects like particle systems for a more dynamic look 