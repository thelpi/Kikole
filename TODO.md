# Kikolé — feuille de route « remaster v2 »

Reprise du projet abandonné en mai 2023. Ce fichier est la référence des chantiers à
mener ; il est rangé par ordre d'attaque recommandé, avec la justification de l'ordre.

Branche de travail : `remaster-v2`.

---

## Où on en est

| | état |
|---|---|
| Script SQL | reconstruit, `utf8mb4` / `utf8mb4_unicode_ci`, 21 tables |
| Base locale | MySQL 9.1.0 (WAMP), rejouable via `kikole_mock.sql` |
| Sites parasites | The Elite et Mets tes tennis supprimés (8 400 lignes) |
| Tests | 224 tests, projet `KikoleSiteUnitTests` |
| Application | tourne en local, boucle de jeu fonctionnelle |
| Code applicatif | ~8 800 lignes C# |

---

## 1. Finir le filet de tests

**Pourquoi d'abord :** on s'apprête à changer de framework puis à réécrire
l'authentification, sur une logique métier qu'il a fallu reconstituer par lecture.
Sans tests, la migration est un acte de foi.

Classé par risque décroissant (bug silencieux ? écrit-il en base ? coût du test ?).

- [ ] **`BadgeService`** (762 l.) — 28 conditions dont des séries multi-jours et des
      agrégats. Résultats **persistés** dans `user_badges`. `ResetBadgesAsync` efface et
      recalcule tout l'historique : la fonction la plus destructrice du projet.
- [ ] **`LeaderService`** (448 l.) — `ComputeMissingLeadersAsync` écrit du score
      définitif. Contient la boucle jour par jour et le `First()` fragile.
- [ ] **`PlayerService`** (359 l.) — déplace les dates de parution (irréversible) et
      décide de l'anonymisation du joueur du jour : un bug ici **spoile**.
- [ ] **`ProposalService`** (reste) — `GetGrantAccessForDayAsync`, soit le contrôle
      d'accès au classement du jour.
- [ ] **`UserStat` / `DailyUserStat`** (151 l.) — agrégats purs, faciles à couvrir.
- [ ] **`HomeModel.SetPropertiesFromProposal`** — machine à états de l'écran de jeu.
- [ ] **`ViewHelper`** (189 l.) — faible criticité **mais** dépend de
      `CultureInfo.CurrentCulture` : à remonter en priorité si on enchaîne sur la
      migration, c'est typiquement ce qu'une bascule .NET casse (évolutions d'ICU).
- [ ] Petits modèles (`Player`, `PlayerCreator`, `Badge`, `Club`…) — surtout du mapping.
- [ ] Contrôleurs (1 974 l.) — coûteux en unitaire, meilleur retour en tests d'intégration.
- [ ] Dépôts — nécessitent des tests d'intégration sur base jetable.

Hors périmètre : `StatisticService` (outil personnel).

---

## 2. Migration .NET 10

**Pourquoi juste après :** `netcoreapp3.1` est en fin de vie depuis décembre 2022, soit
bientôt quatre ans sans correctif de sécurité, sous une application dont
l'authentification est déjà faible. Et surtout **tout le reste coûte moins cher après** :
réécrire l'auth sur 3.1 puis migrer, c'est le faire deux fois.

- [ ] Installer le SDK .NET 10 (seul le 9.0.314 est présent)
- [ ] `KikoleSite` et `KikoleSiteUnitTests` vers `net10.0`
- [ ] Remonter les paquets : `xunit` (2.4.x figé par netcoreapp3.1), `Microsoft.NET.Test.Sdk`,
      `MySql.Data` → `MySqlConnector`, `Moq`
- [ ] Activer les *nullable reference types* et les analyzers — ils feront une bonne
      partie de l'audit tout seuls
- [ ] Vérifier les formatages dépendants de la culture (cf. `ViewHelper`)
- [ ] Garder `FluentAssertions` en 6.x : la 7.0 bascule sous licence commerciale

---

## 3. Sécurité et authentification

Quatre défauts identifiés, par gravité décroissante. La cible raisonnable est
**ASP.NET Core Identity** plutôt que de réparer la cryptographie maison.

- [ ] **Cookie d'authentification falsifiable** — AES-CBC avec `IV = new byte[16]` (IV nul
      et constant) et **aucun MAC**. Le cookie contient `hashDuMotDePasse§§§login`.
      S'y ajoutent `Secure = false` et `HttpOnly` non renseigné : lisible en JavaScript,
      donc une XSS suffit à voler le hash.
- [ ] **Mots de passe en SHA256 avec sel global unique** — pas de sel par utilisateur
      (deux comptes avec le même mot de passe ont le même hash) et fonction rapide, donc
      idéale pour du cassage en masse. Cible : un KDF lent.
