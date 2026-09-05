using System.Collections.Generic;
using KikoleSite.Models.Dtos;

namespace KikoleSite.ViewModels;

/// <summary>
/// Modele du partial d'affichage d'un fil de discussion (Home/Contact et
/// Admin/Discussion) : purement de la presentation, aucune logique metier. Les libelles
/// sont fournis par la vue appelante (chacune a sa propre resx, avec un point de vue
/// different : "Admin" cote utilisateur, le login de l'utilisateur cote admin).
/// </summary>
public class DiscussionThreadModel
{
    public required IReadOnlyCollection<DiscussionMessageDto> Messages { get; set; }

    /// <summary>Le fil est-il affiche du point de vue de l'admin ?</summary>
    public bool ViewerIsAdmin { get; set; }

    /// <summary>Libelle des messages envoyes par le lecteur courant (ex. "Vous").</summary>
    public required string SelfLabel { get; set; }

    /// <summary>Libelle des messages envoyes par l'autre partie.</summary>
    public required string OtherLabel { get; set; }

    public required string NoMessagesText { get; set; }
}
