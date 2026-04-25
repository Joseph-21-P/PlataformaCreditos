using System.ComponentModel.DataAnnotations;

namespace PlataformaCreditosWeb.Models
{
    public class SolicitudCredito
    {
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto solicitado debe ser mayor a 0")]
        public decimal MontoSolicitado { get; set; }

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        [Required]
        public string Estado { get; set; } = "Pendiente"; 

        public string? MotivoRechazo { get; set; }
    }
}