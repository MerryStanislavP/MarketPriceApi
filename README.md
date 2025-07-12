# Market Price API

REST API сервис для получения информации о ценах рыночных активов (EUR/USD, GOOG, etc.) с использованием Fintacharts платформы.

## Архитектура

- **.NET Core 8.0** - основной фреймворк
- **ASP.NET Core Web API** - REST API
- **PostgreSQL** - основная база данных
- **Redis** - кеширование
- **Entity Framework Core** - ORM
- **MediatR** - CQRS паттерн
- **Fintacharts API** - провайдер рыночных данных

## Функциональность

### Эндпоинты

1. **GET /api/marketdata/assets** - Получить список поддерживаемых активов
   - Query параметры: `provider`, `kind`, `symbol`, `isActive`

2. **GET /api/marketdata/prices/current/{symbol}** - Получить текущую цену актива
   - Query параметры: `provider`, `interval`

3. **GET /api/marketdata/prices/historical/{symbol}** - Получить исторические цены
   - Query параметры: `provider`, `interval`, `startDate`, `endDate`, `limit`

4. **POST /api/marketdata/prices** - Сохранить цену (для WebSocket)

5. **POST /api/marketdata/sync/instruments** - Синхронизировать инструменты
   - Query параметры: `provider`, `kind`

6. **POST /api/marketdata/sync/prices/{symbol}** - Синхронизировать цены для символа
   - Query параметры: `provider`, `interval`

7. **POST /api/marketdata/sync/all** - Синхронизировать все активы

8. **POST /api/marketdata/websocket/start** - Запустить WebSocket сервис

9. **POST /api/marketdata/websocket/stop** - Остановить WebSocket сервис

10. **POST /api/marketdata/websocket/subscribe/{symbol}** - Подписаться на символ
    - Query параметры: `provider`

## Запуск приложения

### Требования

- Docker и Docker Compose
- .NET 8.0 SDK (для локальной разработки)

### Запуск через Docker (рекомендуется)

1. Клонируйте репозиторий:
```bash
git clone <repository-url>
cd MarketPriceApi
```

2. Запустите приложение:
```bash
docker-compose up -d
```

3. Приложение будет доступно по адресу:
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger

### Локальная разработка

1. Установите зависимости:
```bash
cd MarketPriceApi
dotnet restore
```

2. Настройте базу данных:
```bash
# Примените миграции
dotnet ef database update
```

3. Запустите приложение:
```bash
dotnet run
```

## Конфигурация

Основные настройки находятся в `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=marketdb;Username=postgres;Password=123",
    "Redis": "localhost:6379"
  },
  "Finta": {
    "BaseUrl": "https://platform.fintacharts.com",
    "WebSocketUrl": "wss://platform.fintacharts.com",
    "Username": "your-username",
    "Password": "your-password"
  }
}
```

## Структура проекта

```
MarketPriceApi/
├── Controllers/
│   └── MarketDataController.cs          # Основной API контроллер
├── Services/
│   ├── Assets/                          # Работа с активами
│   ├── Prices/                          # Работа с ценами
│   ├── Auth/                            # Аутентификация Fintacharts
│   ├── Bars/                            # Получение баров
│   ├── Instruments/                     # Работа с инструментами
│   ├── WebSocket/                       # WebSocket сервис
│   ├── Sync/                            # Синхронизация
│   └── Cache/                           # Кеширование
├── Models/
│   ├── Asset.cs                         # Модель актива
│   ├── AssetPrice.cs                    # Модель цены
│   ├── SyncLog.cs                       # Модель лога синхронизации
│   └── DTOs/                            # DTO модели
├── Persistence/
│   ├── AppDbContext.cs                  # Контекст базы данных
│   └── Migrations/                      # Миграции EF Core
└── Configuration/
    └── FintaConfiguration.cs            # Конфигурация Fintacharts
```

## Примеры использования

### Получить список активов
```bash
curl -X GET "http://localhost:5000/api/marketdata/assets"
```

### Получить текущую цену EUR/USD
```bash
curl -X GET "http://localhost:5000/api/marketdata/prices/current/EURUSD?provider=oanda&interval=1m"
```

### Получить исторические цены
```bash
curl -X GET "http://localhost:5000/api/marketdata/prices/historical/EURUSD?provider=oanda&interval=1m&startDate=2024-01-01&endDate=2024-01-31"
```

### Синхронизировать инструменты
```bash
curl -X POST "http://localhost:5000/api/marketdata/sync/instruments?provider=oanda&kind=forex"
```

## Особенности реализации

- **Кеширование**: Redis используется для кеширования запросов
- **Fallback логика**: Сначала проверяется БД/кеш, затем Fintacharts API
- **WebSocket**: Реальные данные сохраняются в БД через WebSocket
- **CQRS**: Используется паттерн Command Query Responsibility Segregation
- **Логирование**: Все операции логируются
- **Обработка ошибок**: Централизованная обработка исключений

## Мониторинг

- **Swagger UI**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health
- **Логи**: Доступны в контейнере через `docker logs marketapi` 