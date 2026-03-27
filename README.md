# AFI Save Our Ship 2 - Oxygen Now Included

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
![RimWorld Version](https://img.shields.io/badge/RimWorld-1.6-blue)

An SOS2 addon concept that allows you to replace the ship's life support with an alternative method: plants produce oxygen, and ship vents pump breathable air to other rooms via a heat network.

## About This Project

**This is a concept demonstration** - a pretty playable proof-of-concept showing how plants could generate oxygen in spaceships as an alternative life support system for Save Our Ship 2.

Expect a lot of flaws, but be thankful I managed to make some optimizations (the penultimate release reduced the TPS on all maps from 780 to 200. In the latest release, I was unable to notice any serious performance leaks).

The goal is to:
- Demonstrate the mechanics in a playable form
- Inspire other mod creators to use this idea, or to inspire the SOS2 developers to implement similar mechanics in the main mod
- Provide reference code for developers

## Features

- Plants generate oxygen in enclosed spaces (requires light ≥20%, temperature 12-42°C)
- The oxygen generation rate depends on the light intensity
- Each room has individual oxygen levels based on volume
- Atmosphere quality affect pawns:
	- Below 10%: Non-Breathable (SOS2 hypoxia)
	- 11-90%: Depleted Atmosphere (-10% consciousness, -10% blood filtration)
	- 91-110%: Normal Atmosphere (no effect)
	- 111-150%: Enriched Atmosphere (+10% consciousness, +15% movement, +10% work, +5% blood filtration)
	- If Hypoxia Resistance stat is 100% or higher, it neutralizes ALL atmospheric effects for pawn
- Oxygen equalizes between connected rooms via active ship vents (uses SOS2 thermal network)
- Oxygen leaks through holes in the hull and vents directed into the vacuum. All vents-connected rooms lose their atmosphere
- Pawns consume oxygen based on body size if Hypoxia Resistance stat is lower than 100%
- SOS2 life support systems override plant oxygen when active
- Emergency Supply: When Life Support is off, all previously breathing rooms have 15% atmosphere quality.
- When a pawn is in an atmosphere with more than 10% oxygen, it will be exposed to hediffs that can act as an oxygen level indicator, such as "Depleted atmosphere (15%)." Air quality can also be found in the growing plant tooltip, which also provides information on generation rate and reasons why oxygen is not being generated (too dark, too hot, or too cold). The only way to know the air quality in a room now, I was planning other methods

## What I planned but didn't do

- Oxygen reserves in containers
- Oxygen pipes and ventilation with conrollers of the desired oxygen concentration
- Hyperoxia: Potentially lethal. Exceeding the oxygen quality above enriched causes random fires in the room and affects the health of the pawns (periodic vomiting, reduced sight because hallucinations, reduced work speed and consciousness). When adding this condition, I wanted to change the enriched atmosphere to 111-125% and induce hyperoxia at 126-150%. I didn't achieve this primarily due to the inability to conveniently control saturation levels now.
- Pawn oxygen consumption depends on the Hypoxia Resistance stat; the higher it is, the less oxygen is needed.
- Thoughts from a lack of oxygen, an enriched atmosphere, and an excess of oxygen, for example, "Slept in stale air", "It's easy to breathe", etc.
- An oximeter, a wall device, allows you to monitor oxygen levels and alerts you on the interface if the oxygen level in room goes beyond safe levels (similar to warnings about "need treatment," "not enough food," or "fatal diseases"), i think it must colored yellow or red?
- I tried to implement the air quality widget into the interface where SOS2 shows "Non-Brethable/Brethable/Breach/Vacuum" info, but I failed
- 

## Requirements

- RimWorld 1.6
- [Save Our Ship 2](https://github.com/Bqr1s/SaveOurShip2) or Steam release version

## Installation

1. Download from Releases
2. Extract to `RimWorld/Mods/`
3. Enable in game (load after Save Our Ship 2)

## Configuration

Mod settings available in: Options → Mod Settings → Save Our Ship 2 - Oxygen Now Included

## What is AFI?

"Away From the Internet." Using every available means, I urgently created a series of concept mods that embody my ideas in a more tangible form than just words or text.
To put it bluntly, I'm in a room with oxygen saturation at about 11% and I have little time to do anything. These last sparks, these ideas, are what I'm giving to the community of my most favorite mod.

## License

MIT License - see [LICENSE](LICENSE) file for details