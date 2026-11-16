namespace Memorandum.Application.Abstractions;

/// <summary>Данные текущего запроса, которые не относятся к бизнес-логике.</summary>
public interface ICurrentUser
{
    /// <summary>Id вошедшего пользователя; <c>null</c> — запрос анонимный.</summary>
    long? Id { get; }

    /// <summary>Имя для аудита; <c>null</c> для анонимных операций (регистрация).</summary>
    string? Name { get; }
}
