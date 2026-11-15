# InnoShop

Два микросервиса ASP.NET Core: Users и Products. Веб-страница находится в Users API и использует оба REST API.

## Запуск

Нужен .NET 10 SDK. При первом запуске после клонирования подготовить локальные настройки:

```powershell
New-Item -ItemType Directory -Force .local | Out-Null
if (!(Test-Path .local/development.json)) {
    Copy-Item development.example.json .local/development.json
}
notepad .local/development.json
```

В файле заполнить `Jwt:Key` случайной строкой минимум из 32 латинских букв и цифр, а `Integration:ApiKey` — минимум из 24. Оба сервиса читают один файл, поэтому ключи будут совпадать.

`Bootstrap` можно оставить пустым. Для создания администратора указать email и пароль от 12 символов с большой и маленькой буквами и цифрой.

1. Открыть `InnoShop.slnx` в Visual Studio.
2. Запустить профиль `InnoShop - Users and Products`.
3. Открыть **http://localhost:5081**.

При запуске используются локальные SQLite-базы. Они создаются автоматически в `.local`. Эта папка и файл `.env` исключены из Git. SMTP-настройки задаются через User Secrets проекта `InnoShop.Users.Api`; пример находится в [docs/EMAIL.md](docs/EMAIL.md). Без SMTP регистрация не сможет отправить письмо подтверждения.

Запуск из двух терминалов:

```bash
dotnet run --project src/InnoShop.Users.Api
dotnet run --project src/InnoShop.Products.Api
```

Каждая команда запускается в своём терминале из папки решения.

## Веб-страница

- Каталог: поиск, фильтр цены и наличия, сортировка и страницы.
- Вход, регистрация, повторное письмо подтверждения и восстановление пароля.
- Мои товары: создание, изменение и удаление.
- Профиль: изменение имени и деактивация аккаунта.
- Пользователи: активация и деактивация, доступно Admin.

Регистрация отправляет письмо. Ссылка подтверждения открывает сайт и подтверждает email. Ссылка восстановления открывает форму нового пароля. После сброса нужно войти заново.

Администратор создаётся при первом запуске, если заполнена секция `Bootstrap` в `.local/development.json`. После деактивации аккаунта его товары скрываются; для активации нужно войти администратором.

Разметка и JavaScript находятся в `src/InnoShop.Users.Api/wwwroot/index.html` и `app.js`. CSS и внешних библиотек интерфейса нет. Всего три проекта: два API и `InnoShop.Tests`.

## Настройки и проверка

- Users API: http://localhost:5081/scalar/v1
- Products API: http://localhost:5082/scalar/v1
- Готовность: `/health/ready` у каждого сервиса.
- Адрес Products для браузера: `Frontend:ProductsApiUrl` в Users API.
- Разрешённый адрес сайта: `Frontend:Origin` в Products API.

```bash
dotnet restore InnoShop.slnx --locked-mode
dotnet test InnoShop.slnx -c Release
```

[Почта](docs/EMAIL.md), [API](docs/API.md), [структура](docs/ARCHITECTURE.md), [тесты](docs/VERIFICATION.md), [ТЗ](docs/TASK.txt).

## Docker

PowerShell: `.\scripts\init-dev.ps1`, затем `docker compose up --build -d`.
Bash: `bash scripts/init-dev.sh`, затем `docker compose up --build -d`.

Скрипт создаёт `.env`. Добавить в него SMTP-параметры из `.env.example`. Compose запускает два API и две PostgreSQL-базы. Сайт открывается по адресу http://localhost:5081. User Secrets автоматически в контейнеры не передаются. Scalar включён только в Development.

Локальный запуск и Docker используют разные базы и настройки. `docker compose down` сохраняет данные; добавление `-v` удаляет данные баз.
