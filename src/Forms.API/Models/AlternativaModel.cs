namespace Forms.API.Models
{
    public class AlternativaModel
    {
        public int Id { get; set; }
        public required string Texto { get; set; }

        public int PerguntaId { get; set; }
        public PerguntaModel? Pergunta { get; set; }
    }
}
