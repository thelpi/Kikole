-- Une seule victoire par utilisateur et par jour : supprime les doublons existants puis
-- interdit d'en recreer. A lancer AVANT de deployer la version du code qui intercepte
-- l'erreur de doublon (CreateLeaderAsync).

-- 1. Doublons actuels (attendu : 1 ligne, user 42 / 2026-10-13)
SELECT user_id, proposal_date, COUNT(*) AS nb, GROUP_CONCAT(id ORDER BY id) AS ids
FROM leaders
GROUP BY user_id, proposal_date
HAVING COUNT(*) > 1;

-- 2. Garde la plus ancienne ligne (plus petit id) de chaque doublon
DELETE l
FROM leaders AS l
JOIN leaders AS k
  ON k.user_id = l.user_id
 AND k.proposal_date = l.proposal_date
 AND k.id < l.id;

-- 3. Contrainte d'unicite
ALTER TABLE leaders
  ADD UNIQUE KEY user_id_proposal_date (user_id, proposal_date);

-- 4. Verification (attendu : 0 ligne)
SELECT user_id, proposal_date, COUNT(*) AS nb
FROM leaders
GROUP BY user_id, proposal_date
HAVING COUNT(*) > 1;
