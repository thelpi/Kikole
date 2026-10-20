-- Clubs mexicains : noms d'usage en nom principal, noms officiels et surnoms en alias de recherche.
-- Rejouable : le script remplace les noms et les alias de ces 19 clubs. A redemarrer l'appli ensuite (liste des clubs en cache).

-- 1. Controle prealable : doit renvoyer 19 lignes (les clubs portent bien l'ancien nom attendu)
SELECT id, name FROM clubs WHERE (id, name) IN (
  (1497, 'Club de Fútbol América'),
  (1498, 'Club de Fútbol Atlante'),
  (1501, 'Club de Fútbol Monterrey'),
  (1502, 'Club de Fútbol Universidad'),
  (1503, 'Club Deportivo Atlas'),
  (1504, 'Club Deportivo Cruz Azul'),
  (1505, 'Club Deportivo León'),
  (1506, 'Club Deportivo Necaxa'),
  (1507, 'Club Deportivo Pachuca'),
  (1508, 'Club Deportivo Santos Laguna'),
  (1509, 'Club Guadalajara'),
  (1510, 'Club Monarcas Morelia'),
  (1511, 'Club Puebla de la Franja'),
  (1512, 'Club Tecos de la UAG'),
  (1513, 'Club Universitario de Nuevo León'),
  (1514, 'Deportivo Toluca'),
  (1529, 'Club Deportivo Veracruz'),
  (1532, 'Club Universidad de Guadalajara'),
  (1533, 'Promotora Deportiva Tijuana')
);

-- 2. Mise a jour
START TRANSACTION;

UPDATE clubs SET name = CASE id
  WHEN 1497 THEN 'Club América'
  WHEN 1498 THEN 'Atlante FC'
  WHEN 1501 THEN 'CF Monterrey'
  WHEN 1502 THEN 'Pumas UNAM'
  WHEN 1503 THEN 'Atlas FC'
  WHEN 1504 THEN 'Cruz Azul'
  WHEN 1505 THEN 'Club León'
  WHEN 1506 THEN 'Club Necaxa'
  WHEN 1507 THEN 'CF Pachuca'
  WHEN 1508 THEN 'Santos Laguna'
  WHEN 1509 THEN 'Chivas de Guadalajara'
  WHEN 1510 THEN 'Monarcas Morelia'
  WHEN 1511 THEN 'Club Puebla'
  WHEN 1512 THEN 'Tecos UAG'
  WHEN 1513 THEN 'Tigres UANL'
  WHEN 1514 THEN 'Toluca FC'
  WHEN 1529 THEN 'Tiburones Rojos de Veracruz'
  WHEN 1532 THEN 'Leones Negros UdeG'
  WHEN 1533 THEN 'Club Tijuana'
END
WHERE id IN (1497, 1498, 1501, 1502, 1503, 1504, 1505, 1506, 1507, 1508, 1509, 1510, 1511, 1512, 1513, 1514, 1529, 1532, 1533);

DELETE FROM club_translations WHERE club_id IN (1497, 1498, 1501, 1502, 1503, 1504, 1505, 1506, 1507, 1508, 1509, 1510, 1511, 1512, 1513, 1514, 1529, 1532, 1533);

-- priority 0 = nom principal, > 0 = alias de recherche ; memes noms en anglais (1) et en francais (2)
INSERT INTO club_translations (club_id, language_id, priority, name)
SELECT t.club_id, l.id, t.priority, t.name
FROM (
  SELECT 1497 AS club_id, 0 AS priority, 'Club América' AS name
  UNION ALL
  SELECT 1497, 1, 'Club de Fútbol América'
  UNION ALL
  SELECT 1497, 2, 'América'
  UNION ALL
  SELECT 1497, 3, 'Águilas del América'
  UNION ALL
  SELECT 1497, 4, 'Las Águilas'
  UNION ALL
  SELECT 1498 AS club_id, 0 AS priority, 'Atlante FC' AS name
  UNION ALL
  SELECT 1498, 1, 'Club de Fútbol Atlante'
  UNION ALL
  SELECT 1498, 2, 'Atlante'
  UNION ALL
  SELECT 1498, 3, 'Potros de Hierro'
  UNION ALL
  SELECT 1501 AS club_id, 0 AS priority, 'CF Monterrey' AS name
  UNION ALL
  SELECT 1501, 1, 'Club de Fútbol Monterrey'
  UNION ALL
  SELECT 1501, 2, 'Monterrey'
  UNION ALL
  SELECT 1501, 3, 'Rayados'
  UNION ALL
  SELECT 1501, 4, 'Rayados de Monterrey'
  UNION ALL
  SELECT 1502 AS club_id, 0 AS priority, 'Pumas UNAM' AS name
  UNION ALL
  SELECT 1502, 1, 'Club de Fútbol Universidad'
  UNION ALL
  SELECT 1502, 2, 'Club Universidad Nacional'
  UNION ALL
  SELECT 1502, 3, 'Universidad Nacional'
  UNION ALL
  SELECT 1502, 4, 'UNAM'
  UNION ALL
  SELECT 1502, 5, 'Pumas'
  UNION ALL
  SELECT 1503 AS club_id, 0 AS priority, 'Atlas FC' AS name
  UNION ALL
  SELECT 1503, 1, 'Club Deportivo Atlas'
  UNION ALL
  SELECT 1503, 2, 'Atlas'
  UNION ALL
  SELECT 1503, 3, 'Rojinegros'
  UNION ALL
  SELECT 1504 AS club_id, 0 AS priority, 'Cruz Azul' AS name
  UNION ALL
  SELECT 1504, 1, 'Club Deportivo Cruz Azul'
  UNION ALL
  SELECT 1504, 2, 'La Máquina'
  UNION ALL
  SELECT 1504, 3, 'Los Cementeros'
  UNION ALL
  SELECT 1505 AS club_id, 0 AS priority, 'Club León' AS name
  UNION ALL
  SELECT 1505, 1, 'Club Deportivo León'
  UNION ALL
  SELECT 1505, 2, 'León'
  UNION ALL
  SELECT 1505, 3, 'La Fiera'
  UNION ALL
  SELECT 1506 AS club_id, 0 AS priority, 'Club Necaxa' AS name
  UNION ALL
  SELECT 1506, 1, 'Club Deportivo Necaxa'
  UNION ALL
  SELECT 1506, 2, 'Necaxa'
  UNION ALL
  SELECT 1506, 3, 'Rayos del Necaxa'
  UNION ALL
  SELECT 1507 AS club_id, 0 AS priority, 'CF Pachuca' AS name
  UNION ALL
  SELECT 1507, 1, 'Club Deportivo Pachuca'
  UNION ALL
  SELECT 1507, 2, 'Club de Fútbol Pachuca'
  UNION ALL
  SELECT 1507, 3, 'Pachuca'
  UNION ALL
  SELECT 1507, 4, 'Tuzos'
  UNION ALL
  SELECT 1507, 5, 'Los Tuzos'
  UNION ALL
  SELECT 1508 AS club_id, 0 AS priority, 'Santos Laguna' AS name
  UNION ALL
  SELECT 1508, 1, 'Club Deportivo Santos Laguna'
  UNION ALL
  SELECT 1508, 2, 'Santos'
  UNION ALL
  SELECT 1508, 3, 'Guerreros'
  UNION ALL
  SELECT 1509 AS club_id, 0 AS priority, 'Chivas de Guadalajara' AS name
  UNION ALL
  SELECT 1509, 1, 'Club Guadalajara'
  UNION ALL
  SELECT 1509, 2, 'Club Deportivo Guadalajara'
  UNION ALL
  SELECT 1509, 3, 'CD Guadalajara'
  UNION ALL
  SELECT 1509, 4, 'Guadalajara'
  UNION ALL
  SELECT 1509, 5, 'Chivas'
  UNION ALL
  SELECT 1509, 6, 'Rebaño Sagrado'
  UNION ALL
  SELECT 1510 AS club_id, 0 AS priority, 'Monarcas Morelia' AS name
  UNION ALL
  SELECT 1510, 1, 'Club Monarcas Morelia'
  UNION ALL
  SELECT 1510, 2, 'Morelia'
  UNION ALL
  SELECT 1510, 3, 'Monarcas'
  UNION ALL
  SELECT 1511 AS club_id, 0 AS priority, 'Club Puebla' AS name
  UNION ALL
  SELECT 1511, 1, 'Club Puebla de la Franja'
  UNION ALL
  SELECT 1511, 2, 'Puebla'
  UNION ALL
  SELECT 1511, 3, 'La Franja'
  UNION ALL
  SELECT 1512 AS club_id, 0 AS priority, 'Tecos UAG' AS name
  UNION ALL
  SELECT 1512, 1, 'Club Tecos de la UAG'
  UNION ALL
  SELECT 1512, 2, 'Tecos'
  UNION ALL
  SELECT 1513 AS club_id, 0 AS priority, 'Tigres UANL' AS name
  UNION ALL
  SELECT 1513, 1, 'Club Universitario de Nuevo León'
  UNION ALL
  SELECT 1513, 2, 'Tigres de la UANL'
  UNION ALL
  SELECT 1513, 3, 'Tigres'
  UNION ALL
  SELECT 1513, 4, 'Los Felinos'
  UNION ALL
  SELECT 1513, 5, 'UANL'
  UNION ALL
  SELECT 1514 AS club_id, 0 AS priority, 'Toluca FC' AS name
  UNION ALL
  SELECT 1514, 1, 'Deportivo Toluca'
  UNION ALL
  SELECT 1514, 2, 'Deportivo Toluca Fútbol Club'
  UNION ALL
  SELECT 1514, 3, 'Toluca'
  UNION ALL
  SELECT 1514, 4, 'Diablos Rojos'
  UNION ALL
  SELECT 1529 AS club_id, 0 AS priority, 'Tiburones Rojos de Veracruz' AS name
  UNION ALL
  SELECT 1529, 1, 'Club Deportivo Veracruz'
  UNION ALL
  SELECT 1529, 2, 'Veracruz'
  UNION ALL
  SELECT 1529, 3, 'Tiburones Rojos'
  UNION ALL
  SELECT 1532 AS club_id, 0 AS priority, 'Leones Negros UdeG' AS name
  UNION ALL
  SELECT 1532, 1, 'Club Universidad de Guadalajara'
  UNION ALL
  SELECT 1532, 2, 'Universidad de Guadalajara'
  UNION ALL
  SELECT 1532, 3, 'UdeG'
  UNION ALL
  SELECT 1532, 4, 'Leones Negros'
  UNION ALL
  SELECT 1533 AS club_id, 0 AS priority, 'Club Tijuana' AS name
  UNION ALL
  SELECT 1533, 1, 'Promotora Deportiva Tijuana'
  UNION ALL
  SELECT 1533, 2, 'Tijuana'
  UNION ALL
  SELECT 1533, 3, 'Xolos'
  UNION ALL
  SELECT 1533, 4, 'Xolos de Tijuana'
) t
CROSS JOIN (SELECT 1 AS id UNION ALL SELECT 2) l;

COMMIT;

-- 3. Verification : un nom principal et des alias par langue
SELECT c.id, c.name, COUNT(*) AS traductions
FROM clubs c JOIN club_translations t ON t.club_id = c.id
WHERE c.id IN (1497, 1498, 1501, 1502, 1503, 1504, 1505, 1506, 1507, 1508, 1509, 1510, 1511, 1512, 1513, 1514, 1529, 1532, 1533)
GROUP BY c.id, c.name ORDER BY c.id;
