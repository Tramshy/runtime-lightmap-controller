# Runtime Lightmap Controller
A system that allows for switching lightmaps at runtime. Allows for switching for individual renderers, switching for light probes and smooth switching by lerping.

## Features
* Switching between pre calculated lightmaps and light probe data at runtime
* Allows for individual control of lightmap and light probes for specific bounds, rather than a whole a scene
* Allows for lerping between lightmaps and light probe data
* Several performance settings to save on CPU and memory usage

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
9. Create as many as needed for the scene (one per room for example) and scale so it covers all static game objects and light probes. (if you have any)
10. Press one of the `Get Static Renderers` buttons and then press the `Get Probes Within Bounds` button.

#### Light Bound Definer Fields
* `Should Warn About Static Nonuse Of Lightmap`: Will warn if a static game object is found within bounds and doesn't make use of lightmap when trying to switch lightmap data for bounds. Disable this bool if this was intentional.
* `Will Use Smooth Light Transition`: If this is disabled, you will save a small amount of CPU and memory usage when starting the game. This will also prevent you from calling the smooth transition methods.
* `Get Static Renderers With Colliders` vs. `Get Static Renderers Without Colliders`: The main difference is that if you use the latter option it will get renderers based on the transform of the object. This means that in order for a renderer to be found its center needs to be within bounds.

## Performance Notes
This system is pretty well optimized, but using the smooth transition methods can be quite costly. (especially if you run this for multiple `LightBoundDefiner` at once) The smooth transition also requires quite a bit of preparation when the game is first started. If performance is a concern: consider disabling smooth transitions all together. This can be done under the `Tools` menu, found at the top of the screen.

## License
This package is licensed under the MIT License. For more information read: `LICENSE`.

## Additional Notes
The use of [UPM Git Extension](https://github.com/mob-sakai/UpmGitExtension) is highly recommended, for easy updating of this package.
