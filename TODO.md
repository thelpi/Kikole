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
| Tests | **577** unitaires (mockés, rapides) + **5** d'intégration (vraie base, `--filter Category=Integration`), projet `KikoleSiteUnitTests` |
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

- [x] ~~Rendre les clubs canoniques~~ — le champ club était un `<input type="text">` libre ;
      contrairement au continent et à la nationalité, l'autocomplétion ne remplissait aucun
      champ caché. Refait : `clubs` + nouvelle table `club_translations` (nom canonique et
      alias par langue, cf. « Partis pris »), `country_id` sur `clubs`, autocomplétion par
      ID des deux côtés (proposition quotidienne et création de joueur). Au passage, bug
      préexistant corrigé : `site.js` lisait `item.Value`/`item.Key` (casse Pascal) alors
      que `Json()` renvoie `value`/`key` — le menu déroulant pays/continent affichait des
      lignes vides depuis toujours.
- [ ] **Remplir la base des clubs** — **en cours**, sourcée pays par pays. Méthode ayant
      évolué au fil du sourcing : Wikipedia (clubs actuels + historiques majeurs) pour la
      France, puis pivot vers `Championship Manager 01/02` (fichiers `.dat`/`.lng` du jeu,
      offset `Nation` reverse-engineered dans `club.dat`, traductions FR authentiques via
      `fra.lng`/`eng.lng`) pour l'Italie et la Grèce — bien plus complet et fiable que
      Wikipedia pour les divisions inférieures. Base empirique de 2023
      (`Restauration/clubs_2023.txt`) gardée en tout dernier recours.
      - [x] **France : 84 clubs** (Ligue 1/2 2025-26 + historiques majeurs + Racing Club de
        France résolu par l'utilisateur, tous les changements de nom en alias sans les
        années). Stade Français volontairement exclu (activité trop brève/discontinue).
      - [x] **Italie : 64 clubs** — Serie A + B 2001-02 (38), puis 26 clubs de Serie C
        (2001-02) ayant un vrai passé Serie A/B avant ou après, sélectionnés au cas par cas
        plutôt que les ~90 clubs C1/C2 en bloc.
      - [x] **Grèce : 28 clubs** — Division A + B 2001-02.
      - **Enjeu de conception découvert en cours de route, pas juste du contenu manquant** :
        à l'époque où le jeu était en ligne, des joueurs se servaient des trous de
        l'autocomplétion (un club obscur présent ou absent) comme signal méta pour
        déduire le joueur du jour parmi plusieurs candidats. Une base de clubs incomplète
        n'est donc pas neutre — elle fuite de l'information. À garder en tête pour la suite
        du sourcing (viser l'exhaustivité des clubs *plausibles* pour les joueurs déjà en
        base, pas juste les clubs les plus connus).
      - Ensuite : encore quelques pays si besoin, puis le Royaume-Uni (clubs déjà possible
        maintenant que la bascule FIFA ci-dessous est faite — Angleterre/Écosse/Galles/
        Irlande du Nord existent).
      - [ ] **Chantier en cours (autonome) : Espagne, Allemagne, Angleterre, Pays-Bas,
        Belgique, Portugal, Écosse, Turquie.** Consigne exacte de l'utilisateur : Division
        1 et 2 (source `Championship/Football Manager 2001/2002`, même méthode que
        Italie/Grèce), + un 3ème échelon pour l'Angleterre spécifiquement, + clubs de
        division inférieure **pour les 8 pays** (pas seulement l'Angleterre — précision
        explicite de l'utilisateur après une première lecture ambiguë) si pertinence
        historique ou club actuellement en D1/D2. "À la moindre ambiguïté : consigner et
        arbitrer a posteriori" — utiliser cette section pour tout ce qui reste ouvert.
        - `country_id` (enum Countries, déjà en base) : Belgique 22, Allemagne 84,
          Pays-Bas 157, Portugal 179, Espagne 210, Turquie 228, Angleterre 235, Écosse 250.
        - Fichier cible : `kikole.sql` (le catalogue de référence complet vit là, pas dans
          `kikole_mock.sql` — vérifié : la base locale actuelle n'a que 12 clubs, la
          fixture volontairement minimale de `kikole_mock.sql`, pas les 176 clubs
          France/Italie/Grèce déjà sourcés qui eux ne vivent que dans `kikole.sql`).
          Prochain `id` disponible dans `clubs` : **177** (vérifié, le max actuel est 176).
        - **Fichiers source localisés** : `C:\Program Files (x86)\Championship Manager 01-02\Data`
          — `club.dat` (6 146 980 octets, **tous pays confondus**, pas un fichier par
          pays — correction de l'utilisateur, ma demande initiale de "fichiers par pays"
          était une mauvaise hypothèse) + `fra.lng`/`eng.lng` (traductions, pas encore
          exploitées — cf. point ouvert plus bas).
        - **Format binaire de `club.dat` reverse-engineered (nouveau, à partir de zéro —
          aucune note de la session Italie/Grèce n'a survécu)** :
          - Enregistrements de longueur fixe **581 octets**, **10 580 clubs** au total
            (6 146 980 / 581, division exacte).
          - Nom complet du club : chaîne C (ASCII/Windows-1252, terminée par `\0`) à
            l'offset **+4** dans l'enregistrement, largeur max 51 octets.
          - Offset **+83** (1 octet) = **ID Nation**. Validé par isolement : filtrer sur
            une valeur donne exactement l'ensemble des clubs (grands et petits/amateurs)
            d'un seul pays.
          - Offset **+87** (1 octet) = **ID Division/compétition (saison 2001-02)**.
            Validé : filtrer nation+compétition reproduit exactement l'effectif D1 connu
            de chaque championnat (ex. Espagne → 20 clubs = les 20 de Liga 2001-02 pile,
            Angleterre → 20 = Premier League pile, etc.) Note : offset +91 recopie
            toujours la même valeur que +87 dans tous les échantillons testés — cause non
            investiguée (champ redondant ?), sans conséquence puisque +87 seul suffit à
            filtrer.
          - Extraction faite en PowerShell (`[System.IO.File]::ReadAllBytes`, encodage
            Windows-1252 pour les caractères accentués) : `Bash`/Git Bash n'a pas `strings`
            ni d'outil binaire pratique dans cet environnement. Attention si un script
            PowerShell génère du SQL réinjecté ensuite via `sed`/sed -i 'Nr fichier' :
            `Set-Content -Encoding UTF8` ajoute un BOM, qui casse la syntaxe SQL une fois
            spliced au milieu d'un fichier existant — buté dessus deux fois cette session,
            `sed -i '<ligne>s/^\xEF\xBB\xBF//'` pour le retirer.
          - **Nation / Division 1 / Division 2 confirmés, effectifs validés par les noms
            (tous vérifiés club par club, correspondent exactement aux championnats
            2001-02 réels). Les ID ci-dessous ("Espagne 171", etc.) sont l'ID Nation
            interne au jeu (offset +83), pas le `country_id` de l'enum Countries de
            l'appli (déjà donné plus haut : Espagne 210, Allemagne 84, etc.) — deux
            systèmes d'ID différents, à ne pas confondre lors d'un futur import** :
            - Espagne 171 : D1=52 (20, Liga), D2=53 (22, Segunda División).
            - Allemagne 73 : D1=16 (18, Bundesliga), D2=17 (18, 2.Bundesliga).
            - Angleterre 60 : D1=7 (20, Premier League), D2=8 (24, Football League
              Division One), **D3=9 (24, Football League Division Two)** — le 3ème
              échelon demandé spécifiquement pour ce pays.
            - Pays-Bas 83 : D1=22 (18, Eredivisie), D2=23 (18, Eerste Divisie).
            - Belgique 19 : D1=0 (18, Division 1), D2=1 (18, Division 2).
            - Portugal 149 : D1=46 (18, Primeira Liga), D2=47 (18, Segunda Divisão).
            - Turquie 192 : D1=174 (18, Süper Lig), D2=29 (20, 1.Lig).
            - Écosse 160 : D1=34 (12, Scottish Premier League — effectif réel de
              l'époque, la SPL était déjà réduite à 12 clubs), D2=35 (10, Division One).
            - **Total D1+D2(+D3 Angleterre) = 314 clubs** (42+36+68+36+36+36+38+22) —
              nettement plus que France+Italie+Grèce réunis (176). Rien qu'avec ces
              deux/trois échelons obligatoires, le volume est déjà important — cf.
              remarque de l'utilisateur ("ça va déjà en faire pas mal").
            - **Échelons inférieurs repérés mais PAS inclus par défaut** (comptage
              disponible, en attente de jugement "pertinence historique ou club
              actuellement D1/D2" - piste pour une passe ultérieure séparée, comme la
              Serie C italienne l'a été après la Serie A/B) : Espagne comp 54/55/56/57
              (Segunda B, 4 groupes régionaux, 80 clubs dont beaucoup de réserves "B") ;
              Allemagne comp 20/21 (Regionalliga Nord/Süd, 36, dont plusieurs grands noms
              historiques déchus : Kickers Offenbach, Rot-Weiss Essen, Fortuna Düsseldorf) ;
              Angleterre comp 10 (Division Three officielle, 24 — 4ème palier réel, hors
              périmètre du "3ème échelon" demandé, sauf pertinence historique individuelle) ;
              Belgique comp 2/130/131/132/133 (régionalisé, ~80) ; Portugal comp 48/49/50
              (3 zones régionales, 60, dont Académica de Coimbra dans la D2 elle-même en
              fait, à vérifier zone par zone) ; Turquie comp 33/34/35/36 (2.Lig régionalisé,
              ~40) ; Écosse comp 36/37 (Division Two/Three, 20).
          - **Point non résolu, contournement pragmatique adopté** : le champ "nom court"
            repéré juste après le nom complet (offset +55, préfixé d'un octet `0xFF`) n'a
            pas été décodé — pas nécessaire pour les noms canoniques. Idem pour `fra.lng`/
            `eng.lng` : un premier test (recherche de "Juventus" dans `eng.lng`) n'a rien
            donné d'exploitable en l'état (pas d'alignement positionnel simple malgré une
            taille de fichier identique aux deux langues). **Décision prise pour avancer** :
            nom canonique EN = nom canonique FR pour tous les clubs de ces 8 pays, sauf
            cas particulier connu avec certitude (aucun identifié pour l'instant) — cohérent
            avec la remarque déjà actée pour la France ("identique EN/FR pour la quasi-
            totalité de ces clubs"), et les divergences EN/FR type "Juventus Torino" vs
            "Juventus FC" semblent être une particularité italienne, pas une règle générale
            du jeu. Les alias restent possibles au cas par cas (jugement humain/notoriété),
            pas extraits mécaniquement du fichier.
      - [x] **314 clubs insérés dans `kikole.sql`** (ids 177-490, `clubs` +
        `club_translations` EN/FR) : Espagne 42 (D1 20 + D2 22), Allemagne 36 (18+18),
        Angleterre 68 (D1 20 + D2 24 + D3 24), Pays-Bas 36 (18+18), Belgique 36 (18+18),
        Portugal 36 (18+18), Turquie 38 (18+20), Écosse 22 (12+10). Généré par script
        (extraction directe depuis `club.dat`, cf. offsets ci-dessus) plutôt que saisi à
        la main — 314 lignes, aucune ambiguïté sur cette partie. **Vérifié avant
        d'écrire dans le fichier final** : chargement isolé dans une base de test
        jetable (`kikole_test`, détruite après coup) à partir de la section
        `clubs`/`club_translations` de `kikole.sql` — 0 erreur, 490 clubs au total
        (176 + 314), répartition par pays exacte, 314 clubs avec traductions EN+FR.
      - [x] **Tension `kikole_mock.sql`/catalogue résolue** (décision de l'utilisateur,
        deux consignes) : (1) `kikole_mock.sql` ne truncate/ne définit plus jamais
        `clubs`/`club_translations` — ces tables rejoignent officiellement les données
        de référence préservées entre deux rejeux (comme `countries`/`badges`), le
        commentaire d'en-tête du script mis à jour en conséquence. (2) Les joueurs de la
        fixture (Andrea Pirlo + le pool de 8 joueurs mockés) référencent désormais les
        vrais id du catalogue complet plutôt qu'une numérotation locale 1-12 :
        correspondance documentée en commentaire dans `kikole_mock.sql` (ex. Real
        Madrid C.F. = 189, Manchester United = 268...). "New York City FC" (sans
        équivalent dans le sourcing pays par pays) a été essayé un temps comme entrée de
        catalogue à part, puis **retiré** sur retour de l'utilisateur : plus simple de
        raccourcir la carrière mockée d'Andrea Pirlo (elle s'arrête à la Juventus) que
        d'ajouter une entrée hors périmètre au milieu d'un import par ailleurs propre —
        une carrière mockée incomplète n'a aucune importance, c'est un fixture de test.
        **Migration one-shot appliquée à la base locale** (justifiée : elle n'avait en
        réalité jamais eu le catalogue complet chargé, seulement les 12 clubs de
        l'ancienne fixture, avec un id 1 en collision directe avec `kikole.sql` lui-même
        - Angers SCO côté catalogue, AS Cannes côté mock) : `clubs`/`club_translations`
        vidées puis rechargées avec les 490 lignes de `kikole.sql`, puis
        `kikole_mock.sql` rejoué en entier — 0 erreur, vérifié que les carrières
        (Zidane, Beckham, Ronaldinho, Pirlo...) pointent vers les bons clubs du
        catalogue réel. Ce sera désormais le comportement normal et permanent : plus
        besoin d'y revenir à chaque futur rejeu.
      - **Reste (passe séparée, curation manuelle, pas commencée)** : les échelons
        inférieurs listés ci-dessus (Espagne Segunda B, Allemagne Regionalliga,
        Angleterre Division Three, Belgique/Portugal/Turquie 3ème palier régionalisé,
        Écosse Division Two/Three) — décider club par club sur la base de la pertinence
        historique, pas d'inclusion en bloc. Volume total disponible si tout était inclus :
        largement supérieur aux 314 déjà ajoutés, donc vraiment une passe à part.
- [x] ~~Pays/continent au sens FIFA plutôt qu'ONU~~ — `countries` est désormais la liste des
      211 fédérations FIFA (plus 4 nations sportives disparues, voir plus bas), codes à 3
      lettres, `continent_id NOT NULL` sur chaque ligne (confédération réelle, pas la
      géographie — Israël/Chypre/Kazakhstan en UEFA, Australie en AFC, Guyana/Suriname en
      CONCACAF). Royaume-Uni éclaté en 4 (Angleterre/Écosse/Galles/Irlande du Nord), les
      ~42 territoires ISO sans fédération FIFA (Åland, Monaco, Vatican...) supprimés plutôt
      que gardés avec un continent nul. Voir « Partis pris » pour le détail du recodage.
- [x] ~~Nationalités doubles et sportives~~ — **pas un système multi-nationalités
      générique** : `players.alternative_country_id` (nullable, un seul) couvre le cas
      réel identifié (nation sportive disparue → successeur), voir « Partis pris ». Le cas
      général (double sélection, ex. Algérie puis France) reste à traiter séparément le
      jour où il se présente.
- [x] ~~Lier `country_id`/`continent_id`~~ — `players.continent_id` **supprimé** : le
      continent n'est plus stocké du tout, il est déduit à la volée de `country_id` (et
      `alternative_country_id`) via `countries.continent_id`, jamais persisté sur le
      joueur. Voir « Partis pris » pour l'architecture (le calcul vit dans
      `ProposalResponse`/`ScoreCalculator`, classes pures sans accès aux données ; la
      correspondance pays→continent est chargée une fois et **passée en paramètre**,
      comme les dictionnaires déjà utilisés par `HomeModel`).
      - [x] Badge `OneMinuteChrono` (`BadgeService`) : la condition n'exige plus de
        proposition Continent séparée dans l'historique, le pays suffit.
      - [x] Création de joueur (admin) : le champ Continent a disparu du formulaire,
        entièrement déduit du pays choisi côté serveur.
      - [x] Cas Algérie/France resolu **via `alternative_country_id`**, pas via une
        révélation auto du pays vers le continent : deviner le continent du pays **ou**
        du pays alternatif valide la proposition (même principe que pour le pays), les
        deux s'affichent au reveal quand ils diffèrent (`"Amérique du Sud / Europe"`).
      - [x] **Révélation automatique du continent une fois le pays trouvé** — quand une
        proposition Country réussit, `HomeModel.SetPropertiesFromProposal` déduit
        directement `ContinentName` (pays + pays alternatif) au lieu d'attendre une
        proposition Continent séparée. Le champ de saisie sur `Views/Home/Index.cshtml`
        disparaît par le même `@if (string.IsNullOrWhiteSpace(Model.ContinentName))` que
        celui déjà utilisé pour le pays — aucun changement de vue nécessaire, seul le
        modèle change d'état plus tôt. Même code que le chemin « reveal complet »
        (`HomeController.SetAndGetViewModelAsync`, déjà écrit ainsi).
- [x] ~~Réécrire la page de règles (nationalité administrative vs sportive)~~ — l'ancien
      texte affirmait "le jeu ne gère pas la nationalité sportive" (exemple Ryan Giggs =
      "Royaume-Uni", pas "Pays de Galles"), un principe contredit par la bascule FIFA
      ci-dessus (Écosse/Galles/Irlande du Nord n'existent qu'au sens sportif, pas
      administratif). Réécrit entièrement (`AboutCountryDetails`, FR et EN,
      `Resources/Views/Home/Partial/Rules.*.resx`) : la nationalité affichée est
      désormais présentée comme sportive dès la première phrase, avec un lien direct vers
      la liste FIFA, la mention des 4 sélections disparues conservées (URSS, ex-Yougoslavie,
      RDA, Tchécoslovaquie) et de leurs cas de fusion (RFA, Serbie-et-Monténégro), et les
      deux cas de double sélection (`alternative_country_id` seul, puis le cas à deux
      continents différents). Les anciens cas d'arbitrage (Mendy, Darcheville, Simons)
      disparaissent : ils n'étaient des "cas complexes" que sous l'ancien système
      administratif, la logique sportive ne laisse plus d'ambiguïté à leur sujet.
- [x] ~~Postes multiples (plainte v1, ex. Eden Hazard milieu/attaquant)~~ — calqué à
      l'identique sur `alternative_country_id` : `players.alternative_position_id`
      (nullable, un seul poste secondaire), deviner l'un ou l'autre valide la proposition
      Position, les deux s'affichent au reveal (`"Milieu de terrain / Attaquant"`). Les 4
      catégories existantes (Gardien/Défenseur/Milieu/Attaquant) restent inchangées, aucun
      affinage. Voir « Partis pris ». Vérifié en base (colonne + FK) et en direct (joueur
      test créé en admin, poste alternatif deviné et affiché, reveal complet aussi
      vérifié) avant remise à zéro de `kikole_mock.sql`.
- [x] ~~Ajouter les clés étrangères~~ — 29 relations `_id`, contrainte `RESTRICT` par défaut
      (aucun `ON DELETE`/`ON UPDATE` explicite), voir « Partis pris ».
- [x] ~~`IClubService` — non justifié aujourd'hui (CRUD nu) ; le deviendra si les clubs
      passent en canonique.~~ **Décision : pas nécessaire.** Passage en canonique fait
      (traductions par langue, `country_id`, autocomplétion par ID), tout absorbé par
      `InternationalService` existant (déjà le point d'entrée pour pays/continents) sans
      ajouter de couche — même précédent que `Message`/`Discussion`, qui ne méritent pas
      de service dédié.

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
- [x] ~~Audit des zones non couvertes par les tests unitaires~~ — comblé : `StatisticService`
      (seul service du projet sans test, anonymisation/tri/moyennes),
      `KikoleBaseController` (`UserId`/`UserType`/`GetSubmitAction`, premier contrôleur
      testé du projet, via une sous-classe de test minimale), et les deux helpers privés
      d'`AdminController` (`SplitAlternativeNames`, `AddClubIfValid`, passés en `internal`
      pour les exposer, même motif que `ProposalRequest.GetTip`). **Correction en cours de
      route** : `ScoreCalculator.GetProposalResponsesWithPoints`, initialement identifié
      comme un trou, s'est révélé déjà entièrement couvert (`ProposalServiceTests.cs`, dont
      le commentaire de classe le précise) — juste rangé sous un nom de fichier trompeur.
      Reste volontairement hors périmètre (repos délibéré, pas repris ici) : les
      dépôts (décision déjà actée plus haut) et le reste des contrôleurs
      (`AccountController`, le reste d'`AdminController`, `HomeController`,
      `LeaderboardController`) — plus coûteux, demanderait de mocker systématiquement
      `HttpContext`/`ClaimsPrincipal` par action plutôt que sur des méthodes isolées.
- [x] ~~Audit des calculs de badges (`BadgeService`)~~ — trous identifiés en relisant
      `BadgeService.cs` ligne par ligne : `LeaderBasedBadgeCondition` (badges liés au score/
      horaire du jour) et la plupart des `ProposalsBasedBadgeCondition` étaient déjà couverts,
      mais pas le reste. **Premier lot comblé** (23 nouveaux tests, tous dans
      `BadgeServiceTests.cs`) :
      - `PlayersHistoryBasedBadgeCondition` (`FourFourtwo`, `AroundTheWorld`) — nécessitait un
        nouvel helper `RunWithPastFinds` pour simuler un historique multi-jours (le jour du
        gain doit être décalé après `FirstDate` pour laisser de la place à un passé, sinon la
        fenêtre `[FirstDate, gain]` ne contient que le jour même).
      - `OneMinuteChrono` (la condition la plus longue du fichier) — 5 tests couvrant le cas
        nominal et les rejets (trop lent, catégorie manquante, indice demandé, moins de clubs
        proposés que la carrière n'en compte).
      - `PrepareNonLeaderBadgesAsync` (badge `Dedicated`, streak de 30 jours) — y compris le
        cas où un jour de la série est couvert par la création d'un joueur publié plutôt
        qu'une proposition.
      - `GetUserBadgesAsync` — la règle de visibilité des badges cachés obtenus le jour même
        (soi-même ou administrateur voient, un autre utilisateur standard non) et le filtre
        `foundToday`.
      - `AddBadgeToUserAsync`, `GetAllBadgesAsync` (tri par nombre d'utilisateurs, et la
        branche description traduite/repli jamais exercée jusqu'ici — tous les tests
        existants appelaient le service en `Languages.en`).
      **Second lot comblé** (20 nouveaux tests) :
      - `OverTheTopPart1`/`Part2` (unicité du meilleur temps/score du jour) — cas solo,
        égalité (personne ne l'obtient, l'ancien détenteur du jour se le fait retirer),
        battu par plus rapide (aucun appel de vérification de réattribution, court-circuit),
        et dépassement strict (réattribution effective, `RemoveUserBadgeAsync` puis
        `InsertUserBadgeAsync`).
      - Les 7 badges "en série" (`ThreeInARow`, `AWeekInARow`, `LegendTier`, `MakeItDouble`,
        `TheBreakfastClub`, `MetroBoulotKikoleDodo`, `HellOfAWeek`), tous portés par
        `RespectLeadersRunConditionsInternal` — nouvel helper `RunStreak`/`ConsecutiveWins`
        pour simuler une série de gains consécutifs se terminant par un jour de gain décalé
        (`WinDay`, +40 jours après `FirstDate`, pour laisser la place à la série de 30 jours
        de `LegendTier`). Comportement notable capturé par un test dédié
        (`ADayWhereThePlayerWasCreatedInsteadOfFoundDoesNotBreakTheStreak`) : un jour où
        l'utilisateur a **créé** le kikolé plutôt que de le trouver est ignoré par
        l'algorithme (ni requis, ni ne casse la série) — différent d'un jour sans aucune
        activité, qui l'interrompt net. `HellOfAWeek` a en plus un test où la série n'est
        pas cassée mais le cumul de points est insuffisant (7 gains consécutifs, total sous
        le seuil).
      Couverture de `BadgeService` désormais complète sur les deux lots identifiés à
      l'audit initial.
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

- [x] **Rendre le graphisme plus attrayant.** Direction validée : la page de jeu comme un
      dossier de scout (`kikole-board.css`, `Views/Home/Index.cshtml`), cf. le plan
      `linear-pondering-wind.md`. Toutes les pages passées, sauf exception explicite :
      menu de navigation global (`_Layout.cshtml`), datepicker jQuery UI, page Compte,
      présentation/règles, fiches badge, classement, palmarès, détail des statistiques
      d'un utilisateur, puis dans un dernier lot — page Contact, page Concours d'octobre,
      page d'erreur générique, et les cinq pages `Admin` (créer un joueur, créer/éditer un
      club, éditer les indices d'un joueur publié, valider les joueurs proposés, actions
      d'administration). Tableaux de données réhabillés avec les classes historiques de
      `site.css` conservées telles quelles (certaines sont régénérées par `site.js` en
      AJAX, cf. `initializeLeaderboards`) ; nouveaux composants génériques ajoutés au
      passage (`textarea.blank`, `input[type="datetime-local"]`, `input[type="checkbox"]`
      accentué, `.actions a` pour un lien qui a l'air d'un bouton). Formulaires à
      interactions JS non triviales (autocomplétion club/pays/année, lignes de club
      dynamiques `Admin/Index.cshtml`, vérif. club par nom `Admin/Club.cshtml`) revérifiés
      en direct sans changement d'`id`/`name` : soumissions, autocomplétion (requêtes AJAX
      confirmées), cycle complet proposition → acceptation/refus testé avec un compte
      temporairement promu PowerUser (reremis à son palier d'origine ensuite, joueur/
      message de test nettoyés de la base locale). Volontairement laissées de côté, choix
      confirmé avec l'utilisateur : les pages statistiques réservées aux administrateurs
      (`Statistics/Stats.cshtml`, `Statistics/KikolesStats.cshtml`, déplacées depuis
      `Leaderboard/` lors de l'extraction du contrôleur dédié, cf. item dédié plus bas) — la
      seconde étant de toute façon prévue pour fusionner dans la première (cf. item dédié
      plus bas).
  - [x] **`Leaderboard/Palmares.cshtml` n'était pas localisée, et son intégration au reste
        du site ne convainquait pas** (un lien depuis le classement vers une carte vide à
        part le titre). **Résolu par une fusion complète** plutôt qu'une simple
        localisation : `Palmares.cshtml`/`PalmaresModel`/l'action `Palmares()` supprimés,
        leurs deux tableaux ("par mois" et "total") ajoutés comme deux
        cartes supplémentaires directement sur `Leaderboard/Index.cshtml`, à la suite du
        classement quotidien et général — `/Leaderboard/Palmares` n'existe plus du tout
        (404), plus besoin d'un point d'accès dédié. Contenu localisé au passage (nouvelles
        clés dans `Leaderboard/Index.*.resx`), y compris le reliquat anglais "No data to
        display". Vocabulaire changé de "Palmarès" à "Podium" (demande explicite) :
        "Podium mensuel" / "Podium général" — ce dernier fait écho à "Classement général"
        juste au-dessus (même portée : cumul sur toute la période), plutôt que la
        proposition "Cumul des podiums" avec laquelle l'utilisateur n'était pas satisfait.
        Bug corrigé au passage : `PalmaresModel` projetait le tableau "total" sans l'id de
        l'utilisateur (`x.user.Login` seul, pas `x.user.Id`), rendant les lignes non
        cliquables contrairement au tableau "par mois" — aurait aussi empêché le surlignage
        "vous" ci-dessous.
  - [x] **Surbrillance de l'utilisateur connecté dans les tableaux** (classement quotidien,
        classement général, les deux podiums) — n'existait pas, ajoutée à la demande de
        l'utilisateur en marge du chantier Palmarès. Cellule utilisateur en vert gras +
        petit suffixe "(vous)"/"(you)", plutôt qu'un fond de ligne : reste lisible même
        combiné à la couleur d'une médaille (`.medal-gold/-silver/-bronze`) ou au fond doré
        de la ligne "créateur du jour" (`.creator`), sans avoir à trancher une priorité
        entre les deux. Un seul point d'entrée CSS (`.kikole-board td.you`, sélectionné par
        spécificité plutôt que `!important`) couvre toutes les combinaisons. Les deux
        tableaux de classement se régénèrent en AJAX au changement de tri/date
        (`site.js`) : `initializeLeaderboards`/`loadGlobalLeaderboard`/
        `loadDailyLeaderboard` reçoivent désormais l'id de l'utilisateur connecté et le
        libellé "(vous)", via une fonction `appendUsernameCell` partagée pour ne pas
        dupliquer la logique de surlignage à 3 endroits. Vérifié en direct (FR et EN) :
        surlignage correct dans les 4 tableaux, persiste après un changement de tri
        (rafraîchissement AJAX), route `/Leaderboard/Palmares` bien introuvable (404).
  - [x] **Certains badges peuvent-ils donner un indice gratuit par le seul fait d'être
        obtenus ?** Tout le monde peut voir les badges de tout le monde. **Vérifié :** la
        règle est déjà en place et testée (`BadgeService.GetUserBadgesAsync`, paramètre
        `foundToday`) — tant qu'on n'a pas trouvé le joueur du jour (et qu'on n'est ni
        administrateur, ni le créateur du joueur du jour, ni passé par l'accès payant au
        classement — cf. `DayGrantTypes`), on ne voit aucun badge obtenu par un autre
        utilisateur *le jour même*, quel que soit le badge (pas seulement les badges
        secrets). Confirmé par les tests unitaires existants
        (`FoundTodayFalseExcludesBadgesEarnedToday` et les tests voisins dans
        `BadgeServiceTests.cs`) et re-vérifié en direct dans le navigateur (joueur2, qui
        n'avait pas trouvé le joueur du jour, ne voyait pas les badges du jour de joueur1 ;
        un compte administrateur les voyait). Aucun changement de code nécessaire.
  - [ ] **Bug graphique : superposition des blocs beige au moment de la victoire.**
        Une fois le joueur trouvé, quand la page s'affiche avec les badges puis le bloc
        présentation/règles en dessous, un rendu bizarre apparaît (les blocs beige se
        superposent). Semble ne se produire que sur le rendu de la page juste après la
        victoire (POST qui affiche le résultat), pas en quittant puis revenant sur la page
        (GET) — donc probablement lié à un état transitoire de layout (badges qui
        s'animent/se dimensionnent après coup ?) plutôt qu'au HTML/CSS statique. À
        reproduire et diagnostiquer.
  - [x] **Popup "Etes vous sûr ?" au clic sur "Montrer la réponse" en style par défaut du
        navigateur.** `Views/Home/Index.cshtml` utilisait `onclick="return confirm(...)"`
        (bouton Give up) — sortait complètement de l'habillage papier/encre. Remplacé par
        une vraie modale (`.confirm-modal`, cachée par défaut, `.open` l'affiche — même
        convention que `.site-nav-drawer.open`). Le bouton déclencheur reste un vrai
        `type="submit" name="submit-GiveUp"` (pour que `GetSubmitAction()` le lise côté
        serveur) : `onclick="return openGiveUpModal(event)"` bloque juste la soumission
        tant que la modale n'est pas confirmée (`site.js`) ; le bouton "confirmer" de la
        modale appelle `form.requestSubmit(triggerButton)` en repassant le bouton d'origine
        comme *submitter*, seule façon que son `name`/`value` soit inclus dans le POST sans
        dupliquer le formulaire. Nouvelle clé resx `CancelAction` (`Home/Index.*.resx`).
        Vérifié en direct (`joueur1`, un jour non résolu) : ouverture, Annuler (aucune
        requête envoyée, état inchangé), puis Confirmer (POST réel, `submit-GiveUp` bien lu
        côté serveur, page affichant la réponse avec 0 point).
  - [x] **Affichage "Le joueur du xxxx était xxxx." très bizarre (police, taille).**
        `Views/Home/Index.cshtml`, cas "jour passé raté" (et son jumeau "PlayerIs", même
        souci, cas où le créateur consulte son propre kikolé du jour) : `<h1>` brut sans
        style, un oubli du passage en `.kikole-board`. Corrigé par une règle
        `.kikole-board .dossier-head h1` (Bebas Neue, même traitement que le reste du
        dossier) ; les deux `<h1>` n'ont plus que leur `<span>` de couleur inline. Vérifié
        en direct sur un jour passé.
  - [x] **Datepicker du classement différent de celui de la page d'accueil (style natif du
        navigateur).** `Leaderboard/Index.cshtml` utilisait `<input type="date">` natif.
        Corrigé : les 3 champs (`LeaderboardDay`, `MinimalDate`, `MaximalDate`) sont
        maintenant de vrais champs texte `readonly` avec le même widget jQuery UI que la
        page d'accueil (`kikoleDatepickerRegional`, hissé en variable partagée dans
        `site.js` pour les deux usages). Format forcé en ISO (`yy-mm-dd`) quelle que soit la
        langue — c'est la valeur brute lue par `initializeLeaderboards` pour les appels
        AJAX, seuls les libellés du calendrier (mois, "Aujourd'hui"...) restent localisés.
        jQuery UI ne déclenchant pas l'évènement `change` natif à la sélection, `onSelect`
        déclenche `$(this).trigger("change")` pour réutiliser les handlers déjà posés par
        `initializeLeaderboards`, sans toucher à cette fonction. Vérifié en direct :
        sélection dans le calendrier stylé → tableau rafraîchi en AJAX, comme avant.
  - [x] **Meilleur accès au palmarès depuis le classement.** Résolu par la fusion des deux
        pages (cf. item "Palmarès" plus haut) — la carte vide ne pointe plus vers une page
        séparée, les deux tableaux sont directement sur cette page.
  - [x] **Mise en valeur des kikolés "tentés/trouvés le jour même" à revoir.**
        `Leaderboard/User.cshtml`, tableau "Statistiques quotidiennes" : l'ancienne
        convention (texte en gras + astérisque dans l'en-tête, expliqué par une légende
        sous le tableau) obligeait à lire la légende pour comprendre. Remplacée par une
        puce/teinte de fond (`.same-day-tag`, fond `--pitch-soft`/texte `--pitch-strong`,
        forme pilule) directement sur "Oui"/"Non" quand `AttemptDayOne`/`SuccessDayOne` est
        vrai, lisible sans légende ; un `title` (attribut natif, réutilise la clé resx
        existante `CurrentDay`) donne le détail à qui survole. Astérisques d'en-tête et
        légende (`BoldFirstDay`) retirés, clé resx devenue inutile supprimée (FR/EN).
        Vérifié en direct et par inspection DOM (`getComputedStyle`) sur le profil de
        `joueur1`.
- [x] **Formulaire "Changer la question et réponse de récupération" (page Compte) devrait
      redemander le mot de passe actuel.** Corrigé : nouveau champ mot de passe actuel sur
      ce formulaire (réutilise `AccountModel.PasswordSubmission`, déjà partagé par les
      formulaires Connexion/Changer le mot de passe), vérifié côté serveur via
      `UserManager.CheckPasswordAsync` avant d'accepter la mise à jour (message d'erreur :
      `InvalidPassword`, une clé resx de `AccountController` qui existait déjà mais n'était
      utilisée nulle part). Pas de verrou anti-bruteforce ajouté ici (contrairement à la
      réponse de récupération sur le formulaire mot de passe oublié) : ce formulaire exige
      déjà une session authentifiée, donc un attaquant qui devine ce mot de passe a de toute
      façon déjà accès au compte via la session volée — pas de surface d'attaque nouvelle.
      Vérifié en direct : mauvais mot de passe → rejeté (`Mot de passe invalide`), bon mot
      de passe → mise à jour acceptée.
  - [x] **Bug découvert en testant ci-dessus, plus large que ce formulaire :**
        `AccountController.Index` (POST) ne renseignait `model.IsAuthenticated`/
        `model.Login` que dans les branches de **succès** de chaque formulaire ; sur
        n'importe quelle erreur de validation en étant connecté (mot de passe actuel
        incorrect, mots de passe qui ne correspondent pas...), le bandeau d'erreur
        s'affichait bien mais la page basculait sur le jeu de formulaires "non connecté"
        (Connexion/Créer un compte/Récupération) au lieu de rester sur les 3 cartes
        "connecté" — confirmé aussi bien sur `submit-changepassword` (préexistant, pas
        introduit par le point ci-dessus) que sur `submit-resetqanda`. L'état de connexion
        réel n'était pas affecté (juste l'affichage). **Corrigé** : les affectations
        ad hoc dans chaque branche de succès sont retirées, remplacées par une affectation
        unique juste avant `return View(model)`, basée sur l'état réel (`UserId > 0`) —
        fonctionne aussi pour "logoff", qui réinitialise `HttpContext.User` avant d'arriver
        à ce point. Sans arbitrage : c'était mécanique, pas une question de design.
        Re-vérifié en direct : mot de passe incorrect sur les deux formulaires → reste sur
        la vue connectée avec l'erreur ; mot de passe déjà compromis (vérif HIBP) → idem ;
        changement de mot de passe réussi → reste connecté ; déconnexion → bascule
        correctement sur la vue "non connecté".
  - [x] **`AccountController.Index` (POST) éclaté en une action par formulaire**, à la
        place du `if/else if` unique dispatché par nom de bouton (`GetSubmitAction()`,
        toujours utilisé tel quel par `AdminController`/`HomeController`, hors périmètre
        ici). Sept actions dédiées (`LogOut`, `LogIn`, `GetLoginQuestion`, `ResetPassword`,
        `ResetQAndA`, `Create`, `ChangePassword`), chaque `<form>` de
        `Views/Account/Index.cshtml` pointant directement sur la sienne (routing
        conventionnel déjà en place, pas de nouvelle route à déclarer) ; les `name="submit-
        xxx"` des boutons n'ont plus lieu d'être, retirés. `ResetQAndA`/`ChangePassword`
        passent de la vérification manuelle `if (UserId == 0) return
        RedirectToAction("ErrorIndex", "Home")` à `[Authorization]` (même attribut déjà
        utilisé par `LeaderboardController`, policy déjà branchée sur `/Home/ErrorIndex`
        via `LoginPath`/`AccessDeniedPath` — comportement identique, juste déclaratif). La
        redirection interne "create" réussi → connexion automatique appelle maintenant
        directement l'action `LogIn` au lieu de rappeler `Index` avec un indicateur
        `ForceLoginAction` (propriété supprimée de `AccountModel`, elle n'existait que pour
        ça). Fin de méthode factorisée en un seul point (`RenderIndex`), ce qui rend la
        classe de bug ci-dessus structurellement impossible à réintroduire par erreur.
        Aucun test existant sur ce contrôleur (déjà noté hors périmètre plus haut) ; les
        7 parcours (connexion, inscription, déconnexion, changement de mot de passe,
        changement Q&A, question puis réponse de récupération) et le rejet `[Authorization]`
        d'un accès non connecté ont été revérifiés en direct dans le navigateur.
  - [x] **Même chantier appliqué à `AdminController`**, seul autre contrôleur où le motif
        s'appliquait vraiment (vérifié aussi `HomeController.Index` : là le nom du bouton
        encode une valeur de `ProposalTypes`, pas une opération distincte — tout le corps
        est déjà partagé, éclater en 8 actions aurait dupliqué ~90% du code pour rien ;
        laissé tel quel, volontairement).
      - `Actions` (POST) → 4 actions dédiées (`RecomputeBadges`, `RecomputeLeaders`,
        `ReassignPlayers`, `InsertMessage`), le seul `<form>` à 4 boutons de
        `Views/Admin/Actions.cshtml` éclaté en 4 `<form>` ; fin de méthode factorisée
        (`RenderActionsAsync`) pour les valeurs par défaut (dates du formulaire de message,
        discussions).
      - `PlayerSubmission` (POST) → `pchoice` devient `ChoosePlayer` (déjà un formulaire
        séparé). `accepted`/`refusal` partageaient presque tout leur code (ne divergent que
        sur `IsAccepted`) : plutôt que de dupliquer, `AcceptPlayer`/`RefusePlayer` délèguent
        toutes les deux à une méthode privée commune (`RespondToSubmissionAsync`), même
        principe que la garde `Players.Count == 0 → redirect` factorisée à part
        (`EnsurePendingSubmissionsAsync`) puisque les 3 actions en ont besoin.
      - Comme pour Account, aucun test existant sur ce contrôleur. Les 7 actions
        revérifiées en direct dans le navigateur : les 3 boutons simples, l'ajout de
        message (jusqu'à sa réapparition en bandeau sur la page d'accueil), et le cycle
        complet proposition → validation avec un compte temporairement promu PowerUser en
        base locale (`ChoosePlayer`, `RefusePlayer`, puis `AcceptPlayer` avec ses indices de
        substitution requis) — compte reremis à son palier d'origine une fois le test
        terminé. Ça laisse deux joueurs de test dans la base locale (un refusé, un accepté)
        : sans conséquence, à emporter par un prochain rejeu de `kikole_mock.sql`.
