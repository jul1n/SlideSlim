# PPTimiz Next

Application Windows indépendante pour réduire la taille des présentations PowerPoint `.pptx` et `.pptm`, en créant une nouvelle copie. Version actuelle : **2.1**.

Le traitement est entièrement local. PowerPoint n’a pas besoin d’être installé ou ouvert et aucun complément Office n’est chargé.

## Installation et utilisation

Avec une archive de distribution : extraire tous les fichiers, puis lancer `Installer.cmd`. Un raccourci **PPTimiz Next** est créé sur le Bureau et dans le menu Démarrer de l’utilisateur.

Depuis les sources :

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Standalone.ps1
```

Windows et .NET Framework 4.8 sont les prérequis recommandés. L’installation est par utilisateur dans `%USERPROFILE%\Applications\PPTimizNext\Standalone-2.1`. Aucun droit administrateur n’est normalement requis.

1. Enregistrer les modifications de la présentation dans PowerPoint.
2. Ouvrir PPTimiz Next et choisir ou déposer un fichier.
3. Choisir le niveau de compression.
4. Choisir un nouveau fichier de sortie, puis attendre la synthèse.

L’application lit le fichier sur disque : les modifications non enregistrées dans PowerPoint ne sont pas incluses. L’installateur désactive uniquement les anciens compléments `PPTimizNext.*` de ce projet.

## Niveaux de compression

| Profil | Résolution cible | Qualité JPEG |
|---|---|---:|
| Standard | 150 pixels par pouce selon la taille affichée | 90 |
| Intermédiaire Full HD | Diapositive projetée dans 1920 × 1080 | 82 |
| Maximum HD | Diapositive projetée dans 1280 × 720 | 72 |

La résolution est calculée **selon la place de chaque image dans la diapositive**, en tenant compte du recadrage et des groupes. Une image occupant un quart de la largeur d’une diapositive 16:9 projetée en 1080p nécessite environ 480 pixels de large avant compensation du recadrage. Si une image est réutilisée, sa plus grande utilisation détermine la résolution conservée. Les images ne sont jamais agrandies.

Les profils Intermédiaire et Maximum convertissent les PNG opaques de plus de 64 Kio en JPEG lorsque cela réduit leur taille et mutualisent les images dont le contenu final est identique. Les véritables transparences restent en PNG. Standard conserve les PNG. Sans géométrie exploitable, une limite de 2560, 1920 ou 1280 pixels est utilisée selon le profil.

Le moteur supprime aussi les dispositions inutilisées, tout en conservant au moins une disposition par masque. Les textes, formes et graphiques vectoriels ne sont pas rasterisés. Les vidéos, objets incorporés et macros sont conservés sans recompression ; les macros ne sont pas exécutées par le moteur.

## Protection des fichiers et limites

- L’original n’est jamais remplacé ni supprimé ; une destination existante est refusée.
- La copie est vérifiée avant publication ; sans gain de taille, aucune copie n’est publiée.
- Les signatures numériques Open XML sont refusées afin de ne pas les invalider.
- Les anciens `.ppt`, les fichiers chiffrés, macOS et PowerPoint Web ne sont pas pris en charge.
- Les images multitrames ou avec orientation EXIF non normale sont conservées. Les erreurs de décodage sont signalées dans la synthèse.
- Le redimensionnement et le JPEG entraînent une perte de qualité. Vérifier visuellement une présentation complexe avant diffusion.

Les contrôles de structure et de relations ne constituent pas une validation exhaustive du schéma Open XML. Aucun taux de réduction de taille n’est garanti. Les exécutables ne sont pas signés par un certificat d’éditeur.

## Développement

Le code utilise C#, Windows Forms, System.Drawing et les API ZIP/XML de .NET Framework. Aucun paquet NuGet ni accès réseau n’est nécessaire à la compilation. Le compilateur est celui de .NET Framework sous Windows.

```powershell
# Compiler
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1

# Exécuter les tests synthétiques
powershell -NoProfile -ExecutionPolicy Bypass -File .\test.ps1

# Construire l’archive distribuable
powershell -NoProfile -ExecutionPolicy Bypass -File .\package.ps1
```

Les **35 tests automatisés** couvrent notamment la préservation de l’original, les macros, vidéos, vecteurs, transparences, recadrages, résolution selon la taille affichée, conversions PNG/JPEG, images identiques, collisions de noms, XML invalide et nettoyage des dispositions. Ils créent leurs propres données synthétiques. Aucun document personnel n’est inclus dans ce dépôt.

| Fichier | Rôle |
|---|---|
| `src/Launcher.cs` | Fenêtre indépendante, dépôt de fichiers et traitement en arrière-plan |
| `src/Program.cs` | Point d’entrée |
| `src/Engine.cs` | Compression, vérification et publication |
| `src/ImageGeometry.cs` | Dimensions, recadrages et relations Open XML |
| `src/LayoutCleaner.cs` | Nettoyage des dispositions inutilisées |
| `src/AddIn.cs` | Dialogues communs et ancien prototype COM conservé pour compatibilité du code |
| `Install-Standalone.ps1` | Installation par utilisateur et raccourcis |
| `tests/Tests.cs`, `tests/SmartTests.cs` | Tests du moteur |

## Historique de l’intégration

Une intégration COM au ruban a été expérimentée puis abandonnée à cause de chargements intermittents sur le poste de test. La version distribuée est l’application indépendante. Aucun enregistrement COM n’est nécessaire pour l’utiliser.

Le projet s’appuie sur les formats Open XML ; aucun binaire ni code décompilé du programme original n’est distribué dans ce dépôt.
