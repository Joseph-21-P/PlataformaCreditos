using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlataformaCreditosWeb.Data;
using PlataformaCreditosWeb.Models;

namespace PlataformaCreditosWeb.Controllers
{
    [Authorize]
    public class SolicitudesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDistributedCache _cache; 

        public SolicitudesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IDistributedCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
        }

        // GET: Solicitudes/Index
        public async Task<IActionResult> Index(string? estado, decimal? montoMin, decimal? montoMax, DateTime? fechaInicio, DateTime? fechaFin)
        {
            if (montoMin < 0 || montoMax < 0) ModelState.AddModelError("", "No se aceptan montos negativos.");
            if (fechaInicio > fechaFin) ModelState.AddModelError("", "El rango de fechas es inválido.");

            var userId = _userManager.GetUserId(User);
            var cacheKey = $"solicitudes_list_{userId}";
            List<SolicitudCredito> solicitudesBase;

            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {

                solicitudesBase = JsonSerializer.Deserialize<List<SolicitudCredito>>(cachedData) ?? new List<SolicitudCredito>();
            }
            else
            {

                solicitudesBase = await _context.SolicitudesCredito
                    .Where(s => s.Cliente!.UsuarioId == userId)
                    .ToListAsync();


                var cacheOptions = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(60));


                var jsonOptions = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };
                var jsonToCache = JsonSerializer.Serialize(solicitudesBase, jsonOptions);

                await _cache.SetStringAsync(cacheKey, jsonToCache, cacheOptions);
            }

            var query = solicitudesBase.AsQueryable();

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(estado)) query = query.Where(s => s.Estado == estado);
                if (montoMin.HasValue) query = query.Where(s => s.MontoSolicitado >= montoMin.Value);
                if (montoMax.HasValue) query = query.Where(s => s.MontoSolicitado <= montoMax.Value);
                if (fechaInicio.HasValue) query = query.Where(s => s.FechaSolicitud.Date >= fechaInicio.Value.Date);
                if (fechaFin.HasValue) query = query.Where(s => s.FechaSolicitud.Date <= fechaFin.Value.Date);
            }

            ViewBag.Estado = estado;
            ViewBag.MontoMin = montoMin;
            ViewBag.MontoMax = montoMax;
            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");

            return View(query.ToList());
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

            HttpContext.Session.SetString("UltimaSolicitudId", solicitud.Id.ToString());
            HttpContext.Session.SetString("UltimaSolicitudMonto", solicitud.MontoSolicitado.ToString("C"));

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
            var userId = _userManager.GetUserId(User);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null)
            {
                ViewBag.ErrorMessage = "No se encontró un perfil de cliente asociado a su cuenta.";
                return View(solicitud);
            }

            if (!cliente.Activo)
            {
                ViewBag.ErrorMessage = "Su perfil de cliente se encuentra inactivo. No puede solicitar créditos.";
                return View(solicitud);
            }

            bool tienePendiente = await _context.SolicitudesCredito.AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == "Pendiente");
            if (tienePendiente)
            {
                ViewBag.ErrorMessage = "Ya tiene una solicitud en estado Pendiente. Solo se permite una activa a la vez.";
                return View(solicitud);
            }

            decimal limiteMonto = cliente.IngresosMensuales * 10;
            if (solicitud.MontoSolicitado > limiteMonto)
            {
                ModelState.AddModelError("MontoSolicitado", $"El monto excede el límite permitido (10 veces sus ingresos: {limiteMonto:C}).");
            }

            if (ModelState.IsValid)
            {
                solicitud.ClienteId = cliente.Id;
                solicitud.FechaSolicitud = DateTime.Now;
                solicitud.Estado = "Pendiente";

                _context.Add(solicitud);
                await _context.SaveChangesAsync();

                var cacheKey = $"solicitudes_list_{userId}";
                await _cache.RemoveAsync(cacheKey);

                ViewBag.SuccessMessage = "¡Su solicitud de crédito ha sido registrada exitosamente y está en evaluación!";
                ModelState.Clear();
                return View(new SolicitudCredito());
            }

            return View(solicitud);
        }
    }
}