- [x] **Plusieurs pages sans accès depuis le menu** (soit un lien caché, soit l'URL à
      connaître) — icônes ajoutées dans `_Layout.cshtml`, gérées par rôle, en miroir dans
      la barre desktop (`.site-nav-links`) et le tiroir mobile (`.site-nav-drawer`) :
      Contact (connecté), Proposer un kikolé (PowerUser+), Actions admin/Statistiques
      (Administrator). `Admin/PlayerSubmission` d'abord ajoutée en plus de la demande
      initiale (trouvée en audit, elle n'était linkée que depuis `/Admin`, lui-même absent
      du menu) — **revenu dessus** : pas d'icône dédiée, c'est une action d'administration
      comme les autres, son seul point d'accès est désormais un lien texte
      (`CheckSubmittedPlayers`) sur `Admin/Actions` (retiré de `Admin/Index`, où il vivait
      avant). `PlayerCreationModel.DisplayPlayerSubmissionLink` gardée : elle sert aussi
      (sans rapport) à conditionner l'affichage du champ indice anglais sur ce même
      formulaire. Stats/KikolesStats : un seul logo (vers `Statistics/Stats`, les
      graphiques) plutôt que deux, `KikolesStats` restant accessible par un lien depuis
      `Stats` (`KikolesStatsLink`) pour ne pas surcharger la barre.
      Nettoyage des accès devenus redondants : liens toujours visibles du footer
      (`SubmitKikole`/`ContactAdmin`, affichés même sans les droits requis) retirés ;
      liens texte Stats/KikolesStats + `LeaderboardModel.IsAdmin` (devenu mort) retirés de
      `Leaderboard/Index.cshtml`. `Admin/Club`/`Admin/PlayerEdit` laissés hors menu : déjà
      linkées depuis leur contexte d'usage (`Home/Contest`, alors dans le même cas, a
      depuis été supprimée — plus d'actualité ; `Leaderboard/Palmares`, alors dans le même
      cas aussi, a depuis été fusionnée dans `Leaderboard/Index` — la question de son accès
      depuis le menu ne se pose plus du tout).
      Bug découvert en vérifiant : `.site-nav-drawer.open` avait un `max-height: 320px`
      fixe (dimensionné pour l'ancienne liste courte) qui rognait silencieusement le bas
      du tiroir pour un compte admin (8 lignes désormais) — corrigé en `80vh` avec défilement
      interne. CSS versionné `?v=1` → `?v=4` dans `_Layout.cshtml` au passage (cache
      navigateur sur ce fichier, sans quoi un visiteur ayant déjà chargé le site ne
      verrait aucun des changements CSS de cette session avant un rechargement forcé).
      Vérifié en direct : icônes visibles/masquées selon le rôle (standard/admin), tous
      les liens fonctionnels, tiroir mobile sans coupure, barre desktop sans chevauchement
      (testé à 1000px).
- [x] **"Votre score final : X points." (une fois le joueur trouvé) faisait doublon avec
      le cadran de score en haut de page.** Ligne retirée (`Home/Index.cshtml`, clé resx
      `FinalScore` devenue inutile supprimée FR/EN) ; au passage, `CurrentScore` — une
      clé adjacente au même endroit, plus référencée nulle part dans cette vue depuis un
      moment — retirée aussi. Le cadran devient la seule source du score, mais avec une
      **surbrillance** quand le joueur du jour vient d'être trouvé (`.scoreboard.success`
      : liseré + halo `--pitch`, valeur en vert clair `#7fd99a` plutôt que l'or habituel)
      pour qu'il continue à jouer le rôle de confirmation visuelle que jouait l'ancienne
      ligne. Condition (`CurrentDay == 0 && !IsCreator && PlayerName renseigné`) : jour
      courant, trouvé, pas le créateur qui consulte son propre kikolé. Vérifié en direct :
      cadran doré normal sur un jour non résolu, halo vert + valeur verte dès qu'un
      kikolé est trouvé le jour même.
      **Question restée ouverte, pas traitée** : sur un jour passé raté consulté, le
      cadran doit-il afficher le score de la journée consultée ou toujours celui du jour
      présent ? Comportement actuel non vérifié/décidé — à reprendre séparément.
- [x] **Les indices peuvent être des images** — un indice d'époque vaut
      `https://i.imgur.com/YwR1hdd.png`, rendu tel quel en texte brut jusqu'ici. Nouvelle
      extension `ViewHelper.IsImageUrl` (URL absolue http/https se terminant par une
      extension d'image usuelle) qui bascule le rendu de `Model.Clue`/`Model.EasyClue` en
      `<img class="clue-image">` plutôt qu'en `<p>` texte, aux 4 endroits concernés
      (`Home/Index.cshtml`, états "trouvé"/"en cours"). Taille plafonnée, bordure papier
      (`.clue-image`), `.clue:has(.clue-image)` corrige l'alignement (`.clue` est
      `align-items:baseline`, pensé pour du texte). Couvert par 9 nouveaux tests
      (`ViewHelperTests.IsImageUrl_*`, extensions valides/query string, rejets texte/URL
      sans extension/non-http). Vérifié en direct : un indice de test basculé
      temporairement sur une vraie image en base locale (rendu correct, `.clue-image`
      bien présente), reverti à son texte d'origine juste après.
- [x] **Mots de passe des comptes de test changés** (`admin` → `admin12345`,
      `joueur1` → `NouveauMdp1234`, laissé tel quel après les sessions de vérification
      précédentes) — `admin123`/`test123` ne respectaient plus le minimum de 10 caractères
      exigé pour tout nouveau mot de passe, sans quoi impossibles à re-saisir depuis
      l'application elle-même. Mis à jour aux deux endroits : la base locale (mêmes
      hashs, format historique SHA256+sel pour rester fixture de démonstration du rehash
      automatique — cf. commentaire en tête de `kikole_mock.sql`) et le script
      `kikole_mock.sql` lui-même (littéraux `INSERT` + commentaire des identifiants), pour
      qu'un rejeu futur reste cohérent avec la base actuelle. `joueur2` inchangé
      (`test123`) : il partageait jusqu'ici le même hash que `joueur1` dans le script,
      désormais deux littéraux séparés puisque leurs mots de passe divergent.
