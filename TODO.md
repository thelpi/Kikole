# Kikolé — feuille de route « remaster v2 »

Reprise du projet abandonné en mai 2023. Rangé par ordre d'attaque recommandé.

Branche de travail : `remaster-v2`.

---

## Où on en est

| | état |
|---|---|
| Script SQL | reconstruit, `utf8mb4` / `utf8mb4_unicode_ci`, 21 tables |
| Base locale | MySQL 9.1 (WAMP), rejouable à l'infini via `kikole_mock.sql` |
| Sites parasites | The Elite et Mets tes tennis supprimés |
| Framework | .NET 10, hébergement minimal |
| Accès aux données | Dapper sur **MySqlConnector** (`MySql.Data` retiré) |
| Références nullables | activées, **zéro avertissement** sur les deux projets |
| Syntaxe | C# moderne : `record`/`init` sur les DTO et requêtes, namespaces à portée fichier, aucun `ConfigureAwait` |
| Tests | **445**, projet `KikoleSiteUnitTests` |
| Base de production | extraite en texte (voir `Restauration/`) |

---

## 1. Sécurité et authentification

Quatre défauts, par gravité décroissante. La cible raisonnable est **ASP.NET Core
Identity** plutôt que de réparer la cryptographie maison.

- [ ] **Cookie d'authentification falsifiable** — AES-CBC avec `IV = new byte[16]` (IV nul
      et constant) et **aucun MAC**. Le cookie contient `hashDuMotDePasse§§§login`.
      S'y ajoutent `Secure = false` et `HttpOnly` non renseigné : lisible en JavaScript,
      donc une XSS suffit à voler le hash.
- [ ] **Mots de passe en SHA256 avec sel global unique** — pas de sel par utilisateur
      (deux comptes avec le même mot de passe ont le même hash) et fonction rapide, donc
      idéale pour du cassage en masse. Cible : un KDF lent.
- [ ] **`SHA256` en champ d'instance sur un singleton** — `ComputeHash` n'est pas
      thread-safe : deux connexions simultanées peuvent se corrompre. Bug de justesse,
      pas seulement de sécurité, et silencieux.
- [ ] **`IUserService`** — les 12 appels directs de `AccountController` à `IUserRepository`
      ne sont pas du CRUD mais de la logique d'authentification : la vérification du mot de
      passe se fait **dans le contrôleur**, et l'inscription enchaîne cinq étapes sans
      transaction. À traiter **dans** ce chantier, puisque le code disparaîtra avec Identity.
- [ ] Ne pas versionner de secrets : passer par *user-secrets* en dev.

---

## 2. Modèle de données et contenu

- [ ] **Rendre les clubs canoniques** — le champ club est un `<input type="text">` libre :
      l'autocomplétion suggère mais ne remplit aucun champ caché, contrairement au continent
      et à la nationalité qui soumettent un identifiant. Un identifiant supprimerait la
      correspondance par chaîne et permettrait une vraie clé étrangère. À arbitrer :
      `clubs.allowed_names` ne servirait plus qu'à l'autocomplétion.
- [ ] **Remplir la base des clubs** — `Restauration/clubs_2023.txt` contient environ
      1 767 clubs d'époque, dont 738 avec leurs alias. Matière première prête.
- [ ] **Nationalités doubles et sportives** — `players.country_id` est unique et `NOT NULL`.
      Changement de modèle, donc à faire avant d'accumuler des données. La table
      `player_federations` retrouvée en production était une tentative abandonnée.
- [ ] Ajouter les clés étrangères : le schéma n'en déclare **aucune**, les seules garanties
      d'intégrité sont les `IsValid` applicatives.
- [ ] `IClubService` — non justifié aujourd'hui (CRUD nu) ; le deviendra si les clubs
      passent en canonique. `Message` et `Discussion` ne méritent pas de service.

---

## 3. Qualité et performance

- [ ] **Requêtes N+1** — `PlayerHandler` fait une requête par club d'une carrière,
      `LeaderService.GetUsersFromIdsAsync` une par utilisateur, `BadgeService` une par badge
      et par jour. Sur un classement mensuel, des centaines d'aller-retours SQL.
- [ ] **Extraire un `IInternationalService`** — `KikoleBaseController` porte trois champs
      `static` (`_countriesCache`, `_continentsCache`, `_clubsCache`), soit de l'état
      partagé entre toutes les requêtes dans une classe instanciée par requête.
      `_clubsCache` est une référence **sans verrou**, contrairement aux deux autres qui
      sont des `ConcurrentDictionary` : même famille de bug que le `SHA256` de `Crypter`.
      *Le seul des cinq domaines sans service où le gain est immédiat.*
