<div align="center">

# SlideSlim

### Vos idées prennent de la place. Vos fichiers, beaucoup moins.

Compressez vos présentations PowerPoint pour les partager et les projeter plus facilement.<br>
**Des images dimensionnées pour vos diapositives. Un original toujours conservé.**

**Windows · PowerPoint .pptx / .pptm · Traitement 100 % local · Version 2.1**

[Démarrer](#démarrer-en-quelques-minutes) · [Voir les résultats](#un-exemple-concret) · [Choisir sa qualité](#trois-profils-pour-trouver-le-bon-équilibre) · [Comprendre le moteur](#des-pixels-utiles-là-où-il-en-faut)

</div>

---

## Une présentation prête à circuler

Les photos s’accumulent, les images restent en haute résolution et une présentation finit par peser des dizaines de mégaoctets. SlideSlim crée une copie plus légère en adaptant les images à leur utilisation réelle dans les diapositives.

**Déposez votre présentation. Choisissez une qualité. Récupérez votre copie optimisée.**

| Pour vous | Ce que fait SlideSlim |
|---|---|
| **Des fichiers plus faciles à partager** | Réduit le poids des images et mutualise les contenus identiques. |
| **Une qualité adaptée à la projection** | Calcule les pixels nécessaires à partir de la place des images sur les diapositives. |
| **Une présentation qui reste modifiable** | Conserve les textes, formes et graphiques vectoriels dans leur format natif. |
| **Vos documents restent chez vous** | Traite les fichiers sur votre ordinateur, sans envoi vers un serveur. |
| **Votre original reste disponible** | Enregistre le résultat dans une nouvelle copie, sans remplacer un fichier existant. |
| **Une application indépendante** | Fonctionne sans charger de complément et sans ouvrir PowerPoint. |

## Un exemple concret

Sur une présentation utilisée pour les essais :

| Fichier | Taille | Réduction par rapport à l’original |
|---|---:|---:|
| Original | **153,6 Mo** | — |
| Intermédiaire Full HD | **4,60 Mo** | **environ 97 %** |
| Maximum HD | **2,15 Mo** | **environ 98,6 %** |

**Environ 33 fois plus léger en Full HD, et 71 fois en Maximum HD sur cet exemple.**

Ces mesures portent sur un seul document et ne garantissent pas le même gain sur d’autres fichiers. Une présentation déjà optimisée ou composée surtout de vidéos peut diminuer beaucoup moins. Les profils réduisent volontairement la résolution et la qualité des images.

## Des pixels utiles, là où il en faut

Une petite photo dans un coin de diapositive n’a pas besoin de conserver tous les pixels de l’appareil qui l’a prise.

Sur une diapositive 16:9 projetée en **1920 × 1080**, une image occupant **un quart de la largeur** nécessite environ **480 pixels de large** avant compensation du recadrage. SlideSlim utilise cette place réelle pour calculer sa résolution cible.

Le moteur tient compte des recadrages et des groupes. Si une image sert à plusieurs endroits, il conserve la résolution nécessaire à sa plus grande utilisation. Il n’agrandit jamais une image.

Les profils Full HD et Maximum vont plus loin : ils peuvent convertir les grands PNG opaques en JPEG et stocker une seule fois les images dont le contenu final est identique. Les images avec une véritable transparence restent en PNG.

## Trois profils pour trouver le bon équilibre

| Profil | À choisir pour… | Résolution cible | Qualité JPEG |
|---|---|---|---:|
| **Standard** | Une compression à qualité JPEG élevée, en conservant les PNG | 150 pixels par pouce selon la taille affichée | 90 |
| **Intermédiaire Full HD** | **Le point de départ conseillé pour la projection en 1080p** | Diapositive dans 1920 × 1080 | 82 |
| **Maximum HD** | Privilégier un fichier compact pour une projection en 720p | Diapositive dans 1280 × 720 | 72 |

La résolution cible concerne la **diapositive entière** : chaque image reçoit les dimensions adaptées à sa place. Un recadrage important peut demander davantage de pixels pour préserver la zone visible.

## Démarrer en quelques minutes

### Depuis les sources

Installez [Git](https://git-scm.com/), puis ouvrez PowerShell :

```powershell
git clone https://github.com/jul1n/SlideSlim.git
cd SlideSlim
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Standalone.ps1
```

**Prérequis : Windows avec .NET Framework 4.8 recommandé.** La compilation utilise le compilateur .NET Framework présent sur le poste, sans paquet NuGet ni téléchargement de dépendances. Microsoft PowerPoint n’est pas requis pour le traitement.

L’installateur crée un raccourci sur le Bureau et dans le menu Démarrer. Le nom affiché dans l’application et les raccourcis est encore **PPTimiz Next** ; SlideSlim est le nouveau nom du projet.

### Depuis une archive de distribution

Si vous disposez d’une archive compilée, extrayez tous ses fichiers puis lancez `Installer.cmd`.

L’installation se fait pour l’utilisateur courant dans `%USERPROFILE%\Applications\PPTimizNext\Standalone-2.1`, normalement sans droit administrateur. Elle désactive uniquement les anciens compléments `PPTimizNext.*` de ce projet.

### Votre première compression

1. **Enregistrez** vos dernières modifications dans PowerPoint.
2. **Ouvrez l’application** et choisissez ou déposez un fichier `.pptx` ou `.pptm`.
3. **Sélectionnez un profil** — commencez par Intermédiaire Full HD.
4. **Choisissez un nouveau fichier de sortie.** La synthèse finale indique le poids obtenu et les optimisations réalisées.

L’application lit le fichier enregistré sur disque. Les modifications encore non sauvegardées dans PowerPoint ne sont pas incluses.

## Votre original reste votre référence

SlideSlim produit une copie et refuse de remplacer un fichier existant. Le résultat est contrôlé avant publication ; s’il n’est pas plus petit, aucune copie supplémentaire n’est créée.

- **Textes, formes et graphiques vectoriels** : conservés dans leur format natif, sans conversion des diapositives en images.
- **Vidéos et objets incorporés** : conservés sans recompression.
- **Macros des fichiers `.pptm`** : conservées sans être exécutées par le moteur.
- **Transparences** : maintenues en PNG.
- **Dispositions inutilisées** : nettoyées, avec au moins une disposition conservée par masque.

## Questions fréquentes

**Faut-il ouvrir PowerPoint ?**

Non. SlideSlim travaille directement sur le fichier enregistré. Il n’ajoute pas de bouton au ruban.

**Mes présentations quittent-elles mon ordinateur ?**

Non. Le moteur traite les documents localement, sans service externe.

**La qualité reste-t-elle identique ?**

Le redimensionnement et la compression JPEG entraînent une perte de qualité des images. Les textes et formes restent natifs. Choisissez le profil selon l’usage et vérifiez visuellement la copie avant diffusion.

**Pourquoi le gain est-il parfois faible ?**

Le fichier peut contenir des images déjà optimisées ou des médias que SlideSlim ne recompresse pas, comme les vidéos. Le gain dépend du contenu.

**Quels fichiers sont pris en charge ?**

Les `.pptx` et `.pptm` non chiffrés. Les anciens `.ppt` doivent être convertis au préalable. Les présentations avec une signature numérique Open XML sont refusées pour ne pas l’invalider. macOS et PowerPoint Web ne sont pas pris en charge.

**Que deviennent les images particulières ?**

Les images multitrames et celles avec une orientation EXIF non normale sont conservées. Les erreurs de décodage sont signalées dans la synthèse. Sans géométrie exploitable, une limite globale de 2560, 1920 ou 1280 pixels est utilisée selon le profil. La conversion PNG/JPEG concerne les PNG opaques de plus de 64 Kio et n’est retenue que si elle réduit leur taille.

## Pour les développeurs

**C# · Windows Forms · .NET Framework · ZIP / XML · System.Drawing**

```powershell
# Compiler l’application
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1

# Exécuter les 35 tests synthétiques du moteur
powershell -NoProfile -ExecutionPolicy Bypass -File .\test.ps1

# Construire une archive de distribution
powershell -NoProfile -ExecutionPolicy Bypass -File .\package.ps1
```

Les tests couvrent la préservation de l’original, les macros, vidéos, vecteurs, transparences, recadrages, résolutions calculées, conversions PNG/JPEG, images identiques, collisions de noms, XML invalide et nettoyage des dispositions. Ils génèrent leurs propres données : aucun document personnel n’est inclus dans le dépôt.

<details>
<summary><strong>Explorer le code et les limites techniques</strong></summary>

| Fichier | Rôle |
|---|---|
| `src/Launcher.cs` | Interface, dépôt de fichiers et traitement en arrière-plan |
| `src/Program.cs` | Point d’entrée |
| `src/Engine.cs` | Compression, vérification et publication de la copie |
| `src/ImageGeometry.cs` | Dimensions, recadrages et relations Open XML |
| `src/LayoutCleaner.cs` | Nettoyage des dispositions inutilisées |
| `src/AddIn.cs` | Dialogues communs et ancien prototype COM conservé dans le code |
| `Install-Standalone.ps1` | Installation et raccourcis |
| `tests/Tests.cs`, `tests/SmartTests.cs` | Tests synthétiques du moteur |

Les contrôles XML et de relations ne constituent pas une validation exhaustive du schéma Open XML ou de tous les effets visuels PowerPoint. Les exécutables ne sont pas signés par un certificat d’éditeur.

Une intégration COM a été expérimentée puis abandonnée à cause de chargements intermittents. L’application actuelle est indépendante et ne nécessite aucun enregistrement COM.

Aucun binaire ni code décompilé du programme original n’est distribué dans ce dépôt.

</details>

---

**Un problème ou une idée ?** [Ouvrez une issue](https://github.com/jul1n/SlideSlim/issues) en précisant le profil utilisé, les tailles avant/après et le message affiché. Évitez de joindre une présentation confidentielle.
