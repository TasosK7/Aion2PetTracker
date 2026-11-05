# Fork notice

**Aion2 Pet Tracker · Glass Edition** is a modified version of
[MyGitStash/Aion2PetTracker](https://github.com/MyGitStash/Aion2PetTracker), forked at upstream commit
`f920126` (release 1.0.2). It is distributed under the same license, the
[GNU General Public License v3.0](../LICENSE). This notice satisfies GPL-3.0 section 5(a): the work has been
modified, and the changes are described below.

- **Modified by:** TasosK7 ([github.com/TasosK7/Aion2PetTracker](https://github.com/TasosK7/Aion2PetTracker))
- **First modified:** 5 November 2026
- **Full history:** every change is a commit on top of the upstream history in this repository

## What changed

The changes are limited to the Windows user interface in `src/Aion2PetTracker.App`. Capture, protocol parsing,
pet tracking, persistence, the pet catalogue and all other `Aion2PetTracker.Core` code are unchanged.

| Area | Change |
|---|---|
| `UI/Theme.cs` | Added the `Glass` drawing style: translucent panels, hairline borders, semibold type, per-group accent colours, chips, pill progress bars and circular portraits. Window chrome colours updated. |
| `Overlays/PetSoulsOverlay.cs` | Pet cards redrawn in the glass style. Card size, layout and transparency margins are unchanged. |
| `Overlays/SoulCurrencyHeader.cs` | Wallet bar redrawn as a single translucent pill. Icon backgrounds are now removed by brightness-derived alpha instead of a black colour key. |
| `Overlays/MerchantOverlay.cs` | Merchant panel redrawn with a title chip, round close button and per-row progress bars. The panel size and close/drag hit areas are unchanged. |
| `UI/Controls.cs` (new) | Custom-drawn, DPI-aware settings controls: toggle rows, slider, button bar, status card, section labels, notes, numbered steps and a dark tray-menu renderer. |
| `UI/SettingsPage.cs` (new) | Settings content separated from the capture service so it can be rendered as a preview. |
| `UI/TrackerWindow.cs` | Uses `SettingsPage`; status lines carry a tone (good, waiting, problem). The manual merchant option is labelled **Manual mode**. The version is read from the assembly. |
| `Startup/NpcapSetup.cs` | Setup screen rebuilt with the new controls. Enter and Escape behave as before. |
| `Program.cs` | `--render-preview` also renders `settings-preview.png`. |
| `.github/workflows/` | `preview.yml` renders UI previews on Windows for `ui/**` branches. `release.yml` builds and publishes a portable ZIP when a `v*` tag is pushed. |
| `README.md`, `docs/screenshots/` | The fork's description, download links and screenshots rendered from demo data. |
| `Directory.Build.props` | Version changed to identify fork builds. |

## Inherited notices

The upstream files in [`licenses/`](../licenses/) are kept **unchanged**, as they were received. Some of them were
written for related upstream projects and describe material that **is not part of this repository**:

- [`licenses/pets/PET-ASSETS.md`](../licenses/pets/PET-ASSETS.md) describes an "Aion 2 Helper" asset archive
  (`assets/pets/pets.zip`, maps, `spawns.json`, `pet-sources.json`). None of those files are included here.
- [`licenses/pets/THIRD_PARTY_NOTICES.md`](../licenses/pets/THIRD_PARTY_NOTICES.md) (in Turkish) also mentions a
  `workspace/` and `reference/` tree, map assets, Velopack, bundled fonts and `servers.json`. None of those files
  are included here either.
- [`licenses/SOURCE-INFO.txt`](../licenses/SOURCE-INFO.txt) and
  [`licenses/pets/SOUL-HEADER-NOTICE.txt`](../licenses/pets/SOUL-HEADER-NOTICE.txt) are in German.

These notices are still relevant, because they record where the code and data in this repository came from.

### What this repository actually contains

| Material | Location | Origin and terms |
|---|---|---|
| Application and core source | `src/`, `tests/` | GPL-3.0. Includes adapted upstream capture/framing code as described in [`licenses/SOURCE-INFO.txt`](../licenses/SOURCE-INFO.txt) |
| Pet catalogue | `config/pets.json` | Data from xCROv/aion2-pet-tracker (GPL-3.0), traced to Questlog. Pet names belong to NCSOFT |
| Shop item mapping | `config/shop-items.json` | SkeeveAN/Aion-DPS-Meter reference (MIT, [`licenses/SkeeveShop-MIT.txt`](../licenses/SkeeveShop-MIT.txt)) |
| Shop header signature | `config/shop-header-signature.json` | Derived from the upstream user-supplied UI reference |
| Pet portraits (207 PNGs) | `src/Aion2PetTracker.App/Resources/Assets/Pets/` | **NCSOFT game artwork**, via Questlog and xCROv's tracker. No redistribution licence from NCSOFT; the GPL does not cover the artwork |
| Wallet icon reference | `src/Aion2PetTracker.App/Resources/Assets/SoulHeaderReference.png` | Upstream user-supplied in-game UI reference; the game artwork belongs to NCSOFT |
| Runtime libraries | NuGet | SharpPcap (MIT), PacketDotNet (MPL-2.0), K4os.Compression.LZ4 (MIT) and the .NET runtime. Notices are in `licenses/` |
| README screenshots | `docs/screenshots/` | Rendered by this application from demo data. They include the pet portraits above |

## Game artwork and trademarks

AION 2 names, portraits, maps, game artwork and trademarks belong to NCSOFT and their respective rights
holders. This fork is an independent fan project. It is not affiliated with, sponsored by or endorsed by NCSOFT,
Questlog, the upstream author or the other referenced projects. It claims no rights in NCSOFT's artwork.

The tracker passively reads the game's network traffic. It never sends packets or reads game memory. The game's
terms of service may still restrict third-party tools, and you use it at your own risk.
