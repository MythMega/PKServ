using System.Collections.Specialized;
using System.Threading.Tasks;

namespace PKServ.Controller
{
    /// <summary>
    /// Classe de base pour tous les contrôleurs HTTP.
    /// Chaque contrôleur couvre le premier segment d'URL qui lui correspond.
    /// </summary>
    public abstract class BaseController
    {
        protected readonly ControllerContext Ctx;

        protected BaseController(ControllerContext ctx) => Ctx = ctx;

        /// <summary>Traite une requête POST. <paramref name="path"/> est le chemin complet.</summary>
        public virtual Task<string> HandlePostAsync(string path, string body)
            => Task.FromResult($"[{GetType().Name}] Route POST non reconnue : {path}");

        /// <summary>Traite une requête GET. <paramref name="path"/> est le chemin complet.</summary>
        public virtual Task<string> HandleGetAsync(string path, NameValueCollection query)
            => Task.FromResult($"[{GetType().Name}] Route GET non reconnue : {path}");
    }
}