- [ ] **`Crypter` échoue en silence et en clair** — les deux `try/catch` renvoient le texte
      non chiffré si quoi que ce soit tourne mal. Défaillance ouverte.
- [ ] **`SHA256` en champ d'instance sur un singleton** — `ComputeHash` n'est pas
      thread-safe : deux connexions simultanées peuvent se corrompre. Bug de justesse,
      pas seulement de sécurité, et silencieux.
- [ ] Ne pas versionner de secrets : passer par *user-secrets* en dev

---

## 4. Modèle de données et contenu

- [ ] **Rendre les clubs canoniques** — aujourd'hui le champ club est un `<input type="text">
      libre : l'autocomplétion suggère mais ne remplit aucun champ caché, contrairement au
      continent et à la nationalité qui soumettent un identifiant. Passer à un identifiant
      supprimerait la correspondance par chaîne, permettrait une vraie clé étrangère et
      rendrait la détection de doublon exacte. À arbitrer : `clubs.allowed_names` ne
      servirait plus qu'à alimenter l'autocomplétion, plus à décider de la réussite.
- [ ] **Nationalités doubles et sportives** — `players.country_id` est aujourd'hui unique et
      `NOT NULL`. C'est un **changement de modèle**, donc à faire avant d'accumuler des
      données à migrer.
- [ ] **Remplir la base des clubs** une bonne fois — prérequis pratique pour jouer.
- [ ] Ajouter les clés étrangères : le schéma n'en déclare **aucune**, les seules garanties
      d'intégrité sont les `IsValid` applicatives.

---

## 5. Qualité et performance

- [ ] **Requêtes N+1** — `PlayerHandler` fait une requête par club d'une carrière,
      `LeaderService.GetUsersFromIdsAsync` une par utilisateur, `BadgeService` une par badge
      et par jour. Sur un classement mensuel, ça se compte en centaines d'aller-retours SQL.
- [ ] **Retirer les `ConfigureAwait(false)`** — 287 occurrences dans 23 fichiers. Utile dans
      une bibliothèque, inutile ici : ASP.NET Core n'a pas de `SynchronizationContext`.
      C'est du bruit pur. À faire **après** la migration, pour ne pas mélanger les diffs.
- [ ] **`ProposalChart.FirstDate`** — figé à la date du jour à titre provisoire. À sortir en
      configuration, ou mieux à déduire du `MIN(proposal_date)` en base.
- [ ] **`LeaderService` : `pDays.First(...)`** — lève une exception dès qu'un jour sans
      joueur apparaît dans l'historique. Masqué tant que `FirstDate` vaut aujourd'hui,
      ressurgira au premier trou. Un test de caractérisation fige déjà le comportement
      équivalent dans `PlayerHandler`.
- [ ] **Durcissement de `RemoveDiacritics`** (optionnel) — il reste 9 caractères non
      convertis en Latin Extended-A (`Ĳ`, `ŉ`, `ŋ`…) et 11 en Latin Extended Additional,
      tous archaïques ou typographiques. Sans impact réaliste sur des noms de footballeurs.

---

## 6. Interface

- [ ] Rendre le graphisme plus attrayant.

**Volontairement en dernier :** c'est le seul poste qui ne bloque rien et ne se déprécie
pas. Le faire avant la migration, c'est risquer de le refaire.

---

## Décisions prises, pour mémoire

- **`utf8mb4_unicode_ci`** plutôt que `utf8mb4_0900_ai_ci` : la seule collation moderne
  disponible à la fois sur MySQL et MariaDB. La base locale est MySQL 9.1, mais on garde
  la portabilité.
- **`ascii_bin`** sur les colonnes de hash, `ascii_general_ci` sur les GUID et les IP :
  valeurs techniques ASCII, comparaison binaire pour un hash.
- **`RemoveDiacritics` conserve le passage par ISO-8859-8.** Ce n'était pas un bug : le
  *best-fit mapping* de la page de code rabat `ø`, `ł`, `Æ` sur leur équivalent ASCII, ce
  que la normalisation Unicode NFD ne sait pas faire (pas de décomposition canonique). Les
  deux passes sont désormais combinées.
- **Barème de soumission à 1 000 points forfaitaires.** L'ancien barème dégressif
  (`500 + max(0, 1000 − 100×N)`) avait été abandonné en novembre 2022 ; sa branche morte a
  été supprimée avec les dates de bascule.
- **Badges 29 (`DoYouSpeakPatois`) et 34 (`TheEnd`) supprimés**, ids réalignés sur 1..28.
- **Libellés et descriptions des badges réécrits** : les originaux ont été perdus avec la
  base, ils sont déduits des conditions réelles du code. À relire.
- **Table `challenges` supprimée** — duels entre joueurs, fonctionnalité abandonnée qui
  déséquilibrait le scoring.
