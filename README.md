# Velo UI

Composants Blazor épurés, beaux par défaut et faciles à personnaliser.

```
Velo.slnx
├─ src/Velo.Blazor        la librairie (Razor Class Library, .NET 10)
│  ├─ Components/         VeloButton, VeloMenu, VeloDatePicker, VeloColorPicker, VeloDataGrid, VeloChart, VeloDonut, VeloTreeView, VeloCarousel… (45+ composants)
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

## Thèmes clair et sombre

Un `VeloTheme` contient une palette par mode, et le mode peut suivre le réglage de l'ordinateur :

```csharp
var theme = new VeloTheme
{
    Mode = VeloThemeMode.System,            // Light, Dark ou System (suit l'OS, en direct)
    Radius = "12px",                        // réglages communs aux deux modes
    Light = new VeloPalette { Primary = "#0369a1", Background = "#f5f9fc" },
    Dark  = new VeloPalette { Primary = "#38bdf8", OnPrimary = "#04121a", Background = "#06111c", Surface = "#0b1a28" },
};

var ocean = VeloThemes.Ocean();             // ou un thème prêt à l'emploi : Indigo, Ocean, Forest, Sunset, Graphite
```

Une couleur non renseignée garde la valeur Velo du mode. `VeloTheme` se sérialise en JSON (sauvegarde par utilisateur).
La page « Thèmes clair et sombre » de la démo permet de composer un thème, avec aperçu des deux modes,
contrôle des contrastes et code C# généré.

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
- Thème restauré côté navigateur (démo) : au rechargement, le thème par défaut s'affiche un instant avant le thème
  sauvegardé. Pour l'éviter dans une application, lisez la préférence côté serveur (cookie, profil) avant le rendu.
- `VeloDonut` : 6 parts maximum (le reste dans « Autres »). Pour comparer des valeurs proches, `VeloBarList` est plus lisible ;
  la forme `SegmentedBar` l'est aussi pour montrer d'un coup d'œil comment se répartit un tout.
- `VeloRating` : saisie par valeurs entières ; les fractions (4,3) ne sont qu'en lecture seule.
- `VeloColorPicker` : la pipette (prendre une couleur à l'écran) n'existe que dans Chrome et Edge ; le bouton n'apparaît pas ailleurs.
- Pas encore de tests (bUnit) ni de package NuGet.
- La partie WPF n'est pas commencée.
