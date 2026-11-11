# Velo UI

Composants Blazor épurés, beaux par défaut et faciles à personnaliser.

```
Velo.slnx
├─ src/Velo.Blazor        la librairie (Razor Class Library, .NET 10)
│  ├─ Components/         VeloButton, VeloMenu, VeloDatePicker, VeloDataGrid, VeloChart, VeloDashboard, VeloDrawer… (40+ composants)
│  ├─ Core/               VeloTheme, builders CSS, enums
│  ├─ Services/           VeloToastService + AddVelo()
│  ├─ wwwroot/velo.css    tokens + styles (spécificité zéro)
│  └─ wwwroot/velo.js     glisser-déposer de la timeline (chargé à la demande)
└─ samples/Velo.Demo      site de démo avec un playground par composant
```

## Lancer la démo

```bash
dotnet run --project samples/Velo.Demo --launch-profile http
```

Puis ouvrir http://localhost:5040.

## Utiliser la librairie

```csharp
// Program.cs
builder.Services.AddVelo();
```

```razor
@* App.razor, dans <head> *@
<link rel="stylesheet" href="_content/Velo.Blazor/velo.css" />

@* _Imports.razor *@
@using Velo.Blazor

@* MainLayout.razor *@
<VeloProvider Theme="theme">@Body</VeloProvider>
```

## Personnalisation, trois niveaux

1. **Paramètre** sur un composant : `<VeloSwitch ThumbColor="#f59e0b" />`
2. **Thème** pour toute l'application : `new VeloTheme { Primary = "#0ea5e9" }.Set("switch-thumb", "#fff")`
3. **CSS** : toutes les règles Velo sont dans `:where()`, donc une simple classe les surcharge sans `!important`.
   Chaque partie a une classe stable (`.velo-switch__thumb`) et lit une variable `--velo-*`.

Règle de conception : chaque paramètre visuel d'un composant écrit une variable CSS `--velo-{composant}-{partie}`.
Ajouter une option de personnalisation = un `[Parameter]` + une ligne `StyleBuilder.Var(...)` + un `var(...)` dans le CSS.

## Limites connues

- Menus et infobulles sont positionnés en CSS pur : pas de retournement automatique au bord de l'écran,
  et un `VeloSelect` dans un conteneur qui défile (corps d'un `VeloDialog`) peut être coupé.
  Prochaine étape : un positionnement JS léger (type Floating UI).
- `VeloDataGrid` : deux modes. `Items` (en mémoire, quelques milliers de lignes) ou `ItemsProvider`
  (une page à la fois depuis votre base/API, recherche temporisée, annulation des requêtes dépassées,
  helpers `ApplySort` / `ApplyPage` pour IQueryable). En mode serveur, « tout sélectionner » porte sur la page
  affichée, et le chargement attend le rendu interactif (pas pendant le prérendu statique).
  Pas encore de virtualisation, de filtres par colonne ni de redimensionnement des colonnes.
  Les textes (« sur », « Lignes »…) sont en français en dur.
- `VeloChart` : un seul axe Y par graphique, volontairement (deux unités = deux graphiques alignés via `From`/`To`).
  Palette des séries `--velo-series-1…8` validée daltonisme en clair et sombre ; au-delà de 8 séries, gris neutre.
  Survol en CSS pur (pas d'aller-retour serveur) ; le zoom du navigateur, lui, passe par le serveur.
  Pas encore d'empilement, de nuage de points ni de mise à jour en flux continu optimisée (chaque changement redessine le SVG).
- `VeloTimeline` : pas de glisser-déposer ni de redimensionnement des éléments, pas de défilement automatique vers « maintenant ».
- Glisser-déposer : `VeloKanban`, `VeloSortableList`, `VeloDashboard` et les en-têtes du `VeloDataGrid` utilisent le
  glisser-déposer natif HTML5, qui ne fonctionne pas au doigt sur mobile (le clavier et les boutons restent disponibles).
  La timeline utilise les événements pointeur (souris, stylet, tactile).
- `VeloDashboard` : largeur réglable (colonnes), pas encore la hauteur ; l'ordre visuel (CSS `order`) peut différer
  de l'ordre de tabulation après réorganisation.
- Pas encore de tests (bUnit) ni de package NuGet.
- La partie WPF n'est pas commencée.
