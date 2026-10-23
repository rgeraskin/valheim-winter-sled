# Changelog

## 0.1.2

- S kicks back on a sled that has (nearly) stopped, like W forward: one push per press, same stamina. Out of a dead end, away from a wall. At speed S is still the brake.
- Gamepad: riding already took the left stick (analog steering, a flick forward or back to push off or brake), but the riding hint read W/A/S/D; with a gamepad it now shows the stick's directions and the gamepad's jump button.
- Air drag was about five times too strong: a sled coasting on the flat (the frozen sea above all) slowed down as if into a gale. It now coasts much farther. The speed cap above `MaxSpeed` is steeper, but the downhill top speed is a little higher than before (about 17 m/s instead of 15-16 on a 28° slope). The setting is renamed to `AirResistance` (0.12), so a value saved under the old `AirDrag` (0.55) no longer applies.
- Steadier on bumps: the centre of mass is lower, down by the runners, and a new `Stability` setting keeps the sled level with the ground under it (and level in the air, so it lands on its runners). On the test run, where 0.1.1 tipped over on small bumps, it no longer rolled over.
- New, and more than the game allows: pulling a sled, you keep your footing on slippery ice. Where the game takes the legs' control away (Deep North ice, and with Seasons frozen water shallower than 4 m) the sled dragged you to a crawl (about 2.5 m/s), and the vanilla cart can't be pulled there at all; now you run at about 6 m/s with the sled, as on other ice. Ice where you keep control anyway (Seasonality's, Seasons' deep sea) is unchanged, glide included. The cart is unchanged. `SureFootedPulling` (on by default) turns it off, for vanilla rules.

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
