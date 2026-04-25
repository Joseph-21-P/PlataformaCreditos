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

        // GET: Solicitudes/Index (Catálogo con filtros)
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

        // GET: Solicitudes/Details
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

        // GET: Solicitudes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Solicitudes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MontoSolicitado")] SolicitudCredito solicitud)
        {
            // 1. Usuario debe estar autenticado (garantizado por [Authorize])
            var userId = _userManager.GetUserId(User);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null)
            {
                ViewBag.ErrorMessage = "No se encontró un perfil de cliente asociado a su cuenta.";
                return View(solicitud);
            }

            // 2. Cliente debe estar activo
            if (!cliente.Activo)
            {
                ViewBag.ErrorMessage = "Su perfil de cliente se encuentra inactivo. No puede solicitar créditos.";
                return View(solicitud);
            }

            // 3. No permitir más de una solicitud Pendiente por cliente
            bool tienePendiente = await _context.SolicitudesCredito
                .AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == "Pendiente");
            
            if (tienePendiente)
            {
                ViewBag.ErrorMessage = "Ya tiene una solicitud en estado Pendiente. Solo se permite una activa a la vez.";
                return View(solicitud);
            }

            // 4. El monto solicitado no puede superar 10 veces los ingresos mensuales
            decimal limiteMonto = cliente.IngresosMensuales * 10;
            if (solicitud.MontoSolicitado > limiteMonto)
            {
                ModelState.AddModelError("MontoSolicitado", $"El monto excede el límite permitido (10 veces sus ingresos: {limiteMonto:C}).");
            }

            if (ModelState.IsValid)
            {
                // Completamos los datos requeridos por el modelo
                solicitud.ClienteId = cliente.Id;
                solicitud.FechaSolicitud = DateTime.Now;
                solicitud.Estado = "Pendiente";

                _context.Add(solicitud);
                await _context.SaveChangesAsync();

                // Feedback claro de éxito
                ViewBag.SuccessMessage = "¡Su solicitud de crédito ha sido registrada exitosamente y está en evaluación!";
                ModelState.Clear(); // Limpiamos el formulario
                return View(new SolicitudCredito()); // Devolvemos un modelo limpio
            }

            return View(solicitud);
        }
    }
}