- [ ] **Sortir `GetProposalResponsesWithPoints` de `ProposalService`** — méthode
      `internal static` que `LeaderService` appelle directement, seul couplage
      service → service du projet, invisible à l'analyse des dépendances injectées. C'est
      une fonction pure sur des DTOs : sa place est dans un **calculateur de score dédié**.
- [ ] **De la logique métier vit dans les dépôts, hors de portée des tests.** Six règles
      fonctionnelles sont encodées dans la couche d'accès aux données, **invisibles pour les
      435 tests unitaires** : ceux-ci simulent les dépôts, donc vérifient que le service
      passe les bons paramètres, jamais ce que le SQL en fait.

      Par gravité décroissante :
      - **`StatisticRepository.UserPlayerLinkSql`** réimplémente en SQL la règle d'accès de
        `ProposalService.GetGrantAccessForDayAsync`. **Deux définitions du même droit
        d'accès, dans deux couches**, qui peuvent diverger en silence.
      - **`BaseRepository.SubSqlValidUsers`** définit le « joueur classable » (ni
        administrateur, ni désactivé), injecté dans sept requêtes depuis la classe de base.
      - **`proposal_date = DATE(creation_date)`**, la définition de « trouvé le jour même »,
        dupliquée **cinq fois**.
      - `ProposalRepository.GetMissingUsersAsLeaderAsync` encode la définition d'un
        classement incomplet.
      - `PlayerRepository.GetPlayersByCreatorAsync` encode l'état d'une soumission via un
        paramètre `@type` 0/1/2.
      - `BadgeRepository.GetUsersOfTheDayWithBadgeAsync` charge tous les détenteurs d'un
        badge puis filtre en C# sur une journée (logique dans le dépôt + N+1).

      Deux chantiers à ne pas mélanger : **(a)** des tests d'intégration sur base jetable —
      `kikole_mock.sql` étant idempotent, la moitié du travail est faite ; **(b)** remonter
      les règles dans le domaine, une fois (a) en place pour servir de filet.
- [ ] **`ProposalChart.FirstDate`** — figé en dur à titre provisoire. À sortir en
      configuration, ou mieux à déduire du `MIN(proposal_date)` en base. Tant qu'il est en
      dur, `kikole_mock.sql` doit garder la même valeur dans `@first_date`.
- [ ] **Modernisation syntaxique : le reste.** Les DTO, les requêtes et les namespaces sont
      faits. Restent les **ViewModels**, qui ne peuvent pas passer en `init` tant que les
      contrôleurs les remplissent après construction (`model.ErrorMessage = …`) — c'est un
      motif à revoir, pas une conversion mécanique. Les expressions de collection ne
      couvrent que ce qui a un type cible : un `var x = new List<T>()` n'en a pas, et
      `IReadOnlyDictionary` n'est pas constructible avec `[]`.
- [ ] **Latin Extended-B dans `Sanitize`** (optionnel) — 107 lettres d'alphabet phonétique
      et d'orthographes africaines deviennent `?`, faute d'équivalent ASCII évident. Hors
      périmètre tant que les noms de joueurs sont saisis dans leur forme médiatique. Un
      test fige la couverture des plages qui comptent.

---

## 4. Interface

- [ ] Rendre le graphisme plus attrayant.
- [ ] **Les indices peuvent être des images** — un indice d'époque vaut
      `https://i.imgur.com/YwR1hdd.png`. Le champ est un texte libre rendu tel quel.

**Volontairement en dernier :** le seul poste qui ne bloque rien et ne se déprécie pas.

---

## Base de production : ce qui a été fait, ce qui reste possible

Les fichiers bruts se trouvent dans `C:\wamp64_ok\bin\mysql\mysql9.1.0\data\dbs6116785` :
**40 tablespaces `.ibd` seuls**, sans `.frm`, sans `ibdata1`, au format **MySQL 5.7**
(vérifié : aucune page SDI). Checksums intacts.

**Fait** — le contenu textuel a été lu directement dans les pages, sans serveur, et déposé
dans `Restauration/` : libellés et descriptions des badges (EN et FR, désormais repris dans
`kikole.sql`), liste des clubs, liste des ~390 kikolés avec leurs indices.

