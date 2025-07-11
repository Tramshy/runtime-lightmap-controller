# Runtime Lightmap Controller
A system that allows for switching lightmaps at runtime. Allows for switching for individual renderers, switching for light probes and smooth switching by lerping.

## Features
* Switching between pre calculated lightmaps and light probe data at runtime
* Allows for individual control of lightmap and light probes for specific bounds, rather than a whole a scene
* Allows for lerping between lightmaps and light probe data
* Several performance settings to save on CPU and memory usage

## Installation
This repository is installed as a package for Unity.
1. `Open Window` > `Package Manager`.
2. Click `+`.
3. Select Add Package from git URL.
4. Paste `https://github.com/Tramshy/runtime-lightmap-controller.git`.
5. Click Add.

NOTE: To do this you need Git installed on your computer.

## Editor Usage
### Usage
1. Create a new `LightState` scriptable object, found in the create asset menu under "Runtime Lightmap Controller".
2. Bake lighting for a specific lighting state of your scene (such as all lights on), then create a new folder anywhere in the assets folder and copy paste all of the generated lightmaps into this new folder.
3. Add the copied textures to the `LightState`, be sure that the textures array index matches its position in the lightmap arrays. Note: Be sure you add the copied textures and not the original!
4. Press `Store Current Baked Light Probes`.
5. Repeat for all light states you need for a scene. (all lights off, all lights a different color, etc)
6. Create a new empty game object and add the `LightSwitcher` component to the game object, each scene needs a different `LightSwitcher`.
7. Add all `LightStates` to the `Data` array, the first index in the array will determine what `Light State` is set upon starting the game.
8. Create a new empty game object and add the `LightBoundDefiner` component, this component allows you to switch the light maps for everything within its bounds.
9. Add the newly created `LightBoundDefiner` to the `LightSwitcher` bounds array.
10. Create as many as needed for the scene (one per room for example) and scale so it covers all static game objects and light probes. (if you have any)
11. Press one of the `Get Static Renderers` buttons and then press the `Get Probes Within Bounds` button.

### Reflection Probes
1. Enable reflection probe support under the `Tools/Runtime Lightmap Controller` menu, if it is not already enabled.
2. Make sure all probes are set to `Bake`.
3. Bake all probe data for a specific state, before copying the textures and adding to the `Light State` array.
   - Make sure the indexes in the name match the cubemaps index in the array.
   - If there is a gap in the indexes of the textures, simply recreate the gap in the array.
4. After baking all reflection probes, press the `Get Reflection Probes Within Bounds` button for all `Bound Definers`.
   - This can be done at any time after having baked all relevant reflection probes.

#### Additional Notes
- Make sure to not ever rename the original cubemaps. The copied versions can have their names changed or be modified in whatever ways are needed.
- It is also recommended to not delete any of the original cubemaps while working on a scene, even if they end up unused. After a scene is completely finished, and you decide to not make any changes to the scene, you may delete any or all original baked cubemaps.
    - If you do remove some originals before being finished with a scene, you will most likely need to set all the data in the `Bound Definers` again.

#### Light Bound Definer Fields
* `Should Warn About Static Nonuse Of Lightmap`: Will warn if a static game object is found within bounds and doesn't make use of lightmap when trying to switch lightmap data for bounds. Disable this bool if this was intentional.
* `Will Use Smooth Light Transition`: If this is disabled, you will save a small amount of CPU and memory usage when starting the game. This will also prevent you from calling the smooth transition methods.

## Usage
To switch light states during runtime, call any of the `SwitchLightState` methods within the `LightBoundDefiner`, or call any of the `SwitchAllSceneBounds` methods in the `LightSwitcher` to switch all bounds in the scene.

## Performance Notes
This system is pretty well optimized, but using the smooth transition methods can be quite costly. (especially if you run this for multiple `LightBoundDefiner` at once) The smooth transition also requires quite a bit of preparation when the game is first started. If performance is a concern: consider disabling smooth transitions all together. This can be done under the `Tools/Runtime Lightmap Controller` menu, found at the top of the screen.

## License
This package is licensed under the MIT License. For more information read: `LICENSE`.

## Additional Notes
You also need to manually enable shadow mask support under the `Tools/Runtime Lightmap Controller` menu.

Since switching lightmap and light probe data involves a lot of switching and editing arrays, there are times where the system can break if it is constantly in use.
