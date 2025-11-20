using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PlataformaInvestimentos.Controllers;

public class UtilController : Controller
{
    protected int ObterUsuarioId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        int.TryParse(idClaim, out int usuarioId);
        
        return usuarioId;
    }
}