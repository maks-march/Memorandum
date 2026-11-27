namespace Memorandum.Api.Contracts.Participants;

/// <summary>Участник документа: застройщик, проектировщик, ГИП, НРС.</summary>
/// <param name="Id">Идентификатор участия.</param>
/// <param name="Kind">Роль: customer, designer, gip, gap, nrs, surveyor, contractor.</param>
/// <param name="PartyType">Тип стороны: organization, entrepreneur, individual, foreign.</param>
/// <param name="FullName">Наименование организации или ФИО.</param>
/// <param name="Position">Должность: принадлежит участию в документе.</param>
/// <param name="Snils">СНИЛС специалиста.</param>
/// <param name="NrsNopriz">Номер в НРС НОПРИЗ.</param>
/// <param name="OrganizationId">Организация сервиса, если участник — клиент системы.</param>
public sealed record DocumentParticipantHttpResponse(
    long Id,
    string Kind,
    string PartyType,
    string FullName,
    string? Position,
    string? Snils,
    string? NrsNopriz,
    long? OrganizationId)
{
    /// <summary>Моковый участник для <c>/api/v1/mocks/documents/{id}/participants</c>.</summary>
    public static DocumentParticipantHttpResponse Mock(long id = 1, string kind = "customer") =>
        kind switch
        {
            "gip" => new(
                id,
                "gip",
                "individual",
                "Петров Пётр Петрович",
                "Главный инженер проекта",
                "123-456-789 00",
                "П-123456",
                OrganizationId: null),
            _ => new(
                id,
                kind,
                "organization",
                "ООО «Проект»",
                Position: null,
                Snils: null,
                NrsNopriz: null,
                OrganizationId: 1),
        };

    /// <summary>Моковый список: заказчик, проектировщик и ГИП.</summary>
    public static IReadOnlyList<DocumentParticipantHttpResponse> MockList() =>
        [Mock(1, "customer"), Mock(2, "designer"), Mock(3, "gip")];
}
