namespace Defender.Portal.Application.Modules.Telegram;

public interface ITelegramLinkHandoffService
{
    Task<TelegramLinkHandoff> CreateAsync(string initData, CancellationToken cancellationToken);
    Task ConsumeAsync(Guid accountId, string code, CancellationToken cancellationToken);
}
