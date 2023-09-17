# Froggymark
This serves as a naive test to find just how fast sprites can be pushed to the gpu. 

**Use Release build without debugger for best results.**

## Controls
- Left Mouse: Spawn frogs
- Right Mouse: Remove frogs
- Space: Enable custom renderer for a few extra frames

## Results
- 340k (475k w/ custom renderer) frogs @ 60 fps on:
  - NVIDIA GeForce RTX 2060 with Max-Q Design/PCIe/SSE2
  - AMD Ryzen 9 4900HS

## Observations
- Foster's default sprite batcher is performant and should serve most use cases well
- Foster allows low level rendering via mesh buffers for any high performance needs

## Assets

| asset                | author      | license | notes |
| :------------------- | :---------: | :------: | :---- |
| monogram.ttf         | [datagoblin](https://datagoblin.itch.io/monogram/) | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) | |
| frog_knight.png     |  [NoelFB](https://github.com/NoelFB/tiny_link/tree/main) | [MIT](https://github.com/NoelFB/tiny_link/blob/main/LICENSE) | |

## Disclaimer
_You should never base your evaluation of any engine/framework on a single benchmark alone, as an extremely specialized solution, such as this one, provides no indication of real world performance or usability._