# Kikolé — feuille de route « remaster v2 »

Reprise du projet abandonné en mai 2023. Rangé par ordre d'attaque recommandé.

Branche de travail : `remaster-v2`.

---

## Où on en est

| | état |
|---|---|
| Script SQL | reconstruit, `utf8mb4` / `utf8mb4_unicode_ci`, 22 tables |
| Base locale | MySQL 9.1 (WAMP), rejouable à l'infini via `kikole_mock.sql` |
| Sites parasites | The Elite et Mets tes tennis supprimés |
| Framework | .NET 10, hébergement minimal |
| Accès aux données | Dapper sur **MySqlConnector** (`MySql.Data` retiré) |
| Références nullables | activées, **zéro avertissement** sur les deux projets |
| Syntaxe | C# moderne : `record`/`init` sur les DTO et requêtes, namespaces à portée fichier, aucun `ConfigureAwait` |
| Tests | **490** unitaires (mockés, rapides) + **5** d'intégration (vraie base, `--filter Category=Integration`), projet `KikoleSiteUnitTests` |
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
- [x] ~~Ne pas versionner de secrets~~ — la chaîne de connexion et `EncryptionKey` sont
      passées en *user-secrets*, voir « Partis pris ».
- [x] ~~Retirer le système d'invitation~~ — **désactivé plutôt que retiré**, derrière
      `Registration:InviteEnabled` (`false` par défaut, voir « Partis pris »). Le mécanisme
      (`registration_guids`, `GetRegistrationGuidAsync`/`LinkRegistrationGuidToUserAsync`)
      reste en place pour une réactivation par simple bascule de config.
- [x] ~~Outiller la lutte anti-multi-compte~~ — associé au point précédent : le système
      d'invitation servait de frein de facto à la fraude, son retrait l'ouvre en grand.
      `ApplicationUser.Ip` capture déjà l'IP à l'inscription, mais c'était insuffisant seul.
      - **Historique des connexions** : table `login_history` (`user_id`/`ip`/
        `creation_date`), une ligne par login réussi via `IUserRepository
        .CreateLoginHistoryAsync`, appelée depuis `AccountController` juste après un
        `PasswordSignInAsync` réussi.
      - **Rate limiting des créations de compte** — solution maison (pas
        `Microsoft.AspNetCore.RateLimiting`, voir « Partis pris »), avec liste blanche d'IP
        configurable (`Registration:RateLimitWhitelistedIps`) pour couvrir l'inscription
        groupée depuis une même IP de bureau.
      - **`ForwardedHeadersOptions` préparé, pas activé** : config-driven
        (`ForwardedProxy:KnownProxies`/`KnownNetworks`, vides par défaut = comportement
        natif inchangé) faute d'hébergement de prod choisi à ce jour. À renseigner une fois
        l'infra connue (nginx/Cloudflare/autre) — voir « Partis pris » pour le diagnostic
        du problème 2023 (même IP toujours capturée).
      - **Vue admin reportée** — `AdminController` n'a aujourd'hui aucune gestion des
        utilisateurs ; l'IP capturée reste invisible tant qu'il n'y a pas au moins une vue
        `GROUP BY ip HAVING COUNT(*) > seuil`. Remis à plus tard, décision explicite.
      - Rappel posé dès le départ : l'IP est un signal, pas une preuve (CGNAT, VPN) —
        l'objectif est de relever le coût de la triche occasionnelle, pas de l'éliminer.

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

- [x] ~~Requêtes N+1~~ — `PlayerHandler.GetPlayerFullInfoAsync` faisait une requête par
      club d'une carrière, `LeaderService.GetUsersFromIdsAsync` une par utilisateur ; les
      deux batchent maintenant via `GetClubsByIdsAsync`/`GetUsersByIdsAsync`
      (`WHERE id IN @ids`). `BadgeService.ResetBadgesAsync` a le même défaut (une requête
      par badge et par jour) mais reste **volontairement non traité** : fonction purement
      administrative, pas sur un chemin chaud.
- [x] ~~Sortir `GetProposalResponsesWithPoints` de `ProposalService`~~ — fusionné avec
      `ProposalChart` dans `Models/ScoreCalculator.cs` (voir « Partis pris »). Au passage,
      `ProposalResponse` expose maintenant `PointsLost` (la perte réelle, plafonnée),
      supprimant un recalcul redondant qui vivait dans `LeaderboardController.UserDay`.
