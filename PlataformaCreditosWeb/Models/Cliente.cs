using System.ComponentModel.DataAnnotations;

namespace PlataformaCreditosWeb.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty; // Vinculación con Identity

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Los ingresos deben ser mayores a 0")]
        public decimal IngresosMensuales { get; set; }

        public bool Activo { get; set; } = true;
    }
}