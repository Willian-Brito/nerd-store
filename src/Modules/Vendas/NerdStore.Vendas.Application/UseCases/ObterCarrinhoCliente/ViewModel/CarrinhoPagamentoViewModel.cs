namespace NerdStore.Modules.Vendas.Application.UseCases.ObterCarrinhoCliente.ViewModel;

public class CarrinhoPagamentoViewModel
{
    public string NomeCartao { get; set; }
    public string NumeroCartao { get; set; }
    public string ExpiracaoCartao { get; set; }
    public string CvvCartao { get; set; }
}