**Reste possible** — une restauration *structurée* (identifiants, dates, scores, historique
des propositions) via `ALTER TABLE ... IMPORT TABLESPACE`. Elle exige :

- une instance **MySQL 5.7** : l'import ne franchit pas la frontière 5.7 → 8.0+, et MariaDB
  est exclue (elle écrit `FSP_SPACE_FLAGS=0x15` là où MySQL 5.7 écrit `0x21`, pour ses trois
  formats de ligne — les encodages ont divergé, il n'y a pas de contournement) ;
- la DDL d'époque, disponible dans l'historique git au commit `59d910d^`
  (19 tables, `utf8` / `utf8_bin`) — les `ALTER TABLE` d'index doivent être appliqués
  **avant** le `DISCARD`, puisqu'ils changent la structure du tablespace ;
- une conversion de collation à l'import, `utf8` → `utf8mb4`, sans quoi les noms accentués
  produisent du mojibake ;
- pour `continents`, `continent_translations`, `registration_guids` et `player_federations`,
  absentes de la DDL d'époque : sans fichier `.cfg`, l'import ne valide pas le schéma, donc
  une DDL fausse produit des données silencieusement fausses. À vérifier à l'œil.

**Les identifiants de badges ont été renumérotés** : les données d'époque référencent
l'ancienne numérotation à trous (3, 5, 6… 41), la nouvelle est contiguë de 1 à 28 **dans le
même ordre**. L'extraction a confirmé cette correspondance. Les lignes référençant les
badges **29** (`DoYouSpeakPatois`) et **34** (`TheEnd`) sont à écarter, ainsi que la table
`challenges`.

**Données personnelles** : logins, hachages faibles, adresses IP, e-mails de `discussions`.
À garder en local ; sans intérêt à réimporter côté comptes, le chantier Identity invalidant
les hachages de toute façon.

---

## Partis pris

**Schéma**
- `utf8mb4_unicode_ci` plutôt que `utf8mb4_0900_ai_ci` : seule collation moderne disponible
  à la fois sur MySQL et MariaDB.
- `ascii_bin` sur les colonnes de hash, `ascii_general_ci` sur les GUID et les IP.
- Badges 29 et 34 supprimés, identifiants réalignés sur 1..28. Table `challenges` supprimée.
- Libellés et descriptions des badges : **ceux d'époque**, récupérés de la base de production.

**Règles de jeu**
- Barème de soumission à 1 000 points forfaitaires. L'ancien barème dégressif avait été
  abandonné en novembre 2022 ; sa branche morte a été supprimée.
- Palmarès : un mois sans podium complet ne rapporte **aucune** médaille. Le cumul global est
  exactement la somme des podiums mensuels, ce qu'un test vérifie désormais.
- **Un joueur par jour est une invariante**, pas un cas à dégrader : son absence lève une
  exception qui nomme la date. C'est à l'administration de garantir le calendrier. Même
  traitement pour les incohérences référentielles — club de carrière ou créateur absent —
  qui levaient déjà, mais sans dire ce qui manquait.
- **`OneMinuteChrono` : 5 clubs minimum.** Les deux descriptions d'époque annonçaient 6 et
  le commentaire du code « more than 5 » : c'est l'implémentation qui avait raison, les
  trois ont été alignées dessus.

**Code**
- `required` plutôt que `null!` sur les DTO et les requêtes. Il n'y a plus aucun `null!`
  dans le projet.
- **DTO et requêtes sont des `record` à propriétés `init`.** Dapper les remplit sans
  problème — vérifié contre la vraie base, pas seulement en compilation. Les builders de
  test remplacent l'instance par une copie (`_dto = _dto with { … }`) au lieu de la muter ;
  c'est `record` qui rend `init` supportable côté tests.
- **Les signatures de dépôt restent nullables.** Lever dans le dépôt économiserait 2 gardes
  sur 10 et en casserait 5 : quatre appelants au moins traitent `null` comme flux de
  contrôle normal, dont `AuthorizationFilter`, sur le chemin de chaque requête. La couche
  d'accès dit « il n'y a pas de ligne » ; l'appelant décide si c'est une erreur.
- `RemoveDiacritics` conserve le passage par ISO-8859-8 : le *best-fit mapping* rabat `ø`,
  `ł`, `Æ` sur leur équivalent ASCII, ce que la normalisation NFD ne sait pas faire.