- [x] ~~De la logique métier vit dans les dépôts, hors de portée des tests.~~ Six règles
      fonctionnelles étaient encodées dans la couche d'accès aux données, **invisibles pour
      les 490 tests unitaires** : ceux-ci simulent les dépôts, donc vérifient que le service
      passe les bons paramètres, jamais ce que le SQL en fait.

      **(a) Infra de tests d'intégration en place** — `KikoleSiteUnitTests/Integration/`,
      vraie base MySQL locale, `[Trait("Category","Integration")]` pour rester filtrable
      (`dotnet test --filter Category!=Integration` pour la suite rapide inchangée ;
      `dotnet test` seul les inclut désormais si WAMP tourne). Voir « Partis pris ».

      Par gravité décroissante :
      - **`StatisticRepository.UserPlayerLinkSql`** — la question qui la motivait est
        tombée : les statistiques sont désormais réservées à l'administrateur (point
        précédent), donc `@userId` y est toujours un administrateur, et sa première branche
        (`u.user_type_id = Administrator`) rend les deux autres (`leaders`/`creation_user_id`)
        mortes en pratique. Plus une divergence à résoudre, un nettoyage à faire —
        simplifier en une vérification unique du palier utilisateur, sans urgence.
      - [x] ~~`BaseRepository.SubSqlValidUsers`~~ — caractérisé par
        `SubSqlValidUsersIntegrationTests`, via `LeaderRepository.GetLeadersAtDateAsync` :
        administrateur et utilisateur désactivé bien exclus.
      - [x] ~~`proposal_date = DATE(creation_date)`~~ — la définition de « trouvé le jour
        même », dupliquée six fois entre `LeaderRepository` et `ProposalRepository` (une
        occurrence en plus des cinq recensées au départ), centralisée en
        `BaseRepository.SubSqlOnTime(bool)`, caractérisée par
        `OnTimeRuleIntegrationTests` (trouvé à temps vs en rattrapage) avant le
        regroupement, verte après.
      - [x] ~~`ProposalRepository.GetMissingUsersAsLeaderAsync` encode la définition d'un
        classement incomplet~~ — caractérisée par `MissingLeadersRuleIntegrationTests` (trouvé
        avec ligne `leaders`, trouvé sans, jamais trouvé). **Pas de (b) ici** : un seul site
        d'appel (`LeaderService.ComputeMissingLeadersAsync`, réparation admin), rien à
        dédupliquer — c'est un anti-join, plus à sa place en SQL qu'en C# (comparer deux
        tables en mémoire coûterait plus cher). Le test sert de filet direct : une règle
        fausse ferait manquer des réparations en silence, pas seulement un test rouge.
      - [x] ~~`PlayerRepository.GetPlayersByCreatorAsync` encode l'état d'une soumission via
        un paramètre `@type` 0/1/2~~ — caractérisée par
        `PlayersByCreatorRuleIntegrationTests` (en attente / accepté / rejeté, et un autre
        créateur jamais mélangé). Règle correcte, mais un premier jet du test s'est trompé :
        `CreatePlayerAsync` n'écrit pas `reject_date` à la création, un rejet passe toujours
        par `RefusePlayerProposalAsync` après coup — révélé par le test qui échouait, pas
        deviné. Constat en passant : seul `accepted: true` est appelé en production
        aujourd'hui (badges, page « mes soumissions ») ; `false`/`null` faisaient partie de
        l'interface sans filet avant ce test. **Pas de (b)** : un seul site de requête, rien
        à dédupliquer.
      - [x] ~~`BadgeRepository.GetUsersOfTheDayWithBadgeAsync` charge tous les détenteurs
        d'un badge puis filtre en C# sur une journée (logique dans le dépôt + N+1)~~ —
        caractérisée par `UsersOfTheDayWithBadgeIntegrationTests` (deux détenteurs, deux
        jours différents, seul celui du jour demandé remonte), verte avant **et** après le
        passage du filtre `get_date = @date` en SQL. Contrairement à
        `BadgeService.ResetBadgesAsync` (laissé tel quel, purement administratif), celle-ci
        est sur le chemin chaud — appelée à chaque soumission gagnante depuis
        `HomeController` — donc corrigée, pas seulement testée.

      Les six règles sont maintenant caractérisées ou n'avaient plus lieu d'être (la
      première, réglée par la restriction des statistiques à l'administrateur). Trois
      centralisations effectives (`SubSqlValidUsers`, `SubSqlOnTime`, le filtre `get_date`
      de cette dernière) ; deux règles laissées en l'état, single-site et déjà en SQL, avec
      leur filet propre.
- [x] ~~Que faire des statistiques ?~~ — **décision : réservées à l'administrateur.** Les
      cinq actions concernées (`Stats`, `GetStatisticPlayersDistribution`,
      `GetStatisticActiveUsers`, `KikolesStats`, `GetKikolesStatisticsAsync`) sont passées à
      `[Authorization(UserTypes.Administrator)]` — deux d'entre elles n'avaient jusqu'ici
      **aucune** protection (`Stats`, `GetStatisticActiveUsers`). Les deux liens vers ces
      pages sur `Leaderboard/Index` sont maintenant masqués hors administrateur
      (`LeaderboardModel.IsAdmin`), pour ne pas proposer un lien qui échoue. Vérifié en
      direct dans les trois cas : anonyme et `joueur1` (standard) ne voient plus les liens
      et sont redirigés en accès direct, `admin` voit les liens et accède normalement.
- [x] ~~Modernisation syntaxique : le reste.~~ Les DTO, les requêtes et les namespaces sont
      faits. **Décision : les ViewModels restent mutables**, voir « Partis pris ». Les
      expressions de collection ne couvrent de toute façon que ce qui a un type cible : un
      `var x = new List<T>()` n'en a pas, et `IReadOnlyDictionary` n'est pas constructible
      avec `[]` — point mineur, sans lien avec la décision ci-dessus, jamais traité.
- [x] ~~Latin Extended-B dans `Sanitize`~~ — **décision : non traité.** 107 lettres
      d'alphabet phonétique et d'orthographes africaines deviennent `?`, faute d'équivalent
      ASCII évident, mais hors périmètre tant que les noms de joueurs sont saisis dans leur
      forme médiatique. Un test fige la couverture des plages qui comptent, pour que la
      décision reste visible si le besoin change.

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

  **Pas d'`IUserService` par-dessus.** Envisagé un temps (voir historique), écarté une fois
  l'invitation désactivée : `UserManager`/`SignInManager` *sont* déjà la couche service pour
  tout ce qui doit l'être (hashing, lockout, tokens), et le reste (vérif Q&A, liaison GUID,
  rate limiting) n'est consommé que par `AccountController` lui-même — comme pour
  `login_history` plus haut, un seul appelant ne justifie pas une abstraction dédiée ; ça
  ajouterait un pass-through sans rien consolider.

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
- **`ProposalChart` et `GetProposalResponsesWithPoints` fusionnés en `ScoreCalculator`.**
  Les deux étaient déjà la même famille de chose — statiques, sans dépendance, sans I/O —
  juste séparés par accident d'implémentation : le second n'avait atterri dans
  `ProposalService` que parce qu'il fallait bien l'écrire quelque part, ce qui a permis à
  `LeaderService` de le contourner en appel statique direct (le seul couplage
  service → service du projet). Le fusionner avec le barème plutôt qu'en faire un vrai
  service évite justement de réintroduire ce genre de couplage, et laisse les Views
  continuer à lire les constantes directement (`ScoreCalculator.ProposalTypesCost`...) —
  un vrai service injecté aurait interdit cet accès direct depuis les Views.

  Au passage, `ProposalResponse` gagne une propriété `PointsLost` (la perte réelle,
  plafonnée à ce qu'il restait de points — pas le tarif brut de `Cost`), calculée une
  fois dans `WithTotalPoints`. `LeaderboardController.UserDay` recalculait la même chose en
  parcourant une seconde fois la séquence déjà ordonnée par `ScoreCalculator` ; il se
  contente maintenant de lire la valeur.
- **Invitation désactivée par config, pas retirée.** `Registration:InviteEnabled` (`false`
  par défaut) est lié via `IOptions<RegistrationOptions>` — le pattern standard, plutôt
  qu'un `IConfiguration` brut injecté (des clés en chaîne dispersées dans chaque classe) ou
  qu'un record résolu une fois à la main : les clés attendues sont visibles au typage, et
  c'est ce que quelqu'un qui connaît déjà ASP.NET Core s'attend à trouver. Premier exemple
  du genre dans le projet ; les autres lectures de config directes (`EncryptionKey`,
  `HibpApiBaseUrl`, la chaîne de connexion) pourront suivre le même chemin plus tard, mais
  ça n'a pas été fait ici — hors périmètre de ce chantier précis.

  À `false`, `AccountController.create` saute entièrement la validation du GUID
  (`registration_guids`, `GetRegistrationGuidAsync`/`LinkRegistrationGuidToUserAsync`) sans
  qu'aucune de ces méthodes ni la table ne disparaissent : remettre l'invitation est une
  bascule de config, pas un chantier de code. Les deux messages qui promettaient une date
  de réouverture fixe (page d'accueil, page « Compte ») ont perdu cette mention : la
  réactivation dépend maintenant d'un admin, plus d'un calendrier.
- **Secrets de dev via *user-secrets*, pas dans `appsettings.Development.json`.** La chaîne
  de connexion et `EncryptionKey` en sont sorties ; le fichier ne porte plus que `Logging`.
  `appsettings.Development.json` était déjà en `skip-worktree` (jamais remonté par
  `git status`, donc jamais commité par accident), mais ça ne protège que *ce* dépôt local
  précis — le fichier reste lisible en clair sur disque, et rien n'empêche une copie de
  dossier ou un `git add -f` de le faire fuiter. *user-secrets* le sort du dossier du projet
  entièrement (`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`), une
  protection qui ne dépend plus de l'historique git.

  Mise en place locale, une fois : `dotnet user-secrets set "ConnectionStrings:Kikole" "..."`
  et `dotnet user-secrets set "EncryptionKey" "..."` depuis `KikoleSite/`. Sans ça,
  l'application refuse de démarrer : `LegacyCompatiblePasswordHasher` lève dès la première
  vérification si `EncryptionKey` est absente.

- **Lutte anti-multi-compte : rate limiting maison plutôt que
  `Microsoft.AspNetCore.RateLimiting`.** Un formulaire web s'accommode mieux d'un message
  d'erreur localisé (`AccountModel.Error`, comme toutes les autres validations de
  `AccountController`) que d'une 429 générique renvoyée par un middleware ; la liste blanche
  d'IP (comptes de bureau créés depuis la même IP réseau) est aussi un simple `if` plus
  lisible qu'un partitioner personnalisé. Concrètement : `IUserRepository
  .GetUserCreationCountSinceAsync(ip, since)` compte les créations des dernières 24h pour
  l'IP courante, comparé à `Registration:MaxCreationsPerIpPerDay` (`5` par défaut), sauf si
  l'IP figure dans `Registration:RateLimitWhitelistedIps`.
- **`ForwardedHeadersOptions` préparé en config, pas activé.** Pas d'hébergement de
  production choisi à ce jour, donc rien à configurer de réel — mais le câblage est prêt
  (`ForwardedProxyOptions`, lu via `IOptions<T>`, listes vides par défaut = comportement
  natif inchangé, aucun proxy de confiance). Diagnostic du problème 2023 (l'IP capturée
  était toujours la même) : `ForwardedHeadersOptions.KnownProxies`/`KnownNetworks` vides par
  défaut, donc `UseForwardedHeaders` ignore silencieusement `X-Forwarded-For` faute de
  proxy explicitement approuvé — c'est la cause la plus probable, à confirmer sur l'infra
  réelle une fois choisie, avant de renseigner `ForwardedProxy:KnownProxies`/`KnownNetworks`.
  `KnownIPNetworks` (`System.Net.IPNetwork`, `.Parse` sur un CIDR) est utilisé plutôt que
  l'ancien `KnownNetworks` de `Microsoft.AspNetCore.HttpOverrides` — c'est la propriété
  moderne de `ForwardedHeadersOptions`, l'autre est un vestige d'API antérieure.
- **Historique des connexions dans une table dédiée (`login_history`), pas une colonne sur
  `users`.** `ApplicationUser.Ip` ne garde que l'IP d'inscription ; corréler une fraude dans
  le temps demande une ligne par connexion, pas juste la dernière. En écriture dans
  `IUserRepository`/`UserRepository`, pas un `ILoginHistoryRepository` séparé : le premier
  gère déjà deux tables (`users` et `registration_guids`), et sans vue admin pour l'instant
  (lecture directe en base en attendant), une seule méthode `CreateLoginHistoryAsync` ne
  justifie pas une interface dédiée.

**Tests d'intégration**
- **Même projet (`KikoleSiteUnitTests/Integration/`), pas un projet dédié.** Filtrable via
  `[Trait("Category","Integration")]` (`dotnet test --filter Category!=Integration` retrouve
  les 490 tests rapides et mockés) ; un `.csproj` séparé aurait ajouté du wiring de solution
  pour une distinction que le trait suffit à faire. Effet de bord assumé : `dotnet test` sans
  filtre inclut maintenant ces tests, donc échoue si WAMP n'est pas démarré.
- **`UserSecretsId` propre au projet de tests**, chaîne de connexion re-posée une fois
  (`dotnet user-secrets set "ConnectionStrings:Kikole" "..." --project KikoleSiteUnitTests`)
  plutôt que d'emprunter celui de `KikoleSite` : autonome et standard, la petite duplication
  vaut mieux qu'un lien caché entre deux projets.
- **`DatabaseFixture` (`IAsyncLifetime`) remet la base à l'état de `kikole_mock.sql`** avant
  chaque run — même mécanisme que les smoke tests manuels de ce chantier, `kikole_mock.sql`
  étant déjà idempotent (TRUNCATE puis re-INSERT). Les scénarios spécifiques à un test
  (utilisateur désactivé, réponse tardive...) s'ajoutent par-dessus dans le test lui-même via
  les repositories réels, pas dans le fixture partagé — garde `kikole_mock.sql` généraliste,
  utilisable tel quel pour le dev manuel.
- **Deux pièges découverts en branchant l'infra**, les deux invisibles dans les 490 tests
  mockés : `Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true` n'est posé que dans
  `Program.cs`, jamais exécuté par les tests — sans lui, aucune colonne snake_case ne se
  mappe (`user_id` → `UserId` silencieusement ignoré, valeurs à zéro) ; et les variables de
  session (`SET @first_date = ...`) de `kikole_mock.sql` exigent `AllowUserVariables=true`
  dans la chaîne de connexion, sans quoi MySqlConnector les interprète comme des paramètres
  de requête liés et rejette `@first_date` comme non défini.

**Code**
- `required` plutôt que `null!` sur les DTO et les requêtes. Il n'y a plus aucun `null!`
  dans le projet.
- **DTO et requêtes sont des `record` à propriétés `init`.** Dapper les remplit sans
  problème — vérifié contre la vraie base, pas seulement en compilation. Les builders de
  test remplacent l'instance par une copie (`_dto = _dto with { … }`) au lieu de la muter ;
  c'est `record` qui rend `init` supportable côté tests.
- **Les ViewModels restent mutables, `init` non poursuivi.** Contrairement aux DTO, ils
  jouent deux rôles à la fois dans ce projet : accumulateur pendant le calcul (le
  contrôleur les remplit par bouts au fil de branches conditionnelles) et forme finale pour
  la vue. `HomeModel` est le cas extrême — plus de 40 propriétés `{ get; set; }`, remplies
  sur ~260 lignes de `HomeController.Index`, et mutées par ses propres méthodes
  (`SetPropertiesFromProposal`) appelées une fois par proposition dans une boucle
  `foreach` : un accumulateur par construction, pas un objet qu'on remplit une fois.
  `AccountModel` aurait pu passer en `init` isolément (un `if`/`else if` par branche, pas de
  boucle), mais rendre *certains* ViewModels immuables sans pouvoir le faire pour tous
  perd l'intérêt : la moitié du bénéfice (cohérence du style, un seul motif à connaître)
  pour tout le coût de la réflexion au cas par cas. Le vrai correctif serait de séparer le
  calcul (un service retourne un résultat complet, comme `ScoreCalculator` le fait déjà
  pour le score) de la projection vers la vue (un seul mapping final, immuable) — un travail
  de conception par action de contrôleur, pas une conversion syntaxique, hors périmètre pour
  l'instant.

  **Piste alternative envisagée puis écartée : `init` sur les propriétés input, `set` sur
  les propriétés output**, propriété par propriété plutôt que modèle par modèle. Vérifiée
  concrètement sur `AccountModel` et `HomeModel` (POST) en croisant modèle, contrôleur et
  vue Razor (`@Html.HiddenFor` pour repérer ce qui est réellement lié) : **aucun cas
  litigieux trouvé** — chaque propriété est déjà proprement soit input jamais réaffectée
  après le binding, soit output jamais vraiment liée à un `<input>`. Écartée pour deux
  raisons : (1) elle ne touche pas le vrai problème de `HomeModel`, les propriétés
  dangereuses (mutées dans la boucle de `SetPropertiesFromProposal`) restant `set`
  quoi qu'il arrive ; (2) la protection est asymétrique — `init` empêche bien une
  réaffectation future d'un input, mais rien n'empêche l'inverse (une propriété `set`/output
  devenant un jour bindable si quelqu'un ajoute un `HiddenFor` dessus, exactement le sens où
  un bug de confiance apparaîtrait). L'audit croisé modèle/contrôleur/vue reste une méthode
  utile si un doute resurgit, même sans en faire une conversion de code.
- **Les signatures de dépôt restent nullables.** Lever dans le dépôt économiserait 2 gardes
  sur 10 et en casserait 5 : quatre appelants au moins traitent `null` comme flux de
  contrôle normal, dont `AuthorizationFilter`, sur le chemin de chaque requête. La couche
  d'accès dit « il n'y a pas de ligne » ; l'appelant décide si c'est une erreur.
- `RemoveDiacritics` conserve le passage par ISO-8859-8 : le *best-fit mapping* rabat `ø`,
  `ł`, `Æ` sur leur équivalent ASCII, ce que la normalisation NFD ne sait pas faire.
