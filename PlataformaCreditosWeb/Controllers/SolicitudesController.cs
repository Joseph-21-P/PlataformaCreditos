using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditosWeb.Data;
using PlataformaCreditosWeb.Models;

namespace PlataformaCreditosWeb.Controllers
{
    [Authorize] 
    public class SolicitudesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SolicitudesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? estado, decimal? montoMin, decimal? montoMax, DateTime? fechaInicio, DateTime? fechaFin)
        {
            if (montoMin < 0 || montoMax < 0)
            {
                ModelState.AddModelError("", "No se aceptan montos negativos.");
            }
            if (fechaInicio > fechaFin)
            {
                ModelState.AddModelError("", "El rango de fechas es inválido.");
            }

            var userId = _userManager.GetUserId(User);

            var query = _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .Where(s => s.Cliente!.UsuarioId == userId)
                .AsQueryable();

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(estado))
                    query = query.Where(s => s.Estado == estado);

                if (montoMin.HasValue)
                    query = query.Where(s => s.MontoSolicitado >= montoMin.Value);

                if (montoMax.HasValue)
                    query = query.Where(s => s.MontoSolicitado <= montoMax.Value);

                if (fechaInicio.HasValue)
                    query = query.Where(s => s.FechaSolicitud.Date >= fechaInicio.Value.Date);

                if (fechaFin.HasValue)
                    query = query.Where(s => s.FechaSolicitud.Date <= fechaFin.Value.Date);
            }

            var solicitudes = await query.ToListAsync();
            
            ViewBag.Estado = estado;
            ViewBag.MontoMin = montoMin;
            ViewBag.MontoMax = montoMax;
            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");

            return View(solicitudes);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(m => m.Id == id && m.Cliente!.UsuarioId == userId);

            if (solicitud == null) return NotFound();

            return View(solicitud);
        }
    }
}