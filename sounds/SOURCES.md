# Sound sources

The sled's sounds are field recordings and foley from [Freesound](https://freesound.org). `src/` holds each one as Freesound's high-quality MP3 preview; `build_sounds.py` takes the steady stretches of it, high-passes them at 70 Hz and normalizes their loudness, then rebuilds an 8 s loop out of short grains (1/8 s, 1/4 s for wood) from random places in them, each grain at the same loudness and none with a click in it: the recording's tone, evened out, with nothing in it you'd hear repeat. The loop is leveled, its peaks softly limited, and written to `WinterSled/Resources/sounds/`. In the game it is pitched up a little and gets louder as the sled goes faster.

| Sound | Recording | Author | License | Used |
|---|---|---|---|---|
| snow; powder (the same, with more bass and softer highs, played lower) | [Snowboard_Slide_Loop_Mono_02.wav](https://freesound.org/s/612664/), [Snowboard_Slide_Loop_Mono_01.wav](https://freesound.org/s/612665/) | Nox_Sound | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 0–10 s; 0.5–11.5 s |
| ice | [Sledging (SoundAction 359)](https://freesound.org/s/863338/) | alexarje | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 1–2.55 s, 3.4–5.6 s (without the child's shout) |
| frozen ground, stone | [Ice Block - Sliding, Crunching, Scraping.wav](https://freesound.org/s/420634/) | Percy Duke | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | 6.5–9.5 s, 11.5–17.5 s |
| ground | [Dragging Kayak](https://freesound.org/s/488335/) | brunoboselli | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 1–6.3 s, 7.5–12.2 s, 13.5–24.5 s |
| wood floors | [Wood Plank Pushed On Wood Floor Long](https://freesound.org/s/710621/) | papimre | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 10.5–17.5 s, 19.5–28 s, 44.5–55 s, 59.5–68.5 s |
| wind (riding fast) | [Car Strong Wind Noise.mp3](https://freesound.org/s/439242/) (above 500 Hz: the air, not the car) | FunWithSound | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 10–40 s |

The CC BY recordings are used under their licenses, modified as described above; the modified sounds are under the same licenses. The CC0 recordings need no credit, but they get one anyway: thank you all.
