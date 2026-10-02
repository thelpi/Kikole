# Kikolé — feuille de route « remaster v2 »

Reprise du projet abandonné en mai 2023. `[x]` traité, `[ ]` à faire.

## État

- Base locale MySQL 9.1 (WAMP), rejouable via `kikole_mock.sql` ; schéma dans `kikole.sql`
- .NET 10, Dapper sur MySqlConnector, nullables activées, zéro avertissement
- Tests : unitaires mockés (`KikoleSiteUnitTests`) + intégration sur vraie base (`KikoleSiteIntegrationTests`)
- Authentification : ASP.NET Core Identity, store Dapper maison
- Base de production 2023 : extraite en texte dans `Restauration/`

---

## 1. Sécurité et comptes

- [x] Cookie d'authentification falsifiable → cookie Identity chiffré
- [x] Mots de passe SHA256 → PBKDF2 (migration au premier login)
- [x] `SHA256` partagé sur un singleton → supprimé
- [x] Secrets hors dépôt (user-secrets)
- [x] Invitation désactivée par config (`Registration:InviteEnabled`)
- [x] Anti-multi-compte : IP à l'inscription, `login_history`, rate limiting
  - [ ] Renseigner `ForwardedProxy` une fois l'hébergement choisi
  - [ ] Vue admin des IP partagées
