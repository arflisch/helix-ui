# Helix UI

Composants Blazor et WPF épurés, beaux par défaut et faciles à personnaliser.

**Documentation et démo : [helix-ui-docs.pages.dev](https://helix-ui-docs.pages.dev/)**

```
Helix.slnx
├─ src/Helix.Blazor        la librairie (Razor Class Library, .NET 10)
│  ├─ Components/         HelixButton, HelixMenu, HelixDatePicker, HelixColorPicker, HelixDataGrid, HelixChart, HelixDonut, HelixTreeView, HelixCarousel… (45+ composants)
│  ├─ Core/               HelixTheme, builders CSS, enums
│  ├─ Services/           HelixToastService + AddHelix()
│  ├─ wwwroot/helix.css    tokens + styles (spécificité zéro)
│  └─ wwwroot/helix.js     glisser-déposer de la timeline (chargé à la demande)
├─ src/Helix.Wpf           la même librairie pour WPF (.NET 8 et 10, Windows)
│  ├─ Controls/           champs, boutons, HelixDataGrid, HelixChart, HelixDonut, HelixTimeline, HelixKanban, HelixDashboard, HelixTreeView… (55+ contrôles)
│  ├─ Core/               HelixResources (thème), HelixPalette, HelixIcon, HelixTexts
│  └─ Themes/Generic.xaml styles par défaut (couleurs en DynamicResource)
├─ samples/Helix.Demo      site de démo Blazor avec un playground par composant
└─ samples/Helix.Wpf.Demo  galerie WPF
```

## Lancer la démo

```bash
dotnet run --project samples/Helix.Demo --launch-profile http
```

Puis ouvrir http://localhost:5040.

## Utiliser la librairie

```csharp
// Program.cs
builder.Services.AddHelix();
```

```razor
@* App.razor, dans <head> *@
<link rel="stylesheet" href="_content/Helix.Blazor/helix.css" />

@* _Imports.razor *@
@using Helix.Blazor

@* MainLayout.razor *@
<HelixProvider Theme="theme">@Body</HelixProvider>
```

## Thèmes clair et sombre

Un `HelixTheme` contient une palette par mode, et le mode peut suivre le réglage de l'ordinateur :

```csharp
var theme = new HelixTheme
{
    Mode = HelixThemeMode.System,            // Light, Dark ou System (suit l'OS, en direct)
    Radius = "12px",                        // réglages communs aux deux modes
    Light = new HelixPalette { Primary = "#0369a1", Background = "#f5f9fc" },
    Dark  = new HelixPalette { Primary = "#38bdf8", OnPrimary = "#04121a", Background = "#06111c", Surface = "#0b1a28" },
};

var ocean = HelixThemes.Ocean();             // ou un thème prêt à l'emploi : Indigo, Ocean, Forest, Sunset, Graphite
```

Une couleur non renseignée garde la valeur Helix du mode. `HelixTheme` se sérialise en JSON (sauvegarde par utilisateur).
La page « Thèmes clair et sombre » de la démo permet de composer un thème, avec aperçu des deux modes,
contrôle des contrastes et code C# généré.

## Personnalisation, trois niveaux

1. **Paramètre** sur un composant : `<HelixSwitch ThumbColor="#f59e0b" />`
2. **Thème** pour toute l'application : `new HelixTheme { Primary = "#0ea5e9" }.Set("switch-thumb", "#fff")`
3. **CSS** : toutes les règles Helix sont dans `:where()`, donc une simple classe les surcharge sans `!important`.
   Chaque partie a une classe stable (`.helix-switch__thumb`) et lit une variable `--helix-*`.

Règle de conception : chaque paramètre visuel d'un composant écrit une variable CSS `--helix-{composant}-{partie}`.
Ajouter une option de personnalisation = un `[Parameter]` + une ligne `StyleBuilder.Var(...)` + un `var(...)` dans le CSS.

## Limites connues

- Menus et infobulles sont positionnés en CSS pur : pas de retournement automatique au bord de l'écran,
  et un `HelixSelect` dans un conteneur qui défile (corps d'un `HelixDialog`) peut être coupé.
  Prochaine étape : un positionnement JS léger (type Floating UI).
- `HelixDataGrid` : deux modes. `Items` (en mémoire, quelques milliers de lignes) ou `ItemsProvider`
  (une page à la fois depuis votre base/API, recherche temporisée, annulation des requêtes dépassées,
  helpers `ApplySort` / `ApplyPage` pour IQueryable). En mode serveur, « tout sélectionner » porte sur la page
  affichée, et le chargement attend le rendu interactif (pas pendant le prérendu statique).
  Pas encore de virtualisation, de filtres par colonne ni de redimensionnement des colonnes.
  Les textes (« sur », « Lignes »…) sont en français en dur.
- `HelixChart` : un seul axe Y par graphique, volontairement (deux unités = deux graphiques alignés via `From`/`To`).
  Palette des séries `--helix-series-1…8` validée daltonisme en clair et sombre ; au-delà de 8 séries, gris neutre.
  Survol en CSS pur (pas d'aller-retour serveur) ; le zoom du navigateur, lui, passe par le serveur.
  Pas encore d'empilement, de nuage de points ni de mise à jour en flux continu optimisée (chaque changement redessine le SVG).
- `HelixTimeline` : pas de glisser-déposer ni de redimensionnement des éléments, pas de défilement automatique vers « maintenant ».
- Glisser-déposer : `HelixKanban`, `HelixSortableList`, `HelixDashboard` et les en-têtes du `HelixDataGrid` utilisent le
  glisser-déposer natif HTML5, qui ne fonctionne pas au doigt sur mobile (le clavier et les boutons restent disponibles).
  La timeline utilise les événements pointeur (souris, stylet, tactile).
- `HelixDashboard` : largeur réglable (colonnes), pas encore la hauteur ; l'ordre visuel (CSS `order`) peut différer
  de l'ordre de tabulation après réorganisation.
- Thème restauré côté navigateur (démo) : au rechargement, le thème par défaut s'affiche un instant avant le thème
  sauvegardé. Pour l'éviter dans une application, lisez la préférence côté serveur (cookie, profil) avant le rendu.
- `HelixDonut` : 6 parts maximum (le reste dans « Autres »). Pour comparer des valeurs proches, `HelixBarList` est plus lisible ;
  la forme `SegmentedBar` l'est aussi pour montrer d'un coup d'œil comment se répartit un tout.
- `HelixRating` : saisie par valeurs entières ; les fractions (4,3) ne sont qu'en lecture seule.
- `HelixColorPicker` : la pipette (prendre une couleur à l'écran) n'existe que dans Chrome et Edge ; le bouton n'apparaît pas ailleurs.
- Pas encore de tests (bUnit).
- La partie WPF n'est pas commencée.

## WPF

```bash
dotnet run --project samples/Helix.Wpf.Demo
```

La galerie WPF ne s'exécute que sous Windows ; la librairie se compile aussi sur macOS et Linux (`EnableWindowsTargeting`).

```xml
<!-- App.xaml : le thème, une ligne -->
<Application.Resources>
    <helix:HelixResources Mode="System" />
</Application.Resources>

<!-- MainWindow.xaml : xmlns:helix="https://helix-ui.dev/wpf" -->
<helix:HelixHost>   <!-- notifications, dialogues et confirmations -->
    <helix:HelixSwitch Content="Pilote auto" ThumbBrush="#F59E0B" OnText="AUTO" OffText="MANUEL" />
</helix:HelixHost>
```

Chaque partie visuelle est une propriété (`Accent`, `TrackBrush`, `ThumbBrush`, `CornerRadius`…). Pour changer une valeur par défaut
dans toute l'application, un style implicite `BasedOn="{StaticResource {x:Type helix:HelixSwitch}}"` suffit : le template reste celui de Helix.

## Mettre la documentation en ligne

Le site de documentation est une application Blazor WebAssembly : une fois publié, ce sont de simples fichiers,
hébergeables gratuitement (Cloudflare Pages, Netlify…).

```bash
dotnet publish samples/Helix.Demo -c Release -o publish-docs
```

Le dossier à mettre en ligne est `publish-docs/wwwroot`. Sur Cloudflare Pages : Workers & Pages › Create ›
Pages › « Upload assets », glisser ce dossier, puis « Deploy ». Pour une mise à jour, refaire la publication et
« Create new deployment » dans le même projet.

## Packages NuGet

Nouveau projet : `dotnet new install Helix.Templates`, puis `dotnet new helix-blazor -n MonApp` ou `dotnet new helix-wpf -n MonApp`.

| Package | Installation |
|---|---|
| `Helix.Blazor` | `dotnet add package Helix.Blazor` |
| `Helix.Wpf` | `dotnet add package Helix.Wpf` |

Créer les packages en local :

```bash
dotnet pack src/Helix.Blazor -c Release -o artifacts
dotnet pack src/Helix.Wpf -c Release -o artifacts
```

Publier une version : pousser un tag `vX.Y.Z` (par exemple `git tag v0.1.0 && git push origin v0.1.0`).
Le workflow `.github/workflows/publish.yml` compile, crée les deux packages avec ce numéro de version et les envoie
sur nuget.org. Il faut une fois ajouter la clé d'API nuget.org dans le secret `NUGET_API_KEY` du dépôt.

Licence MIT.
