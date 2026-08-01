# Credits

Audio stems used by iFlame. Attribution recorded even where the license does not require it.

## Fire - firewood crackle

- **Source:** [Fireplace Sound loop](https://opengameart.org/content/fireplace-sound-loop) by PagDev
- **License:** [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/)
- **Processing:** Converted to mono, acrossfade self-join (~1.5s) for a seamless loop, encoded as Ogg Opus and MP3.

## Rain - soft autumn rain

- **Source:** [Rain on Window Loop](https://opengameart.org/content/rain-on-window-loop) by alxl
- **License:** [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) (attribution appreciated by author, not required)
- **Processing:** Converted to mono, acrossfade self-join (~0.8s) for a seamless loop, encoded as Ogg Opus and MP3.

## Noise - low-frequency ambient

- **Source:** Generated for iFlame with ffmpeg `anoisesrc` (brown noise, 24s base)
- **License:** [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) (original work)
- **Processing:** Acrossfade self-join (~2s), encoded as Ogg Opus and MP3.

## Loop strategy (shipped)

Stems are trimmed and joined with ffmpeg `acrossfade` so loop points match in energy. Runtime playback uses `HTMLAudioElement.loop = true` through Web Audio `MediaElementAudioSourceNode` nodes. No staggered dual-buffer crossfade at runtime.
