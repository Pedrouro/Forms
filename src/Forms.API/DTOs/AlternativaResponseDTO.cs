namespace Forms.API.DTOs
{
    public class AlternativaResponseDTO
    {
        public int Id { get; set; }
        public required string Texto { get; set; }
        public int PerguntaId { get; set; }
    }
}
