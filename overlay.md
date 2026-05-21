# Overlays SSE — Guide d'accès

Tous les overlays listés ici utilisent **Server-Sent Events (SSE)** :  
le serveur pousse les mises à jour en temps réel sans que le client n'ait à re-interroger.

## Comment les utiliser

| Besoin | URL |
|---|---|
| Afficher l'overlay dans OBS/navigateur | `http://localhost:<port>/overlay/<name>` |
| Flux de données brut (debug) | `http://localhost:<port>/overlay/<name>/stream` |

Les fichiers HTML sont également disponibles directement dans le dossier `StreamOverlays/` :  
`StreamOverlays/<name>.html`

> **Note :** Les fichiers HTML SSE sont générés **une seule fois** au premier démarrage du serveur.  
> Si vous souhaitez les regénérer, supprimez le fichier `.html` correspondant dans `StreamOverlays/` et redémarrez.

---

## Liste des overlays SSE

### `raid`
**URL :** `http://localhost:<port>/overlay/raid`  
**Fichier :** `StreamOverlays/raid.html`

Affiche le boss du raid en cours : sprite, barre de HP colorée selon le niveau de vie restant, rareté.  
Si aucun raid n'est actif, l'overlay est masqué automatiquement.

**Mis à jour par :**
- Chaque attaque (`Raid/Attack`)
- Distribution d'un pokémon (`Raid/GiveawayPoke`)
- Chargement d'un raid sauvegardé (`Raid/Load`)
- Lancement d'un raid manuel (`Raid/StartManualRandomRaid`)
- Lancement automatique d'un raid (AutoRaid)
- Fin de raid (l'overlay se masque)

> **Overlay polling équivalent :** `StreamOverlays/raidOverlay.html` (polling toutes les 10 s)  
> **Overlay SSE legacy :** `StreamOverlays/current_raid.html` (accessible via `http://localhost:<port>/current_raid_stream`)

---

### `ball-throw`
**URL :** `http://localhost:<port>/overlay/ball-throw`  
**Fichier :** `StreamOverlays/ball-throw.html`

Affiche le résumé du dernier lancer de pokéball : sprite du pokémon, nom du dresseur, icône de plateforme, résultat (capturé / raté), indicateur shiny, heure du lancer.  
Affiché même en cas d'échec de capture.

**Mis à jour par :**
- Tout lancer de pokéball (`CatchPoke`, `CatchPokeNew`)

> **Overlay polling équivalent :** `StreamOverlays/barBallThrowResume.html` (polling toutes les 3 s)

---

### `last-caught-sprite`
**URL :** `http://localhost:<port>/overlay/last-caught-sprite`  
**Fichier :** `StreamOverlays/last-caught-sprite.html`

Affiche le sprite animé du dernier pokémon **capturé** (captures réussies uniquement) et le pseudo du dresseur.

**Mis à jour par :**
- Capture réussie via `CatchPoke` ou `CatchPokeNew`

> **Overlays polling équivalents :** `StreamOverlays/lastCaughtPokeSprite.html` et `lastCaughtPokeSpriteNew.html`

---

### `global-dex`
**URL :** `http://localhost:<port>/overlay/global-dex`  
**Fichier :** `StreamOverlays/global-dex.html`

Barre de progression : nombre d'espèces **normales** différentes capturées par l'ensemble des dresseurs, sur le total d'espèces disponibles.

**Mis à jour par :**
- Capture (`CatchPoke`, `CatchPokeNew`)
- Giveaway (`Giveaway/Claim`)
- Distribution raid (`Raid/GiveawayPoke`)
- Achat (`BuyElement`)

> **Overlay polling équivalent :** `StreamOverlays/progressGlobalDex.html`

---

### `global-shiny-dex`
**URL :** `http://localhost:<port>/overlay/global-shiny-dex`  
**Fichier :** `StreamOverlays/global-shiny-dex.html`

Barre de progression : nombre d'espèces **shiny** différentes capturées par l'ensemble des dresseurs.

**Mis à jour par :** *(idem `global-dex`)*

> **Overlay polling équivalent :** `StreamOverlays/progressShinyDex.html`

---

### `global-total-caught`
**URL :** `http://localhost:<port>/overlay/global-total-caught`  
**Fichier :** `StreamOverlays/global-total-caught.html`

Barre de progression vers un objectif de captures totales (toutes espèces confondues, normale + shiny) configuré dans les settings (`GlobalTotalCaughtGoal.GoalValue`).

**Mis à jour par :** *(idem `global-dex`)*

> **Overlay polling équivalent :** `StreamOverlays/GlobalTotalCaughtGoal.html`

---

### `global-shiny-caught`
**URL :** `http://localhost:<port>/overlay/global-shiny-caught`  
**Fichier :** `StreamOverlays/global-shiny-caught.html`

Barre de progression vers un objectif de shinies capturés globalement (`GlobalShinyCaughtGoal.GoalValue`).

**Mis à jour par :** *(idem `global-dex`)*

> **Overlay polling équivalent :** `StreamOverlays/GlobalShinyCaughtGoal.html`

---

### `global-money-spent`
**URL :** `http://localhost:<port>/overlay/global-money-spent`  
**Fichier :** `StreamOverlays/global-money-spent.html`

Barre de progression vers un objectif d'argent dépensé globalement (somme des prix de pokéballs lancées par tous les dresseurs depuis le début, `GlobalMoneySpentGoal.GoalValue`).

**Mis à jour par :**
- Capture (`CatchPoke`, `CatchPokeNew`) — l'argent dépensé correspond au prix de la pokéball

> **Overlay polling équivalent :** `StreamOverlays/GlobalMoneySpentGoal.html`

---

### `session-participants`
**URL :** `http://localhost:<port>/overlay/session-participants`  
**Fichier :** `StreamOverlays/session-participants.html`

Barre de progression : nombre de participants inscrits dans la session en cours vers un objectif (`SessionParticipantsGoal.GoalValue`).  
Remis à zéro à chaque redémarrage du serveur.

**Mis à jour par :**
- `SignIn` explicite
- `AutoSignInGiveAway` (inscription automatique lors d'un catch ou d'un lancer)

> **Overlay polling équivalent :** `StreamOverlays/SessionParticipantsGoal.html`

---

### `session-total-caught`
**URL :** `http://localhost:<port>/overlay/session-total-caught`  
**Fichier :** `StreamOverlays/session-total-caught.html`

Barre de progression : nombre de captures totales réussies depuis le démarrage du serveur vers un objectif (`SessionTotalCaughtGoal.GoalValue`).

**Mis à jour par :**
- Capture (`CatchPoke`, `CatchPokeNew`)
- Giveaway (`Giveaway/Claim`)
- Distribution raid (`Raid/GiveawayPoke`)

> **Overlay polling équivalent :** `StreamOverlays/SessionTotalCaughtGoal.html`

---

### `session-shiny-caught`
**URL :** `http://localhost:<port>/overlay/session-shiny-caught`  
**Fichier :** `StreamOverlays/session-shiny-caught.html`

Barre de progression : nombre de shinies capturés pendant la session vers un objectif (`SessionShinyCaughtGoal.GoalValue`).

**Mis à jour par :** *(idem `session-total-caught`)*

> **Overlay polling équivalent :** `StreamOverlays/SessionShinyCaughtGoal.html`

---

### `session-money-spent`
**URL :** `http://localhost:<port>/overlay/session-money-spent`  
**Fichier :** `StreamOverlays/session-money-spent.html`

Barre de progression : argent dépensé en pokéballs pendant la session vers un objectif (`SessionMoneySpentGoal.GoalValue`).  
Remis à zéro à chaque redémarrage du serveur.

**Mis à jour par :**
- Capture (`CatchPoke`, `CatchPokeNew`) — le prix de la pokéball est compté

> **Overlay polling équivalent :** `StreamOverlays/SessionMoneySpentGoal.html`

---

## Tableau récapitulatif

| URL overlay | Fichier généré | Remis à zéro au restart | Déclencheurs de mise à jour |
|---|---|---|---|
| `/overlay/raid` | `raid.html` | — | Attack, GivePoke, Load, StartRaid, AutoRaid, fin raid |
| `/overlay/ball-throw` | `ball-throw.html` | non | CatchPoke, CatchPokeNew |
| `/overlay/last-caught-sprite` | `last-caught-sprite.html` | non | CatchPoke, CatchPokeNew (réussies) |
| `/overlay/global-dex` | `global-dex.html` | non | Catch, Giveaway, Raid GivePoke, Buy |
| `/overlay/global-shiny-dex` | `global-shiny-dex.html` | non | Catch, Giveaway, Raid GivePoke, Buy |
| `/overlay/global-total-caught` | `global-total-caught.html` | non | Catch, Giveaway, Raid GivePoke, Buy |
| `/overlay/global-shiny-caught` | `global-shiny-caught.html` | non | Catch, Giveaway, Raid GivePoke, Buy |
| `/overlay/global-money-spent` | `global-money-spent.html` | non | CatchPoke, CatchPokeNew |
| `/overlay/session-participants` | `session-participants.html` | **oui** | SignIn, AutoSignInGiveAway |
| `/overlay/session-total-caught` | `session-total-caught.html` | **oui** | Catch, Giveaway, Raid GivePoke |
| `/overlay/session-shiny-caught` | `session-shiny-caught.html` | **oui** | Catch, Giveaway, Raid GivePoke |
| `/overlay/session-money-spent` | `session-money-spent.html` | **oui** | CatchPoke, CatchPokeNew |

---

## Architecture technique

```
Client (OBS / navigateur)
    │
    │  GET /overlay/<name>/stream   (connexion SSE longue durée)
    ▼
Program.cs  ──►  OverlaySseManager.RegisterClientAsync(channel, ...)
                        │
                        │  envoi initial de l'état courant (OverlaySseInitialPayload)
                        │  puis heartbeat toutes les 15s
                        │
Action métier (Catch, Attack, etc.)
    │
    ▼
ControllerContext.BroadcastXxx()
    │
    ▼
OverlaySseBroadcaster.BroadcastXxx(...)
    │
    ▼
OverlaySseManager.BroadcastChannel(channel, payload)
    │
    ▼  "data: {json}\n\n"
Client ──► source.onmessage → mise à jour du DOM
```
