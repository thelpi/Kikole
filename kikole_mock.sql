-- Jeu de donnees de developpement pour Kikole.
-- A jouer APRES kikole.sql. Rejouable a l'infini : le script vide d'abord toutes les
-- donnees applicatives (voir plus bas) sans toucher aux donnees de reference
-- (badges, pays, continents, langues, positions, types de proposition, types
-- d'utilisateur, clubs/club_translations - le catalogue complet vit dans kikole.sql,
-- ce script ne fait plus qu'y referencer des id existants, cf. joueurs plus bas).
--
-- Les mots de passe sont hashes en SHA256(motdepasse + EncryptionKey), ou EncryptionKey
-- est la valeur des user-secrets ("KikoleDevSalt2026") : c'est l'ancien format, delibere.
-- Ca sert de fixture pour verifier le rehash automatique vers PBKDF2 a la premiere
-- connexion (LegacyCompatiblePasswordHasher). Changer cette cle invalide les comptes
-- ci-dessous.
--
-- Comptes :
--   admin   / admin12345      (administrateur)
--   joueur1 / NouveauMdp1234  (utilisateur standard)
--   joueur2 / test123         (utilisateur standard)
--   lea, hugo, emma, lucas, chloe, nathan, manon, theo / test123 (utilisateurs standard,
--   ids 4 a 11, ils alimentent les classements et podiums, cf. fin du script)
--   question de recuperation : reponse "kikole" pour tous
--
-- Joueurs du jour : generes de FirstDate a aujourd'hui + 6 mois (voir plus bas).
--
-- Indices media locaux (wwwroot/media/clues/, cf. AdminController.UploadClueMedia) : un
-- exemple de chaque type pour verifier le rendu <img>/<audio controls>/<video controls>
-- (Home/Index.cshtml). Image generee a la main (cercle vert sur fond sombre, memes
-- couleurs que kikole-board.css) ; audio = "Beep of a Cash Register #1" (bigsoundbank.com,
-- CC0) ; video = extrait de 10s de Big Buck Bunny (Blender Foundation, CC BY 3.0), via le
-- paquet npm "sample-files" (cseitz/sample-files). Le clue facile de chacun de ces trois
-- kikolés reste du texte, volontairement, pour illustrer aussi le cas mixte.
-- Fixes sur des jours a decalage constant depuis @first_date (ids 3/4, cf. plus bas) plutot
-- que dans le pool tournant : un index du pool retombe forcement sur "aujourd'hui" ou le
-- futur selon la date de rejeu du script, ce qui empeche de tester le jour courant
-- normalement. @first_date etant toujours "aujourd'hui - 4 mois", ces jours restent
-- toujours dans le passe quel que soit le moment ou le script est rejoue.

SET NAMES utf8mb4;
USE kikole;

-- ---------------------------------------------------------------- remise a zero
-- Les compteurs AUTO_INCREMENT sont remis a 1 pour que deux executions successives
-- produisent exactement la meme base. TRUNCATE n'est pas utilisable : les cles etrangeres
-- ajoutees a la fin de kikole.sql l'interdisent sur une table referencee (players,
-- users), meme vide, des que FOREIGN_KEY_CHECKS n'est pas a 0 dans la session.
-- clubs/club_translations ne sont PLUS truncated : le catalogue complet (490+ clubs,
-- sourced pays par pays) vit dans kikole.sql et doit survivre aux rejeux de ce script,
-- voir le commentaire au-dessus des joueurs plus bas pour les id references ici.

