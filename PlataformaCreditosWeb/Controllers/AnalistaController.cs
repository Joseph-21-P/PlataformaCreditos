using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PlataformaCreditosWeb.Data;
using PlataformaCreditosWeb.Models;

namespace PlataformaCreditosWeb.Controllers
{

    [Authorize(Roles = "Analista")]
    public class AnalistaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

        public AnalistaController(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: Analista/Index
        public async Task<IActionResult> Index()
        {

            var solicitudes = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .Where(s => s.Estado == "Pendiente")
                .ToListAsync();

            return View(solicitudes);
        }

        // POST: Analista/Aprobar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id)
        {
            var solicitud = await _context.SolicitudesCredito.Include(s => s.Cliente).FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null || solicitud.Estado != "Pendiente")
            {
                TempData["Error"] = "La solicitud no existe o ya fue procesada.";
                return RedirectToAction(nameof(Index));
            }


            decimal maximoPermitido = solicitud.Cliente!.IngresosMensuales * 5;
            if (solicitud.MontoSolicitado > maximoPermitido)
            {
                TempData["Error"] = $"No se puede aprobar. El monto excede 5 veces los ingresos del cliente (Máximo: {maximoPermitido:C}).";
                return RedirectToAction(nameof(Index));
            }

            solicitud.Estado = "Aprobado";
            _context.Update(solicitud);
            await _context.SaveChangesAsync();


            await _cache.RemoveAsync($"solicitudes_list_{solicitud.Cliente.UsuarioId}");

            TempData["Success"] = $"Solicitud #{id} aprobada con éxito.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Analista/Rechazar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id, string motivo)
        {
            var solicitud = await _context.SolicitudesCredito.Include(s => s.Cliente).FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null || solicitud.Estado != "Pendiente")
            {
                TempData["Error"] = "La solicitud no existe o ya fue procesada.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(motivo))
            {
                TempData["Error"] = "Debe proporcionar un motivo obligatorio para rechazar la solicitud.";
                return RedirectToAction(nameof(Index));
            }

            solicitud.Estado = "Rechazado";
            solicitud.MotivoRechazo = motivo;
            _context.Update(solicitud);
            await _context.SaveChangesAsync();


            await _cache.RemoveAsync($"solicitudes_list_{solicitud.Cliente!.UsuarioId}");

            TempData["Success"] = $"Solicitud #{id} rechazada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}