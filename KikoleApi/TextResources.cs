using KikoleApi.Models.Enums;

namespace KikoleApi
{
    public class TextResources
    {
        internal Languages Language { get; set; }

        internal string InvalidValue => Language == Languages.fr
            ? "Valeur incorrecte"
            : "Invalid value";

        internal string InvalidName => Language == Languages.fr
            ? "Nom invalide"
            : "Invalid name";

        internal string InvalidAllowedNames => Language == Languages.fr
            ? "Noms autorisés invalide"
            : "Invalid allowed names";

        internal string InvalidCountry => Language == Languages.fr
            ? "Pays invalide"
            : "Invalid country";

        internal string InvalidPosition => Language == Languages.fr
            ? "Position invalide"
            : "Invalid position";

        internal string InvalidBirthYear => Language == Languages.fr
            ? "Année de naissance invalide"
            : "Invalid year of birth";

        internal string EmptyClubsList => Language == Languages.fr
            ? "Liste des clubs vide"
            : "Empty clubs list";

        internal string InvalidClubs => Language == Languages.fr
            ? "Au moins un club invalide"
            : "At least one invalid club";

        internal string InvalidClue => Language == Languages.fr
            ? "Indice invalide"
            : "Invalid clue";

        internal string InvalidProposalDate => Language == Languages.fr
            ? "Date de proposition invalide"
            : "Invalid proposal date";

        internal string TipYoungerPlayer => Language == Languages.fr
            ? "Le joueur est plus jeune"
            : "The player is younger";

        internal string TipOlderPlayer => Language == Languages.fr
            ? "Le joueur est plus vieux"
            : "The player is older";

        internal string InvalidLogin => Language == Languages.fr
            ? "Identifiant invalide"
            : "Invalid login";

        internal string InvalidPassword => Language == Languages.fr
            ? "Pot de passe invalide"
            : "Invalid password";

        internal string InvalidLanguage => Language == Languages.fr
            ? "Langage invalide"
            : "Invalid language";

        internal string InvalidRequest => Language == Languages.fr
            ? "Requête invalide : {0}"
            : "Invalid request: {0}";

        internal string AlreadyExistsAccount => Language == Languages.fr
            ? "Un compte existe déjà avec cet identifiant"
            : "A account already exists with this login";

        internal string UserCreationFailure => Language == Languages.fr
            ? "Echec de la création du compte"
            : "User creation failure";

        internal string InvalidUser => Language == Languages.fr
            ? "Utilisateur invalide"
            : "Invalid user";

        internal string PasswordDoesNotMatch => Language == Languages.fr
            ? "Le mot de passe ne correspond pas"
            : "The password doesn't match";

        internal string UserDoesNotExist => Language == Languages.fr
            ? "Le compte n'existe pas"
            : "The user doesn't exist";

        internal string ResetPasswordError => Language == Languages.fr
            ? "Erreur lors de la réinitalisation du mot de passe"
            : "Error while reseting the password";

        internal string InvalidQOrA => Language == Languages.fr
            ? "Question ou réponse invalide"
            : "Invalid question or answer";

        internal string InvalidDate => Language == Languages.fr
            ? "Date invalide"
            : "Invalid date";

        internal string PlayerCreationFailure => Language == Languages.fr
            ? "Echec de la création du joueur"
            : "Player creation failure";

        internal string RefusalWithoutReason => Language == Languages.fr
            ? "Raison du refus non spécifiée"
            : "Refusal reason not specified";

        internal string InvalidPlayerId => Language == Languages.fr
            ? "Identifiant du joueur invalide"
            : "Invalid player identifier";

        internal string PlayerDoesNotExist => Language == Languages.fr
            ? "Le joueur n'existe pas"
            : "The player doesn't exist";

        internal string RejectAndProposalDateCombined => Language == Languages.fr
            ? "Impossible de spécifier à la fois une date de rejet et de proposition"
            : "Unable to set both reject date and proposal date";

        internal string ClubCreationFailure => Language == Languages.fr
            ? "Echec de la création du club"
            : "Club creation failure";

        internal string SuccessCountSortForbidden => Language == Languages.fr
            ? "Impossible de trier par nombre de succès"
            : "Can't sort by success count";

        internal string InvalidDateRange => Language == Languages.fr
            ? "Les dates de début/fin sont invalides ou incohérentes"
            : "Start/End dates are invalid or inconsistent";

        internal string InvalidSortType => Language == Languages.fr
            ? "Type de tri invalide"
            : "Invalid sort type";

        internal string InvalidOpponentAccount => Language == Languages.fr
            ? "Le compte de l'adversaire est invalide"
            : "Opponent is an invalid account";

        internal string ChallengeAlreadyAccepted => Language == Languages.fr
            ? "Vous ne pouvez pas annuler un challenge accepté"
            : "You can't cancel an accepted challenge";

        internal string ChallengeAlreadyAnswered => Language == Languages.fr
            ? "Vous avez déjà répondu à ce challenge"
            : "You've already respond to this challenge";

        internal string InvalidChallengeId => Language == Languages.fr
            ? "Identifiant de challenge invalide"
            : "Invalid challenge identifier";

        internal string ChallengeNotFound => Language == Languages.fr
            ? "Challenge inexistant"
            : "Challenge not found";

        internal string CantAutoAcceptChallenge => Language == Languages.fr
            ? "Vous ne pouvez pas accepter votre propre challenge"
            : "Can't accept your own challenge";

        internal string BothAcceptedAndCancelledChallenge => Language == Languages.fr
            ? "Challenge à la fois accepté et refusé"
            : "Challenge both accepted and refused";

        internal string InvalidGuestUserId => Language == Languages.fr
            ? "Identifiant d'invité invalide"
            : "Invalid guest identifier";

        internal string InvalidPointsRate => Language == Languages.fr
            ? "Pourcentage de points invalide"
            : "Invalid points rate";

        internal string CantChallengeYourself => Language == Languages.fr
            ? "Vous ne pouvez pas vous challenger vous même"
            : "You can't challenge yourself";

        internal string ChallengeHostIsInvalid => Language == Languages.fr
            ? "Compte utilisateur du créateur invalide"
            : "Invalid host account";

        internal string ChallengeCreatorIsAdmin => Language == Languages.fr
            ? "Impossible de créer le challenge ; vous êtes administrateur"
            : "Can't create challenge; you're administrator";

        internal string ChallengeOpponentIsInvalid => Language == Languages.fr
            ? "Impossible de créer le challenge ; le compte de l'adversaire est invalide"
            : "Can't create challenge; invalid opponent account";

        internal string ChallengeOpponentIsAdmin => Language == Languages.fr
            ? "Impossible de créer le challenge ; l'adversaire est administrateur"
            : "Can't create challenge; opponent account is administrator";

        internal string ChallengeAlreadyExist => Language == Languages.fr
            ? "Impossible de créer le challenge ; un challenge accepté ou en attente existe déjà avec l'adversaire"
            : "Can't create challenge; a challenge against this opponent is already planned or requested";
    }
}
