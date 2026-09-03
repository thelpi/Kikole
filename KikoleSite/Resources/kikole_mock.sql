-- Jeu de donnees de developpement pour Kikole.
-- A jouer APRES kikole.sql. Rejouable a l'infini : le script vide d'abord toutes les
-- donnees applicatives (voir plus bas) sans toucher aux donnees de reference
-- (badges, pays, continents, langues, positions, types de proposition, types d'utilisateur).
--
-- Les mots de passe sont hashes en SHA256(motdepasse + EncryptionKey), ou EncryptionKey
-- est la valeur de appsettings.Development.json ("KikoleDevSalt2026").
-- Changer cette cle invalide les comptes ci-dessous.
--
-- Comptes :
--   admin   / admin123   (administrateur)
--   joueur1 / test123    (utilisateur standard)
--   joueur2 / test123    (utilisateur standard)
--   question de recuperation : reponse "kikole" pour les trois
--
-- Joueurs du jour : generes de FirstDate a aujourd'hui + 7 jours (voir plus bas).

SET NAMES utf8mb4;
USE kikole;

-- ---------------------------------------------------------------- remise a zero
-- TRUNCATE plutot que DELETE : remet aussi les compteurs AUTO_INCREMENT a 1,
-- pour que deux executations successives produisent exactement la meme base.
-- Ordre sans importance ici, le schema ne declare aucune cle etrangere.

TRUNCATE TABLE proposals;
TRUNCATE TABLE leaders;
TRUNCATE TABLE user_badges;
TRUNCATE TABLE player_clue_translations;
TRUNCATE TABLE player_clubs;
TRUNCATE TABLE players;
TRUNCATE TABLE clubs;
TRUNCATE TABLE discussions;
TRUNCATE TABLE messages;
TRUNCATE TABLE registration_guids;
TRUNCATE TABLE users;

-- ---------------------------------------------------------------- utilisateurs

INSERT INTO users (id, login, password, password_reset_question, password_reset_answer, language_id, user_type_id, is_disabled, ip, creation_date) VALUES
(1, 'admin',   '4df227a5023483d53ebea1653c76a8aad6c2a0aa1b07a6b55c6e66842fd8bf25', 'Nom du jeu ?', '0791737f1531a34755485d99a84118c00d1954cf328de370d8da0320b290d509', 2, 3, 0, '127.0.0.1', '2026-09-01 09:00:00'),
(2, 'joueur1', '2ed58959eef5c40f2bef10b524f1ddab9d7367fe215fa5ac968d332767c46150', 'Nom du jeu ?', '0791737f1531a34755485d99a84118c00d1954cf328de370d8da0320b290d509', 2, 1, 0, '127.0.0.1', '2026-09-01 09:05:00'),
(3, 'joueur2', '2ed58959eef5c40f2bef10b524f1ddab9d7367fe215fa5ac968d332767c46150', 'Nom du jeu ?', '0791737f1531a34755485d99a84118c00d1954cf328de370d8da0320b290d509', 1, 1, 0, '127.0.0.1', '2026-09-01 09:10:00');

-- un GUID libre pour tester le parcours d'inscription
INSERT INTO registration_guids (id, user_id, creation_date) VALUES
('11111111-2222-3333-4444-555555555555', NULL, '2026-09-01 09:00:00');

-- ---------------------------------------------------------------- clubs
-- allowed_names suit le format produit par SanitizeJoin : alias sanitises puis nom sanitise, separes par ';'

INSERT INTO clubs (id, name, allowed_names, creation_date) VALUES
(1,  'AS Cannes',           'cannes;as cannes',                        '2026-09-01 09:00:00'),
(2,  'Girondins de Bordeaux', 'bordeaux;girondins de bordeaux',        '2026-09-01 09:00:00'),
(3,  'Juventus',            'juve;juventus turin;juventus',            '2026-09-01 09:00:00'),
(4,  'Real Madrid',         'real;real madrid',                        '2026-09-01 09:00:00'),
(5,  'Brescia',             'brescia calcio;brescia',                  '2026-09-01 09:00:00'),
(6,  'Inter Milan',         'inter;internazionale;inter milan',        '2026-09-01 09:00:00'),
(7,  'AC Milan',            'milan;ac milan',                          '2026-09-01 09:00:00'),
(8,  'New York City FC',    'nycfc;new york city fc',                  '2026-09-01 09:00:00'),
(9,  'FC Barcelone',        'barca;barcelone;fc barcelone',            '2026-09-01 09:00:00'),
(10, 'Paris Saint-Germain', 'psg;paris sg;paris saint-germain',        '2026-09-01 09:00:00'),
(11, 'Manchester United',   'man utd;manchester united',               '2026-09-01 09:00:00'),
(12, 'Bayern Munich',       'bayern;bayern munich',                    '2026-09-01 09:00:00');


-- ---------------------------------------------------------------- joueurs du jour
--
-- Les journees sont generees de FirstDate jusqu'a aujourd'hui + 7 jours, pour que
-- l'environnement local reste valable dans le temps : sans ca, passe minuit il n'y a
-- plus de joueur du jour et l'application tombe en erreur.
--
-- @first_date n'a plus a correspondre a quoi que ce soit dans le code : l'application
-- deduit son calendrier du MIN(proposal_date), qui est la journee cachee inseree
-- juste en dessous. On part donc d'aujourd'hui moins un mois, pour avoir un historique.
--
-- Un pool de 8 joueurs est parcouru en boucle ; l'identifiant vaut l'indice du jour + 2,
-- ce qui rend les insertions dependantes deterministes (carrieres, traductions).

SET @first_date = DATE_SUB(CURDATE(), INTERVAL 1 MONTH);
SET @last_date = DATE_ADD(CURDATE(), INTERVAL 7 DAY);
SET @pool_size = 8;
SET SESSION cte_max_recursion_depth = 10000;

-- journee cachee (FirstDate - 1)
INSERT INTO players (id, name, allowed_names, year_of_birth, country_id, continent_id, proposal_date, clue, easy_clue, position_id, badge_id, creation_user_id, creation_date, reject_date, hide_creator) VALUES
(1, 'Andrea Pirlo', 'pirlo;andrea pirlo', 1979, 111, 1, DATE_SUB(@first_date, INTERVAL 1 DAY),
 'A deep-lying playmaker, famous for his free kicks.', 'He won the 2006 World Cup with Italy.',
 3, NULL, 1, '2026-09-01 09:00:00', NULL, 0);

INSERT INTO player_clubs (player_id, club_id, history_position, is_loan) VALUES
(1, 5, 1, 0), (1, 6, 2, 0), (1, 7, 3, 0), (1, 3, 4, 0), (1, 8, 5, 0);

INSERT INTO player_clue_translations (player_id, language_id, is_easy, clue) VALUES
(1, 1, 0, 'A deep-lying playmaker, famous for his free kicks.'),
(1, 1, 1, 'He won the 2006 World Cup with Italy.'),
(1, 2, 0, 'Meneur de jeu reculé, spécialiste des coups francs.'),
(1, 2, 1, 'Champion du monde 2006 avec l''Italie.');

-- ---------------------------------------------------------------- pool de joueurs

DROP TABLE IF EXISTS mock_pool;
CREATE TABLE mock_pool (
  p tinyint NOT NULL PRIMARY KEY,
  name varchar(255) NOT NULL,
  allowed_names varchar(255) NOT NULL,
  year_of_birth smallint NOT NULL,
  country_id bigint NOT NULL,
  continent_id bigint NOT NULL,
  position_id bigint NOT NULL,
  club1 bigint NOT NULL,
  club2 bigint NOT NULL,
  clue_en varchar(255) NOT NULL,
  easy_en varchar(255) NOT NULL,
  clue_fr varchar(255) NOT NULL,
  easy_fr varchar(255) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO mock_pool VALUES
(0, 'Zinédine Zidane', 'zidane;zizou;zinedine zidane', 1972, 77, 1, 3, 2, 4,
 'He scored twice with his head in a World Cup final.', 'His last professional match ended with a red card.',
 'Deux buts de la tête en finale de Coupe du monde.', 'Son dernier match professionnel s''est terminé par un carton rouge.'),
(1, 'Ronaldinho', 'ronaldinho;ronaldinho gaucho', 1980, 32, 5, 4, 10, 9,
 'He made an entire opposing stadium applaud him.', 'Famous for his smile and his elastico.',
 'Il a fait applaudir un stade adverse tout entier.', 'Célèbre pour son sourire et son elastico.'),
(2, 'David Beckham', 'beckham;david beckham', 1975, 235, 1, 3, 11, 4,
 'His right foot made him famous well beyond football.', 'A film bears his name.',
 'Son pied droit l''a rendu célèbre bien au-delà du football.', 'Un film porte son nom.'),
(3, 'Ronaldo', 'ronaldo;ronaldo nazario;el fenomeno', 1976, 32, 5, 4, 6, 4,
 'Top scorer of the 2002 World Cup.', 'Nicknamed "the phenomenon".',
 'Meilleur buteur de la Coupe du monde 2002.', 'Surnommé « le phénomène ».'),
(4, 'Thierry Henry', 'henry;thierry henry;titi', 1977, 77, 1, 4, 3, 9,
 'France''s all-time top scorer for many years.', 'A statue of him stands outside a London stadium.',
 'Meilleur buteur de l''équipe de France pendant des années.', 'Une statue de lui trône devant un stade londonien.'),
(5, 'Franck Ribéry', 'ribery;franck ribery', 1983, 77, 1, 3, 12, 3,
 'A scar marks his face since childhood.', 'He spent a decade in Bavaria.',
 'Une cicatrice marque son visage depuis l''enfance.', 'Il a passé une décennie en Bavière.'),
(6, 'Patrick Vieira', 'vieira;patrick vieira', 1976, 77, 1, 3, 3, 6,
 'A towering midfielder, World Cup winner at home.', 'He later became a manager.',
 'Un milieu de terrain imposant, champion du monde à domicile.', 'Il est devenu entraîneur par la suite.'),
(7, 'Clarence Seedorf', 'seedorf;clarence seedorf', 1976, 157, 1, 3, 4, 7,
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

INSERT INTO players (id, name, allowed_names, year_of_birth, country_id, continent_id, proposal_date, clue, easy_clue, position_id, badge_id, creation_user_id, creation_date, reject_date, hide_creator)
SELECT mock_days.i + 2, mock_pool.name, mock_pool.allowed_names, mock_pool.year_of_birth, mock_pool.country_id, mock_pool.continent_id,
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

DROP TABLE mock_days;
DROP TABLE mock_pool;
