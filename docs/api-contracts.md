# API Contracts

## Auth

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `POST /api/auth/logout-all`
- `GET /api/auth/me`

## Finance

- `GET/POST /api/transactions`
- `PUT/DELETE /api/transactions/{transactionId}`
- `POST /api/transactions/{transactionId}/split`
- `GET /api/budgets?year=YYYY&month=MM`
- `POST /api/budgets`
- `GET /api/budgets/{categoryId}/progress?year=YYYY&month=MM`
- `POST /api/import/preview`
- `POST /api/import`

## Insights

- `POST /api/insights/leaks`
- `POST /api/insights/monthly-summary`
- `GET /api/insights/what-if/templates`
- `POST /api/insights/what-if/simulate`
