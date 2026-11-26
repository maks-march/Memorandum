namespace Memorandum.Api.Contracts;

/// <summary>Страница списка: общая обёртка для всех коллекций API.</summary>
/// <param name="Items">Элементы текущей страницы.</param>
/// <param name="Total">Сколько всего элементов.</param>
/// <param name="Page">Номер страницы, начиная с 1.</param>
/// <param name="PageSize">Размер страницы.</param>
public sealed record PagedHttpResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    /// <summary>Моковая страница: весь список помещается на одну страницу.</summary>
    public static PagedHttpResponse<T> Mock(IReadOnlyList<T> items) =>
        new(items, items.Count, Page: 1, PageSize: 20);
}
