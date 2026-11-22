namespace Memorandum.Application.Auth;

/// <summary>
/// Результат успешного входа. Cookie ставит Api-слой: Application не знает про HTTP.
/// </summary>
/// <param name="UserId">Кому выдать cookie.</param>
/// <param name="SecurityStamp">Кладётся в claim и сверяется на каждом запросе.</param>
/// <param name="Email">Email для профиля в ответе.</param>
/// <param name="FullName">ФИО для профиля в ответе.</param>
public sealed record LoginOutcome(long UserId, string SecurityStamp, string Email, string FullName);