-- DELETE dans l'ordre des dependances (tables filles d'abord), puis remise a 1 des
-- compteurs : TRUNCATE etait refuse (#1701) des que FOREIGN_KEY_CHECKS n'etait pas
-- effectivement a 0 dans la session (import par certains outils web, qui ne conservent pas
-- le SET d'une instruction a l'autre). Ainsi le script marche avec ou sans ce reglage.
SET FOREIGN_KEY_CHECKS = 0;
DELETE FROM proposals;
DELETE FROM leaders;
DELETE FROM user_badges;
DELETE FROM player_clue_translations;
DELETE FROM player_clubs;
DELETE FROM discussion_messages;
DELETE FROM discussions;
DELETE FROM messages;
DELETE FROM registration_guids;
DELETE FROM login_history;
DELETE FROM players;
DELETE FROM users;
SET FOREIGN_KEY_CHECKS = 1;

ALTER TABLE proposals AUTO_INCREMENT = 1;
ALTER TABLE leaders AUTO_INCREMENT = 1;
ALTER TABLE user_badges AUTO_INCREMENT = 1;
ALTER TABLE discussion_messages AUTO_INCREMENT = 1;
ALTER TABLE discussions AUTO_INCREMENT = 1;
ALTER TABLE messages AUTO_INCREMENT = 1;
ALTER TABLE login_history AUTO_INCREMENT = 1;
ALTER TABLE players AUTO_INCREMENT = 1;
ALTER TABLE users AUTO_INCREMENT = 1;

-- ---------------------------------------------------------------- utilisateurs

-- email_encrypted/email_hash precalcules avec la cle de user-secret locale
-- "EmailEncryptionKey" = "KikoleDevEmailKey2026" (meme principe que "EncryptionKey" =
-- "KikoleDevSalt2026" pour les mots de passe ci-dessus) : ne fonctionnent que si ce
-- secret est bien celui configure en local, sinon la connexion par email de ces comptes
-- de demonstration echouera (la connexion par identifiant n'est pas affectee).
INSERT INTO users (id, login, normalized_login, password, email_encrypted, email_hash, email_confirmed, language_id, user_type_id, is_disabled, concurrency_stamp, security_stamp, ip, creation_date) VALUES
(1, 'admin',   'ADMIN',   '0834071d6bb6ffc7e16b4d6f620c181b4d5b654294eeb94931220c8679e009a1', 'JPoJ00AIxU3YOqBDD47/MEMYEPLscT6tSwBB1qgWHbqGyar9h7BXVVBl3a98', '096aded95520f99a58ecb39b1b53507421464ad2171bac68d90de05f37e584e0', 1, 2, 3, 0, UUID(), UUID(), '127.0.0.1', '2026-09-01 09:00:00'),
(2, 'joueur1', 'JOUEUR1', 'd855fce1f2803c3a3406f05d2bad9db4df589a5fdf047dcedc7bc8bc29e898ba', 'oqHNFcdVc8PX1l+wrt+yUYLgyzKif9xaUofZbrywOfqhWDibIussV60BJBQlGmo=', '6e6ee8ef82512199647175d5c7ea7f9d75cefc07d00dea054391e0dad5fa0ff5', 1, 2, 1, 0, UUID(), UUID(), '127.0.0.1', '2026-09-01 09:05:00'),
(3, 'joueur2', 'JOUEUR2', '2ed58959eef5c40f2bef10b524f1ddab9d7367fe215fa5ac968d332767c46150', '9L/azw+825JCHU/BRPCtt3nblqmz9kEmyIf3OJlgp4DduVNDEbUMZ6N1V7lDB1Q=', '8254001b863c5eebd3ceed427c9dc363644687898d2ee696bafc290e11918e5b', 1, 1, 1, 0, UUID(), UUID(), '127.0.0.1', '2026-09-01 09:10:00');

-- un GUID libre pour tester le parcours d'inscription
INSERT INTO registration_guids (id, user_id, creation_date) VALUES
('11111111-2222-3333-4444-555555555555', NULL, '2026-09-01 09:00:00');

-- ---------------------------------------------------------------- clubs referencees
-- Ce script ne definit plus aucun club (cf. remise a zero plus haut) : les joueurs
-- ci-dessous pointent vers des id du catalogue complet de kikole.sql. Table de
-- correspondance (ancien id local de ce script -> id reel, pour retrouver un club en
-- cas de besoin) : 1 AS Cannes->70, 2 Girondins de Bordeaux->37, 3 Juventus->94,
-- 4 Real Madrid->189, 5 Brescia->89, 6 Inter->93, 7 Milan AC->85,
-- 9 FC Barcelone->179, 10 Paris Saint-Germain->13, 11 Manchester United->268,
-- 12 Bayern Munich->223. (l'ancien id 8, New York City FC, n'a pas d'equivalent dans
-- le catalogue sourcing pays par pays - retire plutot que rapatrie, la carriere
-- mockee d'Andrea Pirlo s'arrete a la Juventus, pas grave que ce soit incomplet)


-- ---------------------------------------------------------------- joueurs du jour
--
-- Les journees sont generees de FirstDate jusqu'a aujourd'hui + 6 mois, pour que
-- l'environnement local reste valable dans le temps meme apres une longue pause sans
-- rejouer ce script : sans ca, passe la derniere journee generee, il n'y a plus de
-- joueur du jour et l'application tombe en erreur (verifie en pratique : le tampon de
-- 7 jours d'origine ne tenait qu'une grosse semaine avant de tomber en panne).
--
-- @first_date n'a plus a correspondre a quoi que ce soit dans le code : l'application
-- deduit son calendrier du MIN(publication_date), qui est la journee cachee inseree
-- juste en dessous. On part donc d'aujourd'hui moins QUATRE mois, pour avoir un historique
-- assez long pour plusieurs podiums mensuels (cf. joueurs fictifs en fin de script).
--
-- Un pool de 8 joueurs est parcouru en boucle ; l'identifiant vaut l'indice du jour + 2,
-- ce qui rend les insertions dependantes deterministes (carrieres, traductions).

SET @first_date = DATE_SUB(CURDATE(), INTERVAL 4 MONTH);
SET @last_date = DATE_ADD(CURDATE(), INTERVAL 6 MONTH);
SET @pool_size = 8;
SET SESSION cte_max_recursion_depth = 10000;

-- journee cachee (FirstDate - 1) - clue en image locale (cf. note plus haut)
INSERT INTO players (id, name, allowed_names, year_of_birth, country_id, publication_date, clue, easy_clue, position_id, badge_id, creation_user_id, creation_date, reject_date, hide_creator) VALUES
(1, 'Andrea Pirlo', 'pirlo;andrea pirlo', 1979, 111, DATE_SUB(@first_date, INTERVAL 1 DAY),
 '/media/clues/mock-clue-image.png', 'He won the 2006 World Cup with Italy.',
 3, NULL, 1, '2026-09-01 09:00:00', NULL, 0);

INSERT INTO player_clubs (player_id, club_id, history_position, is_loan) VALUES
(1, 89, 1, 0), (1, 93, 2, 0), (1, 85, 3, 0), (1, 94, 4, 0);

INSERT INTO player_clue_translations (player_id, language_id, is_easy, clue) VALUES
(1, 1, 0, '/media/clues/mock-clue-image.png'),
(1, 1, 1, 'He won the 2006 World Cup with Italy.'),
(1, 2, 0, '/media/clues/mock-clue-image.png'),
(1, 2, 1, 'Champion du monde 2006 avec l''Italie.');

-- ---------------------------------------------------------------- pool de joueurs

DROP TABLE IF EXISTS mock_pool;
CREATE TABLE mock_pool (
  p tinyint NOT NULL PRIMARY KEY,
  name varchar(255) NOT NULL,
  allowed_names varchar(255) NOT NULL,
  year_of_birth smallint NOT NULL,
  country_id bigint NOT NULL,
  position_id bigint NOT NULL,
  club1 bigint NOT NULL,
  club2 bigint NOT NULL,
  clue_en varchar(255) NOT NULL,
  easy_en varchar(255) NOT NULL,
  clue_fr varchar(255) NOT NULL,
  easy_fr varchar(255) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO mock_pool VALUES
(0, 'Zinédine Zidane', 'zidane;zizou;zinedine zidane', 1972, 77, 3, 37, 189,
 'He scored twice with his head in a World Cup final.', 'His last professional match ended with a red card.',
 'Deux buts de la tête en finale de Coupe du monde.', 'Son dernier match professionnel s''est terminé par un carton rouge.'),
(1, 'Ronaldinho', 'ronaldinho;ronaldinho gaucho', 1980, 32, 4, 13, 179,
 'He made an entire opposing stadium applaud him.', 'Famous for his smile and his elastico.',
 'Il a fait applaudir un stade adverse tout entier.', 'Célèbre pour son sourire et son elastico.'),
(2, 'David Beckham', 'beckham;david beckham', 1975, 235, 3, 268, 189,
 'His right foot made him famous well beyond football.', 'A film bears his name.',
 'Son pied droit l''a rendu célèbre bien au-delà du football.', 'Un film porte son nom.'),
(3, 'Ronaldo', 'ronaldo;ronaldo nazario;el fenomeno', 1976, 32, 4, 93, 189,
 'Top scorer of the 2002 World Cup.', 'Nicknamed "the phenomenon".',
 'Meilleur buteur de la Coupe du monde 2002.', 'Surnommé « le phénomène ».'),
(4, 'Thierry Henry', 'henry;thierry henry;titi', 1977, 77, 4, 94, 179,
 'France''s all-time top scorer for many years.', 'A statue of him stands outside a London stadium.',
 'Meilleur buteur de l''équipe de France pendant des années.', 'Une statue de lui trône devant un stade londonien.'),
(5, 'Franck Ribéry', 'ribery;franck ribery', 1983, 77, 3, 223, 94,
 'A scar marks his face since childhood.', 'He spent a decade in Bavaria.',
 'Une cicatrice marque son visage depuis l''enfance.', 'Il a passé une décennie en Bavière.'),
(6, 'Patrick Vieira', 'vieira;patrick vieira', 1976, 77, 3, 94, 93,
 'A towering midfielder, World Cup winner at home.', 'He later became a manager.',
 'Un milieu de terrain imposant, champion du monde à domicile.', 'Il est devenu entraîneur par la suite.'),
(7, 'Clarence Seedorf', 'seedorf;clarence seedorf', 1976, 157, 3, 189, 85,
 'The only player to win the Champions League with three different clubs.', 'He is Dutch.',
 'Seul joueur à avoir gagné la Ligue des champions avec trois clubs différents.', 'Il est néerlandais.');


-- ---------------------------------------------------------------- generation des journees

DROP TABLE IF EXISTS mock_days;
CREATE TABLE mock_days (i int NOT NULL PRIMARY KEY, d date NOT NULL)
  ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO mock_days (i, d)
WITH RECURSIVE seq AS (
    SELECT 0 AS i, CAST(@first_date AS DATE) AS d
    UNION ALL
    SELECT i + 1, DATE_ADD(d, INTERVAL 1 DAY) FROM seq WHERE d < @last_date
)
SELECT i, d FROM seq;

INSERT INTO players (id, name, allowed_names, year_of_birth, country_id, publication_date, clue, easy_clue, position_id, badge_id, creation_user_id, creation_date, reject_date, hide_creator)
SELECT mock_days.i + 2, mock_pool.name, mock_pool.allowed_names, mock_pool.year_of_birth, mock_pool.country_id,
       mock_days.d, mock_pool.clue_en, mock_pool.easy_en, mock_pool.position_id, NULL, 1,
       TIMESTAMP(DATE_SUB(@first_date, INTERVAL 1 DAY), '09:00:00'), NULL, 0
FROM mock_days
JOIN mock_pool ON mock_pool.p = MOD(mock_days.i, @pool_size);

INSERT INTO player_clubs (player_id, club_id, history_position, is_loan)
SELECT mock_days.i + 2, mock_pool.club1, 1, 0 FROM mock_days JOIN mock_pool ON mock_pool.p = MOD(mock_days.i, @pool_size)
UNION ALL
SELECT mock_days.i + 2, mock_pool.club2, 2, 0 FROM mock_days JOIN mock_pool ON mock_pool.p = MOD(mock_days.i, @pool_size);

INSERT INTO player_clue_translations (player_id, language_id, is_easy, clue)
SELECT mock_days.i + 2, 1, 0, mock_pool.clue_en FROM mock_days JOIN mock_pool ON mock_pool.p = MOD(mock_days.i, @pool_size)
UNION ALL
SELECT mock_days.i + 2, 1, 1, mock_pool.easy_en FROM mock_days JOIN mock_pool ON mock_pool.p = MOD(mock_days.i, @pool_size)
UNION ALL
SELECT mock_days.i + 2, 2, 0, mock_pool.clue_fr FROM mock_days JOIN mock_pool ON mock_pool.p = MOD(mock_days.i, @pool_size)
UNION ALL
SELECT mock_days.i + 2, 2, 1, mock_pool.easy_fr FROM mock_days JOIN mock_pool ON mock_pool.p = MOD(mock_days.i, @pool_size);

-- indices media (suite) : ids 3/4 = @first_date + 1/+2 jour, toujours dans le passe quelle
-- que soit la date de rejeu (cf. note en tete de fichier) - le clue facile de chacun reste
-- celui du pool, volontairement, pour illustrer le cas mixte.
UPDATE players SET clue = '/media/clues/mock-clue-audio.mp3' WHERE id = 3;
UPDATE players SET clue = '/media/clues/mock-clue-video.mp4' WHERE id = 4;
UPDATE player_clue_translations SET clue = '/media/clues/mock-clue-audio.mp3' WHERE player_id = 3 AND is_easy = 0;
UPDATE player_clue_translations SET clue = '/media/clues/mock-clue-video.mp4' WHERE player_id = 4 AND is_easy = 0;

-- ---------------------------------------------------------------- joueurs fictifs + historique
--
-- De quoi alimenter les classements et surtout les podiums (mensuel et global), qui
-- exigent au moins 3 joueurs classes par mois COMPLET de jeu. 8 comptes en plus de
-- joueur1/joueur2, ids 4 a 11, tous avec le mot de passe de joueur2 (test123) et la meme
-- reponse de recuperation ("kikole") : lea, hugo, emma, lucas, chloe, nathan, manon, theo.
--
-- Les victoires sont generees de facon deterministe (pas de RAND : deux rejeux donnent la
-- meme base) pour chaque jour PASSE, jamais aujourd'hui, pour laisser l'etat du jour
-- courant vierge : proba de victoire propre a chaque joueur ET variable d'un mois a
-- l'autre (le podium change donc d'un mois sur l'autre), points de 200 a 1000, heure de
-- victoire entre 00h10 et 23h29 du jour meme (donc comptee "a temps" par les classements).
-- Chaque victoire est doublee d'une proposition "nom" gagnante, pour que les tentatives
-- de la fiche joueur restent coherentes avec le classement.

INSERT INTO users (id, login, normalized_login, password, email_encrypted, email_hash, email_confirmed, language_id, user_type_id, is_disabled, concurrency_stamp, security_stamp, ip, creation_date) VALUES
(4,  'lea',    'LEA',    '2ed58959eef5c40f2bef10b524f1ddab9d7367fe215fa5ac968d332767c46150', 'xPrTejzJ67kPInDXg/aUNx96vJubxgilCyskUudg8qd6nF/GVFlMS/QGSQ==', '86b9136823e7ff553ef5df5787f343b29fa3fd3381c4f0e00e6b649543440c22', 1, 2, 1, 0, UUID(), UUID(), '127.0.0.1', TIMESTAMP(DATE_SUB(@first_date, INTERVAL 1 DAY), '10:00:00')),
(5,  'hugo',   'HUGO',   '2ed58959eef5c40f2bef10b524f1ddab9d7367fe215fa5ac968d332767c46150', '7oMIYyOWaA//oeoseztrDG38EQgbC7FL6OUX6K8lfJH6jOL80ypvHv0YEzE=', 'c83b718b74d0e6f22c8bd2314d9adb14bcf2f4bf938ff1e586b78f41c40ea1ab', 1, 2, 1, 0, UUID(), UUID(), '127.0.0.1', TIMESTAMP(DATE_SUB(@first_date, INTERVAL 1 DAY), '10:05:00')),
(6,  'emma',   'EMMA',   '2ed58959eef5c40f2bef10b524f1ddab9d7367fe215fa5ac968d332767c46150', 'fc1Kn0O3En1IV2o+MJDW1/NSXPSI80yBUBL3iS/nnS3Z5aHKX4wMLK1rX/A=', 'ecd6499d7f748c0403788e63acedd6bb6492a0023a56e27933a69608f728a5c4', 1, 2, 1, 0, UUID(), UUID(), '127.0.0.1', TIMESTAMP(DATE_SUB(@first_date, INTERVAL 1 DAY), '10:10:00')),
(7,  'lucas',  'LUCAS',  '2ed58959eef5c40f2bef10b524f1ddab9d7367fe215fa5ac968d332767c46150', 'o2xZ/8Hc9x9KvMGmuDUOBlS51x6of0dpFXL3w3chCIUlcCXWGpkx2CrWXmsi', 'bfdacb10a0979908d1876466eaec22ce0429ce8beeec1c66817ea868ab48cf9b', 1, 2, 1, 0, UUID(), UUID(), '127.0.0.1', TIMESTAMP(DATE_SUB(@first_date, INTERVAL 1 DAY), '10:15:00')),
(8,  'chloe',  'CHLOE',  '2ed58959eef5c40f2bef10b524f1ddab9d7367fe215fa5ac968d332767c46150', 'nKJFYgDk24eFMLKmisrKms9uykahMzb+0+KY88sdMjDugIc78Lfr4ta/gPue', 'bd360e467d896e435373fc5afbc31f77754002ab277c1ac00611dcd15b3b42b8', 1, 2, 1, 0, UUID(), UUID(), '127.0.0.1', TIMESTAMP(DATE_SUB(@first_date, INTERVAL 1 DAY), '10:20:00')),
(9,  'nathan', 'NATHAN', '2ed58959eef5c40f2bef10b524f1ddab9d7367fe215fa5ac968d332767c46150', 'aofonfoVviCabYtbPGNUdSsX9CFwYF+Abn/AZLUiIVyasrIhbiCNDsANUgk6OQ==', '3e7d4ada9a527e8e8530d81fa7a5af53913f348430ec841fc2d5fd25cf13dca5', 1, 1, 1, 0, UUID(), UUID(), '127.0.0.1', TIMESTAMP(DATE_SUB(@first_date, INTERVAL 1 DAY), '10:25:00')),
(10, 'manon',  'MANON',  '2ed58959eef5c40f2bef10b524f1ddab9d7367fe215fa5ac968d332767c46150', 'tNaCL0L46CeoC+MBxr6HalUDEtc+pI0LCOsLOk5B95B61jsPiKBXnfMMTloM', 'c599dd57081f2d0855ee179052b01476cee90332c1b68d0508a3ccdb902876aa', 1, 2, 1, 0, UUID(), UUID(), '127.0.0.1', TIMESTAMP(DATE_SUB(@first_date, INTERVAL 1 DAY), '10:30:00')),
(11, 'theo',   'THEO',   '2ed58959eef5c40f2bef10b524f1ddab9d7367fe215fa5ac968d332767c46150', 'ClFq/XiQp7ZIrzv4p1cKvK/393BRL/Y6FtSXqPp7hktJ5VLN9AF4hCqdbEs=', '37f8631fdaf08a431b72febd369d1b89fbeeb27571ac8d049eaa9adcc29a410c', 1, 2, 1, 0, UUID(), UUID(), '127.0.0.1', TIMESTAMP(DATE_SUB(@first_date, INTERVAL 1 DAY), '10:35:00'));

DROP TABLE IF EXISTS mock_wins;
CREATE TABLE mock_wins (
  user_id bigint unsigned NOT NULL,
  proposal_date date NOT NULL,
  points smallint unsigned NOT NULL,
  minutes int unsigned NOT NULL,
  PRIMARY KEY (user_id, proposal_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO mock_wins (user_id, proposal_date, points, minutes)
SELECT u.id, d.d,
       1000 - 100 * MOD(u.id * 7 + d.i * 3, 9),
       10 + MOD(u.id * 97 + d.i * 131, 1400)
FROM mock_days d
JOIN users u ON u.id BETWEEN 2 AND 11
WHERE d.d < CURDATE()
  AND MOD(u.id * 31 + d.i * 17, 100) < 45 + 9 * MOD(u.id * 13 + MONTH(d.d) * 7, 6);

INSERT INTO leaders (user_id, proposal_date, points, `time`, creation_date)
SELECT user_id, proposal_date, points, minutes,
       DATE_ADD(TIMESTAMP(proposal_date, '00:00:00'), INTERVAL minutes MINUTE)
FROM mock_wins;

INSERT INTO proposals (user_id, proposal_type_id, value, successful, ip, proposal_date, creation_date)
SELECT w.user_id, 1, p.name, 1, '127.0.0.1', w.proposal_date,
       DATE_ADD(TIMESTAMP(w.proposal_date, '00:00:00'), INTERVAL w.minutes MINUTE)
FROM mock_wins w
JOIN players p ON p.publication_date = w.proposal_date;

DROP TABLE mock_wins;

DROP TABLE mock_days;
DROP TABLE mock_pool;
