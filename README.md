# config-Wot-Alsain-mod

Ma config personnelle pour le **modpack Aslain** de *World of Tanks EU*, accompagnée d'un
installateur custom (`RoboAslainInstaller`) qui télécharge la config depuis ce dépôt et
lance l'installateur Aslain automatiquement.

## Fichiers de config

- `robo_configv4.inf` — **config actuelle** (celle appliquée par l'installateur)
- `robo_configv3.inf` — version précédente
- `OldConfig/` — anciennes versions archivées

## Installation manuelle (sans l'outil)

```
.\Aslains_WoT_Modpack_Installer.exe /LOADINF=robo_configv4.inf
```

## RoboAslainInstaller (recommandé)

Application console .NET 8 qui automatise tout :

1. Localise le dossier `Aslain_Modpack` (emplacements connus → scan des disques → saisie manuelle)
2. Télécharge `robo_configv4.inf` depuis GitHub (branche `main`)
3. Sauvegarde l'ancienne config, copie la nouvelle
4. Lance l'installateur Aslain avec `/LOADINF=robo_configv4.inf`

### Build

```
dotnet build -c Release RoboAslainInstaller/RoboAslainInstaller.csproj
```

### Utilisation

```
# Installation normale de la config
RoboAslainInstaller.exe

# Mettre à jour l'installateur Aslain
RoboAslainInstaller.exe --update-aslain

# Mode verbeux (logs détaillés)
RoboAslainInstaller.exe --verbose

# Aide
RoboAslainInstaller.exe --help
```
