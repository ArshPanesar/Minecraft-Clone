# Minecraft-Clone
This Project contains Minecraft's Terrain Generation made using the Unity Game Engine.

A Demo is provided below which includes a Debug Menu to change Terrain Generation Parameters at Run-Time. There is also a Section in this ReadMe which gives a description about the Debug Menu and its Options. Performance may not be optimal, and the game can crash when some values are set too high.
### [Play The Demo Here!](https://arsh-panesar.itch.io/minecraft-clone)

Use WASD to Move Around.
Hold Right-Click and Move Mouse to Rotate.
Hold Shift to Move Faster.

## Preview

![Pic](https://user-images.githubusercontent.com/43693790/236690216-e9dfee96-9ec9-427c-ae28-34fcf8affdeb.png)

![Pic with Debug](https://user-images.githubusercontent.com/43693790/236690220-f4253e8e-4d85-4f70-9b9b-420f86e54071.png)

## About the Debug Menu
The Debug Menu provides Options for changing the Terrain's Generation Parameters at Run-Time. The following options are available:

<img width="160" alt="Parameters" src="https://user-images.githubusercontent.com/43693790/236690293-45117254-f919-4500-b1a3-be6dabe07d01.png">

To Apply the Changes, Click the **Generate** Button.

### Debug Options
The Terrain is generated using Perlin Noise with Fractal Brownian Motion. This means that the Noise Values are sampled a certain number of times with some Amplitude and Frequency.
The gradient values produced by Perlin Noise can be imagined as a Wave in 1D:

<img width="50%" height="50%" alt="Perlin Noise" src="https://blog.hirnschall.net/perlin-noise/resources/img/perlin-noise-1d.webp">

These values act as Input to the Fractal Brownian Motion, where they are Sampled over and over to produce a Realistic Looking Terrain.

> #### Noise Sampling Amount
> Number of Times the Noise Values are Sampled. 
> Larger Numbers will produce Less Smooth Terrain. 

> #### Noise Scale Amount
> This value determines the Scale used for Perlin Noise. 
> Higher scales produce Spikes all over the Terrain.

> #### Base Amplitude
> This value determines the Starting Amplitude for the Noise. 

> #### Amplitude Gain (Per Sample)
> This value is multiplied to the Base Amplitude at every Sample.
> Higher values produce Tall Hills with very Steep Slopes.

> #### Base Frequency
> This value determines the Starting Frequency for the Noise.

> #### Frequency Gain (Per Sample)
> This value is multiplied to the Base Frequency at every Sample.
> Higher values produce Tall Spikes (Similar to High Noise Scale Values).

> #### Wave Shift Amount
> This value Shifts the Noise, Moving it Forward or Backward by Some Amount.
> Changing this Value will Shift the Terrain.
