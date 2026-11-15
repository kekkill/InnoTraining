# Организация по примеру Arda

Inno остаётся магазином с двумя API из ТЗ. Из предоставленных Arda-проектов взяты организация кода и привычные способы работы с ним.

| Пример в Arda | Где искать аналог в Inno |
| --- | --- |
| Anduin `Endpoints/StaffEndpoints` | `Endpoints/UserEndpoints`, `AuthEndpoints`, `ProductEndpoints` |
| Именованные private handlers | Обработчики внутри каждого `*Endpoints` |
| `Application/Services/StaffService` | `Application/Services/UserService`, `AuthenticationService`, `ProductService` |
| `Application/Abstractions` | `IUserRepository`, `IProductRepository`, `IUserStateClient`, интерфейсы авторизации и почты |
| Fellowship: закрытые setters, `Create`, методы изменения состояния | `Domain/Users/UserAccount`, `Domain/Products/Product` |
| `Infrastructure/DependencyInjection` | `AddUsersInfrastructure`, `AddProductsInfrastructure` |
| Options и регистрация в Composition | `AuthenticationOptions`, `MailOptions`, `*Registrations` |
| Централизованные версии NuGet | Корневой `Directory.Packages.props` |
| Отдельные тестовые проекты Arda | Один `InnoShop.Tests` с папками Users и Products |

Каждый API содержит папки `Endpoints`, `Application`, `Domain`, `Infrastructure`, `Composition`, `Middleware`. Пространства имён следуют папкам: например, `InnoShop.Users.Api.Application.Services`.

Направление зависимостей по соглашению: API обращается к Application; Application использует Domain и свои интерфейсы; Infrastructure реализует эти интерфейсы. Отдельные сборки слоёв убраны, поэтому компилятор больше не обеспечивает их изоляцию. Это осознанное упрощение учебного проекта.

## Пример: создание товара

`POST /api/v1/products` требует JWT. `ActiveAccountMiddleware` через `IUserStateClient` проверяет у Users, что аккаунт доступен, а версия токена актуальна. `ProductEndpoints.CreateProduct` берёт ID владельца из JWT и передаёт запрос в `ProductService`. Сервис валидирует данные, создаёт `Product`, добавляет его через `IProductRepository` и сохраняет изменения. `ProductRepository` использует EF Core.

Проверка активности не дублируется внутри каждого метода записи `ProductService`: её выполняет middleware всех защищённых endpoints. Сервис проверяет принадлежность товара. Если появится другой способ вызывать записи помимо этих endpoints, он также должен проверять актуальность аккаунта.

## Скрытие товаров

У товара есть собственный `IsDeleted`. Он отвечает за удаление товара владельцем. Доступность владельца получается через REST из Users: подтверждённая почта, активный и не удалённый аккаунт.

При деактивации владельца товары пропадают из публичного каталога и выдачи по ID. После активации возвращаются только товары, которые владелец сам не удалил. Физически массово переписывать строки Products при деактивации не требуется. Такой способ выбран вместо второго soft-delete флага, чтобы не синхронизировать две копии состояния пользователя.

Если Users недоступен, Products возвращает `503`, когда запросу требуется состояние владельца. Состояния не кэшируются, поэтому изменения учитываются при следующем запросе.

## Веб-страница

Users API отдаёт файлы из `wwwroot`: `index.html` и `app.js`. Формы вызывают существующие REST endpoints. JWT хранится в sessionStorage вкладки и передаётся в Authorization. После истечения токена или деактивации аккаунта требуется новый вход.

Публичный `/site-config` содержит только адрес Products API для браузера. Products разрешает запросы с адреса `Frontend:Origin`. SMTP-настройки используются внутри Users API.

Ссылки в письмах открывают страницу подтверждения или форму восстановления. Серверные endpoints остаются доступны для HTTP-клиентов.

## Локальный запуск

`DevelopmentConfiguration` находит папку решения, читает `.local/development.json` и задаёт путь к SQLite-базе своего сервиса. Генерации ключей и временных файлов при запуске нет. Настройки окружения и аргументы командной строки имеют приоритет. Если файл удалён, для запуска его нужно восстановить или заполнить заново.

`Infrastructure/DependencyInjection.cs` также содержит инициализацию базы: `EnsureCreatedAsync` для локальной SQLite и `MigrateAsync` для PostgreSQL. `DesignTimeFactory` оставлен для команд EF с PostgreSQL; строка подключения берётся из `users_connection` или `products_connection`. Для создания миграций подключение к серверу не требуется, для обновления базы строку нужно задать.
