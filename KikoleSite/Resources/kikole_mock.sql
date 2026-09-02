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
-- Joueur du jour : 2026-09-02 (FirstDate) et 2026-09-01 (journee cachee).

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

INSERT INTO players (id, name, allowed_names, year_of_birth, country_id, continent_id, proposal_date, clue, easy_clue, position_id, badge_id, creation_user_id, creation_date, reject_date, hide_creator) VALUES
(1, 'Andrea Pirlo',    'pirlo;andrea pirlo',            1979, 111, 1, '2026-09-01', 'Meneur de jeu reculé, spécialiste des coups francs.', 'Champion du monde 2006 avec l''Italie.', 3, NULL, 1, '2026-09-01 09:00:00', NULL, 0),
(2, 'Zinédine Zidane', 'zidane;zizou;zinedine zidane',  1972,  77, 1, '2026-09-02', 'Deux buts de la tête en finale de Coupe du monde.', 'Son dernier match professionnel s''est terminé par un carton rouge.', 3, NULL, 1, '2026-09-01 09:00:00', NULL, 0);

INSERT INTO player_clubs (player_id, club_id, history_position, is_loan) VALUES
(1, 5, 1, 0),
(1, 6, 2, 0),
(1, 7, 3, 0),
(1, 3, 4, 0),
(1, 8, 5, 0),
(2, 1, 1, 0),
(2, 2, 2, 0),
(2, 3, 3, 0),
(2, 4, 4, 0);

-- indices traduits (1 = en, 2 = fr ; is_easy 0 = indice normal, 1 = indice facile)
INSERT INTO player_clue_translations (player_id, language_id, is_easy, clue) VALUES
(1, 1, 0, 'A deep-lying playmaker, famous for his free kicks.'),
(1, 1, 1, 'He won the 2006 World Cup with Italy.'),
(1, 2, 0, 'Meneur de jeu reculé, spécialiste des coups francs.'),
(1, 2, 1, 'Champion du monde 2006 avec l''Italie.'),
(2, 1, 0, 'He scored twice with his head in a World Cup final.'),
(2, 1, 1, 'His last professional match ended with a red card.'),
(2, 2, 0, 'Deux buts de la tête en finale de Coupe du monde.'),
(2, 2, 1, 'Son dernier match professionnel s''est terminé par un carton rouge.');
