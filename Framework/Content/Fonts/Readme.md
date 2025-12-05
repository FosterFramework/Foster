### Generating MSDF Fonts

The default MSDF font is generaed using [msdf-atlas-gen](https://github.com/Chlumsky/msdf-atlas-gen) with the following parameters:

```bash
./msdf-atlas-gen -font ./Roboto-Medium.ttf -yorigin top -imageout ./Compiled/Roboto.png -json ./Compiled/Roboto.json
```

Note that many fonts have overlapping or incorrect glyphs and can create errors in the MSDF output. It's often recommended to first correct overlaps in something like Font Forge before running it through msdf-atlas-gen.