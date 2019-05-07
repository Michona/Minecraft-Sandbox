## Overview

Minecraft-like Unity game. It utilizes the power of **Entity Component Systems**, C# Jobs and the BurstCompiler. It has the basic features of destroying and creating blocks of different types. As the player moves (or jumps out of the current island) new fields of blocks are generated (using noise generators) and blocks further away are destroyed. 

## Demo

Its uploaded on my itch.io profile: https://michona.itch.io/minecraft-sandbox </br>
If runnning from editor feel free to test out the limit by increasing the number of block entities spawned in a field (in the GameSettings object). Make sure to have BurstCompiler enabled. </br>

Have fun!

#

## Controls
* Basic FPS controlls for the character.
* Left Mouse Click - Create block
* Right Mouse Click - Destroy block
* 1 to 4 - Switch block types