# Tile selection contract

Cartographer guarantees a class grid only. The DeerStand `tiles` MapProvider
(not built here) decides which sprite permutation to draw.

## Iteration 1: solid tiles

One sprite per terrain class, blitted per cell. No transitions.

```
draw(cell) => sheet[cell.class][0]
```

## Iteration 2: blocky cardinal autotiling

For each cell, compare against N/E/S/W neighbors only. No diagonals. That is
what keeps transitions blocky.

### Bitmask

| Bit | Neighbor |
|---:|---|
| 0 | North |
| 1 | East |
| 2 | South |
| 3 | West |

Bit set when the neighbor is the *same* terrain class as the cell (for
same-class continuity) or when computing an edge ownership mask for a
terrain pair (see priority below).

```
mask = 0
if neighborN matches: mask |= 1
if neighborE matches: mask |= 2
if neighborS matches: mask |= 4
if neighborW matches: mask |= 8

tile = sheet[class][mask]   // 16 permutations, index 0..15
```

### Bitmask diagram (16 tiles)

```
 index   NESW bits   neighbors present
 -----   ---------   -----------------
   0      0000       none
   1      0001       N
   2      0010       E
   3      0011       N+E
   4      0100       S
   5      0101       N+S
   6      0110       E+S
   7      0111       N+E+S
   8      1000       W
   9      1001       N+W
  10      1010       E+W
  11      1011       N+E+W
  12      1100       S+W
  13      1101       N+S+W
  14      1110       E+S+W
  15      1111       N+E+S+W
```

Sprite sheet layout: 16 tiles per terrain pair in order 0 to 15 so tile choice
is `sheet[class][mask]` with zero conditional logic.

### Terrain priority (edge ownership)

When two different classes meet, the higher-priority terrain owns the
transition edge and draws its edge tiles over the lower:

```
water > swamp > paved_road > dirt_road > structure > trees > field > sand > grass > dirt
```

`unknown` is not drawn as a transition partner; treat it as dirt for
neighbor matching until refreshed.

## What Cartographer does not decide

- Which sprite asset pack is loaded
- Diagonal blending or Wang tiles beyond the 4-bit cardinal mask
- Elevation, biomes beyond the v1 enum, or labels
