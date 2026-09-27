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
| Tests | **702** unitaires (mockés, rapides, `KikoleSiteUnitTests`) + **5** d'intégration (vraie base, `KikoleSiteIntegrationTests`, projet séparé) |
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
- [ ] **Outillage admin sur les comptes et les abus de droits `PowerUser`** — reprend et
      précise la "vue admin reportée" ci-dessus, plus des demandes explicites de
      l'utilisateur. Rien commencé, posé ici pour une session future :
      - **Limiter le nombre de clubs créés par un `PowerUser` (non admin)** — pas de
        plafond aujourd'hui (`AdminController`/`Admin/Club.cshtml`, ouvert à tout
        `PowerUser`). Décider d'un seuil (par jour ? au total ? glissant ?) et du
        comportement au dépassement (blocage silencieux, message, notification admin).
      - **Limiter le nombre de kikolés proposés par un `PowerUser` (non admin)** — même
        besoin, sur `Admin/Index.cshtml` (formulaire de proposition de joueur) cette
        fois. Probablement la même mécanique de plafond que pour les clubs, à
        factoriser plutôt qu'à dupliquer si l'implémentation converge.
      - **Vue admin : changer le palier d'un utilisateur** (standard ↔ `PowerUser`,
        dans les deux sens) — aujourd'hui aucune vue ne liste les utilisateurs ni ne
        permet de modifier `user_type_id` après l'inscription (le seul chemin vers
        `PowerUser` est manuel, en base). C'est aussi le mécanisme qui donnerait suite
        aux demandes envoyées via le nouveau bouton "Créer un kikolé" → Contact (cf.
        item précédent) : l'admin lit la demande, puis irait ici pour l'accorder.
      - **Vue admin : désactiver un compte** — colonne `is_disabled` déjà présente sur
        `users` (utilisée par `SubSqlValidUsers`, un utilisateur désactivé est déjà
        exclu des classements/statistiques), mais rien dans `AdminController` ne
        permet de la faire passer à vrai depuis l'interface — modification en base
        directe uniquement pour l'instant.
      - **Vue admin : forcer un mot de passe sur un compte** — cas d'usage : compte
        perdu (mot de passe oublié, réponse de récupération oubliée aussi ou jamais
        renseignée), mais l'utilisateur est joignable par un canal externe (email,
        Discord...) pour confirmer son identité autrement. Passerait par
        `UserManager<ApplicationUser>` (déjà utilisé partout ailleurs pour les
        opérations de mot de passe, ex. `AccountController`), pas de nouvelle
        primitive de sécurité à inventer.
      - Les trois vues admin ci-dessus (palier, désactivation, mot de passe forcé)
        cohabiteraient naturellement dans un même écran "gestion des utilisateurs"
        (liste + détail, même schéma que `Leaderboard/Index`→`User.cshtml` ou
        `Admin/Discussions`→`Discussion.cshtml`) plutôt que trois pages séparées — à
        confirmer le jour où ce chantier démarre.
- [x] **Système de parrainage.** Noté en aparté pendant le chantier badges (2026-09-08),
      implémenté dans la foulée. Spec de l'utilisateur suivie telle quelle :
      - **Schéma** : colonne `users.sponsor_user_id` (nullable, FK vers `users(id)`,
        indexée) — `kikole.sql` mis à jour. Fixée à l'inscription, ne change jamais
        ensuite (pas de champ dans `UpdateUserAsync`, volontairement).
      - **Inscription** (`Account/Index.cshtml`, nouveau champ "Login de votre parrain
        (optionnel) :") : résolu dans `AccountController.ResolveSponsorUserIdAsync`,
        appelé juste avant la création du compte. Les trois cas d'échec (login inconnu,
        parrain désactivé, auto-parrainage) sont **tous silencieux** — `sponsor_user_id`
        reste `null`, aucune erreur affichée, comme demandé. L'auto-parrainage est détecté
        via `ILookupNormalizer` (le même que celui qui garantit l'unicité des logins,
        `SanitizingLookupNormalizer` — Sanitize + majuscules), pas une comparaison de
        chaînes brute, pour rester cohérent avec la casse/les accents.
      - **Page "Mon compte"** : nouvelle carte "Parrainage", visible uniquement si
        `SponsorLogin != null || Godchildren.Count > 0` (`AccountModel.HasSponsorshipInfo`).
        Piège évité : `GetUserByIdAsync`/`GetUsersByIdsAsync` filtrent `is_disabled = 0` —
        inutilisables ici, sinon un parrain désactivé disparaîtrait silencieusement de la
        page de son filleul. Nouvelle méthode dédiée
        `IUserRepository.GetUserByIdIncludingDisabledAsync` pour le parrain, et
        `GetGodchildrenAsync` (tous les filleuls, désactivés inclus) pour la liste — les
        filleuls désactivés s'affichent barrés (`<s>`, même convention que les propositions
        incorrectes ailleurs sur le site), pas masqués.
      - `RenderIndex` (appelée par les 8 actions du contrôleur) passée en async pour
        peupler cette section — appelée systématiquement quand connecté, y compris après
        un simple changement de mot de passe : léger coût accepté pour ne pas dupliquer la
        logique d'affichage entre les 8 points d'entrée.
      - `ApplicationUser`/`DapperUserStore` (le store Identity maison) mis à jour pour
        porter `SponsorUserId` de bout en bout — même mécanique déjà en place pour
        `UserType`/`LanguageId`/etc.
      - Testé (`AccountControllerTests.cs`, +6 : les 3 cas silencieux de résolution du
        parrain dont un dédié à la casse/normalisation, + affichage section vide/peuplée).
      - Migration (`ALTER TABLE users ADD COLUMN sponsor_user_id ...`) appliquée sur la
        base locale via un test d'intégration jetable, sur confirmation explicite de
        l'utilisateur (`kikole.sql` ne sert qu'à la création initiale, pas rejoué en
        incrémental).
      - **Badges associés** (2026-09-08, demandé juste après) : `Don Corleone` (1er
        filleul parrainé) et `The Famous Five` (5e filleul parrainé) — ids 32/33,
        `kikole.sql` + `Badges` enum mis à jour. Décompte basé sur
        `IUserRepository.GetGodchildrenAsync`, qui inclut déjà les filleuls désactivés :
        désactiver un filleul plus tard ne fait donc jamais perdre un badge déjà acquis,
        comme demandé — que ce soit au moment de l'inscription
        (`AccountController.Create` appelle `IBadgeService.PrepareSponsorshipBadgesAsync`
        juste après la création réussie du compte, seulement si un parrain a été résolu)
        ou lors du recalcul global (`BadgeService.ResetBadgesAsync`, étendu pour boucler
        sur `IUserRepository.GetSponsorUserIdsAsync` en plus de la boucle jour par jour
        existante — les badges de parrainage ne sont pas liés à une victoire du jour,
        contrairement à la quasi-totalité du reste du fichier). Date du badge = date de
        création du filleul qui a fait franchir le seuil (1er ou 5e par ordre
        chronologique), pour un rendu cohérent quel que soit le déclencheur.
        Testé (`BadgeServiceTests.cs`, +6). `dotnet test` : 699 tests unitaires verts.
      - **Reste à faire, volontairement pas fait ici** : moyen de notifier l'utilisateur
        qu'il vient de gagner un badge de parrainage — contrairement aux badges gagnés en
        trouvant un kikolé, rien n'est affiché en direct au sponsor (qui n'est pas sur la
        page au moment où le filleul s'inscrit) ; il ne le découvre aujourd'hui qu'en
        consultant sa page badges. Réflexion à mener, pas bloquant pour le reste.
      - **À reconsidérer (2026-09-08, changement d'avis annoncé mais pas encore fait)** :
        l'utilisateur souhaite revenir sur la prise en compte des filleuls désactivés dans
        le décompte des deux badges ci-dessus — comportement actuel à retravailler la
        prochaine fois que ce chantier reprend (détail de ce qu'il faut changer à discuter
        à ce moment-là, pas encore précisé).
      - [x] **Rendu optionnel via config (2026-09-26)** — l'utilisateur a un doute sur la
        pertinence/validité globale du système (chantier de toute façon pas tout à fait
        terminé, cf. point ci-dessus) et préfère pouvoir le couper sans décider maintenant.
        Nouveau `Registration:SponsorshipEnabled` (bool, `false` par défaut dans
        `appsettings.json` — choix délibéré vu le doute exprimé, à remonter à `true`
        quand la décision sera prise). Désactivé : le champ "Login de votre parrain" ne
        s'affiche plus sur le formulaire d'inscription (`Account/Index.cshtml`), aucun
        `sponsor_user_id` n'est résolu à l'inscription même si le champ était rempli
        (`AccountController.ResolveSponsorUserIdAsync` court-circuité en tête de
        méthode), la section "Parrainage" de la page "Mon compte" ne s'affiche plus **même
        pour un compte qui a déjà un parrain ou des filleuls en base** (`RenderIndexAsync`
        saute entièrement le chargement, et `AccountModel.HasSponsorshipInfo` vérifie le
        flag en plus des données comme filet de sécurité), et le recalcul des badges de
        parrainage lors d'un recalcul global admin est sauté (`BadgeService.ResetBadgesAsync`,
        nouvelle dépendance `IOptions<RegistrationOptions>` injectée). Les données
        (`sponsor_user_id`, badges déjà obtenus) ne sont jamais effacées, seulement
        masquées/ignorées tant que le flag est à `false` — remettre à `true` restaure tout
        instantanément sans perte. **Portée volontairement limitée** : les badges déjà
        obtenus (`DonCorleone`/`TheFamousFive`) restent visibles sur la page badges d'un
        utilisateur qui les a — décision de ne pas les masquer rétroactivement, un badge
        gagné reste gagné, seul le *mécanisme* d'acquisition est coupé. Testé (+3 tests :
        `AccountControllerTests.cs` ×2, `BadgeServiceTests.cs` ×1, avec un nouvel helper
        `BuildController`/`BuildService` dans chaque fichier pour construire une instance
        avec une config différente de celle par défaut — `RegistrationOptions` étant un
        `record` à propriétés `init`, la config ne peut pas être mutée après coup sur
        l'instance déjà câblée dans le contrôleur/service de test). `dotnet build` propre,
        `dotnet test` : 702 tests unitaires verts.
