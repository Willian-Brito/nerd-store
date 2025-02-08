using MediatR;
using NerdStore.Modules.Core.Communication.Mediator;
using NerdStore.Modules.Core.Messages;
using NerdStore.Modules.Core.Messages.CommonMessages.Notifications;
using NerdStore.Modules.Vendas.Application.UseCases.AdicionarItemPedido.Events;
using NerdStore.Modules.Vendas.Application.UseCases.AtualizarItemPedido.Events;
using NerdStore.Modules.Vendas.Domain.Aggregates;
using NerdStore.Modules.Vendas.Domain.Entities;
using NerdStore.Modules.Vendas.Domain.Repositories;

namespace NerdStore.Modules.Vendas.Application.UseCases.AdicionarItemPedido.Commands;

public class AdicionarItemPedidoCommandHandler : IRequestHandler<AdicionarItemPedidoCommand, bool>
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IMessageBus _messageBus;


    public AdicionarItemPedidoCommandHandler(IPedidoRepository pedidoRepository, IMessageBus messageBus)
    {
        _pedidoRepository = pedidoRepository;
        _messageBus = messageBus;
    }

    public async Task<bool> Handle(AdicionarItemPedidoCommand message, CancellationToken cancellationToken)
    {
        if(!ValidarComando(message)) return false;

        var result = await AdicionarPedido(message);

        return result;
    }

    private async Task<bool> AdicionarPedido(AdicionarItemPedidoCommand message)
    {
        var pedido = await _pedidoRepository.ObterPedidoRascunhoPorClienteId(message.ClienteId);
        var pedidoItem = new PedidoItem(message.ProdutoId, message.Nome, message.Quantidade, message.ValorUnitario);

        if(pedido is null)
        {
            CriarRascunho(message, pedido, pedidoItem);
        }
        else
        {
            AdicionarItem(pedido, pedidoItem);
            pedido.AdicionarEvento(new PedidoAtualizadoEvent(pedido.ClienteId, pedido.Id, pedido.ValorTotal));
        }

        pedido.AdicionarEvento(new PedidoItemAdicionadoEvent(
            pedido.ClienteId, pedido.Id, message.ProdutoId, message.Nome, message.ValorUnitario, message.Quantidade
        ));

        return await _pedidoRepository.UnitOfWork.Commit();
    }

    private void CriarRascunho(AdicionarItemPedidoCommand message, Pedido pedido, PedidoItem pedidoItem)
    {
        pedido = Pedido.PedidoFactory.NovoPedidoRascunho(message.ClienteId);
        pedido.AdicionarItem(pedidoItem);

        _pedidoRepository.Adicionar(pedido);
        pedido.AdicionarEvento(new PedidoRascunhoIniciadoEvent(message.ClienteId, message.ProdutoId));
    }

    private void AdicionarItem(Pedido pedido, PedidoItem pedidoItem)
    {
        var pedidoItemExistente = pedido.PedidoItemExistente(pedidoItem);
        pedido.AdicionarItem(pedidoItem);

        if(pedidoItemExistente)
        {
            var item = pedido.PedidoItems.FirstOrDefault(p => p.PedidoId == pedido.Id);
            _pedidoRepository.AtualizarItem(item);
            return;
        }

        _pedidoRepository.AdicionarItem(pedidoItem);
    }

    public bool ValidarComando(Command message)
    {
        if(message.EhValido()) return true;


        foreach(var error in message.ValidationResult.Errors)
        {
            _messageBus.PublicarNotificacao(new DomainNotification(message.MessageType, error.ErrorMessage));
        }

        return false;
    }
}