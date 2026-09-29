using System.Text.Json.Serialization;

namespace Forms.API.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TipoPergunta
    {
        RespostaCurta = 0,
        TextoLongo = 1,
        MultiplaEscolha = 2,
        CaixaDeSelecao = 3
    }

    public static class TipoPerguntaExtensions
    {
        public static bool PermiteAlternativas(this TipoPergunta tipo)
        {
            return tipo == TipoPergunta.MultiplaEscolha || tipo == TipoPergunta.CaixaDeSelecao;
        }
    }
}
