-- Propositions enregistrees deux fois par une double soumission (meme utilisateur, jour, type
-- et valeur, a quelques secondes d'ecart). Chaque doublon d'une proposition payante a ete
-- facture deux fois dans le score affiche.

-- 1. Diagnostic : a lire avant de supprimer quoi que ce soit
SELECT p.user_id, p.proposal_date, p.proposal_type_id, p.value, p.successful,
       k.id AS id_conserve, p.id AS id_doublon,
       TIMESTAMPDIFF(SECOND, k.creation_date, p.creation_date) AS ecart_s
FROM proposals AS p
JOIN proposals AS k
  ON k.user_id = p.user_id
 AND k.proposal_date = p.proposal_date
 AND k.proposal_type_id = p.proposal_type_id
 AND k.value <=> p.value
 AND k.id < p.id
ORDER BY p.creation_date;

-- 2. Suppression : ne garde que la premiere ligne, et seulement si le doublon est tombe
--    dans les 5 secondes (double clic), jamais une vraie proposition ulterieure
DELETE p
FROM proposals AS p
JOIN proposals AS k
  ON k.user_id = p.user_id
 AND k.proposal_date = p.proposal_date
 AND k.proposal_type_id = p.proposal_type_id
 AND k.value <=> p.value
 AND k.id < p.id
 AND TIMESTAMPDIFF(SECOND, k.creation_date, p.creation_date) BETWEEN 0 AND 5;

-- 3. Verification : il ne doit rester que les doublons espaces de plus de 5 secondes
SELECT COUNT(*) AS doublons_restants
FROM proposals AS p
JOIN proposals AS k
  ON k.user_id = p.user_id
 AND k.proposal_date = p.proposal_date
 AND k.proposal_type_id = p.proposal_type_id
 AND k.value <=> p.value
 AND k.id < p.id;