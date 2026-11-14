-- Demande de suppression de compte faite par l'utilisateur depuis "Mon compte" : l'administrateur
-- la voit (point rouge sur les actions admin, puis sur la page des utilisateurs) et supprime le
-- compte lui-meme. A lancer AVANT de deployer la version du code qui lit ces colonnes.

ALTER TABLE users
  ADD COLUMN deletion_requested tinyint(3) UNSIGNED NOT NULL DEFAULT '0' AFTER disabled_reason,
  ADD COLUMN deletion_requested_date datetime DEFAULT NULL AFTER deletion_requested,
  ADD COLUMN deletion_reason varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL AFTER deletion_requested_date;