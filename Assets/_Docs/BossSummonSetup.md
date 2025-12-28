# Boss Summon Attack – Setup Guide

## Prehľad
- `SummonLayoutAsset`: definuje rozloženie spawnov, časovanie (startDelay/spacingDelay/waveDelay) a `waveCount` (počet vĺn).
- `SummonLayoutSpawner`: drží layout, arénu, default prefab, gating po X útokoch, pauzu pred summon animáciou a spúšťa spawn.
- Boss/AI: po dokončení svojho bežného útoku zavolá `summonSpawner.OnAttackResolved()`.

## 1) Príprava
- Prefab, ktorý sa má spawnovať (napr. rock/meteor).
- Objekt, ktorý reprezentuje arénu (Renderer alebo Collider2D) – použije sa ako `arenaReference`.

## 2) Vytvor `SummonLayoutAsset`
- Project: `Create > Summoning > Summon Layout`.
- Nastav: `attackType`, `startDelay`, `spacingDelay`, `waveDelay`, `waveCount` (default 1, zvýš na 2–3 pre viac vĺn).
- Grid sekcia:
  - `gridColumns` / `gridRows` (počet stĺpcov/riadkov).
  - Nechaj zapnuté "Use Arena Bounds For Cell Size" (bunky sa odvodia z arény).
  - (Advanced) `Grid Origin` (offset od min rohu arény), `Grid Default Delay`.
  - `Paint Delay`: per-bunka delay, ak chceš oneskorenie v rámci vlny.
  - Klikaj bunky v grid malbe, tým pridávaš spawn body.
  - Stlač "Update Entries From Grid", tým sa zapíšu `entries` (spawn body v strede buniek). `prefabOverride` nechaj prázdne, ak používaš `defaultPrefab` zo spawneru.

## 3) Pridaj `SummonLayoutSpawner`
- Na bossa alebo child pridať komponent.
- Nastav:
  - `layout`: tvoj `SummonLayoutAsset`.
  - `defaultPrefab`: rock/meteor (fallback, ak entry nemá `prefabOverride`).
  - `spawnRoot`: nechaj prázdne (Transform spawneru) alebo stred arény/bossa.
  - `useRootRotation`: off, ak netreba rotovať podľa rootu.
  - `arenaReference`: objekt arény (Renderer/Collider2D).
  - `useGridAuthoring`: on, ak používaš grid painter.
  - Gating: `enableSummon` on, `attacksBeforeSummon` (napr. 2), `summonPauseDuration` (napr. 2 s), `summonTrigger` (Animator trigger, voliteľné), `summonAnimator` (nechaj prázdne pre auto-find).

## 4) Prepojenie s bossom/AI
- Nezáleží na konkrétnom boss scripte – po dokončení každého bežného útoku zavolaj coroutine/spawn: `yield return StartCoroutine(summonSpawner.OnAttackResolved());`.
- Ak chceš pauzovať pohyb počas summon pauzy, rešpektuj `summonSpawner.IsSummoning` vo svojom pohybovom/AI kóde.
- Ak máš animátor, voliteľne nastav trigger pomenovaný ako `summonTrigger` a pre-summon animáciu; spawner trigger odpáli pred pauzou.

## 5) Test checklist
- `arenaReference` pokrýva plochu, kde chceš spawny.
- Gizmos on: vidíš wire arénu a spawn body v strede označených buniek.
- Layout: grid naklikaný, "Update Entries From Grid" stlačené.
- Prefaby: `defaultPrefab` nastavený alebo per-bunka `prefabOverride` v entries.
- V hre: po `attacksBeforeSummon` normálnych útokoch: pauza `summonPauseDuration`, voliteľná summon animácia, spustenie vĺn (`waveCount`) s odstupom `waveDelay` medzi vlnami.

## 6) Reuse pre ďalších bossov
- Na novom bossovi len pridaj `SummonLayoutSpawner`, priraď layout, arénu, prefab a vyplň gating. V AI/útokovej rutine zavolaj `OnAttackResolved()` po svojom bežnom útoku. Layout asset môžeš zdieľať alebo vytvoriť nový.
