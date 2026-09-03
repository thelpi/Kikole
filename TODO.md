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
| Tests | **490**, projet `KikoleSiteUnitTests` |
| Authentification | **ASP.NET Core Identity**, store Dapper maison (`KikoleSite/Identity/`) |
| Base de production | extraite en texte (voir `Restauration/`) |

---

## 1. Sécurité et authentification

- [x] ~~Cookie d'authentification falsifiable~~ — remplacé par le cookie Identity, chiffré
      par la Data Protection API du framework.
- [x] ~~Mots de passe en SHA256 avec sel global unique~~ — remplacé par PBKDF2 salé par
      utilisateur (`PasswordHasher<ApplicationUser>`). Les comptes existants sont réécrits
      automatiquement au premier login réussi, voir « Partis pris ».
- [x] ~~`SHA256` en champ d'instance sur un singleton~~ — `Crypter`/`ICrypter` ont disparu
      du projet, plus aucun appelant depuis la refonte Identity ; le seul SHA256 restant
      (`LegacyCompatiblePasswordHasher`, pour la
      compatibilité ascendante) utilise `SHA256.HashData` (statique, thread-safe).
- [ ] **`IUserService`** — `AccountController` orchestre maintenant `UserManager`/
      `SignInManager` (le standard Identity, pas un défaut), mais la vérification de la
      réponse de sécurité et le flux d'inscription par GUID restent directement dans le
      contrôleur. Moins urgent qu'avant : à réévaluer une fois le système d'invitation
      retiré (voir plus bas), pour ne pas extraire un service autour d'un code qui va
      encore bouger.
- [ ] Ne pas versionner de secrets : passer par *user-secrets* en dev.
- [ ] **Retirer le système d'invitation** (`registration_guids`) — demandé par l'utilisateur
      en même temps que la refonte de l'authent, mais **délibérément découplé** : aucun lien
      technique avec Identity, et l'inscription libre est prévue pour novembre 2026 (cf.
      page d'accueil). Impacte `AccountController.create`, `IUserRepository`
      (`GetRegistrationGuidAsync`/`LinkRegistrationGuidToUserAsync`), la table
      `registration_guids` et sans doute le formulaire de création de compte.

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
- **`IInternationalService` est un singleton à cache explicite**, pas un `IMemoryCache` :
  les référentiels sont minuscules et ne changent que par action d'administration, donc
  l'expiration ne sert à rien et son éviction non déterministe rendrait les tests fragiles.
  Le service reçoit la langue **en paramètre** et ne lit aucun état ambiant — c'est ce qui
  le rend testable ; ce sont les contrôleurs qui résolvent la culture de la requête.
  **Toute écriture sur les clubs passe par `CreateOrUpdateClubAsync`**, qui rafraîchit le
  cache lui-même : l'invalidation n'est plus à la charge de l'appelant, donc impossible à
  oublier. Les contrôleurs ne dépendent plus du tout d'`IClubRepository`.
- **`IGameCalendar` déduit les dates du `MIN(publication_date)`** : le premier joueur publié
  est la journée cachée, le jeu commence le lendemain. **Sans joueur en base, l'application
  refuse de démarrer** plutôt que de servir des dates inventées.

  Il est **scindé en deux** : `GameCalendar` ne porte que trois dates et **ne dépend de
  rien**, ce qui le range à côté d'`IClock` — un fournisseur transverse, hors des couches,
  injectable partout. `GameCalendarLoader` porte la seule dépendance à un dépôt et amorce
  le calendrier au démarrage (`IHostedService`) ; personne ne l'injecte.

  Cette scission n'est pas cosmétique : elle **évite d'avoir à trancher la question des
  couches**. Un `GameCalendarService` aurait fait dépendre trois services d'un service ;
  un `GameCalendarHandler` aurait fait court-circuiter la couche service par trois
  contrôleurs, ce qu'aucun n'avait jamais fait — `IPlayerHandler` n'est injecté que par
  des services. Avec zéro dépendance à l'appel, il n'y a plus rien à arbitrer.

  Séparé d'`IClock` en revanche : l'horloge ne lit jamais la base, et les fusionner ferait
  traîner un dépôt derrière chaque `_clock.Today` du projet.
- **`players.proposal_date` renommée `publication_date`.** Le mot *proposal* portait trois
  sens dans ce code : la tentative d'un participant (table `proposals`, `ProposalTypes`),
  la soumission d'un joueur par un utilisateur (« proposer un kikolé »), et — ici — le jour
  où le joueur est le joueur du jour. Seul ce dernier était un faux ami ; les deux tables
  `proposals` et `leaders` gardent une vraie colonne `proposal_date` (le jour visé par la
  tentative), qui n'a pas bougé. `submission_date` aurait été un piège : ça se serait
  confondu avec `creation_date`, juste à côté. `publication_date` rejoint le vocabulaire
  déjà en place côté code (`GetPlayerOfTheDayAsync`, la doc de `PlayerRequest.ToDto` parlait
  déjà de « date de parution »).

  Renommage propagé à `PlayerDto`, `PlayerRequest`, `Player`, `PlayerSorts`, aux méthodes de
  dépôt (`ChangePlayerPublicationDateAsync`), aux clés de ressources (`InvalidProposalDate`
  → `InvalidPublicationDate`) et au schéma (`kikole.sql`, `kikole_mock.sql`). Les variables
  locales qui ne représentent pas ce champ mais un jour de jeu générique, utilisé aussi bien
  contre `players` que contre `proposals` (`actualDate` dans les contrôleurs, `UserDayModel`)
  n'ont pas été touchées : les renommer aurait suggéré à tort qu'elles ne portent qu'un seul
  des deux sens.
