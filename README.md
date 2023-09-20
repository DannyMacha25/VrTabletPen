# Vr Tablet Pen

## Setup
To start things off, this git is to be used in conjuction with the 2dTabletPen 
repository. To use the VR feature is very simple though!

1. Install the proper Unity XR plugins (I should update this later with the actual plugin names :3)
2. Clone this repository and [this](https://github.com/DannyMacha25/2dTabletPen) repository into your project
3. Add the XR Player Prefab to your scene
4. Enjoy :3

## Preparing an Object to be Drawn On
An object in unity needs a few components before it can be drawn on.
1. Change the tag to "Whiteboard"
2. Make sure the object has a "Mesh Collider" and "Mesh Renderer" component
3. Add the "Whiteboard.cs" script from the 2dTabletPen folder
4. Put the "transparent" material from the 2dTabletPen/Materials folder under "Transparent Material"
5. Adjust texture size accordingly (leaving it default should work out most of the time)
## Controls
* Left Joystick for movement
* Right Joystick to turn
* Right Grip button to draw
* Left Grip button to open up the pallete
* 'A' (On Oculus Controllers) to interact with the pallete
* Right/Left trigger for ascension/descension