- [x] **Page Contact : remplacer l'email par une vraie logique d'échange dans le site.**
      `Home/Contact.cshtml` demandait une adresse email alors que la page créait déjà une
      ligne en base liée à `UserId` — l'email était redondant avec le compte déjà
      connecté, et il n'existait aucun moyen de répondre. Remplacé par un vrai fil de
      discussion, une par utilisateur (jamais initiée par l'admin), avec accusé de
      lecture et pastille "à lire" dans le menu.
      **Schéma** : `discussions` redéfinie (`id, user_id` UNIQUE+FK, `creation_date` —
      plus d'`email`/`message`/`update_date`, ce dernier délibérément absent pour éviter
      un champ dénormalisé à resynchroniser à chaque insert : le tri "dernière activité"
      côté admin passe par un `MAX(creation_date)` joint sur la nouvelle table plutôt).
      Nouvelle table `discussion_messages` (`id, discussion_id, message, creation_date,
      is_from_admin, is_read`) — un bool explicite plutôt qu'un ID de message précédent
      pour indiquer l'auteur : pas de threading non-linéaire à modéliser ici, les
      messages d'un fil sont déjà strictement ordonnés par date. Base locale recréée
      directement (confirmé : pas de données à migrer), `kikole.sql`/`kikole_mock.sql`
      mis à jour en miroir.
      **Backend** : `IDiscussionRepository`/`DiscussionRepository` entièrement
      redessinés (get-or-create, marquage lu/non-lu, requête agrégée pour la liste admin
      avec jointure sur `users` pour le login, évite le N+1). Nouvelle couche
      `IDiscussionService`/`DiscussionService` — contrairement à avant (contrôleurs
      parlant directement au dépôt), justifiée ici par 3 points d'appel différents (page
      Contact, inbox admin, layout pour la pastille) qui doivent tous appliquer la même
      logique de marquage lu/non-lu.
      **L'admin n'a pas de fil personnel** : comme il n'initie jamais de discussion,
      l'icône "Contact" du menu lui est désormais masquée (`isLoggedIn && !isAdmin`,
      vérifié en direct desktop + tiroir mobile) et `HomeController.Contact` le
      redirige quand même vers `Admin/Discussions` en ceinture-bretelles. Son point
      d'entrée devient l'icône "Actions admin" existante, via un lien texte sur
      `Admin/Actions.cshtml` (même schéma que `CheckSubmittedPlayers`) vers deux
      nouvelles vues : `Admin/Discussions.cshtml` (liste, pastille "non lu" par
      utilisateur) → `Admin/Discussion.cshtml` (historique + réponse), même principe
      liste→détail que `Leaderboard/Index.cshtml`→`User.cshtml`. La table brute
      auparavant sur `Admin/Actions.cshtml` (non localisée, `@Html.Raw` sans échappement)
      a disparu avec.
      **Rendu du fil** partagé entre `Home/Contact.cshtml` et `Admin/Discussion.cshtml`
      via un nouveau partial (`Shared/Partial/DiscussionThread.cshtml`, le premier de ce
      projet à vivre sous `Views/Shared/` plutôt que sous le contrôleur qui l'utilise,
      puisque celui-ci est réellement partagé entre deux contrôleurs) : purement de la
      présentation (bulles "self"/"other"), chaque vue appelante fournit ses propres
      libellés ("Vous"/"Admin" côté utilisateur, "Vous"/le login côté admin) — le
      partial n'a pas sa propre resx, pas besoin de connaître le point de vue.
      **Pastille "à lire"** (`.site-nav-badge`/`-inline`, palette `--stamp` déjà "à
      traiter" dans ce projet) : calculée directement dans `_Layout.cshtml` via
      `@inject IDiscussionService` et un appel dans le bloc `@{ }` existant (comme
      `localizer` déjà injecté) plutôt qu'un `ViewComponent` — jamais utilisé dans ce
      projet, disproportionné pour un seul badge.
      `IDiscussionService.HasUnreadMessagesAsync(userId, isAdmin)` (un seul point d'entrée
      avec un bool) repérée après coup comme mal conçue : `userId` devient mort quand
      `isAdmin` est vrai, et rien n'empêchait un appelant de passer une combinaison
      incohérente (ex. un `userId` d'administrateur avec `isAdmin: false`). Éclatée en
      deux méthodes (`HasUnreadMessagesForUserAsync(userId)` /
      `HasUnreadMessagesForAdminAsync()`), miroir de ce qui existait déjà à ce niveau
      côté repository — la fusion en un seul point d'entrée n'apportait rien et
      recréait exactement le problème que le repository évitait déjà.
      Testé (`DiscussionServiceTests`, 9 nouveaux tests : get-or-create au premier
      message seulement, pas de création sur une simple lecture de fil vide, marquage
      lu/non-lu dans les deux sens, bascule utilisateur/admin de la pastille). Vérifié
      en direct de bout en bout : `joueur1` envoie un message (fil vide → échange
      affiché, pas de pastille), `admin` voit la pastille sur "Actions admin", ouvre le
      fil (pastille de la ligne disparaît), répond ; retour `joueur1` : pastille sur
      "Contact", fil à jour, pastille disparaît après lecture. Icône "Contact" confirmée
      absente du menu admin (desktop et tiroir mobile) tout du long.
- [ ] **Revoir complètement le footer.** Réduit à la mention de copyright après avoir
      retiré "Proposer un kikolé !"/"Contact" (redondants avec le nouveau menu, cf.
      ci-dessus) et "Vous aimez le vélo ?" (lien personnel, retiré à la demande) — il ne
      reste presque plus rien dedans, l'occasion de repenser ce qui doit vraiment y vivre
      plutôt que de le laisser à l'état de résidu.
- [ ] **Dépendances front datées, à moderniser.** Chargées en prod uniquement en CDN, sans
      fallback ni SRI : jQuery **1.12.4** (`_Layout.cshtml`, sortie en 2016, ligne 1.x
      abandonnée — actuelle : 3.7.x), jQuery UI **1.12.1** (même génération, utilisé pour
      l'autocomplétion club/pays/année et les datepickers — pas anodin à toucher),
      Bootstrap **3.4.1** en CDN mais **3.3.7** dans le fallback local
      (`wwwroot/lib/bootstrap/.bower.json`, versions divergentes entre les deux chemins) —
      Bootstrap 3 lui-même très daté (actuel : Bootstrap 5, classes CSS différentes,
      rupture probable sur les vues qui en dépendent). Repéré au passage, mort : `wwwroot
      /lib/jquery` (3.3.1), `jquery-validation`, `jquery-validation-unobtrusive` présents
      sur disque mais jamais chargés par `_Layout.cshtml` — résidus du scaffold ASP.NET MVC
      d'origine. Chantier à part entière (rupture potentielle jQuery UI et Bootstrap 3→5),
      pas juste un bump de version — à cadrer avant de s'y lancer.
- [x] **Fusionné `Statistics/KikolesStats` dans `Statistics/Stats`**, en 3ème bloc
      "collapsible" au même titre que "Répartition des joueurs par critère" et "Nombre
      d'utilisateurs actifs" — la page séparée reliée par un simple lien (`KikolesStatsLink`)
      disparaît. `KikolesStats.cshtml` supprimée, action `KikolesStats()` retirée du
      contrôleur (`GetKikolesStatisticsAsync`/route `kikoles-stats` inchangée, c'est
      l'endpoint JSON qui alimente le tableau, pas une vue). Contenu localisé au passage
      (il était en français en dur, alors que le reste de la page utilise déjà des resx) :
      nouvelles clés `SortLabel`/`DescendingLabel`/en-têtes de colonnes dans
      `Statistics/Stats.*.resx` ; les libellés du menu de tri (`PlayerSorts`) suivent le
      même principe que `LeaderSorts`/`DayLeaderSorts` déjà en place, nouveau
      `ViewHelper.GetLabel(PlayerSorts)`, testé (`ViewHelperTests`). `KikolesStatsLink`
      renommée `KikolesStatsTitle` (c'est un titre de section, plus un lien).
      **Charte graphique appliquée** à tout le contenu : page passée en `.kikole-board`,
      tableau/sélecteur de tri/case à cocher réutilisent les composants existants
      (`table-wrap`/`tabData`, `select.blank`, accent-color coché) ; nouveau style pour
      `.collapsible`/`.collapsiblecontent` (bouton pleine largeur, chevron +/− qui
      bascule sur `.active`, remplace le gris plat de `site.css` dans ce scope). Bug
      trouvé au passage en touchant ce code : la boucle de génération des lignes du
      tableau (`loadKikolesStats`, `site.js`) utilisait une variable `i` jamais déclarée
      (fuite de global, `NaN % 2` en permanence) — les lignes ne zébraient jamais
      correctement ; corrigé (`var i = 0;` avant la boucle), vérifié en direct
      (alternance beige/crème correcte).
- [x] **Rien ne s'affichait dans "Répartition des joueurs par critère"/"Nombre
      d'utilisateurs actifs" (`Statistics/Stats`), erreur navigateur "Data column(s) for
      axis #0 cannot be of type string".** Deux bugs distincts, malgré le même symptôme :
      - **Répartition des joueurs** : régression liée à la migration du sérialiseur JSON.
        `site.js` lisait `item.Key`/`item.Value` (PascalCase, casse historique de
        Newtonsoft.Json/l'ancien projet 2023) alors que `System.Text.Json` (utilisé par
        `Json()` depuis la refonte ASP.NET Core, casse par défaut camelCase) sérialise les
        `KeyValuePair<,>` en `key`/`value` — chaque ligne remontait `[undefined,
        undefined]`, d'où l'erreur de type côté Google Charts. Confirmé en tapant
        directement l'URL de l'endpoint JSON en admin (`{"key":"France","value":50.0}`,
        pas `Key`/`Value`). Corrigé : `item.key`/`item.value` dans `site.js` (seul endroit
        du fichier resté en PascalCase — `loadKikolesStats` utilisait déjà la bonne casse,
        preuve que la régression datait bien d'avant que cette fonction soit écrite/revue).
      - **Nombre d'utilisateurs actifs** : pas un bug de code mais une vraie absence de
        données sur la base locale actuelle — `GetActiveUsersAsync` filtre
        `creation_date < Yesterday`, et toutes les propositions en base (48, vérifié par
        requête directe) sont datées d'aujourd'hui/hier soir (activité de test récente),
        donc aucune ne passe le filtre. Ceci dit, le vrai défaut est ailleurs : un jeu de
        données vide fait planter `google.visualization` au lieu d'afficher un état vide
        propre — n'importe quelle fenêtre de dates trop récente (base fraîchement rejouée,
        par exemple) retombe dans le même crash. Corrigé à la racine : les 3 fonctions de
        construction de graphique (`site.js`) vérifient désormais `sourceDatas.length > 1`
        avant d'appeler `arrayToDataTable`/`.draw()`, et affichent "No data available yet."
        (nouvelle classe `.chart-empty`) sinon. Vérifié en direct : les deux sections
        affichent maintenant leurs vrais graphiques (camemberts pays/poste/décennie,
        histogramme clubs) quand la donnée existe, et un message propre sinon.
- [x] **Les routes statistiques vivaient dans `LeaderboardController`, jugé peu cohérent.**
      Extraites dans un `StatisticsController` dédié (`Stats`, `GetStatisticPlayersDistribution`,
      `GetStatisticActiveUsers`, `GetKikolesStatisticsAsync` (route `kikoles-stats`
      inchangée), `KikolesStats` — les 5 actions et `IStatisticService`, plus rien lié aux
      statistiques dans `LeaderboardController`). Vues et resx associées déplacées de
      `Views/Leaderboard/` vers `Views/Statistics/` (routage conventionnel par nom de
      contrôleur : `Stats()`/`KikolesStats()` n'ont pas de `[Route]` explicite). Références
      mises à jour : icône `_Layout.cshtml` (desktop + tiroir mobile), lien
      `KikolesStatsLink` sur `Stats.cshtml`, appels AJAX dans `site.js`
      (`GetStatisticPlayersDistribution`/`GetStatisticActiveUsers`). Au passage, une entrée
      `<Content Update>` fantôme dans `KikoleSite.csproj` référençant
      `Views\Leaderboard\Palmares.cshtml` (fichier supprimé lors de la fusion Palmarès→Podium,
      cf. plus haut, l'entrée csproj n'avait pas suivi) a été retirée. Vérifié en direct en
      admin : icône de nav pointant vers `/Statistics/Stats`, page Stats et ses deux blocs
      graphiques (requêtes AJAX en 200 sur les nouvelles URLs), lien vers `KikolesStats`
      fonctionnel avec son tableau peuplé.
- [x] **Namespaces à portée fichier : le reste.** 29 fichiers encore en syntaxe bloc
      (`namespace X { ... }`), presque tous des DTO/enum (`Models/Dtos`, `Models/Enums`)
      plus quelques ViewModels et `Translations.cs` — passés en `namespace X;`. Aucun autre
      changement (juste une dé-indentation d'un niveau).
- [x] **Palmarès → Podium côté back.** Vocabulaire déjà changé côté UI (cf. fusion
      Palmarès/Podium plus haut) ; le back ne suivait pas. Tout renommé, aucun cas où
      "palmarès" ne désignait pas un podium (vérifié en lisant `LeaderService.GetPalmaresAsync`
      avant renommage) : `Models/Palmares.cs` → `Models/Podiums.cs` (classe `Palmares` →
      `Podiums`, `MonthlyPalmares`/`GlobalPalmares` → `MonthlyPodiums`/`OverallPodium`, mêmes
      noms que `LeaderboardModel` pour rester cohérent), `ILeaderService`/`LeaderService
      .GetPalmaresAsync` → `GetPodiumsAsync`, `LeaderService.CreditPalmaresPosition` →
      `CreditPodiumPosition`, variable locale `palmares` → `podiums` dans
      `LeaderboardController.InitializeModelAsync`. Tests renommés en miroir
      (`LeaderServicePalmaresTests.cs` → `LeaderServicePodiumsTests.cs`).
- [x] **Perf : `LeaderboardController.InitializeModelAsync` enchaînait 3 `await` alors que
      deux des trois appels sont indépendants.** Le classement général dépend de
      `foundToday` (issu du dayboard du jour), mais le dayboard et les podiums ne dépendent
      de rien d'autre : les deux partent maintenant en parallèle (`Task` démarrées avant le
      premier `await`), le classement général reste séquentiel après le dayboard puisqu'il
      a besoin de son résultat. Option choisie plutôt que le chargement par bloc en AJAX
      (l'autre option proposée) : gain similaire pour un changement contenu à une seule
      méthode, sans toucher au rendu de la page ni à `site.js`.

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
- **`countries` recodé intégralement en codes FIFA à 3 lettres, `id` numérique stable.**
  Source unique : le premier tableau de
  [Liste des codes pays de la FIFA](https://fr.wikipedia.org/wiki/Liste_des_codes_pays_de_la_FIFA)
  (211 fédérations), pas l'ISO 3166 utilisé jusque-là. `Countries.cs` n'avait qu'un seul
  membre explicite (`AF = 1`, le reste implicite) — supprimer des membres au milieu du
  fichier aurait décalé silencieusement tous les suivants, donc les 211 membres restants
  ont **tous** une valeur explicite désormais. Les 206 pays qui correspondent 1-pour-1 à un
  pays ISO déjà présent gardent leur `id` (donc `clubs.country_id`/`players.country_id`
  existants restent valides sans migration) et changent juste de `code` (ex. `DE`→`GER`,
  `IT`→`ITA`). `GB` (id 235, "Royaume-Uni") ne correspond à aucun membre FIFA (pas de
  sélection unifiée) : recyclé en Angleterre plutôt que supprimé puis recréé, pour que les
  2 données mock qui le référençaient (Manchester United, Beckham) restent valides sans y
  toucher. Écosse/Galles/Irlande du Nord + Kosovo (fédération FIFA sans code ISO) ajoutés à
  la fin (ids 250-253). Les ~42 territoires ISO sans fédération FIFA (Åland, Antarctique,
  Monaco, Vatican, Guadeloupe, Kiribati...) sont **supprimés**, pas gardés avec un
  `continent_id` nul — décision explicite : `countries` est la liste des nationalités
  sportives, pas un sur-ensemble administratif. `continents`/`Continents.cs` ne bougent
  pas : déjà 6 entrées, 1-pour-1 avec les 6 confédérations FIFA, pas de renommage
  nécessaire sur les noms.
- **4 nations sportives disparues ajoutées comme pays à part entière** (Tchécoslovaquie,
  RDA, URSS, Yougoslavie — ids 254-257, confédération UEFA qu'elles avaient à l'époque).
  Tri effectué sur une liste de ~44 codes FIFA obsolètes : la plupart sont de simples
  renommages d'un pays qui existe toujours (Ceylan→Sri Lanka, Haute-Volta→Burkina Faso,
  RFA→Allemagne, Serbie-et-Monténégro→Serbie...), remappés vers l'entrée actuelle sans
  ligne dédiée. Une dizaine de cas limites (Inde britannique, CEI, Antilles néerlandaises,
  Yémen du Nord/Sud...) volontairement laissés de côté : aucune culture footballistique
  a priori dans ces cas, à ajouter au cas par cas si un joueur concerné se présente.

- **29 clés étrangères ajoutées, en fin de fichier après tous les `INSERT`** (l'ordre des
  `ALTER TABLE` n'a donc pas d'importance). Couvrent chaque colonne `_id` du schéma :
  toutes les tables de traduction vers leur table mère et vers `languages`, `clubs`/
  `players` vers `countries`, `players.alternative_country_id` vers `countries` (même
  table que `country_id`), `countries.continent_id` vers `continents`, et toutes les
  références à `users`. **Pas de `ON DELETE`/`ON UPDATE` explicite** (donc `RESTRICT` par
  défaut MySQL dans les deux sens) : décision délibérée, l'application ne supprime jamais
  rien dans son domaine (utilisateurs désactivés, jamais supprimés ; joueurs/clubs créés
  ou modifiés, jamais supprimés) — `RESTRICT` fait juste échouer bruyamment une suppression
  qui n'a de toute façon aucun chemin de code aujourd'hui, sans inventer une politique de
  cascade spéculative. Deux index manquants découverts et ajoutés au passage
  (`countries.continent_id`, `user_badges.badge_id` — la PK composite de cette dernière ne
  couvre pas `badge_id` en préfixe gauche), les FK MySQL l'exigeant sur la colonne
  référençante. Vérifié avant application : zéro ligne orpheline sur les 29 relations.
  `kikole_mock.sql` : `players`/`clubs`/`users` étant désormais des tables référencées,
  MySQL refuse un `TRUNCATE TABLE` dessus même sans ligne fille — le bloc de reset est
  maintenant encadré par `SET FOREIGN_KEY_CHECKS = 0;`/`= 1;`.

**Règles de jeu**
- **`players.alternative_country_id` (nullable) plutôt qu'une vraie liste de
  nationalités.** Deviner l'un ou l'autre valide la proposition Country, les deux
  s'affichent au reveal (`ProposalResponse.AlternativeCountryId`, combiné dans
  `HomeModel.CountryName` en `"RDA / Allemagne"`). Couvre le cas identifié (nation
  disparue → successeur, ex. Matthias Sammer RDA puis Allemagne), pas un système
  multi-nationalités générique — un joueur ayant représenté 3 entités successives
  (ex. ex-Yougoslavie → Serbie-et-Monténégro → Monténégro) n'est pas couvert,
  volontairement (cas jugé assez rare pour être accepté tel quel). Saisie admin
  uniquement (`AdminController.Index`), pas de nouvel écran d'édition : le seul point
  d'entrée pour la nationalité d'un joueur est déjà sa création.
- **`players.continent_id` supprimé : le continent n'est plus une donnée, c'est un
  calcul.** Gardé indépendant, il était devenu contre-productif — rien n'empêchait un
  continent incohérent avec le pays, et il ne pouvait pas représenter un joueur au
  parcours international double (ex. Algérie puis France, deux pays donc potentiellement
  deux continents valides). Décidé une fois `countries.continent_id` en place (bascule
  FIFA) et `alternative_country_id` ajouté : le continent se déduit désormais de
  `country_id` (+ `alternative_country_id`) à chaque calcul, jamais stocké.

  `ProposalResponse` et `ScoreCalculator` sont des classes pures, sans dépendance ni I/O
  (voir plus bas) : elles ne peuvent pas interroger la base elles-mêmes. Comme
  `HomeModel.SetPropertiesFromProposal` recevait déjà ses dictionnaires de référentiel en
  paramètre, la correspondance `country_id → continent_id`
  (`InternationalService.GetCountryContinentsAsync`, cache dédié car indépendant de la
  langue contrairement à `GetCountriesAsync`/`GetContinentsAsync`) suit le même principe :
  chargée une fois puis **passée en paramètre**.

  **Premier jet erroné, corrigé en repassant derrière** : `IInternationalService` avait
  été injecté directement dans `PlayerService`/`ProposalService`/`LeaderService` pour
  aller chercher cette correspondance elles-mêmes — un couplage service → service que le
  projet avait justement pris soin d'éviter jusqu'ici (voir la fusion
  `ScoreCalculator`/`ProposalChart` plus bas, motivée par le même principe : le seul
  couplage service → service du projet passait par un contournement en appel statique,
  précisément parce qu'un service ne doit pas dépendre d'un autre). Corrigé : les 3
  services ne connaissent plus `IInternationalService` du tout ; `GetPlayerSubmissionsAsync`,
  `GetProposalsAsync`, `ManageProposalResponseAsync`, `ComputeMissingLeadersAsync` et
  `GetDayboardAsync` reçoivent désormais `countryContinents` en paramètre, résolu par
  l'appelant — toujours un contrôleur, qui a déjà `IInternationalService` via
  `KikoleBaseController`. Seuls des contrôleurs consomment `IInternationalService`, comme
  avant ce chantier.

  Deviner le continent du pays **ou** du pays alternatif valide la proposition — même
  principe que `alternative_country_id` pour le pays — et les deux s'affichent au reveal
  quand ils diffèrent.

  **Révélation automatique une fois le pays trouvé.** Trouver le pays révèle aussitôt le
  continent, sans qu'une proposition Continent séparée soit nécessaire — `HomeModel`
  reçoit `countryContinents` en plus de `countries`/`continents` (même mécanisme de
  paramètre que le reste de ce chantier) et le calcule directement dans le cas
  `ProposalTypes.Country` réussi, avant même qu'un `ProposalTypes.Continent` n'ait été
  soumis. La vue n'a rien à changer : elle cachait déjà le champ de saisie dès que
  `ContinentName` est renseigné (`@if (string.IsNullOrWhiteSpace(Model.ContinentName))`,
  le même motif que pour le pays) — c'est l'état du modèle qui change plus tôt, pas la
  vue elle-même.

  **Un double (voire triple) appel à `GetCountryContinentsAsync` s'était glissé dans
  `HomeController`**, repéré après coup : `Index` (POST) le chargeait une fois pour les
  `ManageProposalResponseAsync`, puis appelait `SetAndGetViewModelAsync`, qui le
  rechargeait lui-même pour la boucle `SetPropertiesFromProposal`, et une troisième fois
  si le joueur venait d'être trouvé (bloc reveal complet). Sans conséquence mesurable —
  `InternationalService` le met en cache après le premier appel — mais contraire au
  principe « chargé une fois, passé en paramètre » de ce chantier. Corrigé :
  `SetAndGetViewModelAsync` reçoit désormais `countryContinents` en paramètre, calculé une
  seule fois par chacun de ses deux appelants (`Index` GET et POST).
- **`players.alternative_position_id` (nullable) plutôt qu'une vraie liste de postes,
  calqué à l'identique sur `alternative_country_id`.** Plainte récurrente depuis la v1 :
  un joueur n'a qu'un seul poste alors que beaucoup en occupent plausiblement deux (ex.
  Eden Hazard, milieu offensif/attaquant). Deviner l'un ou l'autre valide la proposition
  Position, les deux s'affichent au reveal (`ProposalResponse.AlternativePositionId`,
  combiné dans `HomeModel.Position` en `"Milieu de terrain / Attaquant"`, même code que
  `HomeController` pour le reveal complet). Les 4 catégories existantes restent
  inchangées — décision explicite de ne pas les affiner (pas de "latéral", "meneur de
  jeu"...). Plus simple que le pays : pas de table de traduction en base, les libellés
  viennent de `Positions.GetLabel()` (`ViewHelper.cs`), donc uniquement le second FK sur
  `players` et sa propagation (DTO, requête, contrôleur admin, vue, domaine, score,
  affichage). Champ admin `<select>` (pas d'autocomplétion JS, contrairement au pays) :
  `PlayerCreationModel.Positions` (liste avec option vide déjà construite par
  `SetPositionsOnModel`) réutilisée telle quelle pour les deux champs. Comme pour le
  pays, ni `PlayerRequest.IsValid` ni `AdminController` ne vérifient que l'alternative
  diffère du poste principal, et le badge `FourFourtwo` (`BadgeService`) continue de ne
  compter que `PositionId` — même précédent que le badge `AroundTheWorld`, qui ne compte
  que `CountryId` sans l'alternative.

  **Après coup** : les cas `Country` et `Position` du `switch` de `ProposalResponse`
  étant devenus rigoureusement identiques dans leur forme (deviner, comparer au principal
  ou à l'alternatif, exposer l'alternatif si trouvé), factorisés dans une méthode privée
  partagée `ResolveMainOrAlternative`. `Continent` n'y participe pas : sa valeur est
  dérivée du pays (pas stockée) et porte en plus une déduplication quand principal et
  alternatif tombent sur le même continent — assez différent pour que le forcer dans la
  même abstraction nuise plus qu'il n'aide.

  Deux libellés ajustés à la marge dans la foulée : "Nationalité :" devient "Nationalité
  sportive :" (`NationalityTitle`/`FinalNationality`, cohérent avec le vocabulaire déjà
  posé par `AboutCountryDetails`), et une mention "(le joueur peut avoir deux positions,
  une seule est requise)" apparaît sous le menu déroulant Position en jeu
  (`TipAboutPosition`, même motif que `TipAboutNationality`).
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
- **Clubs traduits par langue (`club_translations`), pas un `allowed_names` texte libre.**
  Même motif que `continent_translations`/`country_translations`/`player_clue_translations`
  (PK composite avec `language_id`) plutôt qu'un blob `;`-séparé sans notion de langue —
  découvert en creusant le formulaire admin existant (labels `MainNameEn`/`MainNameFr`,
  déjà pensé « titre de page Wikipédia EN/FR », jamais représenté correctement en base).
  `priority = 0` est le nom canonique par langue (obligatoire pour FR et EN), les priorités
  suivantes sont des alias de recherche pour cette langue uniquement — l'autocomplétion
  cherche et affiche dans la langue courante de l'utilisateur, pas dans un mélange des deux.
  `clubs.name` survit comme simple miroir du nom canonique FR, pour explorer la base sans
  jointure ; `clubs.allowed_names` a disparu, entièrement remplacée. Contrainte
  d'unicité sur `(name, country_id)`, pas `name` seul : deux clubs de pays différents
  peuvent légitimement partager un nom.
- **Proposition de club par ID, pas par correspondance de texte.** Même motif que
  pays/continent (champ visible + champ caché rempli par l'autocomplétion). Éliminait au
  passage un bug latent côté admin : `AddClubIfValid` cherchait un club par égalité de
  texte **exacte** contre le référentiel, et ignorait silencieusement un club mal
  orthographié sans le signaler.
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
