# Stage Timeline Editor

## Open

- `Isekai Slime Summoner > Stage Timeline Editor`: opens the editor window directly.
- Select a `StageTimeline` asset and use `Open Stage Timeline Editor` in the Inspector.
- `Isekai Slime Summoner > Create Starter Stage Timeline`: creates a 20-wave sample timeline, a shared balance profile, and starter monster profiles.
- `Assets > Create > Isekai Slime Summoner > Stage Timeline`: creates an empty timeline asset.

## Data model

`StageTimeline` owns the stage metadata, run seed, direction-weight variation, and an ordered list of waves. Each `StageWave` owns:

- preparation time, boss flag, wave HP/speed multipliers;
- N/E/S/W relative spawn weights;
- one or more `MonsterSpawnEntry` rows.

Each spawn row selects a reusable `MonsterData` profile and sets count, spawn interval, HP, speed, and reward multipliers.

`StageBalanceProfile` is an optional shared multiplier layer. The effective runtime value is calculated as:

`shared profile x wave multiplier x monster spawn multiplier`

This keeps global tuning, wave progression, and encounter-specific exceptions separate.