- [x] **Inscription par email, abandon de la question secrète (2026-09-27)** — demandé
      explicitement par l'utilisateur ("on va faire l'inscription par adresse email"),
      arbitrages discutés puis "tu peux démarrer". Colonnes `password_reset_question`/
      `password_reset_answer` supprimées de `users`, remplacées par `email_encrypted`
      (AES-GCM, `KikoleSite/Identity/EmailProtector.cs`), `email_hash` (HMAC-SHA256,
      `UNIQUE KEY`, sert à la connexion par email et à l'unicité sans déchiffrer) et
      `email_confirmed`. Clé dédiée `EmailEncryptionKey` (user-secret local, séparée
      d'`EncryptionKey`) — portable contrairement aux clés Data Protection par défaut
      (liées au profil Windows), deux sous-clés dérivées (chiffrement/HMAC) comme pour
      `LegacyCompatiblePasswordHasher`.
      - **Confirmation par lien** (`Identity.SignIn.RequireConfirmedEmail = true`,
        `Account/ConfirmEmail`) : tant que non confirmé, la connexion échoue
        (`SignInResult.IsNotAllowed`). **Mot de passe oublié** : un seul champ email
        (`Account/RequestPasswordReset`), réponse toujours identique que l'adresse soit
        connue ou non (comme demandé), token Identity standard
        (`GeneratePasswordResetTokenAsync`), page dédiée `Account/ResetPassword.cshtml`.
        Un administrateur reste exclu de cette voie (`IsRecoveryForbidden`, hérité de
        l'ancien système), invisible de l'extérieur puisque la réponse ne change jamais.
        **Changement d'email connecté** (`Account/ChangeEmail`) : mot de passe + nouvelle
        adresse + confirmation, lien envoyé à la nouvelle adresse
        (`GenerateChangeEmailTokenAsync`/`ChangeEmailAsync`, le token auto-encode la
        cible) — l'ancienne adresse reste seule active tant que le lien n'est pas suivi,
        pas de risque de blocage entre-temps (préoccupation soulevée par l'utilisateur).
      - **Connexion par login OU email** (standard, demandé explicitement) : `LogIn`
        essaie `FindByNameAsync` puis `FindByEmailAsync`, libellé du formulaire mis à
        jour en conséquence ("Login / Email :", nouvelle clé `LoginOrEmail` dédiée —
        distincte de `SetLogin`, réutilisée telle quelle pour le champ identifiant de
        l'inscription, qui lui n'accepte pas d'email).
      - **Liste noire de domaines jetables** en config (`Registration:BlockedEmailDomains`,
        comparaison insensible à la casse), pas d'unicité vérifiée côté Identity
        (`RequireUniqueEmail` non activé) : le contrôle se fait à la main via
        `IUserRepository.GetUserByEmailHashIncludingDisabledAsync`, cohérent avec le
        contrôle d'unicité déjà manuel sur le login — unicité incluant les comptes
        désactivés, comme demandé.
      - **Contournement local** (`Email:SendingEnabled = false`, appsettings.Development)
        : à l'inscription, le compte est auto-confirmé immédiatement (pas d'email
        envoyé) ; pour la réinitialisation et le changement d'adresse, le lien est
        seulement journalisé (`ILogger`), pas auto-appliqué. MailKit ajouté
        (`SmtpEmailSender`) pour l'envoi réel — SMTP non renseigné à ce jour, prévu par
        l'hébergement au moment du déploiement (`Email:SmtpHost` vide dans
        `appsettings.json`).
      - Testé : `EmailProtectorTests.cs` (nouveau, chiffrement non déterministe/hash
        déterministe/normalisation), `AccountControllerTests.cs` largement réécrit
        (connexion par email, non-confirmation, mot de passe oublié, changement
        d'email, blocage de domaine, email déjà utilisé, envoi réel vs. auto-
        confirmation). `dotnet test` : 739 tests unitaires verts.
      - `kikole_mock.sql` : emails de démonstration (`<login>@kikole.test`)
        pré-chiffrés avec une clé de dev fixe et documentée
        (`EmailEncryptionKey = "KikoleDevEmailKey2026"`, même principe que
        `EncryptionKey = "KikoleDevSalt2026"` pour les mots de passe) — ne fonctionnent
        que si ce secret est bien celui configuré en local.
      - **Migration de la vraie base locale (`kikole`) appliquée** (2026-09-27, décision
        explicite de l'utilisateur : "valeurs bouchon générées") : `ALTER TABLE` (colonnes
        ajoutées nullables, backfillées, puis repassées `NOT NULL` + `UNIQUE KEY` sur
        `email_hash`, `password_reset_question`/`password_reset_answer` supprimées),
        emails bouchon `<login>@kikole.test` générés pour les 16 comptes existants avec le
        vrai algorithme (petit projet jetable référençant `KikoleSite.Identity
        .EmailProtector`, supprimé après usage) et la clé de dev déjà en place.
        **Incident en cours de route** : une première tentative de vérification sur
        `kikole_pod` (base jetable) a en réalité rejoué `kikole_mock.sql` sur la vraie base
        `kikole` — les deux scripts commencent par `USE kikole;`, qui écrase le nom de
        base passé en ligne de commande à `mysql`. Toutes les données applicatives réelles
        (joueurs, propositions, classements, discussions, 5 comptes de test) ont été
        perdues ; sans conséquence ("la base locale n'a aucune importance", reconstruite
        proprement ensuite depuis `kikole.sql` + `kikole_mock.sql`). **Point de méthode
        pour la prochaine fois** : pour cibler une base autre que `kikole` avec ces deux
        scripts, il faut une copie retouchée (`CREATE DATABASE`/`USE` réécrits), jamais un
        simple nom de base passé à `mysql -D`.
      - **Vérification bout-en-bout faite** (sur `kikole_pod`, base jetable retouchée comme
        ci-dessus, supprimée après coup) : connexion par identifiant, connexion par email,
        inscription + auto-confirmation locale + connexion automatique, mot de passe
        oublié (message générique + lien journalisé) + réinitialisation, changement
        d'email (ancienne adresse active entre-temps, lien journalisé, confirmation).
        Tout fonctionnel.
      - **Piège d'environnement rencontré ensuite, spécifique à cette machine** : l'appli
        lancée depuis Visual Studio 2026 ne trouvait pas `EmailEncryptionKey`
        ("La cle... est absente de la configuration"), alors que `dotnet run` en ligne de
        commande fonctionnait avec le même secret. Deux contournements dans `Program.cs`
        tentés puis **retirés** (`AddUserSecrets<Program>()` explicite, puis lecture
        directe du fichier `secrets.json` par son chemin) : aucun n'a résolu le problème,
        ce qui a fini par pointer vers la vraie cause — l'agent qui a développé ce
        chantier tourne dans un environnement séparé de la session Windows où Visual
        Studio s'exécute (fichiers projet partagés via `D:\`, mais pas le profil
        utilisateur `%APPDATA%` où vivent les user-secrets) : le secret posé par l'agent
        était donc invisible pour Visual Studio. Résolu en posant `EmailEncryptionKey`
        directement depuis Visual Studio (clic droit sur `KikoleSite` → "Gérer les secrets
        utilisateur"). **À garder en tête** : tout secret nécessaire en local doit être
        posé par l'utilisateur lui-même (ou vérifié avec lui), pas seulement par l'agent.
      - Mentions légales (RGPD/CNIL) liées à la collecte d'une adresse email —
        explicitement reportées par l'utilisateur ("on assumera les conséquences
        légales plus tard, quand on fera le footer"). Seul point encore ouvert.

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
- [x] **Remplir la base des clubs** — sourcée pays par pays. Méthode ayant
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
      - [x] **Chantier (autonome) : Espagne, Allemagne, Angleterre, Pays-Bas,
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
      - [x] **Échelons inférieurs des 8 pays — 82 clubs curés** (2026-09-09). Extraction
        identique (`club.dat`, offsets nation/compétition déjà reverse-engineered),
        340 candidats au total (Espagne Segunda B 80, Allemagne Regionalliga 36,
        Angleterre Division Three 24, Belgique Promotion régionalisée 80, Portugal 3
        zones régionales 60, Turquie 2.Lig régionalisé 40, Écosse Division Two/Three 20).
        Décision club par club (pas d'inclusion en bloc), sur deux critères cumulés :
        **pertinence historique** (a déjà évolué en D1/D2 à un moment de son histoire —
        titre national, coupe majeure, longue présence en 2ème division) **ou club
        actuellement en D1/D2**. Équipes réserve/B exclues d'office avant même ce
        jugement (`... B`, `... Amateure`, `... U21` — 26 sur les 340, mécanique, aucun
        jugement de pertinence nécessaire puisque le club "premier" correspondant est
        déjà au catalogue). Résultat : **82 retenus sur 314 candidats premier-effectif**
        (Espagne 19/63, Allemagne 22/32, Angleterre 7/24, Belgique 4/80, Portugal 12/55,
        Turquie 10/40, Écosse 8/20) — la Belgique en particulier confirme être restée un
        vrai niveau amateur régional en 2001-02 (seuls trois clubs de ce palier bas ont
        depuis perçé en D1 : Zulte Waregem, tout juste fusionné cette année-là, Oostende,
        Kortrijk). Exemples de l'autre sens (inclus malgré un nom peu connu aujourd'hui) :
        Fortuna Düsseldorf/Kickers Offenbach/SC Rot-Weiss Essen (grands noms allemands
        déchus, déjà repérés au moment du cadrage), Real Unión de Irún (double vainqueur
        de la Copa del Rey 1913/1918, membre fondateur de la Liga 1929), Queen's Park
        (club fondateur du football écossais, 1867).
        - Ids **491-572** dans `clubs`/`club_translations` (EN=FR, même convention que
          le lot précédent). **Vérifié avant écriture finale** : chargement isolé dans
          une base de test jetable (`kikole_test`, détruite après coup) — 0 erreur, 572
          clubs au total (490 + 82), répartition par pays exacte, traductions EN+FR
          complètes pour les 82 nouveaux (vérifié par `LEFT JOIN ... IS NULL`).
        - **Angleterre — nuance de périmètre** : la compétition 10 (`Division Three`
          officielle 2001-02, aujourd'hui League Two) est le 4ème palier réel, hors du
          "3ème échelon" explicitement demandé pour ce pays (déjà couvert par la
          compétition 9 dans le lot précédent) — inclus ici seulement à titre de
          pertinence historique individuelle (Hull City, Luton Town, Swansea City,
          Oxford United : passages Premier League/Championship ; Carlisle United,
          Leyton Orient : une saison en First Division historique ; Plymouth Argyle :
          promotion récente en Championship), pas comme un échelon entier à couvrir.
        - **Point de méthode, à garder en tête si ce chantier reprend** : cette passe
          repose sur des connaissances factuelles (parcours de club, années de titre,
          division actuelle) reconstituées de mémoire plutôt que sourcées club par club
          comme Wikipedia l'avait été pour la France — fiabilité nécessairement moindre
          sur les clubs les moins connus (~80 pays × divisions confondues). Pas de
          vérification croisée effectuée club par club faute de temps ; à corriger au cas
          par cas si une erreur factuelle est repérée en jouant.
        - **Toujours pas couvert (laissé tel quel, décision inchangée)** : les paliers
          encore plus bas repérés lors du cadrage initial (Espagne Tercera, Allemagne
          Oberliga, etc.) — hors périmètre de cette passe, qui s'arrêtait au palier
          explicitement listé pour chaque pays.
      - [x] **Nouveaux pays d'Europe (terminé, 2026-09-09)** — extraction de `club.dat`
        sur les 182 nations qu'il contient (10 580 clubs), filtrage aux pays d'Europe
        pas encore traités (France/Italie/Grèce/Espagne/Allemagne/Angleterre/Pays-Bas/
        Belgique/Portugal/Turquie/Écosse exclus, déjà faits). L'utilisateur a ensuite
        fixé, pays par pays, la ligne la plus profonde à importer (D1 seul, ou D1+D2) ;
        17 pays ignorés (case vide dans son tableau) : Albanie, Andorre, Arménie,
        Azerbaïdjan, Estonie, Îles Féroé, Géorgie, Kazakhstan, Lettonie, Liechtenstein,
        Lituanie, Luxembourg, Macédoine, Malte, Moldavie, Saint-Marin, Slovénie — 24 pays
        à importer. Pour les pays marqués "D2" (Autriche, Croatie, Rép. tchèque, Danemark,
        Norvège, Pologne, Russie, Serbie-et-Monténégro, Suède, Suisse), consigne
        supplémentaire : ajouter aussi, au cas par cas, les clubs de palier inférieur
        "à passé ou avenir plus glorieux" (ex. un club de Regionalliga autrichienne
        aujourd'hui en Bundesliga). Import fait par lots de 3 pays, avec pause de
        vérification (chargement isolé dans `kikole_test`, détruite après coup) et commit
        après chaque lot validé.
        - [x] **Lot 1/8 : Autriche, Biélorussie, Bosnie-Herzégovine** — ids 573-630
          dans `clubs`/`club_translations` (EN=FR). Autriche : D1 (10, comp245) + D2
          (10, comp246) complets, + 7 clubs de Regionalliga/D3 (comp248/254) au passé ou
          avenir glorieux (Altach, First Vienna FC 1894, FC Hartberg, Wiener Neustadt,
          WSG Wattens, Kapfenberger SV, FCN St. Pölten — tous montés en Bundesliga à un
          moment depuis 2001, sauf First Vienna, club le plus ancien de Vienne). Biélorussie :
          D1 complet (15, comp58), pas de scan inférieur (pays marqué "D1" seul).
          Bosnie-Herzégovine : D1 complet (16, comp58) — **point notable** : cette D1
          2001-02 ne couvre que la fédération croato-bosniaque, seule présente dans
          `club.dat` pour cette saison (la Republika Srpska, dont le très ancien
          Borac Banja Luka, n'a fusionné dans un championnat national unifié qu'en 2002,
          absente ici) ; pas de scan inférieur ajouté puisque le pays est marqué "D1" seul,
          malgré la présence de Borac Banja Luka en palier "254" du fichier — décision
          prise à la lettre de la consigne, pas d'exception. Vérifié (`kikole_test`,
          détruite après coup) : 0 erreur, 630 clubs au total, répartition exacte
          (Autriche 27, Biélorussie 15, Bosnie-Herzégovine 16), traductions EN+FR
          complètes.
        - [x] **Lot 2/8 : Bulgarie, Croatie, Chypre** — ids 631-699. Bulgarie : D1
          complet (14, comp58), pas de scan inférieur (marqué "D1" seul). Croatie : D1
          (16, comp138 — Hajduk Split et Dinamo Zagreb confirmés dedans) + D2 (20,
          comp140) complets, + 5 clubs de palier régional inférieur (comp147/149/150/153)
          au passé ou avenir glorieux : NK Split, NK Istra (T), NK Lokomotiva, NK
          Karlovac, HNK Segesta — **confiance moyenne seulement** sur ces 5 (lien de
          filiation avec les clubs actuels pas toujours certain, ex. NK Istra (T) vs
          l'actuel NK Istra 1961 formé par fusion en 2006 ; à corriger si erreur repérée).
          Chypre : D1 complet (14, comp58), pas de scan inférieur. Vérifié
          (`kikole_test`) : 0 erreur, 699 clubs au total, répartition exacte (Bulgarie
          14, Croatie 41, Chypre 14), traductions EN+FR complètes, aucun id dupliqué.
        - [x] **Lot 3/8 : République tchèque, Danemark, Finlande** — ids 700-771.
          République tchèque : D1 (16, comp206) + D2 (15, comp207 — `SK Sigma Olomouc B`
          exclue comme réserve) complets, aucun club de palier régional inférieur retenu
          (rien de reconnaissable dans les 8 groupes régionaux scannés). Danemark : D1
          (12, comp4) + D2 (16, comp5) complets, + `FC Nordjylland` (comp101, avenir
          glorieux : plusieurs titres de Superligaen depuis 2010, sous le nom de scène
          « Farum Boldklub » de l'époque déjà présent en D2 sous son propre nom séparément
          — pas de doublon). **Point de méthode notable** : Finlande a d'abord semblé
          ambiguë (`comp38-45` avaient la même taille que `comp114-117`), résolu en
          repérant HJK Helsinki (club le plus titré du pays) dans `comp114` — c'est la
          vraie Veikkausliiga (D1, 12 clubs), `comp38-45`/`117` ne sont que des groupes
          régionaux inférieurs. Finlande importée en D1 seul (12, comp114), sans doute
          possible une fois HJK repéré. Vérifié (`kikole_test`) : 0 erreur, 771 clubs au
          total, répartition exacte (Tchéquie 31, Danemark 29, Finlande 12), traductions
          EN+FR complètes, aucun id dupliqué.
        - [x] **Lot 4/8 : Hongrie, Islande, Irlande** — ids 772-810, les trois en D1
          seul (comp58 pour Hongrie/Islande, comp119 pour l'Irlande — repéré via
          Shamrock Rovers/Bohemian FC/Cork City, clubs historiques de la League of
          Ireland), aucun scan de palier inférieur (aucun des trois marqué "D2").
          Vérifié (`kikole_test`) : 0 erreur, 810 clubs au total, répartition exacte
          (Hongrie 16, Islande 11, Irlande 12), traductions EN+FR complètes, aucun id
          dupliqué.
        - [x] **Lot 5/8 : Israël, Irlande du Nord, Norvège** — ids 811-870. Israël : D1
          seul (17, comp58). Irlande du Nord : D1 seul (10, comp154 — Linfield/Glentoran/
          Cliftonville confirmés dedans, grands noms historiques de l'Irish League).
          Norvège : D1 (14, comp59 — Rosenborg BK et FK Bodø/Glimt confirmés dedans) + D2
          (16, comp60) complets, + 3 clubs de palier régional inférieur (comp61/63)
          montés depuis en Eliteserien ou historiquement dominants : Fredrikstad FK
          (champion à répétition avant l'ère Tippeligaen), Ullensaker-Kisa et Ranheim IL
          (montées ponctuelles en Eliteserien dans les années 2010). Vérifié
          (`kikole_test`) : 0 erreur, 870 clubs au total, répartition exacte (Israël 17,
          Irlande du Nord 10, Norvège 33), traductions EN+FR complètes, aucun id dupliqué.
        - [x] **Lot 6/8 : Pologne, Roumanie, Russie** — ids 871-968. Pologne : D1 (16,
          comp133) + D2 (20, comp134) complets, + 8 clubs de palier inférieur (comp135,
          60 candidats) au passé ou avenir glorieux : Cracovia Krakow (club historique,
          plusieurs titres avant-guerre), Korona Kielce, Lechia Gdansk, Miedz Legnica,
          Warta Poznan, Chrobry Glogow (tous montés en Ekstraklasa depuis), Polonia Bytom
          (champion 1962-63), Rakow Czestochowa (**champion de Pologne 2023**, le plus
          notable des huit). Roumanie : D1 seul (16, comp58 — Steaua et Dinamo Bucarest
          confirmés dedans), pas de scan inférieur. Russie : D1 (16, comp176) + D2 (18,
          comp177) complets, + 4 clubs de palier régional inférieur (comp180/181/183,
          ~110 candidats répartis en 6 zones) au passé ou avenir glorieux : Terek Grozny
          (actuel Akhmat Grozny, vainqueur de Coupe de Russie 2004), Luch Vladivostok,
          Uralmash Yekaterinburg (probable lignée de l'actuel FC Ural, ancien nom du club
          avant son renommage de 2009 — confiance moyenne sur la filiation), Gazovik
          Orenburg (lignée du FC Orenburg actuel). **Zones non exhaustivement scannées**
          (comp184 russe, 122 clubs amateurs) faute de temps — aucun grand nom
          manifestement manqué à la relecture, mais pas une garantie absolue. Vérifié
          (`kikole_test`) : 0 erreur, 968 clubs au total, répartition exacte (Pologne 44,
          Roumanie 16, Russie 38), traductions EN+FR complètes, aucun id dupliqué.
        - [x] **Lot 7/8 : Slovaquie, Suède, Serbie-et-Monténégro (Yougoslavie)** — ids
          969-1100. Slovaquie : D1 seul (10, comp58). Suède : D1 (14, comp38) + D2 (16,
          comp39) complets, + 4 clubs de palier inférieur (Degerfors IF, Falkenberg FF,
          IK Sirius FK, Varbergs BoIS FC — tous montés en Allsvenskan depuis 2015,
          identifiés par recherche ciblée plutôt qu'en relisant les ~700 clubs suédois un
          par un). Serbie-et-Monténégro : D1 (18, comp208 — **Étoile Rouge et Partizan
          Belgrade confirmés dedans**, cf. vérification de la semaine dernière) + D2
          régionalisée en 4 zones (66, comp114/115/116/117 — Voïvodine, Belgrade/Sud,
          Ouest, Monténégro) complets, + 4 clubs de palier inférieur (Metalac Gornji
          Milanovac, Jagodina, Macva Sabac, Vozdovac Belgrade). **Zone Kosovo (comp254,
          4 clubs) volontairement exclue** : c'est aujourd'hui une fédération FIFA/UEFA
          distincte, pas rattachée à la Serbie dans le tableau de l'utilisateur. Vérifié
          (`kikole_test`) : 0 erreur, 1100 clubs au total, répartition exacte (Slovaquie
          10, Suède 34, Serbie 88), traductions EN+FR complètes, aucun id dupliqué.
        - [x] **Lot 8/8 (dernier) : Suisse, Ukraine, Galles** — ids 1101-1156. Suisse :
          D1 (12, comp250) + D2 (12, comp251) complets, aucun club de palier inférieur
          retenu (comp252, 45 candidats scannés, rien de reconnaissable). Ukraine : D1
          seul (14, comp58 — Dinamo Kiev et Shakhtar Donetsk confirmés dedans). Galles :
          D1 seul (18, comp186 — mêmes clubs fondateurs de la Welsh Premier League déjà
          repérés lors du cadrage initial). Vérifié (`kikole_test`) : 0 erreur, **1156
          clubs au total** (490 + 666 sur les 8 lots), répartition exacte (Suisse 24,
          Ukraine 14, Galles 18), traductions EN+FR complètes, aucun id dupliqué.
        - **Chantier "nouveaux pays d'Europe" terminé** (24/24 pays importés). Reste
          identique au lot précédent (82 clubs des échelons inférieurs des 8 premiers
          pays) : appliquer la migration sur la base locale via un test d'intégration
          jetable, pas fait sans confirmation explicite de l'utilisateur au préalable.
      - [x] **Grandes nations Afrique/Asie/Océanie (terminé, 2026-09-10)** — même
        méthode (`club.dat`), mais **D1 seule à chaque fois**, pas de D2/palier
        inférieur. Liste arrêtée avec l'utilisateur sur trois critères (gros vivier de
        joueurs connus, destination courante de fin de carrière, sélection nationale
        solide) — proposition initiale de l'utilisateur, quatre ajouts suggérés et
        acceptés (Côte d'Ivoire, Ghana, Sénégal — plus incontournables que Cameroun/
        Afrique du Sud sur ces mêmes critères ; Irak), Océanie finalement laissée de
        côté (seule la Nouvelle-Zélande aurait tenu la route, l'utilisateur a tranché
        de ne pas l'inclure) : **19 pays au total**, aucun pays d'Océanie donc au final.
        Australie, Corée du Sud, Japon, Chine, Iran, Inde, Qatar, Arabie Saoudite,
        Égypte, Maroc, Tunisie, Algérie, Nigeria, Cameroun, Afrique du Sud, Côte
        d'Ivoire, Ghana, Sénégal, Irak. Import par lots de 3-4 pays, vérification
        (`kikole_test`) + commit après chaque lot, comme pour le chantier Europe.
        **Consigne supplémentaire** : quand un alias de nom actuel est identifiable avec
        certitude (club renommé/relocalisé depuis 2001-02), l'ajouter en
        `club_translations` priorité 1 en plus du nom d'époque — exhaustif uniquement
        pour Arabie Saoudite et Chine (demandé explicitement), best-effort ailleurs (fait
        seulement quand la filiation est sûre sans recherche approfondie, pour limiter le
        risque d'erreur factuelle sur des clubs moins connus).
        - [x] **Lot 1/5 : Australie, Corée du Sud, Japon** — ids 1157-1196, D1 seule pour
          les trois (National Soccer League `comp151` pour l'Australie — repérée via
          Football Kingz/Marconi Stallions/Adelaide City, à distinguer des ligues
          régionales par État qui composent le reste du fichier ; K-League `comp229` pour
          la Corée ; J1 League `comp69` pour le Japon). 10 alias de nom actuel ajoutés
          (best-effort, confiance haute) : Corée — Anyang LG Cheetahs→FC Seoul, Bucheon
          SK→Jeju United, Pusan I.cons→Busan IPark, Songnam Ilhwa Chunma→Seongnam FC,
          Taejon Citizen→Daejeon Hana Citizen, Ulsan Hyundai Horang-I→Ulsan HD ; Japon —
          JEF United Ichihara→JEF United Chiba, Nagoya Grampus Eight→Nagoya Grampus,
          Tokyo Verdy 1969→Tokyo Verdy, Consadole Sapporo→Hokkaido Consadole Sapporo.
          **Panne WAMP rencontrée en démarrant ce chantier** : le service Windows
          `wampmysqld64` était arrêté (`net start` refusé, accès admin requis) — contourné
          en lançant `mysqld.exe --standalone` directement avec le `my.ini` existant,
          sans passer par le Service Control Manager ; fonctionne, mais le service Windows
          reste arrêté (à redémarrer proprement via WampServer au prochain lancement
          normal). Vérifié (`kikole_test`) : 0 erreur, 1196 clubs au total, répartition
          exacte (Australie 14, Corée du Sud 10, Japon 16), traductions EN+FR complètes,
          aucun id dupliqué.
        - [x] **Lot 2/5 : Chine, Iran, Inde, Qatar** — ids 1197-1245, D1 seule
          (`comp194` Jia-A League, `comp58` Azadegan League, `comp58` National Football
          League, `comp58` Qatar Stars League). **Alias exhaustif pour la Chine** (14/14
          clubs passés en revue un par un, filiation actuelle documentée quand elle est
          sûre) : Beijing Guo'an→Beijing Guoan (variante translittération), Shandong
          Luneng→Shandong Taishan, Tianjin Teda→Tianjin Jinmen Tiger, Shenzhen Pingan→
          Shenzhen Peng City, Chongqing Lifan→Chongqing Liangjiang Athletic (club dissous
          depuis 2022, alias gardé pour la traçabilité historique) ; 9 des 14 sans alias
          ajouté faute de filiation certaine (renommages/relocalisations trop enchevêtrés
          pour trancher sans recherche dédiée — cas notable : "Chinese Army", l'équipe de
          l'armée chinoise (Bayi), dissoute en 2003, aucun successeur). 3 alias
          best-effort ajoutés côté Iran/Inde (hors obligation, faits par cohérence) :
          Pirouzi Tehran→Persepolis F.C. (le club a changé de nom plusieurs fois depuis
          les années 1980, "Pirouzi" est l'ancien nom), Teraktor Sazi Tabriz→Tractor
          F.C., Mohun Bagan Athletic Club→Mohun Bagan Super Giant. Vérifié
          (`kikole_test`) : 0 erreur, 1245 clubs au total, répartition exacte (Chine 14,
          Iran 14, Inde 12, Qatar 9), apostrophe de "Beijing Guo'an" correctement échappée
          et restituée, traductions EN+FR complètes, aucun id dupliqué.
        - [x] **Lot 3/5 : Arabie Saoudite, Égypte, Maroc** — ids 1246-1294, D1 seule
          (`comp58` pour les trois). **Alias exhaustif pour l'Arabie Saoudite** : 12/12
          clubs passés en revue individuellement, **aucun changement de nom identifié**
          (Al Hilal, Al Nassr, Al Ittihad, Al Ahli, Al Shabab, Al Ittifaq, Al Wehda sont
          tous des noms encore utilisés tels quels aujourd'hui) — contrairement à la
          Chine, les clubs saoudiens ont des noms historiquement très stables, "exhaustif"
          donne donc légitimement zéro alias plutôt qu'une recherche interrompue. Égypte :
          rien de confiant identifié non plus (Al-Ahly/Zamalek/Al Masry inchangés).
          **Maroc — particularité de source** : `club.dat` ne modélise pour ce pays qu'un
          seul groupe générique (`comp254`, pas de découpage D1/D2 comme pour la plupart
          des autres pays) — les 20 clubs de ce groupe (dont Raja et Wydad Casablanca,
          les deux clubs dominants) ont été pris comme l'équivalent de la D1 faute de
          mieux, décision similaire à celle prise pour la Côte d'Ivoire/Ghana/Sénégal
          (même limite de source, voir lot 4). Vérifié (`kikole_test`) : 0 erreur, 1294
          clubs au total, répartition exacte (Arabie Saoudite 12, Égypte 17, Maroc 20),
          traductions EN+FR complètes, aucun id dupliqué.
        - [x] **Lot 4/5 : Tunisie, Algérie, Nigeria, Cameroun** — ids 1295-1357, D1 seule
          (`comp58` pour les quatre). Nigeria : un alias ajouté, Iwuanyanwu Nationale→
          Heartland FC (renommage confirmé après changement de propriétaire ~2004).
          **Cameroun — même particularité de source que le Maroc** : `club.dat` ne
          modélise que 4 clubs sous un comp dédié (58) + 15 autres sous le comp générique
          254, sans découpage D1/D2 réel ; les deux groupes combinés (19 clubs, dont Coton
          Sport de Garoua et Canon Yaoundé, les deux clubs dominants de l'époque) ont été
          pris comme l'équivalent complet de la D1. Vérifié (`kikole_test`) : 0 erreur,
          1357 clubs au total, répartition exacte (Tunisie 12, Algérie 16, Nigeria 16,
          Cameroun 19), apostrophes des noms algériens (d'Aïn M'lila, d'Oran, d'Alger,
          d'Annaba) correctement échappées et restituées, traductions EN+FR complètes,
          aucun id dupliqué.
        - [x] **Lot 5/5 (dernier) : Afrique du Sud, Côte d'Ivoire, Ghana, Sénégal, Irak**
          — ids 1358-1431 (5 pays sur ce dernier lot plutôt que 3-4, pour clore le
          chantier proprement plutôt que de laisser un lot solitaire d'un seul pays).
          Afrique du Sud : D1 seule (18, `comp201` — Kaizer Chiefs et Orlando Pirates
          confirmés dedans), 1 alias ajouté (Sundowns→Mamelodi Sundowns, nom encore en
          usage aujourd'hui avec le préfixe complet). **Côte d'Ivoire, Ghana, Sénégal,
          Irak — même particularité de source que Maroc/Cameroun** : aucun découpage
          D1/D2 dans `club.dat` pour ces quatre pays (un seul groupe, `comp254` seul pour
          Ghana/Sénégal, `comp58`+`254` combinés pour Côte d'Ivoire, `comp58`+`101`+`254`
          combinés pour l'Irak) — pris tel quel comme équivalent de la D1, cohérence des
          noms (ASEC Abidjan/Africa Sports, Asante Kotoko/Hearts of Oak, ASC Jeanne
          d'Arc/Jaraaf, Al-Zawraa/Al-Talaba) avec les grands clubs historiques connus de
          ces championnats. Vérifié (`kikole_test`) : 0 erreur, **1431 clubs au total**
          (490 + 941 sur les 2 chantiers), répartition exacte (Afrique du Sud 18, Côte
          d'Ivoire 12, Ghana 17, Sénégal 14, Irak 13), traductions EN+FR complètes, aucun
          id dupliqué.
        - **Chantier "grandes nations Afrique/Asie/Océanie" terminé** (19/19 pays
          importés, 235 clubs ajoutés au total sur les 5 lots). Même reste que pour le
          chantier Europe : migration sur la base locale pas appliquée sans confirmation
          explicite au préalable. **Rappel du contournement WAMP** (lot 1) : le service
          Windows `wampmysqld64` est resté démarré en standalone (`mysqld.exe`) tout le
          long de ce chantier plutôt que via le Service Control Manager — à relancer
          proprement via WampServer à la prochaine session si besoin, ou à ignorer si ça
          fonctionne déjà (le process tourne toujours en tâche de fond).
      - [x] **Amérique (terminé, 2026-09-10)** — même méthode. D1 seule pour la plupart,
        **D1+D2 pour Mexique/Brésil/Argentine** (consigne explicite). Liste "à minima" :
        USA, Mexique, Brésil, Argentine, Colombie, Chili, Uruguay — Costa Rica proposé en
        plus par Claude (Keylor Navas + quart de finale Mondial 2014) mais **finalement
        classé par l'utilisateur en pays "à surveiller"**, pas en import complet, de même
        que Venezuela (mentionné par Claude comme option plus faible). Pays "à
        surveiller/scan opportuniste" (pas d'import D1 complet, juste un coup d'œil aux
        clubs disponibles pour repérer d'éventuelles pépites) : Canada, Bolivie, Pérou,
        Équateur, Paraguay, Costa Rica, Venezuela. **Canada absent de `club.dat`** — aucun
        club canadien trouvé dans les 182 nations du fichier (probablement parce
        qu'aucune ligue domestique canadienne n'existait en 2001-02, les clubs canadiens
        jouaient alors dans l'A-League américaine) : rien à en tirer, pays à laisser de
        côté faute de source. **Consigne spécifique USA** : vérifier les noms actuels des
        clubs (alias) et repérer les clubs pertinents aujourd'hui mais absents de
        `club.dat` faute d'exister encore en 2001 — deux ajoutés directement (pas de
        source club.dat) : **Inter Miami CF** (Messi/Suárez/Busquets/Alba, exemple donné
        par l'utilisateur) et **Los Angeles FC** (Bale/Chiellini, proposé par Claude en
        plus, mêmes critères) — l'utilisateur n'a pas eu l'occasion de confirmer LAFC
        explicitement, à retirer si désapprouvé.
        - [x] **Lot 1 : USA, Colombie, Chili, Uruguay** — ids 1432-1495. USA : MLS 2001
          (`comp31`, 12 clubs) + les 2 clubs ajoutés directement ci-dessus. 4 alias
          ajoutés (franchises renommées depuis, filiation directe certaine) : Dallas
          Burn→FC Dallas (2005), Kansas City Wizards→Sporting Kansas City (2011), NYNJ
          Metrostars→New York Red Bulls (2006, exactement l'exemple cité par
          l'utilisateur), Washington DC United→D.C. United (variante de ponctuation
          actuelle). **Deux franchises de la MLS 2001 ont depuis disparu** (contraction
          de la ligue fin 2001/2002) sans successeur : Miami Fusion FC, Tampa Bay Mutiny —
          conservées telles quelles (exactes pour la saison 2001), aucun alias. Colombie
          (16, `comp58`), Chili (16, `comp58` — Colo-Colo/Universidad de Chile/Universidad
          Católica confirmés dedans), Uruguay (18, `comp58` — Peñarol et Nacional
          confirmés dedans) : D1 seule, aucun alias (pas de renommage confiant identifié).
          Vérifié (`kikole_test`) : 0 erreur, 1495 clubs au total, répartition exacte
          (USA 14, Colombie 16, Chili 16, Uruguay 18), apostrophe de "O'Higgins"
          correctement échappée, traductions EN+FR complètes, aucun id dupliqué.
        - [x] **Lot 2 : Mexique (D1+D2), Argentine (D1+D2)** — ids 1496-1579. Mexique :
          D1 (19, `comp214`) + D2 (20, `comp215`) complets, réserve `comp216` exclue
          (`Club América B` explicite + 4 autres non retenues, pas de vraie D2/D3 réelle
          pour ces dernières). Argentine : D1 (20, `comp63` — Boca/River confirmés
          dedans) + D2 "Primera B Nacional" (25, `comp64`) complets. Aucun alias (pas
          demandé pour ces deux, best-effort non fait faute de temps vu le volume).
          Vérifié (`kikole_test`) : 0 erreur, 1579 clubs au total, répartition exacte
          (Mexique 39, Argentine 45), apostrophe de "Newell's Old Boys" correctement
          échappée, traductions EN+FR complètes, aucun id dupliqué.
        - [x] **Lot 3 (dernier des pays "à minima") : Brésil (D1+D2)** — ids 1580-1635.
          Saison 2001 exceptionnellement large (28 clubs en D1 cette année-là, format
          élargi ponctuel). D1 confirmée via `comp65` (Flamengo/Vasco/Fluminense/
          Botafogo/São Paulo/Santos/Palmeiras/Corinthians tous dedans), D2 confirmée via
          `comp79` (Ceará/Náutico/Paysandu/Fortaleza/Criciúma, noms classiques de Série
          B). `comp80` (Série C, 56 clubs) et `comp254` (amateur, 240 clubs) écartés,
          hors périmètre D1+D2 demandé. Les suffixes entre parenthèses du fichier source
          ("(MG)", "(SP)", "(RN)", "(RJ)", "(AM)") conservés tels quels — désambiguïsation
          entre clubs homonymes de villes différentes, même convention déjà vue ailleurs
          (ex. "Al Ahli (KSA)"). Vérifié (`kikole_test`) : 0 erreur, **1635 clubs au
          total**, répartition exacte (Brésil 56), traductions EN+FR complètes, aucun id
          dupliqué.
        - **Les 7 pays "à minima" sont maintenant tous importés.** Reste : les 7 pays "à
          surveiller" (Canada — impossible, absent de `club.dat` — Bolivie, Pérou,
          Équateur, Paraguay, Costa Rica, Venezuela) en scan opportuniste, puis la
          migration base locale (pas faite sans confirmation explicite, comme d'habitude).
        - [x] **Lot 4 (dernier) : Bolivie, Pérou, Équateur, Paraguay, Costa Rica,
          Venezuela — scan opportuniste** — ids 1636-1701. **Constat commun aux 6 pays**
          (à part le Canada, totalement absent de `club.dat`) : le seul groupe exploitable
          (`comp58`) s'est révélé être, dans chaque cas, une D1 propre et complète, sans
          mélange avec des clubs amateurs à filtrer — rien à trier, le "scan" a donc
          abouti au groupe entier plutôt qu'à une sélection au cas par cas. Bolivie (12,
          Bolívar/The Strongest confirmés dedans), Pérou (12, Alianza Lima/Universitario/
          Sporting Cristal/Cienciano — ce dernier vainqueur de Copa Sudamericana 2003),
          Équateur (10, Barcelona SC/Emelec/El Nacional), Paraguay (10, Olimpia/Cerro
          Porteño/Libertad/Guaraní), Costa Rica (12, Saprissa/Alajuelense — Saprissa est
          l'club formateur de Keylor Navas, motivation initiale de l'ajout de ce pays),
          Venezuela (10, Táchira/Caracas FC). Vérifié (`kikole_test`) : 0 erreur, **1701
          clubs au total**, répartition exacte par pays, traductions EN+FR complètes,
          aucun id dupliqué.
        - **Chantier "Amérique" terminé** (13 pays traités : 7 "à minima" + 6 "à
          surveiller", 270 clubs ajoutés au total — 1431 → 1701). **Los Angeles FC**
          ajouté côté USA sur initiative de Claude (même logique qu'Inter Miami), confirmé
          par l'utilisateur ("bien vu pour LAFC") — gardé.
      - **Chantier clubs déclaré terminé par l'utilisateur (2026-09-10)** : "bon débarras".
        **1701 clubs au total** dans `kikole.sql` (**1702 depuis l'ajout hors lots de « FC
        Libourne » le 2026-09-26** : id 1702, France, club de Valbuena accepté malgré son statut
        amateur, cf. section « Carrière en club » de la page d'accueil ; noms alternatifs
        « Libourne Football Association 2024 », « Libourne », « FC Libourne-Saint-Seurin » ;
        dans la base locale il porte l'id 1703, l'id 1702 y étant occupé par le club de test
        « cest toto ») (France/Italie/Grèce sourcées à part,
        puis 8 grands pays d'Europe + leurs échelons inférieurs, puis 24 pays d'Europe
        supplémentaires, puis 19 pays d'Afrique/Asie/Océanie, puis 13 pays d'Amérique).
        **Reste volontairement non fait, décision explicite de clore ici** : les échelons
        inférieurs des pays d'Afrique/Asie/Amérique (seule l'Europe a eu cette passe),
        d'éventuels pays non couverts.
        - [x] **Migration appliquée à la base locale (2026-09-26)** — les 1211 clubs et
          2470 traductions ajoutés depuis l'id 491 (tout le travail au-delà du baseline
          initial de 490 clubs) insérés en une fois, extraits directement de `kikole.sql`
          via un script PowerShell (ids > 490 uniquement, pour ne pas retoucher les 490
          déjà présents) puis exécutés contre la base `kikole` réelle (pas de base de
          test jetable ici — c'est la migration elle-même, demandée explicitement :
          "tu peux commencer par le dernier point"). Vérifié après coup : 1701 clubs /
          3796 traductions au total, aucune traduction EN ou FR manquante, aucun id
          dupliqué, apostrophes intactes (Beijing Guo'an, O'Higgins, Connah's Quay
          Nomads, Newell's Old Boys), Inter Miami CF et LAFC bien présents.
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
- [ ] **Indices audio/mp3 et vidéo/mp4** (2026-09-27, question théorique de l'utilisateur,
      pas encore implémenté). `Model.Clue`/`EasyClue` sont déjà du texte libre pouvant être
      une URL détectée par `ViewHelper.IsImageUrl()` (URL http(s) + extension) pour basculer
      sur un `<img>` au lieu du texte brut (`Views/Home/Index.cshtml`) — même principe à
      étendre avec `IsAudioUrl()`/`IsVideoUrl()` (extensions `.mp3`/`.mp4`) rendant
      `<audio controls>`/`<video controls>` (pas d'autoplay avec son de toute façon, bloqué
      par les navigateurs — `controls` donne le bouton play nécessaire sans effort
      supplémentaire).
      **Décision de l'utilisateur sur l'hébergement** : solution locale `wwwroot` pour tous
      les médias d'indice, y compris les images — changement d'avis explicite par rapport à
      l'existant (les images d'indice actuelles sont hébergées ailleurs, ex. imgur, juste
      référencées par URL). Implique, le jour où ce chantier démarre, de revoir aussi le
      circuit des images déjà en place (pas seulement ajouter audio/vidéo à côté).

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

      **(a) Infra de tests d'intégration en place** — vraie base MySQL locale. Écrite au
      départ dans `KikoleSiteUnitTests/Integration/` avec `[Trait("Category","Integration")]`
      pour rester filtrable, **déplacée depuis** dans son propre projet
      (`KikoleSiteIntegrationTests`, séparation complète demandée par l'utilisateur — voir
      « Partis pris »). `dotnet test` à la racine construit et exécute les deux projets côte
      à côte ; `KikoleSiteUnitTests` seul ne touche plus jamais WAMP.

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
- [ ] **Refaire une passe sur les badges éventuellement manquants**, demandé par
      l'utilisateur. Portée pas encore précisée avec lui : à clarifier au démarrage de ce
      chantier — badges déjà définis (`Badges`, `BadgeService`) mais dont une condition
      resterait non couverte ou buguée, ou nouvelles idées de badges pas encore
      implémentées.
      - [x] **`DownToTheWire`** — trouver le kikolé avec exactement 13 points restants (le
        plancher mathématique non-nul du barème : 1000 → 25 via des propositions ratées
        classiques, multiples de 25 seulement → indice facile acheté à 25 restants,
        arrondi bancaire de 12.5 sur 12 → 13. Pas atteignable autrement, ni 1 ni 10 ne le
        sont). `LeaderBasedBadgeCondition[Badges.DownToTheWire] = l => l.Points == 13`
        (`BadgeService.cs`).
      - [x] **`Phoenix`** — victoire "propre" (points > 0, sans indice facile) précédée de
        7 jours consécutifs où l'utilisateur a *tenté sa chance* (au moins une proposition
        soumise, précision de l'utilisateur — un jour sans la moindre tentative casse la
        série, ne compte pas comme un "échec") sans jamais décrocher une victoire aussi
        propre (échec, victoire hors délai, ou victoire à 0 point comptent tous comme
        rate). Nouvelle méthode dédiée `RespectsPhoenixConditionAsync` (`BadgeService.cs`)
        plutôt que le patron générique `RespectLeadersRunConditionsInternal` (pensé pour
        l'inverse : une série de *bons* jours, pas de mauvais). Simplification assumée et
        documentée dans le code : contrairement aux badges de série existants, les jours où
        l'utilisateur est le créateur du kikolé du jour ne sont pas traités à part — à
        revoir si le cas se présente en pratique.
      - Les deux ajoutés à `Badges.cs` (29, 30), `kikole.sql` (lignes `badges`/
        `badge_translations`, + seedées directement en base locale via un test
        d'intégration jetable, créé puis supprimé) et testés (`BadgeServiceTests.cs`, 7
        nouveaux tests : 2 pour `DownToTheWire`, 5 pour `Phoenix` couvrant chacune des 3
        façons de rater un jour + les deux conditions du jour de la victoire).
      - **Vérifié en direct, de bout en bout** (compte jetable `testbadge13c`, via `fetch`
        pour piloter 39 propositions "Pays" fausses sans passer par l'UI un par un) : 1000
        → 25 points confirmés après les 39 échecs, → 13 confirmés après achat de l'indice
        facile, victoire réelle sur "Clarence Seedorf" à 13 points sans erreur serveur,
        badge `Down to the wire` bien crédité (page `Leaderboard?userId=...`), `Phoenix` bien
        absent (indice facile utilisé ce jour-là, condition qui l'exclut explicitement).
        **Fausse alerte en cours de route, notée pour la prochaine fois** : une "erreur
        serveur" est apparue lors d'une première tentative de victoire — cause réelle :
        un test d'intégration lancé *entre-temps* (pour lire le nom du joueur du jour) a
        rejoué `kikole_mock.sql` via `DatabaseFixture.InitializeAsync`, supprimant le
        compte de test en cours de partie sous ses pieds. Rien à voir avec le code des
        badges. Leçon : ne jamais lancer `dotnet test` sur `KikoleSiteIntegrationTests`
        pendant une session de vérification live sur la même base — chaque run réinitialise
        `users`/`players`/`proposals`/`leaders` (mais pas `badges`/`badge_translations`,
        non touchées par `kikole_mock.sql`).
      - **Idée écartée après discussion : "Grand Chelem"** (trouver un joueur à chacun des
        4 postes sur une fenêtre de 7 jours) — l'utilisateur varie déjà ses joueurs
        proposés, et avec les postes alternatifs désormais possibles
        (`AlternativePositionId`), ce badge serait quasi automatique au bout d'une semaine
        de jeu normale. Retenu comme angle à retravailler plus tard (préciser la condition
        pour qu'elle reste un vrai défi), pas abandonné.
      - [x] **`TheEnd`** ("The end?" / "La fin ?"), pour le joueur caché de la page 0 (avant
        le tout premier jour, accessible uniquement par l'URL — cf. `PlayerService
        .CanDisplayHiddenPlayerAsync`). Reprend délibérément le nom d'un badge supprimé de
        l'ancien jeu (`Restauration/badges_2023.md`, id 24 "Reach the 'end' of the game").
        **Décision actée avec l'utilisateur : pas de rattachement via `players.badge_id`**
        (le mécanisme dédié déjà en place, cf. discussion précédente — conservé tel quel
        mais toujours inutilisé, aucun joueur ne s'en sert) — badge "standard", détecté dans
        `PrepareNewLeaderBadgesInternalAsync` par `leader.ProposalDate == _gameCalendar
        .HiddenDate`. Piège évité : ce check est placé **hors** du bloc `if
        (leader.IsCurrentDay)` (comme `playerOfTheDay.BadgeId`/`PlayerBasedBadgeCondition`)
        — le jour caché étant antérieur à `FirstDate`, `IsCurrentDay` (qui compare à la date
        de création) n'est jamais vrai pour lui ; placé dans le bloc gaté, le badge ne se
        serait jamais déclenché.
        - **Deuxième demande de l'utilisateur dans la foulée** : ajouter un plancher de
          **30 jours d'ancienneté de partie** avant que le joueur caché soit accessible du
          tout (`PlayerService.CanDisplayHiddenPlayerAsync`, nouvelle constante
          `MinimumDaysBeforeHiddenPlayer`) — sans ça, la condition existante ("aucun jour
          manqué depuis le début") est triviale en tout début de partie (peu de jours à
          couvrir). Placé en tout premier check, avant même le raccourci "déjà trouvé une
          fois" : personne n'a accès avant ce plancher, peu importe son historique.
        - `Badges.cs` (31), `kikole.sql` (`badges`/`badge_translations`), testé
          (`BadgeServiceTests.cs`, 2 tests ; `PlayerServiceTests.cs`, 4 tests existants
          adaptés au nouveau plancher + 1 nouveau dédié à ce plancher).
        - Vérifié en direct (seed du badge + navigation `/?day=32` avec un compte jetable
          sans historique : page "presque au but" affichée sans erreur, comme avant ;
          "The end?" bien rendu sur une page de statistiques existante, nom + description
          FR corrects). Pas de test live d'un vrai déblocage (nécessiterait un historique
          réel de 30+ jours sans trou, pas simulable rapidement) — couvert par les tests
          unitaires à la place.
- [ ] **Classement des participants par % de badges obtenus.** Demandé en aparté par
      l'utilisateur pendant ce même chantier (2026-09-08). Rien commencé — probablement un
      nouveau `LeaderSorts` (cf. `LeaderboardController`/`LeaderService.GetLeaderboardAsync`)
      calculant, par utilisateur, `(badges obtenus / total des badges) %` ; à voir si les
      badges cachés doivent compter dans le total ou être exclus du calcul pour tout le
      monde sauf leur détenteur.
- [x] **Noms de badges en français : mécanisme en place (2026-09-26), traductions à
      fournir.** Demandé par l'utilisateur : les badges ne devaient pas s'afficher en anglais
      en version française. `badge_translations` porte maintenant une colonne `name` en plus
      de `description` (PK inchangée `(badge_id, language_id)`) ; nouveau
      `BadgeTranslationDto` (`Name`, `Description`) et `IBadgeRepository.GetBadgeTranslationAsync`
      (remplace `GetBadgeDescriptionAsync`) ; `Badge` prend nom et description traduits, avec
      repli **champ par champ** sur `badges.name`/`badges.description` si la traduction est
      absente ou vide ; `BadgeService.GetBadgeAsync` ne consulte la table qu'hors anglais.
      Testé (`MappingModelsTests`, `BadgeServiceTests`), vérifié en direct (nom FR temporaire
      sur un badge : affiché en français, inchangé en anglais). **En attendant les
      traductions, chaque ligne FR de `kikole.sql` reprend le nom anglais** (33 badges).
      - [x] **Noms français fournis (2026-09-26)** : les 33 lignes `language_id = 2` de
        `kikole.sql` portent maintenant un vrai nom français (propositions validées telles
        quelles par l'utilisateur, ex. « Archéologie », « Sauvé par le gong », « Le Club des
        Cinq » pour « The Famous Five » ; « Don Corleone » et « The Breakfast Club » restent
        inchangés). Trois noms anglais ont changé dans `badges` : n° 3 « IT'S OVER 900**!** »,
        et n° 9 (« Poop, Coffee, Cigarette, Kikolé ») et 27 (« Commute, work, kikolé,
        sleep »), dont le nom d'origine était en réalité français. Appliqué aussi à la base
        locale (badge 29 : « Down to the wire » côté anglais, « Sur le fil » côté français ;
        les badges 32 et 33 n'y existent pas, la base locale n'a pas le parrainage).
        Sept propositions étaient jugées incertaines (n° 3, 9, 10, 11, 12, 24, 27) : à
        rouvrir si l'un d'eux déplaît à l'usage.
      - [ ] **Migration à jouer sur toute base existante (locale déjà faite, prod à faire)** :
        `ALTER TABLE badge_translations ADD COLUMN name varchar(255) COLLATE
        utf8mb4_unicode_ci NOT NULL AFTER language_id;` puis `UPDATE badge_translations t
        JOIN badges b ON b.id = t.badge_id SET t.name = b.name;`. Rétro-compatible : l'ancien
        code ne lit que `description`, on peut donc migrer la base avant de déployer.- [x] ~~Que faire des statistiques ?~~ — **décision : réservées à l'administrateur.** Les
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
        **Plage min/max ajoutée après coup** — voir l'item dédié plus bas (section
        "datepicker verrouillé sur la plage jouable"), qui couvre ces 3 champs en plus de
        celui de la page d'accueil.
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
      **Question des jours passés, tranchée** : le cadran affichait déjà le score du
      jour consulté (`Points` est calculé sur `CurrentDay`, jamais un total permanent —
      vérifié dans le code, pas une supposition) ; le vrai sujet était donc de signaler
      visuellement qu'on n'est plus sur le jour présent, pas de changer la donnée
      affichée. Discussion des options avec l'utilisateur (rester tel quel / griser /
      double cadran) — retenu : griser, sans ajouter de deuxième cadran (aurait
      réintroduit la question "lequel des deux scores compte vraiment", alors que le
      site n'a nulle part ailleurs la notion de "score courant permanent"). Résultat :
      **4 styles de cadran** selon jour courant/autre jour × trouvé/pas trouvé —
      `.scoreboard` (doré, jour courant en cours), `.scoreboard.success` (vert + halo,
      jour courant trouvé), `.scoreboard.other-day` (grisé/atténué, jour passé non
      résolu), `.scoreboard.other-day.found` (grisé + valeur teintée de vert sans halo,
      jour passé trouvé/abandonné — pour ne pas perdre cette information en s'éloignant
      d'aujourd'hui). Vérifié en direct les 4 cas (y compris en forçant un abandon sur
      un jour passé, qui déclenche bien `.other-day.found` avec 0 pts).
      **Bonus fait dans la foulée** : `Home/Index.cshtml`, le bloc "Jour précédent / date
      / Jour suivant" jugé un peu terne par l'utilisateur — remplacé par des flèches en
      icônes SVG (cohérentes avec les autres pictos du site) encadrant la date, dont
      l'apparence passe d'un simple soulignement à une vraie puce (fond/bordure/coins
      arrondis) ; état désactivé visible (grisé, non cliquable) plutôt qu'absent quand il
      n'y a pas de jour précédent/suivant, pour que le groupe ne se décale pas visuellement
      selon le contexte. `id`/`class="date-field"` du champ conservés (widget jQuery UI
      existant non touché).
- [x] **Trois retouches supplémentaires sur la navigation jour par jour**, demandées par
      l'utilisateur juste après le point précédent :
      - **Flèche "retour à aujourd'hui" (double chevron)** — `Home/Index.cshtml`,
        `<a class="day-nav-arrow">` supplémentaire (icône SVG double flèche, même famille
        que les flèches simples), affichée uniquement quand `CurrentDay != 0` (donc jamais
        visible en plus de la flèche "jour suivant" quand elles pointent au même endroit
        un jour normal — les deux coexistent seulement pour offrir un raccourci direct
        plutôt que de cliquer "suivant" plusieurs fois). Simple lien `href="/"`, pas de JS.
      - **Info-bulle "trouvé dans les délais / en rattrapage" au survol du cadran** —
        nouvelle propriété `HomeModel.FoundOnTime` (`bool?`, `null` tant que non trouvé),
        calculée dans `HomeController.SetAndGetViewModelAsync` en comparant la date de la
        proposition gagnante (`ProposalResponse.IsWin`, déjà `internal`) à la date
        consultée — couvre à la fois le POST gagnant immédiat et une re-consultation GET
        ultérieure, un seul chemin de code pour les deux. Rendu en attribut `title` natif
        sur `.scoreboard` (pas de tooltip JS custom), deux nouvelles clés resx
        (`FoundOnTime`/`FoundLate`, `Home/Index.*.resx`).
      - **Datepicker verrouillé sur la plage jouable** — jusqu'ici aucune borne
        (`minDate`/`maxDate` jQuery UI), un jour avant `FirstDate` ou après aujourd'hui
        restait sélectionnable dans le calendrier ; seul un redirect serveur après coup
        rattrapait le cas. Corrigé sur les 4 champs concernés : `#dayDatepicker`
        (`Home/Index.cshtml`, `data-min-date`/`data-max-date` calculés via
        `@@inject IGameCalendar`/`Model.CurrentDate`, borne haute non plafonnée pour un
        administrateur) et `#LeaderboardDay`/`#MinimalDate`/`#MaximalDate`
        (`Leaderboard/Index.cshtml`, `@@inject IGameCalendar`/`@@inject IClock` ajoutés à
        cette vue, bornes `FirstDate`→`Today` identiques pour tout le monde — pas de cas
        administrateur ici, `LeaderboardController.EnsureDateAsync` plafonne déjà tout le
        monde à `_clock.Today` côté serveur). Nouvel helper partagé `site.js`
        (`parseIsoDate`, une date ISO en `Date` locale plutôt que `new Date(iso)` qui
        interprète UTC et peut décaler d'un jour selon le fuseau) lu par les deux blocs
        d'initialisation datepicker via `data-min-date`/`data-max-date`. Vérifié en direct
        (build 0 avertissement, 596 tests verts, puis navigateur) : `#dayDatepicker` avec
        `minDate`/`maxDate` calendrier correctement bornés, les 3 champs du classement
        idem (jours après aujourd'hui grisés dans le calendrier).
      - **Confirmé au passage (question de l'utilisateur, pas un changement de code)** :
        l'accès au jour caché (`HiddenDate`, le tout premier joueur publié) exige toujours
        les deux mêmes conditions qu'avant — avoir trouvé ou créé tous les jours depuis
        `FirstDate` (y compris en rattrapage, `PlayerService.CanDisplayHiddenPlayerAsync`)
        et taper le numéro de jour directement dans l'URL, `NoPreviousDay` empêchant "Jour
        précédent" d'atteindre `HiddenDate` par construction (`FirstDate` est sa dernière
        valeur). Nuance découverte en implémentant le point ci-dessus : avant ce
        verrouillage, le datepicker n'avait techniquement *aucune* borne — l'impossibilité
        d'atteindre `HiddenDate` par ce biais tenait uniquement au redirect serveur
        (`HomeController.Index`, `DateOfDay < HiddenDate` → `day=0`), pas à un vrai
        verrou côté client. Après ce chantier, c'est désormais un verrou réel des deux
        côtés.
- [x] **Trois derniers réglages fins sur ce même lot**, relus par l'utilisateur après coup :
      - **Datepickers du classement trop "bland"** — les 3 champs (`LeaderboardDay`,
        `MinimalDate`, `MaximalDate`) n'avaient que le style neutre générique
        (`.kikole-board .date-field` : simple soulignement pointillé), le style "puce"
        (fond/bordure/coins arrondis) n'étant appliqué qu'au champ de la page d'accueil
        via le sélecteur plus spécifique `.day-nav .date-field`. Résolu en fusionnant les
        deux règles : le style puce est passé dans la règle de base `.date-field` (seuls 4
        champs au total dans tout le site utilisent cette classe, aucun autre usage à
        préserver), `.day-nav .date-field` ne garde plus qu'une largeur réduite (96px vs
        110px, pour tenir entre les deux flèches). `?v=` de `kikole-board.css` passé à 12.
      - **`isToday`/`isFound`/`hasNextDay` remontés dans `HomeModel`** — trois variables
        `@{ }` locales à `Home/Index.cshtml`, ne dépendant que de propriétés déjà sur le
        modèle (`CurrentDay`, `IsCreator`, `PlayerName`, `IsAdmin`) : promues en propriétés
        calculées (`IsToday`, `IsFound`, `HasNextDay`), même motif que `NextDay`/
        `PreviousDay`/`DateOfDay` déjà sur `HomeModel`. La vue ne calcule plus que
        `scoreboardClass`/`scoreboardTitle`, qui eux dépendent du `localizer` (raison de
        rester dans la vue).
      - **Libellés de l'info-bulle du cadran jugés redondants** — "Trouvé dans les délais,
        le jour même" / "Trouvé en rattrapage, après coup" reformulés en "Trouvé le jour
        même !" / "Trouvé le {0}" (le `{0}` affichant la vraie date de la proposition
        gagnante, pas le jour consulté — les deux ne coïncident que dans le cas "trouvé à
        temps"). Nécessitait de faire remonter cette date : nouvelle propriété
        `HomeModel.FoundDate` (`DateTime?`), posée par `HomeController` à côté de
        `FoundOnTime` (même source, `winningProposal.Date`), formatée en vue via
        `ToNaString()` (extension déjà utilisée partout ailleurs sur le site pour les
        dates lisibles, FR/EN sensible à la culture). Vérifié en direct : jour courant
        trouvé → "Trouvé le jour même !" ; jour passé trouvé en rattrapage (test via
        abandon volontaire sur un jour antérieur) → "Trouvé le 05/09/2026" (la date réelle
        de l'abandon, pas celle du jour affiché) — cadran bien en style grisé + valeur
        verte (`.other-day.found`) dans ce second cas.
- [x] **Nouvelle relecture complète du site par l'utilisateur, 6 points** :
      - **Annonces admin (`Model.Message`) : plus en rouge, repliable, "ne plus
        afficher".** L'ancien rendu (`<div class="banner error">`) empruntait la couleur
        d'erreur alors que ce n'en est pas une. Nouveau composant `Partial/Announcement`
        (+ resx dédié `Announcement.*.resx`) : bandeau neutre (`.banner.info`, palette
        papier plutôt que rouge/vert), icône "i" en cercle, libellé "Annonce", bouton
        replier/déplier et bouton "×" qui retire le bandeau ET mémorise l'id du message
        dans un cookie
        (`kikoleDismissedAnnouncements`, liste d'ids séparés par virgules) — un futur
        message (autre id) n'est donc jamais masqué par erreur, contrairement à un simple
        flag booléen. Le filtrage se fait **côté serveur** (`HomeController
        .SetAnnouncementAsync`/`IsAnnouncementDismissed`, factorisé pour les deux points
        d'entrée qui posaient `model.Message` avant) : le bandeau ne s'affiche même pas
        dans le HTML si son id est dans le cookie, pas de flash côté client. Nouvelle
        propriété `HomeModel.MessageId` (`ulong?`) pour porter l'id jusqu'à la vue.
        **Suite (signalé par l'utilisateur après coup) :** le replier/déplier ne
        persistait pas — se redépliait à chaque changement de page. Fix dans `site.js` :
        état stocké dans `localStorage` (clé unique, valeur = l'id du message replié
        s'il y en a un), clé par id de message donc un futur message (autre id)
        redémarre toujours déplié, même logique que le cookie de "ne plus afficher".
      - **Bandeau "Proposition ... incorrecte/correcte" jugé "collé" au conteneur** —
        discuté avec l'utilisateur (option snackbar bas-droite vs rester en place) :
        **reste en place** (feedback au plus près du champ concerné, plus fiable qu'un
        coin d'écran pour l'interaction la plus fréquente du jeu). Premier essai
        (`border-radius` 4px→6px + `box-shadow` discrète) **insuffisant** — vérifié
        servi correctement (contenu de `kikole-board.css` inspecté en direct via
        `fetch`/`getComputedStyle`, pas un souci de cache), mais visuellement trop
        proche de l'original pour se remarquer. Corrigé plus franchement en deuxième
        passe : liseré de 4px sur le bord gauche (`border-left-color`, vert `--pitch`
        pour info/succès, rouge `--stamp` pour erreur — langage "toast" classique) +
        ombre nettement plus marquée (`0 8px 20px` au lieu de `0 4px 14px`). Cette fois
        le bandeau se détache clairement de la page.
      - **Position : ordre et persistance de la liste des essais ratés.** Deux bugs
        dans `Home/Index.cshtml` : (a) la liste apparaissait **avant** le menu déroulant,
        seule catégorie dans ce cas (club/continent/pays/année ont toutes le motif
        [champ] puis [essais]) — réordonné pour matcher. (b) la liste restait affichée
        même une fois la position trouvée, alors que continent/pays/année la font déjà
        disparaître à ce moment — discuté avec l'utilisateur (garder partout vs disparaître
        partout) : **disparaît une fois trouvé**, position alignée sur les 3 autres
        (une fois la catégorie résolue, plus moyen d'y proposer, donc plus d'utilité
        fonctionnelle à garder l'historique — contrairement aux clubs, laissés à part,
        où le total à trouver reste inconnu). Pur changement de vue, aucune logique
        serveur touchée (`IncorrectPositions` continue d'être peuplée comme avant).
      - **Année de naissance : plafond `2010` en dur → `année courante - 10`.** Present
        à deux endroits distincts : `HomeController.IsValidInput` (validation serveur,
        passé de `static` à instance pour accéder à `_clock`) et `site.js` (liste
        d'autocomplétion `#birthYearValue`, généré via `new Date().getFullYear() - 10`
        côté client — pas besoin de la faire remonter du serveur, un simple calcul de
        date suffit). Vérifié en direct : autocomplétion plafonnée à 2016, soumission de
        2020 rejetée serveur ("Requête invalide").
      - **Classement : lien vers le détail d'un score visible avant d'avoir soi-même
        trouvé le joueur du jour concerné.** Cas cité par l'utilisateur : un utilisateur
        ayant "acheté" l'accès au classement du jour (`DayGrantTypes.PaidBoard`) voit le
        tableau mais tombait sur une page "pas les droits" en cliquant le score d'un
        autre joueur (`LeaderboardController.UserDay` exige `Found`/`Creator`/`Admin`).
        En creusant : le problème n'est pas limité au jour même — un jour passé jamais
        joué a le même souci (le tableau des jours passés est toujours visible, sans
        rapport avec le droit d'accès au détail). Corrigé à la source plutôt qu'au cas
        par cas : nouvelle propriété `Dayboard.CanViewDetails` (bool), calculée dans
        `LeaderboardController.GetDailyboardAsync` via `GetGrantAccessForDayAsync` sur
        la date **réellement affichée** (pas seulement "aujourd'hui" — `todayGrantEnsured`
        existant ne sert qu'à décider si le tableau du jour même doit être masqué, un
        besoin différent). Elle voyage gratuitement jusqu'au JSON de
        `/daily-leaderboard-details` (propriété publique de `Dayboard`, sérialisée
        `canViewDetails` en camelCase comme le reste) — aucun changement necessaire côté
        `LeaderboardModel`/`InitializeModelAsync`, qui portait déjà `Dayboard` tel quel.
        Lien conditionné par `Model.Dayboard.CanViewDetails` sur le rendu serveur initial
        (`Leaderboard/Index.cshtml`, classement et "recherches en cours") et par
        `data.canViewDetails` côté `site.js` (`loadDailyLeaderboard`, régénéré en AJAX au
        changement de tri/date) — même flag des deux côtés, aucune divergence possible.
        Vérifié : flag `false` sur un jour jamais joué par l'utilisateur (même avec des
        scores d'autres joueurs dedans, testé en simulant le rendu AJAX), `true` sur un
        jour où trouvé (y compris un abandon volontaire, qui crée bien une ligne
        `leaders`).
      Build (0 avertissement) + 596 tests verts après chaque étape, vérification
      navigateur complète pour les 6 points.
- [x] **Retour utilisateur après test du lot ci-dessus : point 2 pas encore satisfaisant,
      + une petite salve de corrections mineures sur `Admin/Club.cshtml`.**
      - **Vraie cause du bandeau "collé" enfin identifiée** — le premier essai
        (`border-radius`/`box-shadow` sur `.banner`) était bien servi (revérifié en
        direct via `fetch`/`getComputedStyle`, pas un souci de cache navigateur), mais
        beaucoup trop discret pour se remarquer. **Cause réelle, repérée en re-regardant
        le HTML plutôt que le CSS** : ce bandeau (`Model.MessageToDisplay`) est le seul
        enfant direct de `.dossier` (la carte beige), qui elle-même n'a **aucun**
        padding — tout le padding vit dans `.dossier-head` (22px/26px/18px). Le bandeau
        "Félicitations" juste à côté, lui, était déjà correctement placé *à l'intérieur*
        de `.dossier-head` et en héritait. Corrigé en déplaçant simplement le bandeau
        à l'intérieur de `.dossier-head` (avant le bloc `.clue`) plutôt qu'en ajoutant
        encore du CSS — aucune nouvelle règle nécessaire, le padding existant suffit.
        Gardé au passage le liseré coloré + l'ombre plus marquée de l'essai précédent
        (utiles, l'utilisateur avait confirmé les voir). Vérifié en direct : marge nette
        en haut/gauche/droite avant le contenu de l'indice du jour.
      - **`Admin/Club.cshtml` : ordre et libellés des champs.** Nouvel ordre (après le
        champ de recherche initial, inchangé) : Pays, Nom principal (FR), Nom principal
        (EN), Noms alternatifs (FR), Noms alternatifs (EN) — remplace l'ancien ordre
        EN-avant-FR avec Pays en dernier. Libellés simplifiés en conséquence
        (`MainNameFr`/`MainNameEn` : "Nom principal (titre page wiki français/anglais)"
        → "Nom principal (FR/EN)" ; nouvelles clés dédiées `AlternativeNamesFr`/
        `AlternativeNamesEn` au lieu d'une seule clé `AlternativeNames` réutilisée deux
        fois, ambiguë par construction). Ancien bloc d'aide à deux lignes
        (`AlternativeTip`/`DontMindDiacritics`) remplacé par un nouveau texte à trois
        lignes sous les champs (`MainNameHint`/`AlternativeNamesHint`/
        `AlternativeNamesExample`, FR+EN) : conseil Wikipédia pour le nom principal,
        explication + exemple concret ("PSG"/"Paris Saint-Germain",
        "Matra Racing"/"Racing Club de France") pour les alias — anciennes clés
        devenues inutiles supprimées des deux resx. Pur remaniement de vue/resx,
        `ClubCreationModel`/`AdminController` non touchés (les champs existaient déjà
        tels quels). Vérifié en direct (compte admin) : ordre et libellés corrects.
      Build (0 avertissement) + 596 tests verts, vérification navigateur des deux points.
- [x] **"J'archive" (lot précédent validé) + nouvelle salve de remarques mineures,
      `Admin/Index.cshtml` (page de création d'un joueur) et `Home/Index.cshtml`.**
      - **Indice "utilisez le titre de la page Wikipédia en anglais" (champ Nom)** —
        retiré complètement, y compris la clé resx `WikiPlayerName` (FR+EN), plus
        aucune référence nulle part.
      - **Trois indices trop "collés" au champ au-dessus** (nationalité alternative,
        poste alternatif, anonymat du créateur) — cause : `.form-hint` a un
        `margin-top: -6px` par défaut, pensé pour suivre un champ texte classique,
        trop serré après une `<select>` ou une checkbox. Plutôt que de toucher la
        règle globale (utilisée largement ailleurs, aucune plainte dessus), nouveau
        modificateur `.form-hint.spaced` (`margin-top: 6px`), appliqué uniquement à
        ces 3 indices (`TipAboutAlternativeNationality`, `TipAboutAlternativePosition`,
        `RemainsAnonymousTip`).
      - **Checkbox "Prêt ?" pas alignée avec son libellé** — taille par défaut du
        navigateur pour la checkbox, non maîtrisée. Fixée à 14×14px
        (`@Html.CheckBox(loanChk, new { style = "..." })`) et `line-height:14px`
        ajouté sur le `<label>` voisin pour que les deux boîtes fassent la même
        hauteur ; `align-items:center` déjà présent sur la ligne fait le reste.
        Vérifié par mesure DOM (`getBoundingClientRect`) : les deux éléments font
        bien 14px de haut, centres verticaux à ~2.5px près (l'écart résiduel vient
        de la métrique de la police, pas de la boîte elle-même — jugé suffisant).
      - **Lien "Créer un club (nouvel onglet)" déplacé en fin de section** "Carrière
        en club" — était avant le premier indice et les 15 lignes de club, maintenant
        après tout ça. La marge `margin-top:0` qui compensait sa position d'origine
        (juste sous le label) a été retirée, la marge par défaut de `.small-link`
        (8px) convient mieux après le dernier indice.
      - **"deux positions possibles" (indice à côté du champ Position, page
        d'accueil) retiré** — jugé prêtant à confusion par l'utilisateur, en attente
        d'une meilleure formulation. Seul le `<span class="hint">` a été retiré de
        `Home/Index.cshtml` ; la clé resx `TipAboutPosition` (FR+EN) est conservée
        mais vidée (valeur vide), pour ne pas avoir à la recréer le jour où une
        meilleure formulation est trouvée.
      Build (0 avertissement) + 596 tests verts, vérifications navigateur (DOM/mesures
      pour l'alignement checkbox, lecture directe pour le reste).
- [x] **"Archivé" (lot précédent validé) + 3 nouveautés pour finir la journée : icône
      dédiée + accès facilité à la création d'un kikolé, mention de délai sur la page
      Contact.**
      - **Icône "Créer un kikolé" redessinée** — partageait jusqu'ici le même
        pictogramme que "Compte" (silhouette), seulement distingué par un petit "+".
        Remplacée par un "K" (deux diagonales + une barre verticale, même style de
        trait que les autres icônes du site) suivi du même "+" ; l'icône "Compte" n'a
        pas bougé. Appliqué aux deux endroits où l'icône existe (`_Layout.cshtml`,
        barre desktop et tiroir mobile).
      - **Icône "Créer un kikolé" désormais toujours visible pour un utilisateur
        connecté** (avant : seulement `PowerUser`+) — mais sa destination dépend
        toujours du palier : `PowerUser`/administrateur → `/Admin` (le vrai
        formulaire, comportement inchangé) ; utilisateur standard → `/Home/Contact
        ?requestAccess=true`, un nouveau paramètre optionnel sur l'action `Contact`
        (GET) qui préremplit `ContactModel.NewMessage` avec un texte de demande de
        droits standard (nouvelle clé resx `RequestPowerUserMessage`,
        `Resources/Controllers/HomeController.*.resx`, FR/EN). Objectif : un
        utilisateur standard qui clique dessus n'atterrit pas sur un formulaire de
        contact vide sans savoir quoi écrire — le message est prérempli, modifiable
        avant envoi. Vérifié en direct dans les deux rôles et les deux langues.
      - **Page Contact : mention de délai de réponse ajoutée** en bas de carte, sous
        le bouton d'envoi (nouvelle clé resx `ResponseTimeNote`, FR/EN,
        `Resources/Views/Home/Contact.*.resx`) — avec la classe `.spaced` déjà
        introduite plus haut (le hint suit un bouton, pas un champ texte, le
        `margin-top: -6px` par défaut l'aurait collé au bouton).
      Build (0 avertissement) + 596 tests verts, vérifié en direct (admin, utilisateur
      standard, FR et EN).
- [x] **Popup de victoire au moment où un kikolé est trouvé** — le bandeau "Félicitations
      + liste des badges" était jusqu'ici affiché en permanence sur la page (dans le
      dossier), y compris en revisitant un jour déjà résolu bien plus tard. Demande de
      l'utilisateur : n'afficher ça qu'**une seule fois, au moment réel de la victoire**,
      dans une popup qui ne se ferme qu'au clic (pas d'auto-fermeture), avec le cadran de
      score bien visible dedans et — si faisable — un effet "feu d'artifice".
      - **Distinguer "vient de gagner" de "rouvre un jour déjà trouvé"** — nouvelle
        propriété `HomeModel.JustWon` (bool), posée dans `HomeController` (action POST)
        exactement quand `leader != null` (i.e. `response.IsWin`, la ligne `leaders`
        vient d'être créée à cet instant précis) — jamais vraie sur un GET de
        re-consultation, jamais vraie non plus sur un jour différent d'aujourd'hui
        (`CurrentDay != 0`, scope inchangé par rapport à l'ancien bandeau qui ne
        s'affichait déjà que pour le jour courant).
      - **Affichage "standard" simplifié** — `Home/Index.cshtml`, le titre du dossier
        pour `CurrentDay == 0` est désormais unique pour créateur et non-créateur
        ("Le joueur du jour est X"), l'ancienne branche dupliquant félicitations+badges
        supprimée. Comme la popup est un simple calque par-dessus une page déjà rendue
        dans son état "standard", il n'y a rien à faire au moment de la fermeture — le
        contenu normal est déjà là dessous.
      - **Popup** (`.win-modal`, nouveau, sur le même principe que `.confirm-modal`
        existant — masquée par défaut, `.open` l'affiche — mais rendue *déjà ouverte*
        par le serveur quand `Model.JustWon`, pas de bouton pour l'ouvrir) : bandeau
        félicitations, un **cadran de score dupliqué** dedans (`.scoreboard.success`,
        même style que celui du masthead, juste une seconde instance dans la popup —
        plus simple et plus fiable qu'essayer de "percer" un trou dans le fond assombri
        vers le vrai cadran), la liste des badges (`Partial/Badges` réutilisé tel quel),
        et un indice textuel "cliquez n'importe où pour continuer" (pas de bouton de
        fermeture visible). Fermeture au clic n'importe où sur la popup (`site.js`, un
        seul `addEventListener('click', ...)` sur le conteneur englobant, backdrop et
        carte confondus).
      - **Feu d'artifice** — pas de librairie ajoutée (le projet évite les dépendances
        externes quand une solution maison suffit) : un `<canvas>` en fond de popup
        (`z-index` sous la carte, `pointer-events:none`) et ~140 particules rectangles
        colorées (palette du site) projetées depuis le centre de l'écran dans toutes
        les directions avec gravité, boucle `requestAnimationFrame` de 2,6s puis
        nettoyage du canvas. Vérifié par lecture directe des pixels du canvas
        (`getImageData`, non-transparents en cours d'animation, remis à zéro après) —
        le rendu animé lui-même n'est pas capturable par une capture d'écran statique.
      - **Vérifié en direct** : un vrai gain sur un jour passé (`day=20`, réponse
        "Ronaldo") confirme que la popup ne se déclenche **pas** hors du jour courant
        (comportement voulu, inchangé par rapport à l'ancien bandeau).
      - **Bug réel, trouvé par l'utilisateur, raté par la première vérification** — un
        gain du jour courant n'avait pas pu être rejoué avec de vraies données (le
        kikolé du jour était déjà trouvé depuis le début de la session), donc la popup
        avait été "vérifiée" en injectant à la main, via JS, le HTML qu'on *pensait*
        que le serveur produirait — ce qui valide le CSS/JS mais absolument pas le
        rendu Razor réel. Résultat : l'utilisateur teste en vrai et tombe sur
        `await Html.RenderPartialAsync(...)` imprimé tel quel en toutes lettres à la
        place des badges. Cause : cette ligne (statement C# "nu", sans `@`) était
        imbriquée à deux niveaux de `<div>` de profondeur à l'intérieur du bloc
        `@if { }` (`.win-modal` > `.win-modal-box` > ligne nue) ; le suivi implicite
        code/balisage de Razor perd le fil à cette profondeur et traite la ligne comme
        du texte brut plutôt que du C#, alors que le même appel fonctionne très bien
        ailleurs dans le même fichier quand il est un enfant direct du bloc `@if`
        (aucune imbrication supplémentaire). Corrigé en l'enveloppant explicitement
        dans un mini bloc de code `@{ await Html.RenderPartialAsync(...); }`, qui force
        le mode code quelle que soit la profondeur d'imbrication autour.
        **Leçon retenue pour la suite** : ne plus se fier à une injection DOM manuelle
        pour "vérifier" un rendu Razor conditionnel qu'on ne peut pas déclencher pour
        de vrai — soit trouver un moyen de le déclencher réellement (ici : créer un
        compte de test tout neuf via `/Account`, jouer et gagner le jour courant pour
        de vrai), soit dire explicitement à l'utilisateur que ce point précis n'a pas
        été vérifié en conditions réelles plutôt que de présenter une injection DOM
        comme une vérification équivalente. Re-vérifié ensuite avec un compte fraîchement
        créé (`testwinpopup`) : gain réel du jour courant, badges de premier gain
        correctement affichés (plusieurs, la carte scrolle en interne au-delà de
        `max-height:85vh`), fermeture au clic confirmée, page repasse bien à
        l'affichage standard une fois fermée, popup absente d'un rechargement ultérieur.
      Build (0 avertissement) + 596 tests verts.
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
- [x] **Tampon de jours de `kikole_mock.sql` élargi de 7 jours à 6 mois** (2026-09-26) —
      panne reproduite en local ("Une erreur est survenue" sur la page d'accueil,
      silencieuse : `ErrorFilter` avale l'exception sans logguer par défaut, débusquée en
      activant temporairement `LogsFilePathFormat`) : `PlayerService.GetPlayerClueAsync`
      levait `Aucun joueur n'est programmé pour le {date}`, la table `players` ne
      couvrant que jusqu'au 2026-09-15 (dernier rejeu du script le 2026-09-08, tampon de
      `+7 DAY` d'origine — épuisé en un peu plus d'une semaine). Le script étant déjà
      relatif à `CURDATE()` (aucune date en dur), pas besoin de le réécrire : `@last_date`
      simplement passé de `DATE_ADD(CURDATE(), INTERVAL 7 DAY)` à `... INTERVAL 6 MONTH`,
      pour tenir une pause bien plus longue avant de retomber dans le même état. Rejoué
      contre la base locale (`clubs`/`club_translations`/badges intacts, confirmés non
      truncated) : `players` couvre désormais 2026-08-25 → 2027-03-26 (214 jours), page
      d'accueil vérifiée en direct, plus d'erreur.
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
      plutôt que de le laisser à l'état de résidu. À traiter avec les mentions légales
      (identité de l'éditeur et de l'hébergeur, obligation LCEN) et l'information sur les
      données personnelles (IP conservées : `users.ip`, historique de connexion, limitation
      par IP) — le site collecte déjà des données personnelles sans email, donc le RGPD
      s'applique déjà ; choix confirmé de rester **sans email** (2026-09-26).
- [ ] **Refonte du texte de la page d'accueil** (introduction + règles). Demandé
      (2026-09-26) : réorganiser le contenu (présentation / règles) et le rédiger ; le
      texte est à écrire par l'utilisateur avant la mise en page. Aujourd'hui la page non
      connectée empile annonce, règles (`Views/Home/Partial/Rules.cshtml`, gros bloc de
      puces et sections « À propos de la nationalité / des clubs ») et bandeau de connexion,
      et les règles sont répétées sous le jeu une fois connecté.
- [ ] **Ressources externes chargées chez des tiers.** Chaque visiteur envoie son adresse IP
      à Google Fonts (`fonts.googleapis.com`, IBM Plex Sans/Mono, Bebas Neue),
      `code.jquery.com` (jQuery, jQuery UI + thème smoothness), Google Charts
      (`gstatic.com/charts`, page admin Stats) et au CDN Bootstrap (`stackpath`, hors
      Development). Point de vigilance RGPD (décision allemande de 2022 sur Google Fonts) :
      les héberger dans `wwwroot` règle le problème et supprime une dépendance réseau.
      Chantier lié à « Migrer jQuery et jQuery UI » et « Supprimer Bootstrap » ci-dessous (même fichier `_Layout.cshtml`)
      et au footer / mentions légales ci-dessus ; à cadrer ensemble.
- [x] ~~Petits textes (10 à 11,5 px)~~ — **décision : on les conserve** (2026-09-26).
      Étiquettes d'indice, aides sous les champs, coûts, en-têtes de tableau restent à leur
      taille actuelle, malgré l'avis contraire donné à l'utilisateur (malvoyant) qui avait
      proposé de tout passer à 12–13 px minimum, éventuellement avec des boutons A+/A−.
- [x] ~~Comptes PowerUser : spécificités~~ — **décision : aucune.** La récupération de mot
      de passe par question secrète ne protège que les administrateurs
      (`AccountController.IsRecoveryForbidden`, refus indiscernable d'une mauvaise réponse) ;
      les power users restent traités comme des comptes ordinaires.
- [ ] **Migrer jQuery et jQuery UI** (chantier distinct de la suppression de Bootstrap, cf.
      item suivant). Chargés en prod uniquement en CDN, sans fallback ni SRI (`_Layout.cshtml`) :
      jQuery **1.12.4** (2016, ligne 1.x abandonnée) et jQuery UI **1.12.1**. Failles connues
      (numéros de CVE cités de mémoire, à confirmer par un scan OWASP Dependency-Check ou
      retire.js) : jQuery 1.12.4 — XSS AJAX inter-domaines (CVE-2015-9251), pollution de
      prototype `$.extend` (CVE-2019-11358), XSS `.html()`/`.append()` (CVE-2020-11022/11023),
      toutes corrigées en 3.5 ; jQuery UI 1.12.1 — XSS datepicker `altField`/options `*Text`
      et `.position()` (CVE-2021-41182/41183/41184, corrigées en 1.13.0) et `checkboxradio`
      (CVE-2022-31160, corrigée en 1.13.2). **Exploitabilité faible aujourd'hui** : aucune API
      concernée n'est utilisée (recherche de `.html(`, `.append(`, `innerHTML`, `_renderItem`,
      `altField` dans `site.js` et les vues : aucune occurrence, les données JSON des
      classements passent par `createTextNode`), mais un futur `.html()` sur un login ou un nom
      de club serait exploitable. **Cible : jQuery 3.7.1 + jQuery UI 1.13.3** (Bootstrap 3.4.1
      accepte jQuery 3). Périmètre réel modeste : `$.ajax`, `.on`, `.click`, autocomplétion
      (clubs, pays, continents, années), datepickers (accueil + classements) — aucune API
      supprimée entre jQuery 1 et 3 n'est utilisée (`.size()`, `.load()`, `.bind`, `.live`...
      recherchés, rien trouvé). Trois étapes : (1) passer les deux `<script>` + le thème
      jQuery UI dans `_Layout.cshtml`, avec le plugin jQuery Migrate le temps d'un premier
      passage pour repérer les usages obsolètes, puis le retirer ; (2) retester autocomplétion,
      datepickers (les surcharges de `kikole-board.css` visent les classes `ui-state-*`, qui
      restent en 1.13), menu déroulant maison, classements AJAX, popin de victoire, pages
      admin ; (3) idéalement en même temps que l'hébergement local + SRI (item « Ressources
      externes »). Estimation : 1 à 2 h. À nettoyer au passage : `wwwroot/lib/jquery` (3.3.1,
      lui aussi vulnérable), `jquery-validation` et `jquery-validation-unobtrusive`, présents
      sur disque et servis en statique mais jamais chargés par `_Layout.cshtml` (résidus du
      scaffold ASP.NET MVC d'origine).
- [ ] **Supprimer Bootstrap.** Constat : **aucun composant ni classe Bootstrap n'est utilisé
      dans les vues** (recherche de `data-toggle`, `modal`, `tooltip`, `btn-`, `navbar`,
      `col-*`, `glyphicon`, etc. : rien) ; il ne reste que `container body-content`
      (`_Layout.cshtml`) et sa remise à zéro CSS. Son JavaScript (`bootstrap.js`) est chargé
      pour rien, donc ses failles XSS de composants (tooltip/popover/carousel, dont
      CVE-2024-6531, sans correctif : Bootstrap 3 est en fin de vie) ne s'exécutent jamais.
      Autre défaut : CDN en **3.4.1** mais fallback local en **3.3.7** (`wwwroot/lib/bootstrap
      /.bower.json`), deux versions différentes selon le chemin. À faire : (1) **avant tout,
      relever ce que le CSS Bootstrap apporte silencieusement** — `.container` (largeur,
      marges), `box-sizing`, et surtout la base typographique de `body` (taille et
      `line-height`, 1.42857) dont dépend probablement la mise en page de tout le site ;
      (2) reporter l'essentiel dans `kikole-board.css` (une dizaine de lignes) ; (3) retirer
      les `<link>` Bootstrap (Development et CDN), les `<script>` et le fallback de
      `_Layout.cshtml`, puis supprimer `wwwroot/lib/bootstrap` ; (4) retester toutes les
      pages en desktop et mobile (risque principal : régressions d'interlignes ou de
      largeur). Règle une partie de l'item « Ressources externes » (un CDN de moins) et
      supprime définitivement la question d'un passage à Bootstrap 5.- [x] **Fusionné `Statistics/KikolesStats` dans `Statistics/Stats`**, en 3ème bloc
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
- [x] **`HomeController`/`LeaderboardController`/le reste d'`AdminController` sortis du
      "hors périmètre" test acté plus haut** (le motif invoqué à l'époque —
      `SignInManager<ApplicationUser>` ne peut pas se mocker via une interface — a une
      solution standard restée non essayée jusqu'ici). Nouveau `IdentityMocks.cs`
      (`KikoleSiteUnitTests/Controllers/`) : `UserManager<TUser>`/`SignInManager<TUser>`
      sont des classes concrètes, mais la quasi-totalité de leurs membres sont `virtual` —
      Moq peut donc les mocker en leur fournissant des dépendances bouchon pour satisfaire
      le constructeur, sans jamais réellement appeler leurs méthodes (aucune action testée
      de `HomeController` ne passe par `_signInManager`, seul `Error()` l'utilise). 22
      nouveaux tests, en filet de régression avant le chantier de parallélisation ci-dessous
      (les scénarios fixent le comportement observable actuel, pas l'ordre séquentiel/
      parallèle des appels) :
      - `HomeControllerTests.cs` (10) : `Index` GET (visiteur anonyme, connecté non-créateur
        pas encore trouvé, créateur, joueur trouvé via proposition — ce dernier exerce à la
        fois la branche "proposals" et le bloc final, les deux endroits qui rappellent
        `countries`/`continents`) et `Index` POST (modèle nul, action de soumission
        illisible, valeur invalide donc réentrance dans le GET, proposition valide gagnante/
        perdante, abandon "Give up").
      - `LeaderboardControllerTests.cs` (10) : `UserDay` (chemin nominal + 5 gardes d'accès
        qui redirigent), `GetGlobalLeaderboardDetailsAsync`/`GetDailyLeaderboardDetailsAsync`
        (dont le cas masqué faute de droit sur le jour), `Index` sans `userId` (assemble
        dayboard/classement/podiums via `InitializeModelAsync`).
      - `AdminControllerTests.cs` (2) : `PlayerSubmission` (mappage pays/continent) et le
        garde "plus rien à valider" partagé par `AcceptPlayer`/`RefusePlayer`/`ChoosePlayer`.
      À l'époque laissé hors périmètre : `AccountController` (Identity y est appelé pour de
      vrai sur presque chaque action), les branches d'`AdminController`/`HomeController`/
      `LeaderboardController` non retouchées par le chantier de parallélisation, et
      `StatisticsController`. **Comblé juste après** (demande explicite de l'utilisateur,
      "ce qui reste côté contrôleur") — l'hypothèse de départ sur `AccountController` était
      fausse : `UserManager`/`SignInManager` se mockent exactement comme n'importe quelle
      dépendance (`.Setup(...)`/`.Verify(...)` sur leurs membres `virtual`), rien de
      spécifique à Identity ne bloquait ces tests. 47 tests supplémentaires :
      - `AccountControllerTests.cs` (18) : un scénario heureux + un ou deux échecs par
        action (`LogIn`, `GetLoginQuestion`, `ResetPassword`, `ResetQAndA`, `Create`,
        `ChangePassword`, `LogOut`, `Index`). Piège rencontré et corrigé : `SignInResult`
        existe à la fois dans `Microsoft.AspNetCore.Identity` et `Microsoft.AspNetCore.Mvc`
        (alias `using` nécessaire) ; le login automatique après `Create` interroge
        `FindByNameAsync` une seconde fois avec le même login que la vérification
        "existe déjà" — `SetupSequence` plutôt que `Setup` pour distinguer les deux appels.
      - `StatisticsControllerTests.cs` (4) : les 4 actions ne font que mettre en forme le
        retour du service en JSON (la logique reste testée dans `StatisticServiceTests`) —
        vérifié via `dynamic` sur `JsonResult.Value` (les types anonymes du contrôleur sont
        accessibles depuis les tests grâce à l'`InternalsVisibleTo` déjà en place).
      - `LeaderboardControllerTests.cs` : +2 (`Index` avec `userId` — statistiques
        introuvables → modèle par défaut, utilisateur connu → `UserStatsModel`).
      - `AdminControllerTests.cs` : +20 (`Actions`/`RecomputeBadges`/`RecomputeLeaders`/
        `ReassignPlayers`/`InsertMessage`, `Discussions`/`Discussion` GET+POST, création de
        joueur POST (nom manquant + soumission minimale valide), `Club` GET+POST (dont le
        refus non-admin), `PlayerEdit` GET+POST).
      - `HomeControllerTests.cs` : +7 (`Contact` GET+POST, `Error`, `ErrorIndex`,
        `SwitchLang`).
      Seuls restent hors périmètre, décision actée plus haut et inchangée : les dépôts.
      `dotnet test` : 674 tests verts (601 avant ce chantier de tests de contrôleurs).
- [x] **Parallélisation d'appels service/repo independants, repérés en auditant les
      contrôleurs à la demande de l'utilisateur** (juste après l'ajout du "streak" ci-dessus).
      Vérifié au préalable : `BaseRepository` ouvre une connexion MySQL neuve à chaque
      appel (pas de connexion/contexte partagé façon EF), donc paralléliser des appels
      repo indépendants est sans risque de ce côté. Les 6 candidats sûrs, tous implémentés
      selon le même patron déjà en place dans `LeaderboardController.InitializeModelAsync`
      (démarrer les `Task` avant le premier `await`, puis les attendre dans l'ordre où le
      résultat est utilisé — pas de `Task.WhenAll`, juste des variables de tâche) :
      - `HomeController.Index` (POST) : `pInfo`/`countryContinents`, appelés après toutes
        les validations donc sans travail gâché possible.
      - `HomeController.SetAndGetViewModelAsync` : `playerCreator`/`clue`/`easyClue`/
        `Streak` (ce dernier ne dépendait de rien d'autre dans la méthode — sorti de sa
        position d'origine, en toute fin, pour partir en même temps que les trois
        précédents) ; puis, dans la branche "pas créateur", `proposals`/`countries`/
        `continents`/`clubs`. Au passage, le vrai bug corrigé (pas qu'une histoire de
        parallélisme) : `countries`/`continents` étaient **rappelés une seconde fois** plus
        loin dans la même méthode (bloc "joueur trouvé") alors qu'`IInternationalService`
        ne cache rien — les deux variables sont maintenant déclarées en tête de méthode
        (`null` par défaut), remplies par la branche "proposals" si elle s'exécute, sinon
        calculées paresseusement (`??=`) par le bloc final : un seul aller-retour DB dans
        tous les cas, jamais deux.
      - `LeaderboardController.UserDay` : `db` (`GetDayboardAsync`)/`proposals`
        (`GetProposalsAsync`), une fois `countryContinents` résolu et toutes les gardes
        d'accès passées.
      - `AdminController.GetPlayerSubmissionsList` : `countries`/`continents` démarrés
        avant la chaîne séquentielle `countryContinents` → `pls` (qui, elle, reste
        obligatoirement séquentielle).
      - `LeaderboardController.GetDailyboardAsync` (privée) : `todayGrantEnsured` et
        `EnsureDateAsync(date, DayGrantTypes.Found)` — ce dernier ne lisant jamais
        `todayGrant` (valeur fixe passée en argument), les deux sont indépendants ;
        `todayGrant` déjà connu (non `null`) est enveloppé dans `Task.FromResult` pour
        garder la même structure "deux tâches démarrées ensemble" sans appel réseau en trop.
      - `LeaderboardController.GetLeaderboardAsync` (privée) : les deux `EnsureDateAsync`
        (min/max), indépendants entre eux.
      **Vérifié** : `dotnet build` (0 avertissement), `dotnet test` (623 tests toujours
      verts, y compris les 22 nouveaux du point précédent — écrits justement pour ça, sans
      aucune modification nécessaire côté tests) ; en direct dans le navigateur (`joueur1`) :
      page d'accueil (bandeau série + cadran corrects), achat du classement du jour en POST
      (`-25 pts`, aucune erreur serveur), page `/Leaderboard` (tableau du jour, classement
      général, podiums — tous alimentés par les méthodes retouchées) — aucune erreur dans
      les logs serveur sur l'ensemble de la session.
      Étudié et volontairement écarté (compromis réel, pas juste "pas encore fait") :
      les badges après une victoire (`PrepareNewLeaderBadgesAsync`/
      `PrepareNonLeaderBadgesAsync`, `HomeController.Index` POST) — `Badges.Dedicated`
      n'est aujourd'hui jamais inséré par les deux méthodes à la fois, mais
      `BadgeService.InsertBadgeIfNotAlreadyAsync` fait un check-then-insert sans contrainte
      d'unicité en base (`user_id`, `badge_id`) : paralléliser recréerait une vraie race
      condition le jour où un badge finirait par être atteignable par les deux chemins ; la
      chaîne de gardes de `UserDay` (`user`/`canSee`/`player`, chacune pouvant interrompre
      la requête) ; `AdminController.Index(PlayerCreationModel)` POST, où `clubsReferential`
      n'est chargé qu'après validation du formulaire (paresse volontaire, cas fréquent de
      re-soumission après une faute de frappe) ; `AccountController.Create`, où
      `FindByNameAsync` est court-circuité si déjà rate-limité.

**Volontairement en dernier :** le seul poste qui ne bloque rien et ne se déprécie pas.

- [x] **Mise en valeur de la série en cours ("streak"), première passe.** Affichage
      permanent à gauche du cadran de points (`.masthead-score`, `Views/Home/Index.cshtml`) :
      pictogramme (flèche montante style "trending-up") + deux lignes de texte, "Série en
      cours : X jours" et "Record : X jours" (cette dernière seulement si différente de la
      série en cours) ; rien ne s'affiche avant la toute première victoire à temps de
      l'utilisateur (`UserStreak.HasStreak`). Calcul volontairement indépendant de la
      logique de badges (`RespectLeadersRunConditionsInternal`, pensée pour verifier un
      palier fixe autour d'une victoire donnée, pas pour calculer une série glissante) :
      nouvelle méthode `ILeaderService.GetUserStreakAsync` (`LeaderService.cs`), qui
      s'appuie sur `ILeaderRepository.GetUserLeadersAsync(FirstDate, Today, onTimeOnly:
      true, userId)` (déjà existante) — `onTimeOnly: true` car seul un kikolé trouvé le
      jour même compte pour la série, un rattrapage tardif ne doit pas la faire semblant de
      continuer. Règle retenue pour "série en cours" : le jour courant, tant qu'il n'est pas
      encore joué, ne casse pas une série arrêtée hier (comme Duolingo) ; tout vrai trou
      dans le passé la casse. "Record" = plus longue série jamais réalisée (inclut la série
      en cours si c'est elle la plus longue). Nouveau modèle `UserStreak` (`Models/`).
      Passe volontairement simple, sans les jours de création de kikolé (contrairement à
      certains badges type `creatorIncludeInRun`) : à affiner si l'utilisateur le demande
      une fois le premier rendu vu en usage réel. 5 tests unitaires ajoutés
      (`LeaderServiceTests.cs`, région `GetUserStreakAsync`) : aucune victoire jamais,
      série continue jusqu'à aujourd'hui, jour courant pas encore joué (ne casse pas),
      trou dans le passé (casse), record différent de la série en cours. Vérifié en direct
      (`joueur1`, CSS `kikole-board.css?v=17`).

---

## 5. Portabilité (démo sans WAMP)

- [ ] **Rendre le dépôt auto-portant pour une démo** — besoin exprimé par l'utilisateur :
      pouvoir `git pull` + Visual Studio sur un poste où WAMP est impossible à installer
      (poste de bureau sans droits admin) et faire tourner le site sans étape d'install.
      **Pas commencé, pas urgent — item posé pour une session future.**
      - **Piste retenue, discutée avec l'utilisateur** : une distribution **MySQL
        "no-install"** (l'archive ZIP officielle du serveur, pas l'installeur MSI/WAMP) —
        se dézippe n'importe où sans droits admin, `mysqld.exe` se lance directement avec
        un dossier de données dédié au repo. Choisie explicitement **pour ne toucher à
        aucun code** : le connecteur (`MySqlConnector`), le schéma et tout le SQL brut des
        repositories (Dapper, pas un ORM abstrayant le dialecte) restent identiques —
        seule la façon de démarrer le serveur change. Scénario visé : un script (`.ps1`)
        qui télécharge/dézippe/démarre `mysqld` puis rejoue `kikole.sql` + `kikole_mock.sql`
        contre lui, pour un "clone → un script → F5".
      - **Piste explicitement écartée par l'utilisateur** : basculer sur SQLite (ou un
        autre moteur embarqué) pour un site 100% sans serveur — plus "auto-portant" dans
        l'absolu, mais implique de réécrire une partie du SQL brut des repositories
        (`TRUNCATE`, `AUTO_INCREMENT`, spécificités MySQL), ce que l'utilisateur ne veut
        pas faire pour ce seul besoin de démo.
      - **Reste à faire le jour où ce chantier démarre** : choisir/figer une version MySQL
        "no-install" (cohérente avec la 9.1 utilisée en local, cf. tableau en tête de
        fichier), écrire le script de bootstrap (téléchargement, port dédié pour ne pas
        entrer en conflit avec un WAMP existant sur d'autres postes, dossier de données
        sous le repo ou dans un chemin utilisateur, rejeu des deux scripts SQL), et
        vérifier la chaîne de connexion / user-secrets nécessaires (cf. section
        « Partis pris » plus bas) sur un poste réellement dépourvu de WAMP.

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
- **Projet dédié (`KikoleSiteIntegrationTests`), pas le trait `[Trait("Category","Integration")]`
  dans `KikoleSiteUnitTests`.** Revenu sur la décision initiale (ci-dessous, barrée) à la
  demande explicite de l'utilisateur : séparation complète plutôt qu'une distinction par
  trait au sein du même projet. Les 7 fichiers (`DatabaseFixture`/`DatabaseCollection` +
  les 5 classes de tests) déplacés tels quels, seul le namespace change
  (`KikoleSiteUnitTests.Integration` → `KikoleSiteIntegrationTests.Integration`). Les 4
  builders de DTO utilisés (`PlayerDtoBuilder`/`UserDtoBuilder`/`LeaderDtoBuilder`/
  `ProposalDtoBuilder`) sont **copiés** dans un `Builders/DtoBuilders.cs` propre au nouveau
  projet plutôt que partagés par référence de projet — chaque projet de tests reste
  autonome, sans dépendre de l'autre ; les builders inutilisés ici (`ClubDtoBuilder`,
  `BadgeDtoBuilder`, etc., restés dans `KikoleSiteUnitTests`) n'ont pas été copiés, pour ne
  pas trimballer du code mort. `dotnet test` sans filtre à la racine construit maintenant
  les deux projets côte à côte (visible dans la sortie, chacun avec son propre résumé) ;
  `KikoleSiteUnitTests` seul (`dotnet test KikoleSiteUnitTests/...`) ne dépend plus de WAMP
  du tout, plus besoin du `--filter Category!=Integration` d'avant (le trait a disparu avec
  le déplacement, il n'y a plus rien à filtrer dans ce projet).
- ~~Même projet (`KikoleSiteUnitTests/Integration/`), pas un projet dédié.~~ Décision
  initiale, abandonnée ci-dessus. Le raisonnement de l'époque (un `.csproj` séparé aurait
  ajouté du wiring de solution pour une distinction que le trait suffisait à faire) reste
  vrai en soi, mais la préférence de l'utilisateur pour une séparation complète l'a emporté.
- **`UserSecretsId` propre à `KikoleSiteIntegrationTests`** (nouveau GUID, distinct de
  l'ancien `UserSecretsId` de `KikoleSiteUnitTests` qui a disparu avec le déplacement) —
  chaîne de connexion recopiée depuis l'ancien emplacement
  (`%APPDATA%\Microsoft\UserSecrets\<ancien-id>\secrets.json` → `<nouveau-id>\secrets.json`)
  pour que le nouveau projet fonctionne sans reconfiguration manuelle ; l'ancien dossier de
  secrets n'a pas été supprimé (orphelin inoffensif, à nettoyer à l'occasion si souhaité).
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
