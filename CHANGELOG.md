# Changelog

## 0.1.1

- The vanilla cart gets a seat too: a passenger's, with no steering or brakes (someone pulls, or it rolls). `CartSeat` turns it off.
- Getting off (Jump) stands you up in the sled, where you sat, instead of on the ground beside it; you step or jump off from there.
- The cargo stays in sight while someone rides: only the crates under the rider hide, and they hide or show at once.
- Pushing off with W is now a kick per press that costs stamina (`PushImpulse`, `PushStamina`), instead of a steady push that drove the sled across flat ice like a car. `PushForce` is gone.
- A sled left standing is held by static friction (its runners settle into the snow), but no longer on ice, where a bumped sled now slides. `ParkSpeed` is now `StaticFrictionSpeed`.
- Runner sound: it follows the sled's speed smoothly instead of rising and falling with every step, the loops are longer and seamless, and ice is an iron-on-ice scrape instead of a whistle. The wind plays only for a rider.
- The surface on the status icon (and the hauling speed and sound with it) no longer flickers between snow and deep snow.

## 0.1.0

First release.

- A cargo sled built from the cart (Hammer, Misc, needs a workbench): wooden runners, iron shoes, rope and a fur seat. It carries what the cart carries and is pulled the same way.
- Runner physics per surface: snow, Deep North powder, ice, frozen ground, wood floors, stone and bare ground, each with its own friction and grip. A sled coasts about four times farther on ice than on snow, and drags hard on thawed ground. Parking brake, air drag, a speed limit.
- Hauling: pulling the sled is easy on snow and ice, slower on frozen ground and floors, slow and tiring on bare ground, and slower with load.
- Ride it: sit on it and steer, brake and push off downhill. Any player can ride; the rider takes over the physics of a sled nobody pulls. Hitting something at speed damages the sled and what it hits.
- Snow spray, Deep North powder trails, and synthesized runner sounds for each surface plus wind at speed.
- Wheeled vehicles (the vanilla cart) slow you down in snow (0.8) and powder (0.6); `SlowCartsInSnow` turns it off.
- Optional support for Seasons (shudnal) and Seasonality (RustyMods): seasonal snow counts as snow, frozen water as ice, and the sled uses `SeasonalRequirements` (no iron) while one of them is installed.
- Every coefficient is in the config, synced from the server and applied live.
- All the game's languages (Abenaki falls back to English).
