using System.Threading.Tasks;

namespace KikoleSite.Services;

/// <summary>
/// Rend une vue Razor (ex. <c>Views/Emails/LinkEmail.cshtml</c>) en chaine HTML, en dehors
/// de tout pipeline de requete MVC - utilise pour le corps des emails, afin de garder les
/// templates dans de vraies vues (nommees, avec injection de modele) plutot que de la
/// concatenation de HTML dans les controleurs.
/// </summary>
public interface IRazorViewRenderer
{
    /// <param name="viewPath">Chemin absolu de la vue depuis la racine de l'app, ex. "/Views/Emails/LinkEmail.cshtml".</param>
    /// <param name="model">Modele de la vue.</param>
    Task<string> RenderAsync<TModel>(string viewPath, TModel model);
}