- **Authentification : ASP.NET Core Identity, store Dapper maison.** Contrainte produit
  non négociable : pas d'email, aucun canal de contact avec les joueurs hors formulaire
  libre. Le principe reste identique — login/mot de passe, question de sécurité pour la
  récupération, pas de 2FA — mais porté par le standard plutôt que par la crypto maison.

  Le store par défaut d'Identity est en EF Core ; ce projet est Dapper de bout en bout par
  choix assumé. `DapperUserStore` (`KikoleSite/Identity/`) implémente seulement
  `IUserStore`/`IUserPasswordStore`/`IUserLockoutStore`/`IUserSecurityStampStore` — rien sur
  l'email, le téléphone, la 2FA, les rôles ou les claims externes, puisque rien de tout ça
  n'est utilisé — et délègue à `IUserRepository`, qui reste le seul accès Dapper à la table
  `users`. `ApplicationUser : IdentityUser<ulong>` conserve la clé `ulong` existante :
  migrer vers les clés `string`/`Guid` par défaut d'Identity aurait cassé toutes les FK
  `user_id` du schéma.

  **La question de sécurité n'a pas d'équivalent natif dans Identity** (sa récupération
  standard suppose un canal externe pour livrer un token). Elle est gérée à la main dans
  `AccountController`, mais en réutilisant le même `IPasswordHasher<ApplicationUser>` que
  pour les mots de passe — même algorithme, secret différent, plutôt qu'un SHA256 maison
  pour la réponse. Une mauvaise réponse passe par le même compteur de verrouillage
  (`UserManager.AccessFailedAsync`) que les mots de passe : sinon, la réponse — bien plus
  devinable qu'un mot de passe — serait le maillon faible.

  **Migration des hashes existants sans reset forcé** : `LegacyCompatiblePasswordHasher`
  reconnaît l'ancien format SHA256+sel (64 caractères hex), le vérifie avec l'ancienne
  formule, et signale `SuccessRehashNeeded` — Identity réécrit alors le hash en PBKDF2 à la
  connexion suivante. Le palier utilisateur (`UserTypes`, conservé à trois niveaux) est
  porté par une claim plutôt que par les rôles Identity (un ensemble plat), pour garder
  exactement la sémantique « au moins ce palier » de l'existant
  (`MinimumUserTypeRequirement`) ; `[Authorization(UserTypes.X)]` devient une
  spécialisation d'`AuthorizeAttribute` qui résout la policy correspondante, donc **aucun
  site d'appel n'a eu à changer**.

  Effet de bord découvert en testant : MySqlConnector, sans `GuidFormat=None` dans la
  chaîne de connexion, renvoie les colonnes `CHAR(36)` qui *ressemblent* à un GUID comme
  `System.Guid` plutôt que `string` — cassait déjà silencieusement `registration_guids.id`
  (jamais éprouvé jusque-là) en plus des nouvelles colonnes de stamps. Et
  `BaseRepository.ExecuteNonQueryAndGetInsertedIdAsync` n'ouvrait pas explicitement sa
  connexion : Dapper la refermait après l'`INSERT` puisqu'il l'avait ouverte lui-même, et sa
  réutilisation depuis le pool pouvait perdre `LAST_INSERT_ID()` avant le second appel — un
  bug latent préexistant, débusqué ici par hasard. Les deux sont corrigés.

  `Crypter`/`ICrypter` ont ensuite disparu du projet : leur seul survivant, `Generate()`,
  ne servait qu'à fabriquer une question/réponse de secours inutilisable quand un compte
  est créé sans Q&A — remplacé par `Guid.NewGuid().ToString()`, du CSPRNG plutôt que le
  `System.Random` non cryptographique que `Crypter` utilisait.

  **Politique de mot de passe renforcée pendant qu'il n'y a encore aucun compte réel** :
  longueur minimale 10, **pas** de règle de composition (chiffre/majuscule/spécial). Ce
  n'est pas un relâchement : les règles de composition sont aujourd'hui déconseillées
  (NIST 800-63B, OWASP ASVS) parce que les humains les satisfont de façon prévisible
  (majuscule en tête, chiffre en fin — un motif que les dictionnaires de cassage
  connaissent), alors qu'un mot de passe plus long sans contrainte de forme résiste
  mieux en pratique. S'y ajoute `HibpPasswordValidator`, qui interroge l'API Have I Been
  Pwned en k-anonymity (seuls 5 caractères du hash SHA1 sortent, jamais le mot de passe)
  pour rejeter les mots de passe déjà vus dans une fuite connue — repli tolérant si l'API
  est indisponible, pour qu'un service tiers en panne ne bloque jamais un joueur. Les deux
  validateurs Identity (longueur + HIBP) s'exécutent tous les deux : `IPasswordValidator`
  supporte plusieurs implémentations enregistrées côte à côte, pas de remplacement.

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
