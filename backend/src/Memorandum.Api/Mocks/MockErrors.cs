public static class MockErrors
{
    public static readonly Error InvalidCredentials = Error.New(401, "Неверный email или пароль");
    public static readonly Error SessionNotFound = Error.New(401, "Сессия не найдена или истекла");
    public static readonly Error InvalidXsrf = Error.New(403, "Неверный XSRF-токен");
    public static Error EmailTaken(string email) => Error.New(409, $"Пользователь {email} уже существует");
}