- [x] Inscription par email (confirmation, mot de passe oublié, changement d'email, domaines jetables bloqués)
- [x] Mentions légales / footer
- [x] Parrainage (inscription, page Compte, badges, désactivable par config)
  - [ ] Activation : flag `Registration:SponsorshipEnabled` ET `UPDATE badges SET is_disabled = 0 WHERE id IN (32, 33);`
  - [ ] Notifier le parrain d'un badge de parrainage
  - [ ] Revoir le décompte des filleuls désactivés
- [ ] Outillage admin des comptes
  - [ ] Plafond de clubs créés par un `PowerUser`
  - [ ] Plafond de kikolés proposés par un `PowerUser`
  - [ ] Changer le palier d'un utilisateur
  - [ ] Désactiver un compte
  - [ ] Forcer un mot de passe
  - (à regrouper dans un écran « gestion des utilisateurs »)

## 2. Modèle de données et contenu

- [x] Clubs canoniques et traduits (`club_translations`), proposition par ID
- [x] Base de clubs : 1702 clubs (Europe complète, grandes nations Afrique/Asie, Amérique) + migration locale
  - Échelons inférieurs hors Europe : arrêté volontairement
- [x] Pays au sens FIFA, nationalité double (`alternative_country_id`), continent calculé (non stocké)
- [x] Postes multiples (`alternative_position_id`)
- [x] Page de règles réécrite
- [x] Clés étrangères (29)
- [x] Indices audio/vidéo/image, médias locaux, upload admin
- [x] Date de publication forcée (admin) avec décalage en cascade
- [x] `IClubService` : non nécessaire

## 3. Qualité, badges, classements

- [x] Requêtes N+1 corrigées
- [x] Logique métier des dépôts caractérisée par des tests d'intégration
- [x] Audit des zones non testées (services, contrôleurs, badges)
- [x] `DateOnly` pour les jours calendaires (avec `DateOnlyTypeHandler` pour Dapper)
- [x] Classement général par % de badges (+ rareté moyenne)
- [x] Présentation sortie des modèles (`LeaderboardRow`, `DayboardModel`)
- [x] Statistiques réservées à l'administrateur
- [x] Modernisation syntaxique (records, namespaces fichier, `required`)
- [x] Badges
  - [x] Nouveaux : Down to the wire, The end?, Coupe des confédérations, OK Zoomer
  - [x] Phoenix créé puis désactivé
  - [x] Noms FR des badges
  - [x] `FourFourtwo` / `AroundTheWorld` : postes et nationalités doubles (affectation)
  - [x] Badges d'historique et d'année de naissance : kikolés trouvés à l'heure uniquement
  - [x] We are kikolé à 3 kikolés
  - [x] `badges.is_disabled` (suppression virtuelle) : Phoenix, Don Corleone, The Famous Five désactivés
  - [x] Badges du jour des autres : visibles seulement si on connaît la réponse (fuite `PaidBoard` corrigée)
  - [x] Recalcul global complet (Dedicated, Do it yourself, We are kikolé via `players.acceptance_date`)
- [ ] Reprendre la règle de Phoenix, puis le réactiver
- [ ] Idée de badge « Grand Chelem » à préciser
- [ ] Migrations à rejouer sur toute base existante (pas sur une base créée depuis `kikole.sql`)
  - [ ] `badge_translations.name` (+ `UPDATE` depuis `badges.name`)
  - [ ] `badges.is_disabled` (+ badges 34, 35, `UPDATE` des ids 30, 32, 33)
  - [ ] `players.acceptance_date` (+ `UPDATE ... = creation_date` pour les joueurs publiés)
- [ ] Textes localisés encore produits dans le domaine (`ProposalResponse.Tip`, `GetTip`, `ScoreCalculator`, `IsValid(IStringLocalizer)`)
- [x] `Dayboard` : propriétés mortes de taux de réussite supprimées
- [x] `StatisticRepository.UserPlayerLinkSql` supprimé (stats réservées à l'admin)
- [ ] Évaluer la suppression de Dapper

## 4. Interface

- [x] Refonte graphique de toutes les pages
- [x] Popup de victoire, série en cours (streak), surbrillance de l'utilisateur
- [x] Navigation jour par jour, datepicker, résultat final, palmarès → podium
- [x] Pages Compte / Admin éclatées en une action par formulaire
- [x] Page Contact avec échange dans le site
- [x] Indices en images
- [x] jQuery / jQuery UI et polices auto-hébergés ; Bootstrap supprimé
  - Google Charts laissé en CDN (page admin seule)
- [x] Retouches issues des relectures (menu, textes, accessibilité, formulaires)
- [x] Petits textes conservés à leur taille (décision)
- [x] Comptes PowerUser : aucune spécificité (décision)
- [x] Parallélisations d'appels, contrôleurs allégés, routes statistiques dédiées

## 5. Portabilité

- [ ] Dépôt auto-portant pour une démo sans WAMP (MySQL « no-install » + script de bootstrap)

## Base de production 2023

- [x] Contenu textuel extrait dans `Restauration/`
- [ ] Restauration structurée (exige MySQL 5.7, `IMPORT TABLESPACE`) : possible, non engagée

---

## Partis pris

**Schéma**
- `utf8mb4_unicode_ci` ; `ascii_bin` pour les hash, `ascii_general_ci` pour GUID et IP
- `countries` en codes FIFA à 3 lettres (211 fédérations + 4 nations disparues), ids stables
- Le continent se déduit du pays, jamais stocké
- Clés étrangères en `RESTRICT` par défaut (l'application ne supprime rien)
- Clubs traduits par langue, `priority = 0` = nom canonique

**Règles de jeu**
- Un seul pays et un seul poste alternatifs (pas de liste)
- Un joueur par jour : son absence lève une exception ; l'application refuse de démarrer sans joueur
- Calendrier déduit de `MIN(publication_date)`
- Soumission d'un kikolé : 1 000 points forfaitaires
- Un mois sans podium complet ne rapporte aucune médaille
- Badges : un kikolé trouvé en rattrapage ne fait avancer aucun badge (sauf The End et badge rattaché à un joueur)
- Badge désactivé (`is_disabled`) = supprimé virtuellement, lignes `user_badges` conservées
- Rareté d'un badge = proportion des autres joueurs qui ne l'ont pas

**Architecture**
- Un service ne dépend jamais d'un autre service ; `countryContinents` passé en paramètre par les contrôleurs
- `ScoreCalculator` sans dépendance ni I/O
- `IInternationalService` : singleton à cache explicite, langue passée en paramètre
- Identity avec store Dapper maison, palier utilisateur porté par une claim
- Mot de passe : 10 caractères minimum, pas de règle de composition, contrôle HIBP
- Invitation et parrainage désactivables par config (`IOptions<T>`)
- Rate limiting maison, `ForwardedHeaders` préparé non activé

**Code**
- DTO et requêtes en `record` / `init`, `required` plutôt que `null!`
- ViewModels mutables
- Signatures de dépôt nullables
- `RemoveDiacritics` garde le passage par ISO-8859-8

**Tests**
- Projet d'intégration séparé ; `DatabaseFixture` rejoue `kikole_mock.sql` avant chaque run
