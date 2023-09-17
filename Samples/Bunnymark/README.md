# Bunnymark
This serves as a naive test to find just how fast sprites can be pushed to the gpu. 

**Use Release build without Debugging for best results.**

---

## Controls
- Left Mouse: Spawn bunnies
- Right Mouse: Remove bunnies
- Space: Enable custom renderer for a few extra frames

## Results
- 335k (405k w/ custom renderer) bunnies  @ 60 fps on:
  - NVIDIA GeForce RTX 2060 with Max-Q Design/PCIe/SSE2
  - AMD Ryzen 9 4900HS

## Observations
- Foster's default sprite batcher is VERY performant and should meet most use cases
- Foster allows low level rendering via mesh buffers for any high performance needs

## Assets

| asset                | author      | licence | notes |
| :------------------- | :---------: | :------: | :---- |
| monogram.ttf         | [datagoblin](https://datagoblin.itch.io/monogram/) | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) | |
| wabbit_alpha.png     | ❔ | ❔ | |

## Disclaimer
_You should never base your evaluation of any engine/framework on a bunnymark or any other benchmark alone, as an extremely specialized solution, such as this one, provides no indication of real world performance or usability._